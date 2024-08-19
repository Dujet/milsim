using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitDeathState : IUnitState
{
    private readonly AIStateManager aiStateManager;

    public UnitDeathState(AIStateManager aIStateManager) {
        this.aiStateManager = aIStateManager;
    }

    // TODO: test if squad leader change works
    public void Enter()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Bro died");
        aiStateManager.agent.ResetPath();
        aiStateManager.agent.enabled = false;
        aiStateManager.fov.enabled = false;
        aiStateManager.moveOrderGenerator.enabled = false;
        aiStateManager.Weapon.enabled = false;

        // If the unit is a squad leader, change the leader, otherwise remove the unit from the squad
        // TODO: explore just having a RemoveSquadMember method which check all this
        if (!aiStateManager.IsFollower) {
            aiStateManager.moveOrderGenerator.ReplaceLeader(aiStateManager.agent);
        } else {
            aiStateManager.moveOrderGenerator.RemoveSquadMember(aiStateManager.agent);
        }
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
