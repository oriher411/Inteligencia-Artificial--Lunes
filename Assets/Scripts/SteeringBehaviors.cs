using UnityEngine;

public static class SteeringBehaviors {

    public static Vector3 Seek(Vector3 position, Vector3 target, Vector3 currentVelocity, float maxSpeed) {
        Vector3 desiredVelocity = (target - position);
        desiredVelocity.y = 0f;

        if (desiredVelocity.sqrMagnitude > 0.001f) {
            desiredVelocity = desiredVelocity.normalized * maxSpeed;
        }

        return desiredVelocity - currentVelocity;
    }

    public static Vector3 Flee(Vector3 position, Vector3 threat, Vector3 currentVelocity, float maxSpeed) {
        Vector3 desiredVelocity = (position - threat);
        desiredVelocity.y = 0f;

        if (desiredVelocity.sqrMagnitude > 0.001f) {
            desiredVelocity = desiredVelocity.normalized * maxSpeed;
        }

        return desiredVelocity - currentVelocity;
    }

    public static Vector3 Arrive(Vector3 position, Vector3 target, Vector3 currentVelocity, float maxSpeed, float slowingRadius) {
        Vector3 toTarget = target - position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance < 0.05f) {
            return -currentVelocity;
        }

        float desiredSpeed = maxSpeed;

        if (distance < slowingRadius) {
            desiredSpeed = maxSpeed * (distance / slowingRadius);
        }

        Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
        return desiredVelocity - currentVelocity;
    }

    public static Vector3 Pursue(Vector3 position, Vector3 currentVelocity, Vector3 targetPosition, Vector3 targetVelocity, float maxSpeed, float maxPredictionTime) {
        Vector3 toTarget = targetPosition - position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        float speed = currentVelocity.magnitude;
        float predictionTime = speed > 0.01f ? distance / speed : 0f;
        predictionTime = Mathf.Clamp(predictionTime, 0f, maxPredictionTime);

        Vector3 predictedTarget = targetPosition + targetVelocity * predictionTime;
        return Seek(position, predictedTarget, currentVelocity, maxSpeed);
    }

    public static Vector3 Evade(Vector3 position, Vector3 currentVelocity, Vector3 threatPosition, Vector3 threatVelocity, float maxSpeed, float maxPredictionTime) {
        Vector3 toThreat = threatPosition - position;
        toThreat.y = 0f;

        float distance = toThreat.magnitude;
        float speed = currentVelocity.magnitude;
        float predictionTime = speed > 0.01f ? distance / speed : 0f;
        predictionTime = Mathf.Clamp(predictionTime, 0f, maxPredictionTime);

        Vector3 predictedThreat = threatPosition + threatVelocity * predictionTime;
        return Flee(position, predictedThreat, currentVelocity, maxSpeed);
    }

    public static Vector3 Wander(ref float wanderAngle, Vector3 position, Vector3 forward, float wanderRadius, float wanderDistance, float wanderJitter) {
        wanderAngle += Random.Range(-wanderJitter, wanderJitter);

        Vector3 circleCenter = position + forward.normalized * wanderDistance;
        Vector3 offset = new Vector3(Mathf.Cos(wanderAngle), 0f, Mathf.Sin(wanderAngle)) * wanderRadius;
        Vector3 target = circleCenter + offset;

        return target - position;
    }

}
