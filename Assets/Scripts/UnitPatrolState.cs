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
    }

    // TODO: change states of squad members to chase when a target is found
    public void Execute()
    {
        if (aiStateManager == null) return;

        if (!(aiStateManager.moveOrderGenerator?.hasPath() ?? true) && !aiStateManager.IsFollower){
            Debug.Log($"Patrol State: {aiStateManager.gameObject.name} generating new path");
            aiStateManager.moveOrderGenerator.generateMoveOrders();
        }

        Transform target = aiStateManager.fov.GetClosestTarget();
        if (target == null) return;
        aiStateManager.ChangeState(new UnitChaseState(aiStateManager, target));

        if (aiStateManager.IsFollower) return;
        List<AIStateManager> squadMembers = aiStateManager.GetSquadMembers();
        Debug.Log($"{aiStateManager.gameObject.name}:Alerting {squadMembers.Count} squad members!");
        foreach (AIStateManager member in squadMembers) {
            member.ChangeState(new UnitChaseState(member, target));
        }
    }

    public void Exit()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Exiting Patrol State");
    }
}
