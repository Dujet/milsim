using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CotEntity
{
    public string uid;

    public string cotType = "a-f-G-U-C"; // default to friendly ground combat

    public string callsign = "TANK-01";
    public Transform transform;
    public string remarks = "";
}
