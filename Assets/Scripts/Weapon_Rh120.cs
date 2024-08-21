using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Rh120 : Weapon
{
    [SerializeField] GameObject projectilePrefab;
    public override void Attack(Transform target)
    {
        
    }

    public override void StopAttack()
    {
        throw new System.NotImplementedException();
    }
}
