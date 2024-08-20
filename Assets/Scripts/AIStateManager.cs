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
        set { isFollower = value; }
    }

    void Awake() {
        moveOrderGenerator = GetComponentInParent<MoveOrderGenerator>();
        if (moveOrderGenerator == null) {
            Debug.LogError("MoveOrderGenerator not found");
        }
        currentState = new UnitPatrolState(this);
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<FieldOfView>();
        if (weapon == null) weapon = GetComponent<Weapon>();
        attackRange = weapon.AttackRange;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void LateUpdate()
    {
        currentState?.Execute();
    }

    public void ChangeState(IUnitState newState)
    {

        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public List<AIStateManager> GetSquadMembers() {
        List<AIStateManager> squadMembers = new List<AIStateManager>();
        foreach (var member in moveOrderGenerator.Squad) {
            squadMembers.Add(member.GetComponent<AIStateManager>());
        }
        return squadMembers;
    }
}
