using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitDeathState : IUnitState
{
    private readonly AIStateManager aiStateManager;

    public UnitDeathState(AIStateManager aIStateManager) {
        this.aiStateManager = aIStateManager;
    }

    public void Enter()
    {
        Debug.Log("Bro died");
        aiStateManager.agent.ResetPath();
        aiStateManager.agent.enabled = false;
        aiStateManager.fov.enabled = false;
        aiStateManager.moveOrderGenerator.enabled = false;
        aiStateManager.Weapon.enabled = false;
    }

    public void Execute()
    {
        return;
    }

    public void Exit()
    {
        Debug.Log("Bro resurrected");
    }
}
