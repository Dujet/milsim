using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Raycast : Weapon
{
    public override void Attack(Transform target)
    {
        RaycastHit hit;
        Vector3 direction = target.position - transform.position;
        Debug.DrawLine(transform.position, transform.position + direction.normalized * AttackRange, Color.red, 2.0f);
        if (Physics.Raycast(transform.position, direction, out hit, AttackRange))
        {
            Debug.DrawLine(transform.position, target.position, Color.green, 2.0f);
            Health targetHealth = hit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(Damage);
            }
        }
    }
}
