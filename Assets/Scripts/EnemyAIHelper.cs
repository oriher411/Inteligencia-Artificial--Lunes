using UnityEngine;
using UnityEngine.AI;

public static class EnemyAIHelper {

    const string SteeringEnemyControllerName = "SteeringEnemyController";
    const string SteeringMovementName = "SteeringMovement";

    public static void DisableAIOnGameObject(GameObject target) {
        if (target == null) {
            return;
        }

        EnemyController enemyController = target.GetComponent<EnemyController>();
        if (enemyController != null) {
            enemyController.enabled = false;
        }

        DisableSteeringEnemyComponents(target);

        NavMeshAgent navMeshAgent = target.GetComponent<NavMeshAgent>();
        if (navMeshAgent != null) {
            navMeshAgent.enabled = false;
        }
    }

    public static void DisableAllEnemyAI() {
        EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (EnemyController enemy in enemies) {
            enemy.enabled = false;
        }

        GameObject[] steeringEnemies = GameObject.FindGameObjectsWithTag(Tags.STEERING_ENEMY_TAG);
        foreach (GameObject steeringEnemy in steeringEnemies) {
            DisableSteeringEnemyComponents(steeringEnemy);
        }
    }

    static void DisableSteeringEnemyComponents(GameObject target) {
        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours) {
            if (behaviour == null) {
                continue;
            }

            if (behaviour.GetType().Name == SteeringEnemyControllerName ||
                behaviour.GetType().Name == SteeringMovementName) {
                behaviour.enabled = false;
            }
        }
    }

}
