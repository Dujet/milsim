using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitDeathEvent : DeathEvent
{
    [SerializeField] private GameObject _deathEffect;
    [SerializeField] private Transform _deathEffectSpawnPoint;
    public override void OnDeath()
    {
        AIStateManager _aism = GetComponent<AIStateManager>();
        _aism.ChangeState(new UnitDeathState(_aism));

        if (_deathEffect != null && _deathEffectSpawnPoint != null)
        {
            Instantiate(_deathEffect, _deathEffectSpawnPoint.position, _deathEffectSpawnPoint.rotation);
        }
    }
}
