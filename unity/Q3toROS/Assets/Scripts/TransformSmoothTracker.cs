using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tracks a list of object pairs, smoothly updating each target to follow its corresponding tracked object.
/// </summary>
public class TransformSmoothTracker : MonoBehaviour
{
    [System.Serializable]
    public class TrackPair
    {
        [Tooltip("The object whose transform will be tracked.")]
        public Transform trackedObject;
        [Tooltip("The object that will follow the tracked object's transform.")]
        public Transform targetObject;
        [Tooltip("Smooth time for position. Smaller values = snappier movement.")]
        public float positionSmoothTime = 0.1f;
        [Tooltip("Smooth time for rotation. Smaller values = snappier rotation.")]
        public float rotationSmoothTime = 0.1f;

        // Internal velocity reference for SmoothDamp
        [HideInInspector]
        public Vector3 positionVelocity;
    }

    [Tooltip("List of tracked/target pairs with individual smoothing settings.")]
    public List<TrackPair> trackPairs = new List<TrackPair>();

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        foreach (var pair in trackPairs)
        {
            if (pair.trackedObject == null || pair.targetObject == null)
                continue;

            // Smoothly update position
            Vector3 currentPos = pair.targetObject.position;
            Vector3 desiredPos = pair.trackedObject.position;
            pair.targetObject.position = Vector3.SmoothDamp(
                currentPos,
                desiredPos,
                ref pair.positionVelocity,
                pair.positionSmoothTime,
                Mathf.Infinity,
                dt
            );

            // Smoothly update rotation via Slerp
            Quaternion currentRot = pair.targetObject.rotation;
            Quaternion desiredRot = pair.trackedObject.rotation;
            float t = dt / Mathf.Max(pair.rotationSmoothTime, 0.0001f);
            pair.targetObject.rotation = Quaternion.Slerp(currentRot, desiredRot, t);
        }
    }
}
