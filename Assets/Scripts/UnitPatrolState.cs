using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPatrolState : IUnitState
{
    private readonly AIStateManager aiStateManager;

    public UnitPatrolState(AIStateManager aiStateManager)
    {
        this.aiStateManager = aiStateManager;
    }

    public void Enter()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Entering Patrol State");
        aiStateManager.agent.autoBraking = false;
    }

    public void Execute()
    {
        if (aiStateManager == null) return;

        if (!(aiStateManager.moveOrderGenerator?.hasPath() ?? true) && !aiStateManager.IsFollower){
            Debug.Log($"Patrol State: {aiStateManager.gameObject.name} generating new path");
            aiStateManager.moveOrderGenerator.generateMoveOrders();
        }

        Transform target = aiStateManager.fov.GetClosestTarget();
        if (target == null) return;
        if (!target.GetComponent<Health>().IsDead)
            aiStateManager.ChangeState(new UnitChaseState(aiStateManager, target));

        if (aiStateManager.IsFollower || target.GetComponent<Health>().IsDead) return;
        List<AIStateManager> squadMembers = aiStateManager.GetSquadMembers();
        Debug.Log($"{aiStateManager.gameObject.name}:Alerting {squadMembers.Count} squad members!");
        foreach (AIStateManager member in squadMembers) {
            if (member?.CurrentState is UnitPatrolState)
                member.ChangeState(new UnitChaseState(member, target));
        }
    }

    public void Exit()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Exiting Patrol State");
        aiStateManager.agent.autoBraking = true;
    }
}
