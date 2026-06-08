using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateConverter
{
    // Anchor: Unity (0, 0, 0) = WGS84 (50.095917, 9.610977)
    private const double AnchorLat = 50.095917;
    private const double AnchorLon = 9.610977;

    // At latitude 50.09°:
    //   1 degree of latitude  ≈ 111,320 m  (nearly constant everywhere)
    //   1 degree of longitude ≈ 111,320 * cos(50.09°) ≈ 71,575 m
    private const double MetersPerDegLat = 111320.0;
    private const double MetersPerDegLon = 71575.0;

    public static Vector3 UnityToWgs84(Vector3 unityPos)
    {
        double lat = AnchorLat + (unityPos.z / MetersPerDegLat);
        double lon = AnchorLon + (unityPos.x / MetersPerDegLon);
        double alt = unityPos.y;

        return new Vector3((float)lat, (float)lon, (float)alt);
    }

    public static Vector3 Wgs84ToUnity(double lat, double lon, double alt = 0.0)
    {
        float x = (float)((lon - AnchorLon) * MetersPerDegLon);
        float y = (float)alt;
        float z = (float)((lat - AnchorLat) * MetersPerDegLat);

        return new Vector3(x, y, z);
    }
}
