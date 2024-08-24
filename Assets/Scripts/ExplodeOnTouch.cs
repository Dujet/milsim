using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeOnTouch : MonoBehaviour
{
    [SerializeField] private GameObject _explosionPrefab;
    private Collider _collider;
    private bool _exploded = false;
    [SerializeField] private float _damage = 500f;
    [SerializeField] private GameObject _parentToDestroy;


    // Start is called before the first frame update
    void Start()
    {
        _collider = GetComponentInChildren<Collider>();
        if (_parentToDestroy == null) _parentToDestroy = gameObject;
    }

    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("WARSAW") && !_exploded) {
            // Damage collided enemy
            if (collision.gameObject.TryGetComponent<Health>(out Health health)) {
                health.TakeDamage(_damage);
            }
            Debug.Log("Drone collided with: " + collision.gameObject.name);

            Explode();
        }
    }

    private void Explode() {
        _exploded = true;
        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        _collider.enabled = false;
        Destroy(_parentToDestroy, 0.5f);
    }
}
