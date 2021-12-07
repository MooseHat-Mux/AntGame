using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AntMovementData", menuName = "ScriptableObjects/AntMovementDataScritableObject", order = 1)]
public class AntMovementData : ScriptableObject
{
    [Header("Distance Thresholds")]
    public float minDistance;
    public float maxDistance;
    public Vector2 targetDefaultPos;

    [Header("New Position Settings")]
    public float RayDistance;
    public int AngularStep;

    [Header("Transition Settings")]
    public float centerPosXOffset;
    public float targetPosYOffset;
    public float transitionSpeed;
    public float maxVertOffset;
    public AnimationCurve transitionTween;
    public AnimationCurve transitionVerticalOffset;
}
