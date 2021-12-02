using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbScript : MonoBehaviour
{
    [Header("Limb Status")]
    public bool moving;

    [Header("Limb References")]
    public Transform Center;
    public Transform LimbTip;

    private void Start()
    {
        transform.SetParent(GameManager.instance.IkTargetParent);
    }
}
