using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotionScript : MonoBehaviour
{
    [Header("Limb Settings")]
    public float targetAccuracy;
    public float legAngleModifier;
    public float resetTargetDistance;
    public LayerMask groundLayerMask;

    [Header("Limb References")]
    public AntBrain antController;
    public Transform bodyCenter;
    public Transform[] limbGroups;
    public LimbScript[] limbTargets;

    [Header("Movement Data")]
    public AntMovementData[] thisMovementData;

    private int limbCount;
    private Vector2[] newPosTargets;
    private RaycastHit2D[] results = new RaycastHit2D[2];

    private void Start()
    {
        limbCount = limbTargets.Length;
        newPosTargets = new Vector2[limbCount];
    }

    // Update is called once per frame
    void Update()
    {
        if (limbCount > 0)
        {
            for (int i = 0; i < limbCount; i++)
            {
                UpdateLimb(i);
            }
        }
    }

    private void UpdateLimb(int limbIndex)
    {
        // Calculating distance to IK target
        float dist = Vector2.Distance(limbTargets[limbIndex].transform.position, limbTargets[limbIndex].Center.position);

        if (dist > resetTargetDistance)
        {
            ResetTargetPos();
        }

        // Compare it with min and max limits
        if (dist >= thisMovementData[limbIndex].maxDistance || dist < thisMovementData[limbIndex].minDistance)
        {
            // If the target is out of range find new point and take a step
            newPosTargets[limbIndex] = GetNewTargetPosition(limbIndex);
            //limbTargets[limbIndex].transform.position = GetNewTargetPosition(limbIndex);

            if (!limbTargets[limbIndex].moving)
            {
                limbTargets[limbIndex].moving = true;
                StartCoroutine(MovetargetToNewPosition(limbIndex));
            }
        }
    }

    private IEnumerator MovetargetToNewPosition(int limbIndex)
    {
        float limbTimer = 0;
        while (true)
        {
            limbTimer += Time.deltaTime;
            if (limbTimer > 1)
            {
                limbTimer = 0;
            }

            //Rotate tip of limb to point towards target
            Vector3 rotDir = (newPosTargets[limbIndex] - (Vector2)limbTargets[limbIndex].LimbTip.position).normalized;
            Vector2 redDir = (rotDir - limbTargets[limbIndex].transform.right).normalized;
            float angle = Mathf.Atan2(redDir.y, redDir.x) * Mathf.Rad2Deg + legAngleModifier;
            limbTargets[limbIndex].transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            //Find direction to move target
            Vector3 dir = (newPosTargets[limbIndex] - (Vector2)limbTargets[limbIndex].transform.position).normalized;

            float vertOffset = thisMovementData[limbIndex].transitionVerticalOffset.Evaluate(limbTimer);
            if (vertOffset <= thisMovementData[limbIndex].maxVertOffset)
            {
                dir.y += vertOffset;
            }

            limbTargets[limbIndex].transform.position += thisMovementData[limbIndex].transitionSpeed * Time.deltaTime * dir;

            float currentDist = Vector2.Distance(limbTargets[limbIndex].transform.position, newPosTargets[limbIndex]);
            if (currentDist <= targetAccuracy)
            {
                break;
            }

            yield return new WaitForEndOfFrame();
        };

        limbTargets[limbIndex].moving = false;
    }

    private Vector2 GetNewTargetPosition(int limbIndex)
    {
        AntMovementData thisData = thisMovementData[limbIndex];

        // Assign default target position as new position
        Vector2 _newTargetPosition = limbTargets[limbIndex].Center.TransformPoint(thisData.targetDefaultPos);
        // Determine start and end point for LineCast
        var linecastStartPos = limbTargets[limbIndex].Center.up * thisData.RayDistance;
        // Rotate end point to specified angle around root
        int direction = -1;

        if (antController.dir.x < 0)
        {
            direction = 1;
        }

        var rot = Quaternion.AngleAxis(direction * thisData.AngularStep, transform.forward);
        var steps = Mathf.CeilToInt(180 / Mathf.Abs(thisData.AngularStep));

        // Looping through LineCasts until it finds any point
        for (int i = 0; i < steps; i++)
        {
            Vector3 linecastEndPos = rot * linecastStartPos;

            if (Physics2D.LinecastNonAlloc(
                limbTargets[limbIndex].Center.position + linecastStartPos,
                limbTargets[limbIndex].Center.position + linecastEndPos,
                results, groundLayerMask) > 0)
            {
                // Point found, exiting function
                _newTargetPosition = results[0].point;
                return _newTargetPosition;
            }
            linecastStartPos = linecastEndPos;
        }

        //Debug.LogWarning("Limb did not find target.");

        // Point not found, use default position
        return _newTargetPosition;
    }

    public void ResetTargetPos()
    {
        if (limbCount > 0)
        {
            for (int i = 0; i < limbCount; i++)
            {
                limbTargets[i].transform.position = limbTargets[i].Center.TransformPoint(thisMovementData[i].targetDefaultPos);
            }
        }
    }

    private void OnDrawGizmos()
    {
        limbCount = thisMovementData.Length;

        if (limbCount > 0)
        {
            for (int i = 0; i < limbCount; i++)
            {
                AntMovementData thisData = thisMovementData[i];
                
                // Assign default target position as new position
                Vector2 _newTargetPosition = bodyCenter.TransformPoint(thisData.targetDefaultPos);

                // Determine start and end point for LineCast
                var linecastStartPos = limbTargets[i].Center.up * thisData.RayDistance;

                int direction = -1;

                if (antController.dir.x < 0)
                {
                    direction = 1;
                }

                // Rotate end point to specified angle around root
                var rot = Quaternion.AngleAxis(direction * thisData.AngularStep, transform.forward);
                var steps = Mathf.CeilToInt(180 / Mathf.Abs(thisData.AngularStep));

                Gizmos.color = Color.blue;
                // Looping through LineCasts until it finds any point
                for (int j = 0; j < steps; j++)
                {
                    Vector3 linecastEndPos = rot * linecastStartPos;
                    Gizmos.DrawLine(bodyCenter.position + linecastStartPos, bodyCenter.position + linecastEndPos);

                    if (Physics2D.LinecastNonAlloc(limbTargets[i].Center.position + linecastStartPos, limbTargets[i].Center.position + linecastEndPos, results, groundLayerMask) > 0)
                    {
                        // Point found, exiting function
                        _newTargetPosition = results[0].point;

                        break;
                    }

                    linecastStartPos = linecastEndPos;
                }

                Gizmos.DrawWireSphere(_newTargetPosition, 0.05f);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(limbTargets[i].Center.position, thisMovementData[i].maxDistance);
                Gizmos.DrawWireSphere(limbTargets[i].Center.position, thisMovementData[i].minDistance);
            }
        }
    }

    private void OnBecameInvisible()
    {
        enabled = false;
    }

    private void OnBecameVisible()
    {
        enabled = true;
    }
}
