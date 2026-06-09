using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerTest : MonoBehaviour
{
    public CotMarkerTag Tag;
    
    // Start is called before the first frame update
    void Start()
    {
        Tag.Populate(new CotInboundEvent
        {
            CotType = "a-f-G-U-C",
            Callsign = "TestMarker"
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
