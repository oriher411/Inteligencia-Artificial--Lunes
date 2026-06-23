using UnityEngine;

public static class LineOfSightHelper {

    const float EyeHeight = 1.4f;
    const float SightSphereRadius = 0.35f;

    public static bool HasLineOfSight(
        Transform observer,
        Transform target,
        float sightRadius,
        float fieldOfViewAngle,
        LayerMask obstacleMask) {

        if (observer == null || target == null) {
            return false;
        }

        Vector3 origin = observer.position + Vector3.up * EyeHeight;
        Vector3 targetPoint = target.position + Vector3.up * EyeHeight;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        if (distance > sightRadius) {
            return false;
        }

        if (distance < 0.05f) {
            return true;
        }

        Vector3 flatDirection = target.position - observer.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.01f) {
            flatDirection.Normalize();

            if (Vector3.Angle(observer.forward, flatDirection) >= fieldOfViewAngle * 0.5f) {
                return false;
            }
        }

        Vector3 direction = toTarget / distance;

        return !Physics.SphereCast(
            origin,
            SightSphereRadius,
            direction,
            out RaycastHit hit,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }
}
