using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_RaycastTurret : Weapon_Raycast
{
    [SerializeField] private TurretController turretController;
    [SerializeField] private Transform barrel;
    [SerializeField] private ParticleSystem muzzleFlash;
    public override bool CanAttack(Transform target)
    {
        return attackTimer <= 0 && WeaponIsAligned(target);
    }

    // NOTE: explore calculating combined angle of turret and barrel
    private bool WeaponIsAligned(Transform target) {
        // calculate turret to target angle on the xz plane
        Vector3 targetDirection = target.position - barrel.position;
        //targetDirection.y = 0;
        //Vector3 turretDirection = turret.forward;
        //float angleTurret = Vector3.Angle(targetDirection, turretDirection);

        // calculate barrel to target angle (barrel should be looking directly at target)
        Vector3 barrelDirection = target.position - barrel.position;
        float angle = Vector3.Angle(barrelDirection, barrel.forward);
        
        return angle < 1f;

    }

    public override bool Fire(Transform target) {
        turretController.SetTarget(target);
        if (CanAttack(target))
        {
            Attack(target);
            ResetAttackTimer();
            PlayMuzzleFlash();
            return true;
        }

        return false;
    }

    public override void StopAttack()
    {
        turretController.ClearTarget();
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            var obj = Instantiate(muzzleFlash, transform.position, Quaternion.Euler(-90, 0, 0));
            Destroy(obj.gameObject, 5f);
        }
    }
}
