using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveDroneToTarget : MonoBehaviour
{
    [SerializeField] private TargetSelector targetSelector;
    [SerializeField] private Camera mainCamera;
    [SerializeField] float speed = 10f;
    [SerializeField] private Vector3 offset = new Vector3(0, 10, 0);
    private bool moving = false;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N)) StartMoveDroneToTarget();
    }

    private void StartMoveDroneToTarget()
    {
        if (moving) return;

        if (targetSelector.selectedTarget != null)
        {
            Vector3 targetPosition = targetSelector.selectedTarget.position + offset;
            StartCoroutine(MoveDroneToPosition(targetPosition));
        }
        else
        {
            Debug.LogWarning("No target selected to move the drone to.");
        }
    }

    private IEnumerator MoveDroneToPosition(Vector3 targetPosition)
    {
        moving = true;

        while (Vector3.Distance(transform.position, targetPosition) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            mainCamera.transform.LookAt(targetPosition);
            yield return null;
        }

        moving = false;
    }
}
