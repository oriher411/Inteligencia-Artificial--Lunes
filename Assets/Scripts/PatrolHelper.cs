using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class PatrolHelper {

    const int MaxAttempts = 16;
    const float MinPatrolDistance = 3.5f;

    public static bool TryGetNavMeshPatrolPoint(
        Vector3 patrolOrigin,
        Vector3 fromPosition,
        float patrolRadius,
        NavMeshPath pathBuffer,
        out Vector3 destination) {

        destination = fromPosition;

        if (pathBuffer == null) {
            return false;
        }

        for (int attempt = 0; attempt < MaxAttempts; attempt++) {
            Vector3 candidate = SamplePointAroundOrigin(patrolOrigin, patrolRadius, MinPatrolDistance);

            NavMeshHit navHit;
            if (!NavMesh.SamplePosition(candidate, out navHit, patrolRadius * 0.35f, NavMesh.AllAreas)) {
                continue;
            }

            if (Vector3.Distance(fromPosition, navHit.position) < MinPatrolDistance) {
                continue;
            }

            pathBuffer.ClearCorners();
            if (!NavMesh.CalculatePath(fromPosition, navHit.position, NavMesh.AllAreas, pathBuffer)) {
                continue;
            }

            if (pathBuffer.status != NavMeshPathStatus.PathComplete) {
                continue;
            }

            if (!IsReasonablePath(pathBuffer, fromPosition, navHit.position)) {
                continue;
            }

            destination = navHit.position;
            return true;
        }

        return false;
    }

    public static bool TryGetSteeringPatrolPoint(
        Vector3 patrolOrigin,
        Vector3 fromPosition,
        float patrolRadius,
        out Vector3 destination) {

        destination = fromPosition;

        if (NavigationGrid.Instance == null) {
            destination = patrolOrigin + Random.insideUnitSphere * patrolRadius * 0.5f;
            destination.y = fromPosition.y;
            return true;
        }

        for (int attempt = 0; attempt < MaxAttempts; attempt++) {
            Vector3 candidate = SamplePointAroundOrigin(patrolOrigin, patrolRadius, MinPatrolDistance);
            Vector3 walkableTarget = NavigationGrid.Instance.GetRandomWalkableWorldPosition(
                candidate,
                patrolRadius * 0.25f);

            if (Vector3.Distance(fromPosition, walkableTarget) < MinPatrolDistance) {
                continue;
            }

            if (Vector3.Distance(patrolOrigin, walkableTarget) > patrolRadius + 1f) {
                continue;
            }

            List<Vector3> path = NavigationGrid.Instance.FindPath(fromPosition, walkableTarget);
            if (path == null || path.Count == 0) {
                continue;
            }

            if (!IsReasonableSteeringPath(path, fromPosition, walkableTarget)) {
                continue;
            }

            destination = walkableTarget;
            return true;
        }

        destination = NavigationGrid.Instance.GetRandomWalkableWorldPosition(patrolOrigin, patrolRadius * 0.35f);
        return true;
    }

    static Vector3 SamplePointAroundOrigin(Vector3 origin, float patrolRadius, float minDistance) {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minDistance, patrolRadius);
        return origin + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
    }

    static bool IsReasonablePath(NavMeshPath path, Vector3 fromPosition, Vector3 targetPosition) {
        if (path.corners == null || path.corners.Length < 2) {
            return false;
        }

        float pathLength = GetNavMeshPathLength(path);
        float directDistance = Vector3.Distance(fromPosition, targetPosition);

        return pathLength <= directDistance * 2.75f + 2f;
    }

    static bool IsReasonableSteeringPath(List<Vector3> path, Vector3 fromPosition, Vector3 targetPosition) {
        if (path.Count < 2) {
            return false;
        }

        float pathLength = 0f;
        Vector3 previous = fromPosition;

        for (int i = 0; i < path.Count; i++) {
            pathLength += Vector3.Distance(previous, path[i]);
            previous = path[i];
        }

        float directDistance = Vector3.Distance(fromPosition, targetPosition);
        return pathLength <= directDistance * 2.75f + 2f;
    }

    static float GetNavMeshPathLength(NavMeshPath path) {
        float length = 0f;

        for (int i = 1; i < path.corners.Length; i++) {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return length;
    }
}
