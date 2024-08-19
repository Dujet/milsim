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
        Debug.Log("Entering Patrol State");
    }

    // TODO: change states of squad members to chase when a target is found
    public void Execute()
    {
        if (!aiStateManager.moveOrderGenerator.hasPath() && !aiStateManager.IsFollower){
            Debug.Log("Patrol State: generating new path");
            aiStateManager.moveOrderGenerator.generateMoveOrders();
        }

        Transform target = aiStateManager.fov.GetClosestTarget();
        if (target == null) return;
        aiStateManager.ChangeState(new UnitChaseState(aiStateManager, target));

        if (aiStateManager.IsFollower) return;
        List<AIStateManager> squadMembers = aiStateManager.GetSquadMembers();
        foreach (AIStateManager member in squadMembers) {
            member.ChangeState(new UnitChaseState(member, target));
        }
    }

    public void Exit()
    {
        Debug.Log("Exiting Patrol State");
    }
}
