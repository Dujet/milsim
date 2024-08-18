using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(Drone_Inputs))]
public class Drone_Controller : Base_RigidBody
{

    [Header("Drone properties")]
    [SerializeField] private float minMaxPitch = 30f;
    [SerializeField] private float minMaxRoll = 30f;
    [SerializeField] private float yawPower = 4f;
    [SerializeField] private float lerpSpeed = 2f;

    private Drone_Inputs input;
    private List<IEngine> engines = new List<IEngine>();

    private float finalPitch;
    private float finalRoll;
    private float yaw;
    private float finalYaw;

    // Start is called before the first frame update
    void Start()
    {
        input = GetComponent<Drone_Inputs>();
        engines = GetComponentsInChildren<IEngine>().ToList();
    }

    protected override void HandlePhysics()
    {
        HandleEngines();
        HandleControls();
    }

    protected virtual void HandleEngines()
    {
        foreach(IEngine engine in engines) {
            engine.UpdateEngine(rb, input);
        }
    }

    protected virtual void HandleControls()
    {
        float pitch = -input.CyclicInput.y * minMaxPitch;
        float roll  = input.CyclicInput.x * minMaxRoll;
        yaw        += input.PedalInput * yawPower;

        finalPitch = Mathf.Lerp(finalPitch, pitch, Time.deltaTime * lerpSpeed);
        finalRoll = Mathf.Lerp(finalRoll, roll, Time.deltaTime * lerpSpeed);
        finalYaw = Mathf.Lerp(finalYaw, yaw, Time.deltaTime * lerpSpeed);

        Quaternion rot = Quaternion.Euler(finalPitch, finalYaw, finalRoll);
        rb.MoveRotation(rot);
    }
}
