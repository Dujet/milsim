using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class Drone_Inputs : MonoBehaviour
{

    private Vector2 cyclicInput;
    private float pedalInput;
    private float throttleInput;

    public Vector2 CyclicInput { get => cyclicInput; }
    public float PedalInput { get => pedalInput; }
    public float ThrottleInput { get => throttleInput; }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCyclic(InputValue value)
    {
        cyclicInput = value.Get<Vector2>();
    }

    public void OnPedals(InputValue value)
    {
        pedalInput = value.Get<float>();
    }

    public void OnThrottle(InputValue value)
    {
        throttleInput = value.Get<float>();
    }
}
