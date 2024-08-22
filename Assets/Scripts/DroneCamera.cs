using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneCamera : MonoBehaviour
{
    [SerializeField] private Camera _cam;
    [SerializeField] private Transform _drone;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _lerpSpeed = 0.125f;

    // Start is called before the first frame update
    void Start()
    {
        _cam.transform.position = _drone.position + _offset;
    }

    void FixedUpdate() {
        _cam.transform.position = _drone.position + _offset;
        
        // rotate camera with mouse
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _cam.transform.RotateAround(_drone.position, Vector3.up, mouseX);
        _cam.transform.RotateAround(_drone.position, _cam.transform.right, -mouseY);

        // zoom in/out with mouse wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _cam.fieldOfView -= scroll * 10;



    }
}
