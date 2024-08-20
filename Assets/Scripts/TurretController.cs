using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretController : MonoBehaviour
{
    [SerializeField] private Transform turret;
    [SerializeField] private Transform barrel;
    // [SerializeField] private Transform firePoint;
    [SerializeField] private float rotationSpeed = 0.1f;
    [SerializeField] private Transform target; // serialized for debugging purposes

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (target == null) return;
        AimAtTarget();
    }

    private void AimAtTarget() {
        Vector3 targetDirection = target.position - turret.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Vector3 turretRotation = Quaternion.Lerp(turret.rotation, targetRotation, rotationSpeed * Time.deltaTime).eulerAngles;
        turret.rotation = Quaternion.Euler(0f, turretRotation.y, 0f);

        Vector3 barrelDirection = target.position - barrel.position;
        Quaternion targetBarrelRotation = Quaternion.LookRotation(barrelDirection);
        Vector3 barrelRotation = Quaternion.Lerp(barrel.rotation, targetBarrelRotation, rotationSpeed * Time.deltaTime).eulerAngles;
        barrel.rotation = Quaternion.Euler(barrelRotation.x, barrel.rotation.eulerAngles.y, barrel.rotation.eulerAngles.z);
    }

    public void SetTarget(Transform target) {
        this.target = target;
    }

    public void ClearTarget() {
        target = null;
    }
}
