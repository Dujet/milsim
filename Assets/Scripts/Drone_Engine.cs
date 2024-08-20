using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(BoxCollider))]
public class Drone_Engine : MonoBehaviour, IEngine
{
    [Header("Engine Properties")]
    [SerializeField] private float maxPower = 4f;

    public void InitEngine()
    {
        throw new System.NotImplementedException();
    }

    public void UpdateEngine(Rigidbody rb, Drone_Inputs input)
    {
        //Debug.Log("Running Engine: " + gameObject.name);
        Vector3 upVec = transform.up;
        upVec.x = 0f;
        upVec.z = 0f;
        float diff = 1 - upVec.magnitude;
        float finalDiff = diff * Physics.gravity.magnitude; // added force to counter gravity while pitching/rolling

        Vector3 engineForce = Vector3.zero;
        engineForce = transform.up * ((rb.mass * Physics.gravity.magnitude) + finalDiff + input.ThrottleInput * maxPower) / 4f;

        rb.AddForce(engineForce, ForceMode.Force);
    }
}
