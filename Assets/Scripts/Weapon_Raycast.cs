using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Raycast : Weapon
{
    [SerializeField] private GameObject _impactEffectPrefab;
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
            CreateImpactEffect(hit.point, hit.normal);
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

    // TODO: import spark impact effect from past project
    private void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        if (_impactEffectPrefab != null)
        {
            var impact = Instantiate(_impactEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(impact, 2f);
        }
    }
}
