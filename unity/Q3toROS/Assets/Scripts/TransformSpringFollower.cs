using System.Collections.Generic;
using UnityEngine;

public class TransformSpringFollower : MonoBehaviour
{
    [System.Serializable]
    public class Pair
    {
        [Tooltip("The Transform to follow")]
        public Transform source;
        [Tooltip("The Transform that will be driven to follow")]
        public Transform target;

        // runtime state for the spring integrator
        [HideInInspector] public Vector3 velocity = Vector3.zero;
        [HideInInspector] public Vector3 angularVelocity = Vector3.zero;
    }

    [Header("Pairs of source → target Transforms")]
    public List<Pair> pairs = new List<Pair>();

    [Header("Position spring-damper gains")]
    [Tooltip("Stiffness (spring constant) for position")]
    public float springPosition = 50f;
    [Tooltip("Damping coefficient for position")]
    public float dampingPosition = 10f;

    [Header("Rotation spring-damper gains")]
    [Tooltip("Stiffness (spring constant) for rotation")]
    public float springRotation = 50f;
    [Tooltip("Damping coefficient for rotation")]
    public float dampingRotation = 10f;

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        foreach (var p in pairs)
        {
            if (p.source == null || p.target == null) continue;

            //——— POSITION SPRING ——–
            Vector3 posError = p.source.position - p.target.position;
            // acceleration = k * error – c * v
            Vector3 posAcc = springPosition * posError - dampingPosition * p.velocity;
            p.velocity += posAcc * dt;
            p.target.position += p.velocity * dt;

            //——— ROTATION SPRING ——–
            // find the quaternion that rotates target → source
            Quaternion qErr = p.source.rotation * Quaternion.Inverse(p.target.rotation);
            // ensure shortest path
            if (qErr.w < 0f)
                qErr = new Quaternion(-qErr.x, -qErr.y, -qErr.z, -qErr.w);

            // convert to axis-angle (angle in degrees)
            qErr.ToAngleAxis(out float angleDeg, out Vector3 axis);
            if (axis.sqrMagnitude < 1e-6f || Mathf.Abs(angleDeg) < 0.01f)
                continue;  // no meaningful rotation error

            // express error as a vector (radians)
            Vector3 rotError = axis.normalized * Mathf.Deg2Rad * angleDeg;
            // angular acceleration = k * error – c * omega
            Vector3 angAcc = springRotation * rotError - dampingRotation * p.angularVelocity;
            p.angularVelocity += angAcc * dt;

            // integrate angular velocity
            Vector3 deltaAng = p.angularVelocity * dt;
            float deltaAngDeg = deltaAng.magnitude * Mathf.Rad2Deg;
            if (deltaAngDeg > 0f)
            {
                Quaternion dq = Quaternion.AngleAxis(deltaAngDeg, deltaAng.normalized);
                p.target.rotation = dq * p.target.rotation;
            }
        }
    }
}
