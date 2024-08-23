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

    void Awake() {
        if (_cam == null) {
            _cam = GetComponentInChildren<Camera>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _cam.transform.position = _drone.position + _offset;
        _targetFOV = _cam.fieldOfView;
    }

    void FixedUpdate() {
        _cam.transform.position = _drone.position + _offset;
        
        // rotate camera with mouse
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _cam.transform.RotateAround(_drone.position, Vector3.up, mouseX);
        _cam.transform.RotateAround(_drone.position, _cam.transform.right, -mouseY);

        // zoom in/out with mouse wheel
        // TODO: make zooming in/out logarithmic
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        //_cam.fieldOfView -= scroll * 10;
        _targetFOV -= scroll * 10;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetFOV, Time.deltaTime * _zoomSpeed);
    }

    public void DisableCamera() {
        _cam.enabled = false;
        _cam.GetComponent<AudioListener>().enabled = false;
    }

    public void EnableCamera() {
        _cam.enabled = true;
        _cam.GetComponent<AudioListener>().enabled = true;
    }
}
