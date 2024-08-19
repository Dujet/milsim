using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitDeathState : IUnitState
{
    private readonly AIStateManager aiStateManager;

    public UnitDeathState(AIStateManager aIStateManager) {
        this.aiStateManager = aIStateManager;
    }

    // TODO: change squad leader if the leader dies
    public void Enter()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Bro died");
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
        Debug.LogError($"{aiStateManager.gameObject.name}:Bro resurrected");
    }
}
