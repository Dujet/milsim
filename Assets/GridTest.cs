using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTest : MonoBehaviour
{
    public Grid grid;
    private int terrainSizeX = 1000;
    private int terrainSizeZ = 1000;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        
        int gridWidth = terrainSizeX / (int)grid.cellSize.x;
        int gridLength = terrainSizeZ / (int)grid.cellSize.z;

        for (int z = gridLength - 1; z >= 0; z--) {
            for (int x = 0; x < gridWidth; x++) {
                Vector3 cellCenter = grid.CellToWorld(new Vector3Int(x, 0, z));
                Gizmos.DrawWireSphere(cellCenter, 20);
            }
        }
    }
}
