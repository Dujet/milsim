using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretController : MonoBehaviour
{
    [SerializeField] private Transform turret;
    [SerializeField] private Transform barrel;
    // [SerializeField] private Transform firePoint;
    [SerializeField] private float rotationSpeed = 30f; // degrees per second
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

    // FIXME: barrel rotating along its y axis
    private void AimAtTarget() {
        // Rotate the turret
        Vector3 targetDirection = target.position - turret.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        turret.rotation = Quaternion.Euler(0f, turret.rotation.eulerAngles.y, 0f);

        // Rotate the barrel
        Vector3 barrelDirection = target.position - barrel.position;
        Quaternion targetBarrelRotation = Quaternion.LookRotation(barrelDirection);
        float targetBarrelX = targetBarrelRotation.eulerAngles.x;
        Quaternion newBarrelRotation = Quaternion.Euler(targetBarrelX, 0f, 0f);
        barrel.localRotation = Quaternion.RotateTowards(barrel.localRotation, newBarrelRotation, rotationSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform target) {
        this.target = target;
    }

    public void ClearTarget() {
        target = null;
    }
}
