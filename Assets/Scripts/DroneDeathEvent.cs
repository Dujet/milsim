using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneDeathEvent : DeathEvent
{
    [SerializeField] private GameObject _parentToDestroy;

    public override void OnDeath()
    {
        Destroy(_parentToDestroy);
    }
}
