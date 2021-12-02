using System;
using UnityEngine;

public class CreatureHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float health;
    private Action<CreatureHealth> OnDeath;

    [Header("Creature References")]
    public AntBrain antController;

    public void DamageCreature(float damage)
    {
        health -= damage;

        if (health <= 0)
        {
            if (antController.controlledAnt)
            {
                GameManager.instance.LoadPlayer();
                //call player reset = new ant
                return;
            }

            OnDeath(this);
        }
    }

    public void SetDeath(Action<CreatureHealth> deathToAnt)
    {
        OnDeath = deathToAnt;
    }
}