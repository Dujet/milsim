using System;
using System.Collections;
using System.Collections.Generic;
using Polybrush;
using UnityEngine;

public class DroneCamera : MonoBehaviour
{
    [SerializeField] private Camera _cam;
    [SerializeField] private Transform _drone;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _zoomSpeed = 10f;
    private float _targetFOV;
    private float rotX;
    private float rotY;
    [SerializeField] private float _rotSpeed = 5f;

    void Awake() {
        if (_cam == null) {
            _cam = GetComponentInChildren<Camera>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _cam.transform.position = _drone.position + _offset;
        _cam.transform.rotation = _drone.rotation;
        _targetFOV = _cam.fieldOfView;
        rotX = _cam.transform.rotation.eulerAngles.x;
        rotY = _cam.transform.rotation.eulerAngles.y;
    }

    void FixedUpdate() {
        _cam.transform.position = _drone.position + _offset;
        
        rotX += Input.GetAxis("Mouse X")*_rotSpeed;
        rotY += Input.GetAxis ("Mouse Y")*_rotSpeed;

        rotY = Mathf.Clamp(rotY, -90f, 90f);      

        //Camera rotation only allowed if game us not paused
        _cam.transform.rotation = Quaternion.Euler(-rotY, rotX, 0f);

        // zoom in/out with mouse wheel
        // TODO: make zooming in/out logarithmic
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        //_cam.fieldOfView -= scroll * 10;
        _targetFOV -= scroll * 10;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetFOV, Time.deltaTime * _zoomSpeed);
    }

    public void OnDisable() {
        _cam.enabled = false;
        _cam.GetComponent<AudioListener>().enabled = false;
    }

    public void OnEnable() {
        _cam.enabled = true;
        _cam.GetComponent<AudioListener>().enabled = true;
    }
}
