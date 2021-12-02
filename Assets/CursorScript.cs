using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorScript : MonoBehaviour
{
    public RectTransform thisTransform;

    private void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        thisTransform.position = Mouse.current.position.ReadValue();
    }
}
