using UnityEngine;
using UnityEngine.AI;

public class SteeringMovement : MonoBehaviour {

    [SerializeField]
    private float maxForce = 18f;

    private Vector3 velocity;

    public Vector3 Velocity {
        get { return velocity; }
    }

    public void Stop() {
        velocity = Vector3.zero;
    }

    public void ApplySteering(Vector3 steeringForce, float maxSpeed) {
        steeringForce.y = 0f;
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

        velocity += steeringForce * Time.deltaTime;
        velocity.y = 0f;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        if (velocity.sqrMagnitude < 0.0001f) {
            return;
        }

        Vector3 nextPosition = transform.position + velocity * Time.deltaTime;
        NavMeshHit navHit;

        if (NavMesh.SamplePosition(nextPosition, out navHit, 1.5f, NavMesh.AllAreas)) {
            transform.position = navHit.position;
        } else {
            velocity *= 0.35f;
        }

        Vector3 lookDirection = velocity.normalized;
        if (lookDirection.sqrMagnitude > 0.001f) {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDirection),
                Time.deltaTime * 10f);
        }
    }

}
