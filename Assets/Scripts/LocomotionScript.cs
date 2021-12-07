using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotionScript : MonoBehaviour
{
    [Header("Limb Settings")]
    public float targetAccuracy;
    public float resetTargetDistance;
    public float arcDetectionLength;
    public LayerMask groundLayerMask;

    [Header("Limb References")]
    public AntBrain antController;
    public Transform bodyCenter;
    public LimbGroup[] limbGroups;
    public LimbScript[] limbTargets;

    private int limbCount;
    private Vector2[] newPosTargets = new Vector2[6];
    private RaycastHit2D[] results = new RaycastHit2D[2];
    private int currentLimbGroup;

    private void Start()
    {
        limbCount = limbTargets.Length;
        newPosTargets = new Vector2[limbCount];
    }

    void FixedUpdate()
    {
        if (antController.dir.magnitude > 0)
        {
            if (limbCount > 0)
            {
                for (int i = 0; i < limbCount; i++)
                {
                    UpdateLimb(i);
                }
            }
        }
    }

    private void UpdateLimb(int limbIndex)
    {
        int direction = -1;

        if (antController.dir.x < 0)
        {
            direction = 1;
        }

        AntMovementData thisMovementData = limbGroups[currentLimbGroup].movementData;

        Vector3 centerPos = limbTargets[limbIndex].Center.position + new Vector3(thisMovementData.centerPosXOffset * direction, 0);

        // Calculating distance to IK target
        float dist = Vector2.Distance(limbTargets[limbIndex].transform.position, centerPos);

        if (dist > resetTargetDistance)
        {
            limbTargets[limbIndex].moving = false;
            ResetTargetPos();
        }

        // Compare it with min and max limits
        if (dist >= thisMovementData.maxDistance || dist < thisMovementData.minDistance)
        {
            // If the target is out of range find new point and take a step
            newPosTargets[limbIndex] = GetNewTargetPosition(limbIndex);
            //limbTargets[limbIndex].transform.position = GetNewTargetPosition(limbIndex);

            int moveFinished = 1;
            int targetCount = limbGroups[currentLimbGroup].limbTargets.Length;
            for (int i = 0; i < targetCount; i++)
            {
                if (!limbTargets[limbIndex].moving)
                {
                    limbTargets[limbIndex].moving = true;
                    StartCoroutine(MovetargetToNewPosition(limbIndex));

                    moveFinished++;
                }
            }

            if (moveFinished >= targetCount)
            {
                currentLimbGroup++;

                if (currentLimbGroup >= limbGroups.Length)
                {
                    currentLimbGroup = 0;
                }
            }
        }
    }

    private Vector2 GetNewTargetPosition(int limbIndex)
    {
        AntMovementData thisData = limbGroups[currentLimbGroup].movementData;

        int direction = -1;

        if (antController.dir.x < 0)
        {
            direction = 1;
        }

        Vector3 center = limbTargets[limbIndex].Center.up;
        center.x += thisData.centerPosXOffset * direction;

        // Assign default target position as new position
        Vector2 _newTargetPosition = limbTargets[limbIndex].Center.TransformPoint(thisData.targetDefaultPos);
        // Determine start and end point for LineCast
        var linecastStartPos = center * thisData.RayDistance;
        // Rotate end point to specified angle around root


        var rot = Quaternion.AngleAxis(direction * thisData.AngularStep, transform.forward);
        var steps = Mathf.CeilToInt(arcDetectionLength / Mathf.Abs(thisData.AngularStep));


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
                if (results[0].point.y > results[1].point.y)
                {
                    _newTargetPosition = results[0].point;
                }
                else
                {
                    _newTargetPosition = results[1].point;
                }

                _newTargetPosition.y += thisData.targetPosYOffset;
                return _newTargetPosition;
            }

            linecastStartPos = linecastEndPos;
        }

        //Debug.LogWarning("Limb did not find target.");

        // Point not found, use default position
        return _newTargetPosition;
    }

    private IEnumerator MovetargetToNewPosition(int limbIndex)
    {
        AntMovementData thisData = limbGroups[currentLimbGroup].movementData;
        float totalDist = Vector2.Distance(newPosTargets[limbIndex], limbTargets[limbIndex].transform.position);
        float time = totalDist / thisData.transitionSpeed;
        float limbTimer = 0;
        while (limbTargets[limbIndex].moving)
        {

            //Find direction to move target
            Vector3 dir = (newPosTargets[limbIndex] - (Vector2)limbTargets[limbIndex].transform.position).normalized;

            limbTimer += Time.deltaTime * time;

            float vertOffset = thisData.transitionVerticalOffset.Evaluate(limbTimer);
            limbTimer += limbTimer;

            if (vertOffset < thisData.maxVertOffset)
            {
                dir.y += vertOffset;
            }

            limbTargets[limbIndex].transform.position += thisData.transitionSpeed * Time.deltaTime * dir;

            float currentDist = Vector2.Distance(limbTargets[limbIndex].transform.position, newPosTargets[limbIndex]);
            if (currentDist <= targetAccuracy)
            {
                limbTargets[limbIndex].moving = false;
            }

            yield return new WaitForEndOfFrame();
        };

    }

    public void ResetTargetPos()
    {
        int limbGroupCount = limbGroups.Length;

        if (limbGroupCount > 0)
        {
            for (int i = 0; i < limbGroupCount; i++)
            {
                int thisLimbCount = limbGroups[i].limbTargets.Length;

                if (thisLimbCount > 0)
                {
                    for (int j = 0; j < limbCount; j++)
                    {
                        limbTargets[j].transform.position = limbTargets[j].Center.TransformPoint(limbGroups[i].movementData.targetDefaultPos);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        int limbGroupCount = limbGroups.Length;

        if (limbGroupCount > 0)
        {
            for (int i = 0; i < limbGroupCount; i++)
            {
                int thisLimbCount = limbGroups[i].limbTargets.Length;
                for (int j = 0; j < thisLimbCount; j++)
                {
                    AntMovementData thisData = limbGroups[i].movementData;

                    int direction = -1;

                    if (antController.dir.x < 0)
                    {
                        direction = 1;
                    }

                    int limbIndex = limbGroups[i].limbTargets[j];
                    Vector3 center = limbTargets[limbIndex].Center.up;
                    center.x += thisData.centerPosXOffset * direction;

                    // Determine start and end point for LineCast
                    var linecastStartPos = center * thisData.RayDistance;

                    // Rotate end point to specified angle around root
                    var rot = Quaternion.AngleAxis(direction * thisData.AngularStep, transform.forward);
                    var steps = Mathf.CeilToInt(arcDetectionLength / Mathf.Abs(thisData.AngularStep));

                    Gizmos.color = Color.blue;
                    // Looping through LineCasts until it finds any point
                    for (int k = 0; k < steps; k++)
                    {
                        Vector3 linecastEndPos = rot * linecastStartPos;
                        Gizmos.DrawLine(limbTargets[limbIndex].Center.position + linecastStartPos, 
                            limbTargets[limbIndex].Center.position + linecastEndPos);

                        linecastStartPos = linecastEndPos;
                    }

                    if (newPosTargets.Length > 0)
                    {
                        Gizmos.DrawWireSphere(newPosTargets[limbIndex], 0.05f);
                    }

                    Gizmos.color = Color.red;
                    Vector3 centerPos = limbTargets[limbIndex].Center.position + new Vector3(thisData.centerPosXOffset * direction, 0);
                    Gizmos.DrawWireSphere(centerPos, thisData.maxDistance);
                    Gizmos.DrawWireSphere(centerPos, thisData.minDistance);
                }
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

    private void OnDestroy()
    {
        if (limbCount > 0)
        {
            for (int i = 0; i < limbCount; i++)
            {
                if (limbTargets[i])
                {
                    Destroy(limbTargets[i].gameObject);
                }
            }
        }
    }
}
