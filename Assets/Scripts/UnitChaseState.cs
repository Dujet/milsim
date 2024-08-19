using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitChaseState : IUnitState
{
    private readonly AIStateManager aiStateManager;
    private Transform target;

    public UnitChaseState(AIStateManager aiStateManager, Transform target) {
        this.aiStateManager = aiStateManager;
        this.target = target;
    }
    public void Enter()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Entering Chase State");
        aiStateManager.agent.SetDestination(target.position);
    }

    // TODO: occasionally update navmesh destination
    public void Execute()
    {
        //Debug.Log("Executing Chase State");

        if (!aiStateManager.fov.IsTargetVisible(target) && !aiStateManager.agent.hasPath) {
            aiStateManager.ChangeState(new UnitPatrolState(aiStateManager));
        }

        if (Vector3.Distance(aiStateManager.transform.position, target.position) <= aiStateManager.AttackRange*0.8) {
            aiStateManager.ChangeState(new UnitAttackState(aiStateManager, target));
        }

    }

    public void Exit()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Exiting Chase State");
    }

    public Transform GetTarget() {
        return target;
    }

    public void SetTarget(Transform target) {
        this.target = target;
    }
}
