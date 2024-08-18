using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackTest : MonoBehaviour
{
    public Weapon weapon;
    public Transform target;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        weapon.Fire(target);
    }
}
