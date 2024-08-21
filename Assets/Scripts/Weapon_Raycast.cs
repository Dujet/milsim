using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Raycast : Weapon
{
    // TODO: explore targeting collider center instead of transform position
    // FIXME: raycast hits the weapon/weapon holder instead of the target
    public override void Attack(Transform target)
    {
        RaycastHit hit;
        Vector3 direction = target.position - transform.position;
        Debug.DrawLine(transform.position, transform.position + direction.normalized * AttackRange, Color.red, 1.0f);
        if (Physics.Raycast(transform.position, direction, out hit, AttackRange))
        {
            Debug.Log($"{gameObject.name}:Raycast hit {hit.transform.gameObject.name}");
            Debug.DrawLine(transform.position, hit.point, Color.green, 2.0f);
            Health targetHealth = hit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(Damage);
            }
        }
    }

    public override void StopAttack()
    {
        Debug.Log("Stopping attack");
    }
}
