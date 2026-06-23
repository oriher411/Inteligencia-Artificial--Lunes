using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum SteeringEnemyArchetype {
    HUNTER,
    WATCHER,
    SPRINTER
}

public enum SteeringEnemyState {
    IDLE,
    PATROL,
    CHASE,
    ATTACK,
    RETURN,
    FLEE
}

public class SteeringEnemyController : MonoBehaviour {

    [Header("Enemy Type")]
    public SteeringEnemyArchetype enemyType;

    private CharacterAnimations enemy_Anim;
    private NavMeshAgent navAgent;
    private SteeringMovement steeringMovement;

    private Transform playerTarget;
    private CharacterController playerController;

    [Header("Movement Settings")]
    public float move_Speed = 3.5f;
    public float patrol_Speed = 2f;
    public float flee_Speed = 4.5f;

    [Header("Attack Settings")]
    public float attack_Distance = 1.5f;
    public float chase_Player_After_Attack_Distance = 1f;
    private float wait_Before_Attack_Time = 3f;
    private float attack_Timer;
    public GameObject attackPoint;

    [Header("Line of Sight & Detection")]
    public float sightRadius = 10f;
    [Range(0, 360)] public float fieldOfViewAngle = 110f;
    public LayerMask obstacleMask;

    [Header("Patrol Settings")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 2f;
    private float patrolTimer;
    private Vector3 patrolDestination;

    [Header("Watcher Settings")]
    public float guardMaxDistance = 15f;
    private Vector3 guardPosition;

    [Header("Sprinter Settings")]
    public float fleeDistance = 6f;

    [Header("Steering Settings")]
    public float waypointReachDistance = 1.2f;
    public float arriveSlowRadius = 2.5f;
    public float pathRecalculateTime = 0.6f;
    public float wanderRadius = 2f;
    public float wanderDistance = 4f;
    public float wanderJitter = 0.8f;
    private float wanderAngle;
    private Vector3 patrolOrigin;

    [Header("Visuals (Shorts)")]
    public Renderer shortsRenderer;
    public Color hunterColor = new Color(1f, 0.45f, 0f);
    public Color watcherColor = new Color(0.1f, 0.75f, 0.2f);
    public Color sprinterColor = new Color(0.6f, 0.2f, 0.85f);

    private SteeringEnemyState enemy_State;
    private CharacterSoundFX soundFX;

    private List<Vector3> currentPath;
    private int currentWaypointIndex;
    private float pathRefreshTimer;
    private Vector3 currentPathTarget;
    private bool hasReachedDestination;

    void Awake() {
        enemy_Anim = GetComponent<CharacterAnimations>();
        navAgent = GetComponent<NavMeshAgent>();
        steeringMovement = GetComponent<SteeringMovement>();

        if (steeringMovement == null) {
            steeringMovement = gameObject.AddComponent<SteeringMovement>();
        }

        if (navAgent != null) {
            navAgent.enabled = false;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(Tags.PLAYER_TAG);
        if (playerObj != null) {
            playerTarget = playerObj.transform;
            playerController = playerObj.GetComponent<CharacterController>();
        }

        soundFX = GetComponentInChildren<CharacterSoundFX>();
    }

    void Start() {
        attack_Timer = wait_Before_Attack_Time;

        shortsRenderer = EnemyVisualHelper.ResolveShortsRenderer(transform, shortsRenderer);

        if (shortsRenderer != null) {
            if (enemyType == SteeringEnemyArchetype.HUNTER) {
                EnemyVisualHelper.ApplyShortsColor(shortsRenderer, hunterColor);
            } else if (enemyType == SteeringEnemyArchetype.WATCHER) {
                EnemyVisualHelper.ApplyShortsColor(shortsRenderer, watcherColor);
            } else if (enemyType == SteeringEnemyArchetype.SPRINTER) {
                EnemyVisualHelper.ApplyShortsColor(shortsRenderer, sprinterColor);
            }
        }

        if (enemyType == SteeringEnemyArchetype.WATCHER) {
            guardPosition = transform.position;
            enemy_State = SteeringEnemyState.IDLE;
        } else if (enemyType == SteeringEnemyArchetype.SPRINTER) {
            enemy_State = SteeringEnemyState.IDLE;
        } else {
            patrolOrigin = transform.position;
            enemy_State = SteeringEnemyState.PATROL;
            SetRandomPatrolDestination();
        }
    }

    void Update() {
        switch (enemy_State) {
            case SteeringEnemyState.IDLE:
                Idle();
                break;
            case SteeringEnemyState.PATROL:
                Patrol();
                break;
            case SteeringEnemyState.CHASE:
                ChasePlayer();
                break;
            case SteeringEnemyState.ATTACK:
                AttackPlayer();
                break;
            case SteeringEnemyState.RETURN:
                ReturnToGuardPoint();
                break;
            case SteeringEnemyState.FLEE:
                FleeFromPlayer();
                break;
        }
    }

    bool HasLineOfSight() {
        return LineOfSightHelper.HasLineOfSight(
            transform,
            playerTarget,
            sightRadius,
            fieldOfViewAngle,
            obstacleMask);
    }

    Vector3 GetPlayerVelocity() {
        if (playerController != null) {
            return playerController.velocity;
        }

        return Vector3.zero;
    }

    void Idle() {
        steeringMovement.Stop();
        enemy_Anim.Walk(false);
        ClearPath();

        if (HasLineOfSight()) {
            if (enemyType == SteeringEnemyArchetype.WATCHER) {
                enemy_State = SteeringEnemyState.CHASE;
            } else if (enemyType == SteeringEnemyArchetype.SPRINTER) {
                float distance = Vector3.Distance(transform.position, playerTarget.position);
                if (distance <= fleeDistance) {
                    enemy_State = SteeringEnemyState.FLEE;
                }
            } else {
                enemy_State = SteeringEnemyState.CHASE;
            }
        }
    }

    void Patrol() {
        if (hasReachedDestination) {
            enemy_Anim.Walk(false);
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime) {
                SetRandomPatrolDestination();
                patrolTimer = 0f;
            }
        } else {
            RequestPath(patrolDestination, false);
            AdvanceWaypoint();

            Vector3 pathTarget = GetSteeringTargetFromPath(patrolDestination);
            Vector3 arriveForce = SteeringBehaviors.Arrive(
                transform.position,
                pathTarget,
                steeringMovement.Velocity,
                patrol_Speed,
                arriveSlowRadius);
            Vector3 seekForce = SteeringBehaviors.Seek(
                transform.position,
                pathTarget,
                steeringMovement.Velocity,
                patrol_Speed);

            steeringMovement.ApplySteering(arriveForce * 0.65f + seekForce * 0.35f, patrol_Speed);
            UpdateDestinationStatus(patrolDestination);
            enemy_Anim.Walk(steeringMovement.Velocity.sqrMagnitude > 0.05f);
        }

        if (HasLineOfSight()) {
            enemy_State = SteeringEnemyState.CHASE;
            patrolTimer = 0f;
            ClearPath();
        }
    }

    void SetRandomPatrolDestination() {
        hasReachedDestination = false;
        PatrolHelper.TryGetSteeringPatrolPoint(
            patrolOrigin,
            transform.position,
            patrolRadius,
            out patrolDestination);
        RequestPath(patrolDestination, true);
    }

    void ChasePlayer() {
        Vector3 playerPosition = playerTarget.position;

        RequestPath(playerPosition, false);
        AdvanceWaypoint();

        Vector3 pathSteering = SteeringBehaviors.Seek(
            transform.position,
            GetSteeringTargetFromPath(playerPosition),
            steeringMovement.Velocity,
            move_Speed);

        Vector3 pursueSteering = SteeringBehaviors.Pursue(
            transform.position,
            steeringMovement.Velocity,
            playerPosition,
            GetPlayerVelocity(),
            move_Speed,
            1.5f);

        steeringMovement.ApplySteering(pathSteering + pursueSteering * 0.75f, move_Speed);
        enemy_Anim.Walk(steeringMovement.Velocity.sqrMagnitude > 0.05f);

        if (enemyType == SteeringEnemyArchetype.WATCHER) {
            if (Vector3.Distance(guardPosition, transform.position) > guardMaxDistance) {
                enemy_State = SteeringEnemyState.RETURN;
                ClearPath();
                return;
            }
        }

        if (Vector3.Distance(transform.position, playerTarget.position) <= attack_Distance) {
            enemy_State = SteeringEnemyState.ATTACK;
            steeringMovement.Stop();
            ClearPath();
            return;
        }

        if (!HasLineOfSight() && Vector3.Distance(transform.position, playerTarget.position) > sightRadius) {
            if (enemyType == SteeringEnemyArchetype.WATCHER) {
                enemy_State = SteeringEnemyState.RETURN;
            } else {
                enemy_State = SteeringEnemyState.PATROL;
                SetRandomPatrolDestination();
            }

            ClearPath();
        }
    }

    void AttackPlayer() {
        steeringMovement.Stop();
        enemy_Anim.Walk(false);

        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        attack_Timer += Time.deltaTime;

        if (attack_Timer > wait_Before_Attack_Time) {
            if (Random.Range(0, 2) > 0) {
                enemy_Anim.Attack_1();
                if (soundFX != null) {
                    soundFX.Attack_1();
                }
            } else {
                enemy_Anim.Attack_2();
                if (soundFX != null) {
                    soundFX.Attack_2();
                }
            }

            attack_Timer = 0f;
        }

        if (Vector3.Distance(transform.position, playerTarget.position) > attack_Distance + chase_Player_After_Attack_Distance) {
            enemy_State = SteeringEnemyState.CHASE;
        }
    }

    void ReturnToGuardPoint() {
        RequestPath(guardPosition, false);
        AdvanceWaypoint();

        Vector3 steeringForce = SteeringBehaviors.Arrive(
            transform.position,
            GetSteeringTargetFromPath(guardPosition),
            steeringMovement.Velocity,
            move_Speed,
            arriveSlowRadius);

        steeringMovement.ApplySteering(steeringForce, move_Speed);
        UpdateDestinationStatus(guardPosition);
        enemy_Anim.Walk(steeringMovement.Velocity.sqrMagnitude > 0.05f);

        if (Vector3.Distance(transform.position, guardPosition) <= waypointReachDistance) {
            enemy_State = SteeringEnemyState.IDLE;
            steeringMovement.Stop();
            ClearPath();
        }
    }

    void FleeFromPlayer() {
        Vector3 fleeTarget;

        if (NavigationGrid.Instance != null) {
            fleeTarget = NavigationGrid.Instance.GetFleeWorldPosition(transform.position, playerTarget.position, 8f);
        } else {
            Vector3 fleeDirection = (transform.position - playerTarget.position).normalized;
            fleeTarget = transform.position + fleeDirection * 8f;
        }

        RequestPath(fleeTarget, true);
        AdvanceWaypoint();

        Vector3 pathSteering = SteeringBehaviors.Seek(
            transform.position,
            GetSteeringTargetFromPath(fleeTarget),
            steeringMovement.Velocity,
            flee_Speed);

        Vector3 fleeSteering = SteeringBehaviors.Flee(
            transform.position,
            playerTarget.position,
            steeringMovement.Velocity,
            flee_Speed);

        Vector3 evadeSteering = SteeringBehaviors.Evade(
            transform.position,
            steeringMovement.Velocity,
            playerTarget.position,
            GetPlayerVelocity(),
            flee_Speed,
            1.5f);

        steeringMovement.ApplySteering(pathSteering + fleeSteering * 0.5f + evadeSteering, flee_Speed);
        enemy_Anim.Walk(true);

        if (Vector3.Distance(transform.position, playerTarget.position) > fleeDistance + 2f) {
            enemy_State = SteeringEnemyState.IDLE;
            steeringMovement.Stop();
            ClearPath();
        }
    }

    void RequestPath(Vector3 target, bool forceRefresh) {
        if (NavigationGrid.Instance == null) {
            currentPath = new List<Vector3> { target };
            currentWaypointIndex = 0;
            currentPathTarget = target;
            return;
        }

        pathRefreshTimer -= Time.deltaTime;

        if (forceRefresh || currentPath == null || currentPath.Count == 0 ||
            pathRefreshTimer <= 0f || Vector3.Distance(currentPathTarget, target) > 1.5f) {
            currentPath = NavigationGrid.Instance.FindPath(transform.position, target);
            currentWaypointIndex = 0;
            currentPathTarget = target;
            pathRefreshTimer = pathRecalculateTime;
            hasReachedDestination = false;
        }
    }

    void UpdateDestinationStatus(Vector3 destination) {
        if (currentPath != null && currentPath.Count > 0 &&
            Vector3.Distance(transform.position, currentPath[currentPath.Count - 1]) <= waypointReachDistance) {
            hasReachedDestination = true;
        } else if (Vector3.Distance(transform.position, destination) <= waypointReachDistance) {
            hasReachedDestination = true;
        }
    }

    Vector3 GetSteeringTargetFromPath(Vector3 fallbackTarget) {
        if (currentPath == null || currentPath.Count == 0) {
            return fallbackTarget;
        }

        return currentPath[Mathf.Clamp(currentWaypointIndex, 0, currentPath.Count - 1)];
    }

    void AdvanceWaypoint() {
        if (currentPath == null || currentPath.Count == 0) {
            return;
        }

        while (currentWaypointIndex < currentPath.Count - 1 &&
               Vector3.Distance(transform.position, currentPath[currentWaypointIndex]) <= waypointReachDistance) {
            currentWaypointIndex++;
        }
    }

    void ClearPath() {
        currentPath = null;
        currentWaypointIndex = 0;
        pathRefreshTimer = 0f;
        hasReachedDestination = false;
    }

    void Activate_AttackPoint() {
        if (attackPoint != null) {
            attackPoint.SetActive(true);
        }
    }

    void Deactivate_AttackPoint() {
        if (attackPoint != null && attackPoint.activeInHierarchy) {
            attackPoint.SetActive(false);
        }
    }

}
