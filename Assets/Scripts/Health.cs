using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    private AIStateManager aiStateManager;
    private bool isDead;

    public bool IsDead { get {return isDead; }}
    
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        aiStateManager = GetComponent<AIStateManager>();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return; // can't die twice
        
        isDead = true;
        //Destroy(gameObject);
        // call the death animation or
        // enter death state
        aiStateManager.ChangeState(new UnitDeathState(aiStateManager));
        return;
    }

    public bool IsLethal(float damage) {
        return currentHealth - damage <= 0;
    }
}
