using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitDeathEvent : DeathEvent
{
    public override void OnDeath()
    {
        AIStateManager _aism = GetComponent<AIStateManager>();
        _aism.ChangeState(new UnitDeathState(_aism));
    }
}
