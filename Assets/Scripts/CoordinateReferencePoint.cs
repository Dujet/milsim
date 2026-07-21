using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A surveyed landmark used to calibrate <see cref="CoordinateConverter"/>.
/// Put this component on an object positioned at the matching Unity landmark,
/// then enter that landmark's WGS-84 coordinates in the Inspector.
/// </summary>
public sealed class CoordinateReferencePoint : MonoBehaviour
{
    [Tooltip("Latitude of this landmark in decimal degrees. North is positive.")]
    public double latitude;

    [Tooltip("Longitude of this landmark in decimal degrees. East is positive.")]
    public double longitude;

    [Tooltip("Optional landmark HAE. It is retained for documentation; horizontal calibration uses latitude and longitude.")]
    public double haeMeters;
}
