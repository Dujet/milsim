using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodeOnTouch : MonoBehaviour
{
    [SerializeField] private GameObject _explosionPrefab;
    private Collider _collider;
    private bool _exploded = false;
    [SerializeField] private float _damage = 500f;
    private GameObject _prefabParent;


    // Start is called before the first frame update
    void Start()
    {
        _collider = GetComponentInChildren<Collider>();
        _prefabParent = transform.parent.gameObject;
    }

    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("WARSAW") && !_exploded) {
            Explode();

            // Damage collided enemy
            if (TryGetComponent<Health>(out Health health)) {
                health.TakeDamage(_damage);
            }
        }
    }

    private void Explode() {
        _exploded = true;
        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        _collider.enabled = false;
        Destroy(_prefabParent, 0.5f);
    }
}
