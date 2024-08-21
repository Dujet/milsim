using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class UnitAttackState : IUnitState
{
    private readonly AIStateManager aiStateManager;
    private Transform target;
    private Health targetHealth;

    public UnitAttackState(AIStateManager aiStateManager, Transform target)
    {
        this.aiStateManager = aiStateManager;
        this.target = target;
    }
    
    public void Enter()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Entering Attack State");
        aiStateManager.agent.ResetPath();
        targetHealth = target.GetComponent<Health>();
    }

    // TODO: explore using agent.isStopped instead of ResetPath
    public void Execute()
    {
        // Debug.Log("Executing Attack State");

        // If the (alive) target is not visible or out of range, change to chase state
        if ((!aiStateManager.fov.IsTargetVisible(target) ||
            Vector3.Distance(aiStateManager.transform.position, target.position) > aiStateManager.AttackRange)
            && !targetHealth.IsDead)
        {
            aiStateManager.ChangeState(new UnitChaseState(aiStateManager, target));
        }

        // If the target is dead, try to find a new target or start patrolling
        if (targetHealth.IsDead) {
            if (!updateTarget()) {
                aiStateManager.ChangeState(new UnitPatrolState(aiStateManager));
            }
            return;
        }

        // Attack the target
        bool fired = aiStateManager.Weapon.CanAttack(target);
        aiStateManager.Weapon.Fire(target);
        if (fired) {
            Debug.Log($"{aiStateManager.gameObject.name}:Attacking {target.gameObject.name}");
        }

        // If the target is still alive, return
        if (!targetHealth.IsDead) return;
        
        // After successful kill, find a new target or start patrolling
        if (!updateTarget()) {
            aiStateManager.ChangeState(new UnitPatrolState(aiStateManager));
        }
    
    }

    public void Exit()
    {
        Debug.Log($"{aiStateManager.gameObject.name}:Exiting Attack State");
        aiStateManager.Weapon.StopAttack();
    }

    // TODO: make this iterate through visible targets list if first one is dead
    private bool updateTarget() {
        Transform newTarget = aiStateManager.fov.GetClosestTarget();
        if (newTarget == null || newTarget == target) return false;

        Health newTargetHealth = target.GetComponent<Health>();
        if (newTargetHealth.IsDead) return false;

        target = newTarget;
        targetHealth = newTargetHealth;
        Debug.Log($"{aiStateManager.gameObject.name}:Target changed to {target.gameObject.name}");
        return true;
    }
}
