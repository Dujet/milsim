using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitFollowState : IUnitState
{
    private AIStateManager aiStateManager;

    public UnitFollowState(AIStateManager aiStateManager)
    {
        this.aiStateManager = aiStateManager;
    }

    public void Enter()
    {
        Debug.Log("Entering follow state");
    }

    public void Execute()
    {
        throw new System.NotImplementedException();
    }

    public void Exit()
    {
        Debug.Log("Exiting follow state");
    }
}
