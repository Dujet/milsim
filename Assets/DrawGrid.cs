using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGrid : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private Transform target;
    private int cellCountX = 10;
    private int cellCountZ = 10;


    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        for (int x = 0; x < cellCountX; x++) {
            for (int z = 0; z < cellCountZ; z++) {
                Vector3Int cell = new Vector3Int(x, 0, z);
                Vector3 cellCenter = grid.CellToWorld(cell) + new Vector3(grid.cellSize.x, 0, grid.cellSize.z) * 0.5f;
                Gizmos.DrawWireCube(cellCenter, grid.cellSize);
            }
        }

        Gizmos.color = Color.blue;
        Vector3Int cellshit = grid.WorldToCell(target.position);
        Gizmos.DrawWireCube(grid.GetCellCenterWorld(cellshit), grid.cellSize);
    }
}
