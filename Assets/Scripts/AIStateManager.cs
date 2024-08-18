using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIStateManager : MonoBehaviour
{
    private IUnitState currentState;
    [HideInInspector] public MoveOrderGenerator moveOrderGenerator;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public FieldOfView fov;
    private float attackRange = 5f; // default attack range of the unit
    [SerializeField] private Weapon weapon;
    [SerializeField] private bool isFollower = false;

    public float AttackRange
    {
        get { return attackRange; }
    }

    public IUnitState CurrentState
    {
        get { return currentState; }
    }

    public Weapon Weapon
    {
        get { return weapon; }
    }
    
    public bool IsFollower {
        get { return isFollower; }
    }

    // Start is called before the first frame update
    void Start()
    {
        moveOrderGenerator = GetComponent<MoveOrderGenerator>();
        currentState = new UnitPatrolState(this, isFollower);
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<FieldOfView>();
        weapon = GetComponent<Weapon>();
        attackRange = weapon.AttackRange;
    }

    void LateUpdate()
    {
        currentState.Execute();
    }

    public void ChangeState(IUnitState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter();
    }
}
