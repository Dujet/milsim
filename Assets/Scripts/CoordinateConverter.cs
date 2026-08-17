using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A WGS-84 position suitable for Cursor-on-Target/CIV-TAK messages.
/// Double precision is important here: a Unity Vector3 loses sub-metre
/// precision when it is used to hold a latitude or longitude.
/// </summary>
public readonly struct Wgs84Coordinate
{
    public readonly double Latitude;
    public readonly double Longitude;
    public readonly double HaeMeters;

    public Wgs84Coordinate(double latitude, double longitude, double haeMeters)
    {
        Latitude = latitude;
        Longitude = longitude;
        HaeMeters = haeMeters;
    }
}

/// <summary>
/// Georeferences Unity world coordinates to WGS-84 for CIV-TAK.
///
/// Add exactly one instance to the active scene.  Set <see cref="unityOrigin"/>
/// to the Unity point whose real latitude/longitude is known, then set the
/// metres represented by each Unity unit.  For a terrain that is half the
/// real-world size, use 2 for X and Z.
///
/// Horizontal positions are transformed through a WGS-84 local tangent plane
/// (ECEF/ENU), rather than by using fixed degrees-per-metre constants.  This
/// accounts for the longitude convergence at the chosen latitude and remains
/// reversible for incoming TAK markers.  X and Z have independent calibration
/// values to correct non-uniform stretching introduced by terrain/map export.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class CoordinateConverter : MonoBehaviour
{
    private const double Wgs84SemiMajorAxis = 6378137.0;
    private const double Wgs84Flattening = 1.0 / 298.257223563;
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;

    private static CoordinateConverter active;

    [Header("Unity reference")]
    [Tooltip("This transform's world position is the geographic anchor. Leave empty to use Unity world (0, 0, 0).")]
    [SerializeField] private Transform unityOrigin;

    [Tooltip("Bearing of Unity +Z, clockwise from true north. Use 0 when +Z is north and +X is east.")]
    [Range(-180f, 180f)]
    [SerializeField] private float unityZBearingDegrees = 0f;

    [Header("Real-world anchor (WGS-84)")]
    [Tooltip("Latitude of the Unity origin, in decimal degrees. North is positive.")]
    [SerializeField] private double anchorLatitude = 0.0;

    [Tooltip("Longitude of the Unity origin, in decimal degrees. East is positive.")]
    [SerializeField] private double anchorLongitude = 0.0;

    [Tooltip("Height above the WGS-84 ellipsoid (HAE), in metres, at the Unity origin.")]
    [SerializeField] private double anchorHaeMeters = 0.0;

    [Header("Terrain calibration")]
    [Tooltip("Real ground metres represented by one Unity X unit. A 1:2 terrain usually needs 2.")]
    [Min(0.0001f)]
    [SerializeField] private float realMetersPerUnityX = 2f;

    [Tooltip("Real ground metres represented by one Unity Z unit. Set independently to correct terrain stretching.")]
    [Min(0.0001f)]
    [SerializeField] private float realMetersPerUnityZ = 2f;

    [Tooltip("Real vertical metres represented by one Unity Y unit.")]
    [Min(0.0001f)]
    [SerializeField] private float realMetersPerUnityY = 1f;

    [Header("Control point calibration (recommended)")]
    [Tooltip("Known landmarks placed in the Unity scene. With three or more points, these override the manual X/Z scale and bearing fields.")]
    [SerializeField] private CoordinateReferencePoint[] controlPoints;

    [Tooltip("Fit a least-squares affine transform from the control points. It corrects translation, rotation, unequal scale, and skew/stretch.")]
    [SerializeField] private bool useControlPointCalibration = true;

    private bool hasControlPointCalibration;
    private AffineCalibration controlPointCalibration;

    /// <summary>The configured converter used by the existing static TAK call sites.</summary>
    public static CoordinateConverter Active => GetActive();

    private void Awake()
    {
        if (active != null && active != this)
        {
            Debug.LogError("[CoordinateConverter] More than one converter is active. Disable or remove the duplicate.", this);
            enabled = false;
            return;
        }

        active = this;
        ValidateConfiguration();
        RecalculateCalibration();
    }

    private void OnDisable()
    {
        if (active == this)
            active = null;
    }

    private void OnValidate()
    {
        realMetersPerUnityX = Mathf.Max(0.0001f, realMetersPerUnityX);
        realMetersPerUnityZ = Mathf.Max(0.0001f, realMetersPerUnityZ);
        realMetersPerUnityY = Mathf.Max(0.0001f, realMetersPerUnityY);
        anchorLatitude = Math.Max(-90.0, Math.Min(90.0, anchorLatitude));
        anchorLongitude = WrapLongitude(anchorLongitude);
    }

    /// <summary>Converts a Unity world point to full-precision WGS-84/HAE.</summary>
    public Wgs84Coordinate UnityToWgs84Coordinate(Vector3 unityWorldPosition)
    {
        Vector3 localUnity = unityWorldPosition - UnityOriginPosition;

        // Rotate Unity's horizontal axes into true-east/true-north and then
        // apply the independently measured horizontal terrain scale factors.
        GetEastNorth(localUnity.x, localUnity.z, out double east, out double north);

        GetAnchorFrame(out Vector3d anchorEcef, out Vector3d eastAxis, out Vector3d northAxis, out Vector3d upAxis);
        Vector3d projectedEcef = anchorEcef + eastAxis * east + northAxis * north;
        EcefToGeodetic(projectedEcef, out double latitude, out double longitude, out _);

        // Unity terrains are normally flat map projections, not curved Earth
        // geometry.  Preserve their authored elevation relative to the anchor
        // instead of introducing Earth's curvature into the reported HAE.
        double hae = anchorHaeMeters + localUnity.y * realMetersPerUnityY;
        return new Wgs84Coordinate(latitude, longitude, hae);
    }

    /// <summary>Converts a WGS-84/HAE point to a Unity world position.</summary>
    public Vector3 Wgs84ToUnityPosition(double latitude, double longitude, double haeMeters = 0.0)
    {
        GetAnchorFrame(out Vector3d anchorEcef, out Vector3d eastAxis, out Vector3d northAxis, out Vector3d upAxis);

        // Use the anchor HAE for the horizontal projection.  This makes the
        // inverse agree with UnityToWgs84Coordinate even when incoming TAK
        // markers carry a different terrain altitude.
        Vector3d targetEcef = GeodeticToEcef(latitude, longitude, anchorHaeMeters);
        Vector3d delta = targetEcef - anchorEcef;
        double east = Vector3d.Dot(delta, eastAxis);
        double north = Vector3d.Dot(delta, northAxis);

        GetUnityHorizontal(east, north, out double unityX, out double unityZ);

        Vector3 localUnity = new Vector3(
            (float)unityX,
            (float)((haeMeters - anchorHaeMeters) / realMetersPerUnityY),
            (float)unityZ);
        return UnityOriginPosition + localUnity;
    }

    /// <summary>
    /// Compatibility wrapper for older callers. Prefer
    /// <see cref="UnityToWgs84Coordinate"/> when serialising CoT, so latitude
    /// and longitude remain doubles.
    /// </summary>
    public static Vector3 UnityToWgs84(Vector3 unityWorldPosition)
    {
        Wgs84Coordinate coordinate = GetActive().UnityToWgs84Coordinate(unityWorldPosition);
        return new Vector3((float)coordinate.Latitude, (float)coordinate.Longitude, (float)coordinate.HaeMeters);
    }

    public static Wgs84Coordinate UnityToWgs84Double(Vector3 unityWorldPosition)
    {
        return GetActive().UnityToWgs84Coordinate(unityWorldPosition);
    }

    public static Vector3 Wgs84ToUnity(double latitude, double longitude, double haeMeters = 0.0)
    {
        return GetActive().Wgs84ToUnityPosition(latitude, longitude, haeMeters);
    }

    /// <summary>
    /// Rebuilds the least-squares fit from the assigned reference points.
    /// Use the component's Inspector context menu after moving a point during
    /// play mode. Three non-collinear points are required; four or more are
    /// preferable because inaccurate landmark placement is averaged out.
    /// </summary>
    [ContextMenu("Recalculate control-point calibration")]
    public void RecalculateCalibration()
    {
        hasControlPointCalibration = false;

        if (!useControlPointCalibration || controlPoints == null || controlPoints.Length < 3)
            return;

        GetAnchorFrame(out Vector3d anchorEcef, out Vector3d eastAxis, out Vector3d northAxis, out _);
        double[,] normal = new double[3, 3];
        double[] eastTerms = new double[3];
        double[] northTerms = new double[3];
        int validPointCount = 0;

        foreach (CoordinateReferencePoint point in controlPoints)
        {
            if (point == null || double.IsNaN(point.latitude) || double.IsNaN(point.longitude))
                continue;

            Vector3 localUnity = point.transform.position - UnityOriginPosition;
            Vector3d pointEcef = GeodeticToEcef(point.latitude, point.longitude, anchorHaeMeters);
            Vector3d geographicDelta = pointEcef - anchorEcef;
            double east = Vector3d.Dot(geographicDelta, eastAxis);
            double north = Vector3d.Dot(geographicDelta, northAxis);
            double[] row = { localUnity.x, localUnity.z, 1.0 };

            for (int r = 0; r < 3; r++)
            {
                eastTerms[r] += row[r] * east;
                northTerms[r] += row[r] * north;
                for (int c = 0; c < 3; c++)
                    normal[r, c] += row[r] * row[c];
            }

            validPointCount++;
        }

        if (validPointCount < 3 || !SolveLinearSystem(normal, eastTerms, out double[] eastCoefficients) || !SolveLinearSystem(normal, northTerms, out double[] northCoefficients))
        {
            Debug.LogWarning("[CoordinateConverter] Control-point calibration needs at least three non-collinear reference points. Using manual scale and bearing instead.", this);
            return;
        }

        AffineCalibration calibration = new AffineCalibration(eastCoefficients, northCoefficients);
        if (Math.Abs(calibration.Determinant) < 1e-9)
        {
            Debug.LogWarning("[CoordinateConverter] Control points do not define an invertible area. Spread them around the terrain and recalibrate.", this);
            return;
        }

        controlPointCalibration = calibration;
        hasControlPointCalibration = true;

        double totalSquaredError = 0.0;
        double maximumError = 0.0;
        foreach (CoordinateReferencePoint point in controlPoints)
        {
            if (point == null || double.IsNaN(point.latitude) || double.IsNaN(point.longitude))
                continue;

            Vector3 localUnity = point.transform.position - UnityOriginPosition;
            Vector3d geographicDelta = GeodeticToEcef(point.latitude, point.longitude, anchorHaeMeters) - anchorEcef;
            double expectedEast = Vector3d.Dot(geographicDelta, eastAxis);
            double expectedNorth = Vector3d.Dot(geographicDelta, northAxis);
            calibration.UnityToEastNorth(localUnity.x, localUnity.z, out double fittedEast, out double fittedNorth);
            double error = Math.Sqrt((fittedEast - expectedEast) * (fittedEast - expectedEast) + (fittedNorth - expectedNorth) * (fittedNorth - expectedNorth));
            totalSquaredError += error * error;
            maximumError = Math.Max(maximumError, error);
        }

        Debug.Log($"[CoordinateConverter] Calibrated from {validPointCount} control points. Horizontal residual: RMS {Math.Sqrt(totalSquaredError / validPointCount):F2} m, max {maximumError:F2} m.", this);
    }

    private static CoordinateConverter GetActive()
    {
        if (active == null)
            active = FindObjectOfType<CoordinateConverter>();

        if (active == null)
            throw new InvalidOperationException(
                "No CoordinateConverter is active. Add one to the scene and configure its real-world anchor and terrain scale before sending or receiving TAK positions.");

        return active;
    }

    private Vector3 UnityOriginPosition => unityOrigin != null ? unityOrigin.position : Vector3.zero;

    private void GetEastNorth(double unityX, double unityZ, out double east, out double north)
    {
        if (useControlPointCalibration && hasControlPointCalibration)
        {
            controlPointCalibration.UnityToEastNorth(unityX, unityZ, out east, out north);
            return;
        }

        double bearing = unityZBearingDegrees * DegreesToRadians;
        double scaledX = unityX * realMetersPerUnityX;
        double scaledZ = unityZ * realMetersPerUnityZ;
        east = scaledX * Math.Cos(bearing) + scaledZ * Math.Sin(bearing);
        north = -scaledX * Math.Sin(bearing) + scaledZ * Math.Cos(bearing);
    }

    private void GetUnityHorizontal(double east, double north, out double unityX, out double unityZ)
    {
        if (useControlPointCalibration && hasControlPointCalibration)
        {
            controlPointCalibration.EastNorthToUnity(east, north, out unityX, out unityZ);
            return;
        }

        double bearing = unityZBearingDegrees * DegreesToRadians;
        double scaledX = east * Math.Cos(bearing) - north * Math.Sin(bearing);
        double scaledZ = east * Math.Sin(bearing) + north * Math.Cos(bearing);
        unityX = scaledX / realMetersPerUnityX;
        unityZ = scaledZ / realMetersPerUnityZ;
    }

    private void ValidateConfiguration()
    {
        if (Math.Abs(anchorLatitude) < double.Epsilon && Math.Abs(anchorLongitude) < double.Epsilon)
            Debug.LogWarning("[CoordinateConverter] Anchor is still 0, 0. Set the actual village latitude and longitude before using CIV-TAK.", this);
    }

    private void GetAnchorFrame(out Vector3d anchorEcef, out Vector3d eastAxis, out Vector3d northAxis, out Vector3d upAxis)
    {
        double latitudeRadians = anchorLatitude * DegreesToRadians;
        double longitudeRadians = anchorLongitude * DegreesToRadians;
        double sinLatitude = Math.Sin(latitudeRadians);
        double cosLatitude = Math.Cos(latitudeRadians);
        double sinLongitude = Math.Sin(longitudeRadians);
        double cosLongitude = Math.Cos(longitudeRadians);

        anchorEcef = GeodeticToEcef(anchorLatitude, anchorLongitude, anchorHaeMeters);
        eastAxis = new Vector3d(-sinLongitude, cosLongitude, 0.0);
        northAxis = new Vector3d(-sinLatitude * cosLongitude, -sinLatitude * sinLongitude, cosLatitude);
        upAxis = new Vector3d(cosLatitude * cosLongitude, cosLatitude * sinLongitude, sinLatitude);
    }

    private static Vector3d GeodeticToEcef(double latitude, double longitude, double haeMeters)
    {
        double latitudeRadians = latitude * DegreesToRadians;
        double longitudeRadians = longitude * DegreesToRadians;
        double sinLatitude = Math.Sin(latitudeRadians);
        double cosLatitude = Math.Cos(latitudeRadians);
        double sinLongitude = Math.Sin(longitudeRadians);
        double cosLongitude = Math.Cos(longitudeRadians);
        double eccentricitySquared = Wgs84Flattening * (2.0 - Wgs84Flattening);
        double radius = Wgs84SemiMajorAxis / Math.Sqrt(1.0 - eccentricitySquared * sinLatitude * sinLatitude);

        return new Vector3d(
            (radius + haeMeters) * cosLatitude * cosLongitude,
            (radius + haeMeters) * cosLatitude * sinLongitude,
            (radius * (1.0 - eccentricitySquared) + haeMeters) * sinLatitude);
    }

    private static void EcefToGeodetic(Vector3d ecef, out double latitude, out double longitude, out double haeMeters)
    {
        double eccentricitySquared = Wgs84Flattening * (2.0 - Wgs84Flattening);
        double horizontalDistance = Math.Sqrt(ecef.X * ecef.X + ecef.Y * ecef.Y);
        longitude = Math.Atan2(ecef.Y, ecef.X) * RadiansToDegrees;

        if (horizontalDistance < 1e-8)
        {
            latitude = ecef.Z >= 0.0 ? 90.0 : -90.0;
            haeMeters = Math.Abs(ecef.Z) - Wgs84SemiMajorAxis * (1.0 - Wgs84Flattening);
            return;
        }

        double latitudeRadians = Math.Atan2(ecef.Z, horizontalDistance * (1.0 - eccentricitySquared));
        double radius = 0.0;
        for (int i = 0; i < 7; i++)
        {
            double sinLatitude = Math.Sin(latitudeRadians);
            radius = Wgs84SemiMajorAxis / Math.Sqrt(1.0 - eccentricitySquared * sinLatitude * sinLatitude);
            haeMeters = horizontalDistance / Math.Cos(latitudeRadians) - radius;
            latitudeRadians = Math.Atan2(ecef.Z, horizontalDistance * (1.0 - eccentricitySquared * radius / (radius + haeMeters)));
        }

        double finalSinLatitude = Math.Sin(latitudeRadians);
        radius = Wgs84SemiMajorAxis / Math.Sqrt(1.0 - eccentricitySquared * finalSinLatitude * finalSinLatitude);
        haeMeters = horizontalDistance / Math.Cos(latitudeRadians) - radius;
        latitude = latitudeRadians * RadiansToDegrees;
        longitude = WrapLongitude(longitude);
    }

    private static double WrapLongitude(double longitude)
    {
        longitude %= 360.0;
        if (longitude > 180.0) longitude -= 360.0;
        if (longitude < -180.0) longitude += 360.0;
        return longitude;
    }

    private static bool SolveLinearSystem(double[,] matrix, double[] terms, out double[] result)
    {
        const int size = 3;
        double[,] augmented = new double[size, size + 1];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
                augmented[row, column] = matrix[row, column];
            augmented[row, size] = terms[row];
        }

        for (int pivot = 0; pivot < size; pivot++)
        {
            int bestRow = pivot;
            for (int row = pivot + 1; row < size; row++)
            {
                if (Math.Abs(augmented[row, pivot]) > Math.Abs(augmented[bestRow, pivot]))
                    bestRow = row;
            }

            if (Math.Abs(augmented[bestRow, pivot]) < 1e-10)
            {
                result = null;
                return false;
            }

            for (int column = pivot; column <= size; column++)
            {
                double swap = augmented[pivot, column];
                augmented[pivot, column] = augmented[bestRow, column];
                augmented[bestRow, column] = swap;
            }

            double divisor = augmented[pivot, pivot];
            for (int column = pivot; column <= size; column++)
                augmented[pivot, column] /= divisor;

            for (int row = 0; row < size; row++)
            {
                if (row == pivot) continue;
                double factor = augmented[row, pivot];
                for (int column = pivot; column <= size; column++)
                    augmented[row, column] -= factor * augmented[pivot, column];
            }
        }

        result = new[] { augmented[0, size], augmented[1, size], augmented[2, size] };
        return true;
    }

    private readonly struct AffineCalibration
    {
        // east = EastX * unityX + EastZ * unityZ + EastOffset
        // north = NorthX * unityX + NorthZ * unityZ + NorthOffset
        private readonly double eastX;
        private readonly double eastZ;
        private readonly double eastOffset;
        private readonly double northX;
        private readonly double northZ;
        private readonly double northOffset;

        public double Determinant => eastX * northZ - eastZ * northX;

        public AffineCalibration(double[] eastCoefficients, double[] northCoefficients)
        {
            eastX = eastCoefficients[0];
            eastZ = eastCoefficients[1];
            eastOffset = eastCoefficients[2];
            northX = northCoefficients[0];
            northZ = northCoefficients[1];
            northOffset = northCoefficients[2];
        }

        public void UnityToEastNorth(double unityX, double unityZ, out double east, out double north)
        {
            east = eastX * unityX + eastZ * unityZ + eastOffset;
            north = northX * unityX + northZ * unityZ + northOffset;
        }

        public void EastNorthToUnity(double east, double north, out double unityX, out double unityZ)
        {
            double shiftedEast = east - eastOffset;
            double shiftedNorth = north - northOffset;
            double determinant = Determinant;
            unityX = (northZ * shiftedEast - eastZ * shiftedNorth) / determinant;
            unityZ = (-northX * shiftedEast + eastX * shiftedNorth) / determinant;
        }
    }

    private readonly struct Vector3d
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3d operator +(Vector3d left, Vector3d right) => new Vector3d(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        public static Vector3d operator -(Vector3d left, Vector3d right) => new Vector3d(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        public static Vector3d operator *(Vector3d vector, double scalar) => new Vector3d(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
        public static double Dot(Vector3d left, Vector3d right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    [Header("Neovisna provjera točnosti")]
    [Tooltip("Točke koje NE sudjeluju u umjeravanju; služe samo za mjerenje pogreške.")]
    [SerializeField] private CoordinateReferencePoint[] verificationPoints;

    [ContextMenu("Izmjeri točnost na provjernim točkama")]
    public void MeasureAccuracy()
    {
        if (verificationPoints == null || verificationPoints.Length == 0)
        {
            Debug.LogWarning("[Provjera] Nema zadanih provjernih točaka.");
            return;
        }

        GetAnchorFrame(out Vector3d anchorEcef, out Vector3d eastAxis,
                       out Vector3d northAxis, out _);

        double sumSquared = 0.0;
        double worst = 0.0;
        int count = 0;

        foreach (CoordinateReferencePoint point in verificationPoints)
        {
            if (point == null) continue;

            // Očekivano: očitano s karte, upisano u komponentu.
            Vector3d expectedEcef = GeodeticToEcef(
                point.latitude, point.longitude, anchorHaeMeters);

            // Dobiveno: pretvorba položaja iz scene.
            Wgs84Coordinate converted = UnityToWgs84Coordinate(point.transform.position);
            Vector3d convertedEcef = GeodeticToEcef(
                converted.Latitude, converted.Longitude, anchorHaeMeters);

            Vector3d delta = convertedEcef - expectedEcef;
            double dEast = Vector3d.Dot(delta, eastAxis);
            double dNorth = Vector3d.Dot(delta, northAxis);
            double error = Math.Sqrt(dEast * dEast + dNorth * dNorth);

            sumSquared += error * error;
            worst = Math.Max(worst, error);
            count++;

            Debug.Log($"[Provjera] {point.name}: {error:F2} m " +
                      $"(istok {dEast:F2} m, sjever {dNorth:F2} m)");
        }

        if (count == 0) return;

        Debug.Log($"[Provjera] Točaka: {count}, " +
                  $"RMS {Math.Sqrt(sumSquared / count):F2} m, najveće {worst:F2} m");
    }

    [ContextMenu("Provjeri obratljivost pretvorbe")]
    public void MeasureRoundTrip()
    {
        var samples = new List<Transform>();
        if (controlPoints != null)
            foreach (var p in controlPoints) if (p != null) samples.Add(p.transform);
        if (verificationPoints != null)
            foreach (var p in verificationPoints) if (p != null) samples.Add(p.transform);

        if (samples.Count == 0)
        {
            Debug.LogWarning("[Obratljivost] Nema točaka za uzorkovanje.");
            return;
        }

        Vector3 origin = UnityOriginPosition;
        double sumSquared = 0.0;
        double worst = 0.0;

        foreach (Transform t in samples)
        {
            Wgs84Coordinate g = UnityToWgs84Coordinate(t.position);
            Vector3 back = Wgs84ToUnityPosition(g.Latitude, g.Longitude, g.HaeMeters);

            GetEastNorth(t.position.x - origin.x, t.position.z - origin.z,
                         out double e1, out double n1);
            GetEastNorth(back.x - origin.x, back.z - origin.z,
                         out double e2, out double n2);

            double error = Math.Sqrt((e2 - e1) * (e2 - e1) + (n2 - n1) * (n2 - n1));
            sumSquared += error * error;
            worst = Math.Max(worst, error);

            Debug.Log($"[Obratljivost] {t.name}: pomak {error:F3} m");
        }

        Debug.Log($"[Obratljivost] Točaka: {samples.Count}, " +
                  $"RMS {Math.Sqrt(sumSquared / samples.Count):F3} m, najveće {worst:F3} m");
    }
}
