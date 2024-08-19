using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

using Vector3 = UnityEngine.Vector3;
using NumericsVector3 = System.Numerics.Vector3;
using Math = System.Math;



enum MoveOrderDirection {
    FORWARD = 1,
    BACKWARD = -1
}

[RequireComponent(typeof(NavMeshAgent))]
public class MoveOrderGenerator : MonoBehaviour
{

    [SerializeField] private int terrainSizeX;
    [SerializeField] private int terrainSizeZ;
    private NavMeshAgent agent;
    private Grid grid;
    private Vector3 targetPosition;
    private Vector3Int targetCell;
    private Vector3Int currentCell;
    private int gridSizeZ;
    private int gridSizeX;
    [SerializeField] private List<NavMeshAgent> squad;
    [SerializeField] private MoveOrderDirection direction;
    [SerializeField] private bool goStraight = false;

    public List<NavMeshAgent> Squad {
        get { return squad; }
    }

    // Start is called before the first frame update
    void Start()
    {
        gridSizeZ = terrainSizeZ / (int)grid.cellSize.z;
        gridSizeX = terrainSizeX / (int)grid.cellSize.x;
        currentCell = grid.WorldToCell(transform.position);
        targetCell = currentCell;
        agent = GetComponent<NavMeshAgent>();
        grid = GameObject.Find("Grid").GetComponent<Grid>();
        //advanceToNextCell();
    }

    public void generateMoveOrders() {
        if (targetCell.z <= 0) {
            // stop the agent
            agent.isStopped = true;
            return;
        }
        if (agent.remainingDistance < agent.stoppingDistance * 5 && !agent.pathPending && targetCell.z >= 0) {
            advanceToNextCell();
        }
    }
    
    public bool hasPath() {
        return agent.hasPath || agent.pathPending;
    }

    // TODO: fix bug where a cell is skipped on z axis
    private void advanceToNextCell() {
        //currentCell = grid.WorldToCell(agent.transform.position);
        currentCell = targetCell;
        int xOffset = Random.Range(-1, 2);

        if (goStraight || currentCell.x + xOffset < 0 || currentCell.x + xOffset >= terrainSizeX / grid.cellGap.x) {
            xOffset = 0;
        }

        targetCell = new Vector3Int(currentCell.x + xOffset, currentCell.y, currentCell.z + (int)direction);
        targetPosition = grid.GetCellCenterWorld(targetCell);

        Ray ray = new Ray(targetPosition + Vector3.up*1000, Vector3.down);
        RaycastHit hit;

        generateSquadOrders(ray);

        if (Physics.Raycast(ray, out hit)) {
            targetPosition = hit.point;
            agent.SetDestination(targetPosition);
        }
    }

    // TODO: possibly move squad orders to separate SquadManager class on unit group object
    private void generateSquadOrders(Ray rootRay) {
        Vector3 rayOrigin;
        Vector3 rayDirection = rootRay.direction;
        RaycastHit hit;

        int zOffset = 0;
        int xOffset = 0;
        int offsetDelta = 15 * -(int)direction;

        for (int i = 0; i < squad.Count; i++) {
            xOffset *= -1;
            if (i % 2 == 0) {
                xOffset += offsetDelta;
                zOffset += offsetDelta;
            }

            rayOrigin = new Vector3(rootRay.origin.x + xOffset, rootRay.origin.y, rootRay.origin.z + zOffset);
            Ray ray = new Ray(rayOrigin, rayDirection);

            if (Physics.Raycast(ray, out hit)) {
                squad[i].SetDestination(hit.point);
            }
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPosition, agent.stoppingDistance*5);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(grid.GetCellCenterWorld(currentCell), new Vector3(20, 50, 20));
    }
}
