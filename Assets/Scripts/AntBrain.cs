using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class AntBrain : MonoBehaviour
{
    [Header("Movement Settings")]
    public float MoveSpeed;
    public float SprintSpeed;

    public float jumpForce;
    public float clingForce;

    public float groundCheckDistance;
    public float climbCheckDistance;

    public float rotationSpeed;

    public float minClimbAngle;

    public bool grounded;
    public bool climbing;

    [Header("Ant Info")]
    public AntData dataForAnt;

    [Header("Ant Settings")]
    public bool controlledAnt;
    public int digRadius;
    public float digDistance;
    public float liftDistance;
    public float liftStrength;

    public float hungerLimit;
    public bool foodStored;
    public bool liftedObject;

    [Header("Ant References")]
    public Vector2 dir;
    public Rigidbody2D rb;
    public Animator spriteAnim;
    public CreatureHealth antHealth;
    public Transform antHead;
    public Transform headLookTarget;
    public Transform interestTarget;
    public Transform antBoneBody;
    public Transform groundPosCheck;
    public Transform climbPosCheck;
    public LocomotionScript locomotionController;
    //public SpriteRenderer digDebug;

    private Vector2 lookPos;
    private RaycastHit2D[] climbResults = new RaycastHit2D[2];

    private void Start()
    {
        clingForce = Physics2D.gravity.magnitude;
    }

    private void FixedUpdate()
    {
        CheckSurface();
        //if (controlledAnt)
        //{
        //    return;
        //}

        AntMove(dir);
    }

    public void ResetData(AntData thisData)
    {
        antHealth.health = thisData.health;
        locomotionController.ResetTargetPos();
        SetLookDir(lookPos);
    }

    public void SetDir(Vector2 newDir)
    {
        dir = newDir;
        dir.y = 0;

        if (!controlledAnt)
        {
            if (interestTarget != null)
            {
                lookPos = interestTarget.position;
            } 
            else
            {
                lookPos = newDir;
            }

            SetLookDir(lookPos);
        }
    }

    public void SetLookDir()
    {
        Vector2 flip = spriteAnim.transform.localScale;
        Vector2 spotCheck = Camera.main.ScreenToWorldPoint(lookPos);
        headLookTarget.position = spotCheck;
        spotCheck -= (Vector2)antHead.position;

        if (headLookTarget.position.x > transform.position.x)
        {
            flip.x = 1;

            float angle = Mathf.Atan2(spotCheck.y, spotCheck.x) * Mathf.Rad2Deg;
            headLookTarget.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else
        {
            flip.x = -1;

            float angle = Mathf.Atan2(spotCheck.y, spotCheck.x) * Mathf.Rad2Deg - 180;
            headLookTarget.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }

        int groupLength = locomotionController.limbGroups[0].limbTargets.Length;
        for (int i = 0; i < groupLength; i++)
        {
            locomotionController.limbTargets[locomotionController.limbGroups[0].limbTargets[i]].Center.transform.localScale = flip;
        }

        spriteAnim.transform.localScale = flip;
    }

    public void SetLookDir(Vector2 newDir)
    {
        Vector2 flip = spriteAnim.transform.localScale;

        if (newDir.x >= 0)
        {
            flip.x = 1;

            //Vector3 headPos = antHead.position;
            //headPos.x = newDir.x;
            //headLookTarget.position = headPos + (Vector3)newDir.normalized;

            float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
            headLookTarget.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else
        {
            flip.x = -1;

            //Vector3 headPos = antHead.position;
            //headPos.x = newDir.x;
            //headLookTarget.position = headPos - (Vector3)newDir.normalized;

            float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg - 180;
            headLookTarget.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }

        spriteAnim.transform.localScale = flip;

        flip.x *= -1;
        int groupLength = locomotionController.limbGroups[0].limbTargets.Length;
        for (int i = 0; i < groupLength; i++)
        {
            locomotionController.limbTargets[locomotionController.limbGroups[0].limbTargets[i]].Center.transform.localScale = flip;
        }
    }

    public void AntMove(Vector2 newDir)
    {
        if (newDir.magnitude > 0 && grounded)
        {
            rb.MovePosition(rb.position + MoveSpeed * Time.deltaTime * newDir);
        }
    }

    public void CheckSurface()
    {
        grounded = Physics2D.Raycast(groundPosCheck.position, -transform.up, groundCheckDistance, locomotionController.groundLayerMask);
        int climbHit = Physics2D.CircleCastNonAlloc(climbPosCheck.position, 0.5f, antHead.right, climbResults, climbCheckDistance, locomotionController.groundLayerMask);

        if (climbHit > 0)
        {
            float newAngle = Vector2.Angle(climbResults[0].normal, transform.up);
            if (newAngle > minClimbAngle)
            {
                climbing = true;
                rb.MoveRotation(newAngle * rotationSpeed * Time.deltaTime);
                //rb.AddForce(-climbResults[0].normal * clingForce * rb.mass);
            }
            else
                climbing = false;
        }
    }

    #region Player Input Actions
    public void OnMovement(InputValue input)
    {
        Vector2 newDir = input.Get<Vector2>();
        SetDir(newDir);
    }

    public void OnJump()
    {
        if (grounded)
        {
            grounded = false;
            climbing = false;

            rb.AddForce(dir * jumpForce);
        }
    }

    public void OnLook(InputValue input)
    {
        lookPos = input.Get<Vector2>();

        SetLookDir();
    }

    public void OnDig()
    {
        Vector2 spotCheck = Camera.main.ScreenToWorldPoint(lookPos);
        //digDebug.transform.position = spotCheck;
        //digDebug.color = Color.red;

        if (Vector2.Distance(spotCheck, antHead.position) <= digDistance)
        {
            //digDebug.color = Color.blue;

            RaycastHit2D hitInfo = Physics2D.Raycast(spotCheck, Vector2.zero);

            if (hitInfo.collider != null)
            {
                GameManager.instance.environmentManager.DigChunk(TileType.None, hitInfo.point, digRadius);
            }
        }
    }

    public void OnLift()
    {
        Vector2 spotCheck = Camera.main.ScreenToWorldPoint(lookPos);

        if (Vector2.Distance(spotCheck, antHead.position) <= liftDistance)
        {
            //digDebug.color = Color.blue;

            RaycastHit2D hitInfo = Physics2D.Raycast(spotCheck, Vector2.zero);

            if (hitInfo.collider != null)
            {
                //Lift object?
                //Get weight, compare to strength
            }
        }
    }


    public void OnPause(CallbackContext context)
    {
        GameManager.instance.CallPause();
    }

    #endregion
    private void OnDrawGizmosSelected()
    {
        //Vector2 mousePos = Mouse.current.position.ReadValue();
        //Vector2 spotCheck = Camera.main.ScreenToWorldPoint(mousePos);

        //Gizmos.color = Color.red;
        //Gizmos.DrawLine(antHead.position, digDebug.transform.position);
        //Gizmos.DrawWireSphere(spotCheck, (digRadius + 0.5f) * 2 / 8 * 2f);

        //if (Vector2.Distance(spotCheck, antHead.position) <= digDistance)
        //{
        //    Gizmos.color = Color.blue;
        //}
    }
}
