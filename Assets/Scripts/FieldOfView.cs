using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FieldOfView : MonoBehaviour
{

    [Range (0, 360)]
    public float viewAngle;
    public float viewRadius;
    public LayerMask obstacleMask;
    public LayerMask targetMask;
    public List<Transform> visibleTargets = new List<Transform>(); // contains visible enemy targets which are alive

    [SerializeField] private float scanFrequency = 5f;
    [SerializeField] private bool showDebugGizmos = true;

    void Start() {
        StartCoroutine(FindTargetsWithDelay(1f / scanFrequency));
    }

    IEnumerator FindTargetsWithDelay(float delay) {
        while (true) {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
            //FilterFriendlyTargets();
        }
    }

    private void FindVisibleTargets() {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++) {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2) {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask) && IsValidTarget(target)) {
                    visibleTargets.Add(target);
                }
            }
        }
    }

    private bool IsValidTarget(Transform target) {
        Health health = target.GetComponent<Health>();
        if (health == null) return false;
        return !target.CompareTag(gameObject.tag) && !health.IsDead;
    }

    private void FilterFriendlyTargets() {
        for (int i = 0; i < visibleTargets.Count; i++) {
            if (visibleTargets[i].CompareTag(gameObject.tag)) {
                visibleTargets.RemoveAt(i);
            }
        }
    }

    public Transform GetClosestTarget() {
        Transform closestTarget = null;
        float minDistance = Mathf.Infinity;
        foreach (Transform target in visibleTargets) {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance < minDistance) {
                minDistance = distance;
                closestTarget = target;
            }
        }
        return closestTarget;
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal) {
        if (!angleIsGlobal) {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    void OnDrawGizmos() {
        if (!showDebugGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

        Gizmos.color = Color.green;
        foreach (Transform visibleTarget in visibleTargets) {
            Gizmos.DrawLine(transform.position, visibleTarget.position);
        }
    }

    public bool IsTargetVisible(Transform target) {
        return visibleTargets.Contains(target);
    }

    // NOTE: possible use case when visibleTargets is large or not updated
    public bool IsTargetVisible2(Transform target) {
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2) {
            float dstToTarget = Vector3.Distance(transform.position, target.position);
            if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask)) {
                return true;
            }
        }
        return false;
    }
}
