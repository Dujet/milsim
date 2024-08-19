using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class UnitAttackState : IUnitState
{
    private readonly AIStateManager aiStateManager;
    private Transform target;

    public UnitAttackState(AIStateManager aiStateManager, Transform target)
    {
        this.aiStateManager = aiStateManager;
        this.target = target;
    }
    
    public void Enter()
    {
        Debug.Log("Entering Attack State");
        aiStateManager.agent.ResetPath();
    }

    public void Execute()
    {
        // Debug.Log("Executing Attack State");

        // If the target is not visible or out of range, change to chase state
        // TODO: add health check so that the unit doesn't chase a dead target
        if (!aiStateManager.fov.IsTargetVisible(target) ||
            Vector3.Distance(aiStateManager.transform.position, target.position) > aiStateManager.AttackRange)
        {
            aiStateManager.ChangeState(new UnitChaseState(aiStateManager, target));
        }

        //
        //
        // Add attack logic here
        bool fired = aiStateManager.Weapon.CanAttack();
        aiStateManager.Weapon.Fire(target);

        Health targetHealth = target.GetComponent<Health>();
        if (!targetHealth.IsDead) return;
        
        if (!changeTarget()) {
            aiStateManager.ChangeState(new UnitPatrolState(aiStateManager));
        }
        

    }

    public void Exit()
    {
        Debug.Log("Exiting Attack State");
    }

    // TODO: test if spam fixed
    private bool changeTarget() {
        Transform target = aiStateManager.fov.GetClosestTarget();
        if (target == null || target == this.target) return false;

        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth.IsDead) return false;

        this.target = target;
        Debug.Log("Target successfully changed");
        return true;
    }
}
