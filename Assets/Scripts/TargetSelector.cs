using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    private List<Transform> targets = new List<Transform>();
    public Transform selectedTarget { get; private set; }
    [SerializeField] private HUDMarkerController hudMarkerController;
    [SerializeField] private Color selectedColor = Color.yellow;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SelectTargetByRaycast();
        }    
    }

    public void AddTarget(Transform target)
    {
        if (target != null && !targets.Contains(target))
        {
            targets.Add(target);
            Debug.Log($"Target {target.name} added.");
            DecideNextTarget();
        }
        else
        {
            Debug.LogWarning("Target is null or already exists in the list.");
        }
    }

    public void RemoveTarget(Transform target)
    {
        if (targets.Contains(target))
        {
            targets.Remove(target);
            Debug.Log($"Target {target.name} removed.");
            if (selectedTarget == target)
            {
                selectedTarget = null;
                Debug.Log("Selected target removed, no target is currently selected.");
            }
        }
    }

    private void SelectTarget(Transform target)
    {
        if (target != null && targets.Contains(target))
        {
            if (selectedTarget != null)
            {
                hudMarkerController.SetMarkerColor(selectedTarget, 
                !target.CompareTag("Tank") ? Color.red : Color.gray);
            }

            selectedTarget = target;
            hudMarkerController.SetMarkerColor(target, selectedColor);
            Debug.Log($"Target {target.name} selected.");
        }
        else
        {
            Debug.LogWarning("Target is null or not in the list of targets.");
        }
    }

    public void DecideNextTarget()
    {
        if (targets.Count == 0) return;

        float maxScore = float.MinValue;
        Transform bestTarget = null;
        foreach (Transform target in targets)
        {
            if (target == null)
            {
                Debug.LogWarning("Encountered a null target in the list.");
                continue;
            }

            float score = TargetScore(target);
            if (score > maxScore)
            {
                maxScore = score;
                bestTarget = target;
            }
        }

        if (bestTarget != null) SelectTarget(bestTarget);
    }

    private float TargetScore(Transform target)
    {
        float score = 1000 - Vector3.Distance(transform.position, target.position);
        if (target.CompareTag("Tank"))
            score *= 2;

        return score;
    }

    public void SelectTargetByRaycast()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
        if (Physics.Raycast(ray, out RaycastHit hit, layerMask))
        {
            Transform hitTransform = hit.transform.root;
            if (targets.Contains(hitTransform))
            {
                SelectTarget(hitTransform);
            }
            else
            {
                Debug.LogWarning($"Hit object {hitTransform.name} is not a target.");
            }
        }
    }
}
