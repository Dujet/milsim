using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected float damage = 30f;
    [SerializeField] protected float attackRange = 50f;
    [SerializeField] protected float attackInterval = 3f;
    private float attackTimer = 0f;

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

    public abstract void Attack(Transform target);

    public bool Fire(Transform target) {
        if (CanAttack())
        {
            Attack(target);
            ResetAttackTimer();
            return true;
        }

        return false;
    }

    public void UpdateAttackTimer()
    {
        if (attackTimer < 0) return;
        attackTimer -= Time.deltaTime;
    }

    public bool CanAttack()
    {
        return attackTimer <= 0;
    }

    public void ResetAttackTimer()
    {
        attackTimer = attackInterval;
    }


}
