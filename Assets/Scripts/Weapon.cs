using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected float damage = 30f;
    [SerializeField] protected float attackRange = 50f;
    [SerializeField] protected float attackInterval = 3f;
    protected float attackTimer = 0f;

    void Update() {
        UpdateAttackTimer();
    }

    public float Damage
    {
        get { return damage; }
    }

    public float AttackRange
    {
        get { return attackRange; }
    }

    public float AttackInterval
    {
        get { return attackInterval; }
    }

    public float AttackTimer
    {
        get { return attackTimer; }
        set { attackTimer = value; }
    }

    // TODO: change to protected and test
    public abstract void Attack(Transform target);

    public virtual bool Fire(Transform target) {
        if (CanAttack(target))
        {
            Attack(target);
            ResetAttackTimer();
            return true;
        }

        return false;
    }

    private void UpdateAttackTimer()
    {
        if (attackTimer < 0) return;
        attackTimer -= Time.deltaTime;
    }

    public virtual bool CanAttack(Transform target)
    {
        return attackTimer <= 0;
    }

    public void ResetAttackTimer()
    {
        attackTimer = attackInterval;
    }

    public abstract void StopAttack();
}
