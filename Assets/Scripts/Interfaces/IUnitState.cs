using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitState
{
    void Execute();
    void Enter();
    void Exit();
}
