using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPatrolState : IUnitState
{
    private readonly AIStateManager aiStateManager;
    private bool isFollower;

    public UnitPatrolState(AIStateManager aiStateManager, bool isFollower = false)
    {
        this.aiStateManager = aiStateManager;
        this.isFollower = isFollower;
    }

    public void Enter()
    {
        Debug.Log("Entering Patrol State");
    }

    public void Execute()
    {
        if (!aiStateManager.moveOrderGenerator.hasPath() && !isFollower){
            Debug.Log("Patrol State: generating new path");
            aiStateManager.moveOrderGenerator.generateMoveOrders();
        }

        Transform target = aiStateManager.fov.GetClosestTarget();
        if (target != null) {
            aiStateManager.ChangeState(new UnitChaseState(aiStateManager, target));
        }
    }

    public void Exit()
    {
        Debug.Log("Exiting Patrol State");
    }
}
