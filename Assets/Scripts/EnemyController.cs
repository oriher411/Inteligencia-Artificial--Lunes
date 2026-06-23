using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyArchetype {
    PATROLLER,
    GUARDIAN,
    COWARD
}

public enum EnemyState {
    IDLE,
    PATROL,
    CHASE,
    ATTACK,
    RETURN,
    FLEE
}

public class EnemyController : MonoBehaviour {

    [Header("Enemy Type")]
    public EnemyArchetype enemyType;

    private CharacterAnimations enemy_Anim;
    private NavMeshAgent navAgent;

    private Transform playerTarget;

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
    public LayerMask playerMask;

    [Header("Patrol Settings")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 2f;
    private float patrolTimer;
    private Vector3 patrolDestination;
    private Vector3 patrolOrigin;
    private NavMeshPath patrolPathBuffer;

    [Header("Guardian Settings")]
    public float guardMaxDistance = 15f;
    private Vector3 guardPosition;

    [Header("Coward Settings")]
    public float fleeDistance = 6f;

    [Header("Visuals (Shorts)")]
    public Renderer shortsRenderer;
    public Color patrollerColor = Color.red;
    public Color guardianColor = Color.blue;
    public Color cowardColor = Color.yellow;

    private EnemyState enemy_State;
    private CharacterSoundFX soundFX;

    void Awake() {
        enemy_Anim = GetComponent<CharacterAnimations>();
        navAgent = GetComponent<NavMeshAgent>();
        patrolPathBuffer = new NavMeshPath();

        if (navAgent != null) {
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            navAgent.autoBraking = true;
            navAgent.stoppingDistance = 1.1f;
            navAgent.angularSpeed = 240f;
            navAgent.acceleration = 12f;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(Tags.PLAYER_TAG);
        if (playerObj != null) {
            playerTarget = playerObj.transform;
        }

        soundFX = GetComponentInChildren<CharacterSoundFX>();
    }

    void Start() {
        attack_Timer = wait_Before_Attack_Time;

        shortsRenderer = EnemyVisualHelper.ResolveShortsRenderer(transform, shortsRenderer);

        if (shortsRenderer != null) {
            if (enemyType == EnemyArchetype.PATROLLER) {
                EnemyVisualHelper.ApplyShortsColor(shortsRenderer, patrollerColor);
            } else if (enemyType == EnemyArchetype.GUARDIAN) {
                EnemyVisualHelper.ApplyShortsColor(shortsRenderer, guardianColor);
            } else if (enemyType == EnemyArchetype.COWARD) {
                EnemyVisualHelper.ApplyShortsColor(shortsRenderer, cowardColor);
            }
        }

        if (enemyType == EnemyArchetype.GUARDIAN) {
            guardPosition = transform.position;
            enemy_State = EnemyState.IDLE;
        } else if (enemyType == EnemyArchetype.COWARD) {
            enemy_State = EnemyState.IDLE;
        } else {
            patrolOrigin = transform.position;
            enemy_State = EnemyState.PATROL;
            SetRandomPatrolDestination();
        }
    }

    void Update() {
        switch (enemy_State) {
            case EnemyState.IDLE:
                Idle();
                break;
            case EnemyState.PATROL:
                Patrol();
                break;
            case EnemyState.CHASE:
                ChasePlayer();
                break;
            case EnemyState.ATTACK:
                AttackPlayer();
                break;
            case EnemyState.RETURN:
                ReturnToGuardPoint();
                break;
            case EnemyState.FLEE:
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

    void Idle() {
        navAgent.isStopped = true;
        enemy_Anim.Walk(false);

        if (HasLineOfSight()) {
            if (enemyType == EnemyArchetype.GUARDIAN) {
                enemy_State = EnemyState.CHASE;
            } else if (enemyType == EnemyArchetype.COWARD) {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                if (dist <= fleeDistance) {
                    enemy_State = EnemyState.FLEE;
                }
            } else {
                enemy_State = EnemyState.CHASE;
            }
        }
    }

    void Patrol() {
        navAgent.speed = patrol_Speed;

        if (navAgent.pathPending) {
            return;
        }

        if (navAgent.remainingDistance <= navAgent.stoppingDistance) {
            enemy_Anim.Walk(false);
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime) {
                SetRandomPatrolDestination();
                patrolTimer = 0f;
            }
        } else {
            enemy_Anim.Walk(true);
        }

        if (HasLineOfSight()) {
            enemy_State = EnemyState.CHASE;
            patrolTimer = 0f;
        }
    }

    void SetRandomPatrolDestination() {
        Vector3 nextDestination;

        if (PatrolHelper.TryGetNavMeshPatrolPoint(
            patrolOrigin,
            transform.position,
            patrolRadius,
            patrolPathBuffer,
            out nextDestination)) {
            patrolDestination = nextDestination;
            navAgent.isStopped = false;
            navAgent.SetDestination(patrolDestination);
        }
    }

    void ChasePlayer() {
        navAgent.isStopped = false;
        navAgent.speed = move_Speed;
        navAgent.SetDestination(playerTarget.position);

        if (navAgent.velocity.sqrMagnitude == 0) {
            enemy_Anim.Walk(false);
        } else {
            enemy_Anim.Walk(true);
        }

        if (enemyType == EnemyArchetype.GUARDIAN) {
            if (Vector3.Distance(guardPosition, transform.position) > guardMaxDistance) {
                enemy_State = EnemyState.RETURN;
                return;
            }
        }

        if (Vector3.Distance(transform.position, playerTarget.position) <= attack_Distance) {
            enemy_State = EnemyState.ATTACK;
            return;
        }

        if (!HasLineOfSight() && Vector3.Distance(transform.position, playerTarget.position) > sightRadius) {
            if (enemyType == EnemyArchetype.GUARDIAN) {
                enemy_State = EnemyState.RETURN;
            } else {
                enemy_State = EnemyState.PATROL;
                SetRandomPatrolDestination();
            }
        }
    }

    void AttackPlayer() {
        navAgent.velocity = Vector3.zero;
        navAgent.isStopped = true;

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
            navAgent.isStopped = false;
            enemy_State = EnemyState.CHASE;
        }
    }

    void ReturnToGuardPoint() {
        navAgent.isStopped = false;
        navAgent.speed = move_Speed;
        navAgent.SetDestination(guardPosition);

        if (navAgent.velocity.sqrMagnitude == 0) {
            enemy_Anim.Walk(false);
        } else {
            enemy_Anim.Walk(true);
        }

        if (navAgent.remainingDistance <= navAgent.stoppingDistance && !navAgent.pathPending) {
            enemy_State = EnemyState.IDLE;
        }
    }

    void FleeFromPlayer() {
        navAgent.isStopped = false;
        navAgent.speed = flee_Speed;
        enemy_Anim.Walk(true);

        Vector3 dirToPlayer = transform.position - playerTarget.position;
        Vector3 newPos = transform.position + dirToPlayer.normalized * 5f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPos, out hit, 5f, NavMesh.AllAreas)) {
            navAgent.SetDestination(hit.position);
        }

        if (Vector3.Distance(transform.position, playerTarget.position) > fleeDistance + 2f) {
            enemy_State = EnemyState.IDLE;
        }
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
