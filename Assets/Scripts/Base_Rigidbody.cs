using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Base_RigidBody : MonoBehaviour
{

    [Header("Rigidbody properties")]
    [SerializeField] private float weight = 1f;
    protected Rigidbody rb;
    [SerializeField] protected float startDrag = 0.5f;
    [SerializeField] protected float startAngularDrag = 0.05f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if(rb) {
            rb.mass = weight;
            startDrag = rb.drag;
            startAngularDrag = rb.angularDrag;
        }
    }


    void FixedUpdate()
    {
        if (!rb) return;

        HandlePhysics();
    }

    protected virtual void HandlePhysics()
    {
        // Override this method to add custom physics
    }
}
