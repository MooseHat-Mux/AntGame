using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentDamage : MonoBehaviour
{
    [Header("Hazard Settings")]
    public float damage;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CreatureHealth thisCreature = collision.gameObject.GetComponent<CreatureHealth>();

        if (thisCreature != null)
        {
            thisCreature.DamageCreature(damage);
        }
    }
}
