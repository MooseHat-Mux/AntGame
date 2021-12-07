using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbScript : MonoBehaviour
{
    [Header("Limb Status")]
    public bool moving;
    public float angleModifier = 25;

    [Header("Limb References")]
    public Transform Center;

    private void Start()
    {
        transform.SetParent(GameManager.instance.IkTargetParent);
    }

    private void Update()
    {
        //Rotate tip of limb to point towards target
        Vector2 rotDir = Vector2.up;

        if (transform.position.x > Center.position.x)
        {
            float angle = Mathf.Atan2(rotDir.y, rotDir.x) * Mathf.Rad2Deg + angleModifier;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else
        {
            float angle = Mathf.Atan2(rotDir.y, rotDir.x) * Mathf.Rad2Deg - angleModifier;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }
}
