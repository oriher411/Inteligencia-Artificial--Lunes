using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyArchetype {
    PATROLLER, // Enemigo 1: Patrulla agresivo
    GUARDIAN,  // Enemigo 2: Cuida una zona y no se aleja de ella
    COWARD     // Enemigo 3: Escapa si te acercas mucho
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

    [Header("Guardian Settings")]
    public float guardMaxDistance = 15f; 
    private Vector3 guardPosition;

    [Header("Coward Settings")]
    public float fleeDistance = 6f; // Distancia para huir

    [Header("Visuals (Shorts)")]
    public Renderer shortsRenderer;
    public Color patrollerColor = Color.red;
    public Color guardianColor = Color.blue;
    public Color cowardColor = Color.yellow;

    private EnemyState enemy_State;
    private CharacterSoundFX soundFX;

	void Awake () {
        enemy_Anim = GetComponent<CharacterAnimations>();
        navAgent = GetComponent<NavMeshAgent>();

        // Find the player
        GameObject playerObj = GameObject.FindGameObjectWithTag(Tags.PLAYER_TAG);
        if (playerObj != null) playerTarget = playerObj.transform;

        soundFX = GetComponentInChildren<CharacterSoundFX>();
    }

    void Start() {
        attack_Timer = wait_Before_Attack_Time;

        // Cambiar el color de los shorts si se asignó el renderer
        if (shortsRenderer != null) {
            if (enemyType == EnemyArchetype.PATROLLER) shortsRenderer.material.color = patrollerColor;
            else if (enemyType == EnemyArchetype.GUARDIAN) shortsRenderer.material.color = guardianColor;
            else if (enemyType == EnemyArchetype.COWARD) shortsRenderer.material.color = cowardColor;
        }

        // Initialize state based on enemy chosen type
        if (enemyType == EnemyArchetype.GUARDIAN) {
            guardPosition = transform.position; // Se ubica originalmente en su puesto
            enemy_State = EnemyState.IDLE;
        } else if (enemyType == EnemyArchetype.COWARD) {
            enemy_State = EnemyState.IDLE;
        } else { // PATROLLER
            enemy_State = EnemyState.PATROL;
            SetRandomPatrolDestination();
        }
    }

    void Update () {
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
        if (playerTarget == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= sightRadius) {
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            
            // Check angle
            if (Vector3.Angle(transform.forward, directionToPlayer) < fieldOfViewAngle / 2f) {
                // Check if obstacles block the view
                if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, obstacleMask)) {
                    return true;
                }
            }
        }
        return false;
    }

    void Idle() {
        navAgent.isStopped = true;
        enemy_Anim.Walk(false);

        if (HasLineOfSight()) {
            if (enemyType == EnemyArchetype.GUARDIAN) {
                enemy_State = EnemyState.CHASE;
            } else if (enemyType == EnemyArchetype.COWARD) {
                float dist = Vector3.Distance(transform.position, playerTarget.position);
                if(dist <= fleeDistance) {
                    enemy_State = EnemyState.FLEE;
                }
            } else {
                enemy_State = EnemyState.CHASE;
            }
        }
    }

    void Patrol() {
        navAgent.speed = patrol_Speed;

        if (navAgent.pathPending) return;

        // If arrived at patrol destination
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

        // Transition: If player is seen
        if (HasLineOfSight()) {
            enemy_State = EnemyState.CHASE;
            patrolTimer = 0f; // Reset for next patrol
        }
    }

    void SetRandomPatrolDestination() {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randomDirection, out navHit, patrolRadius, -1)) {
            patrolDestination = navHit.position;
            navAgent.SetDestination(patrolDestination);
            navAgent.isStopped = false;
        }
    }

    void ChasePlayer() {
        navAgent.isStopped = false;
        navAgent.speed = move_Speed;
        navAgent.SetDestination(playerTarget.position);

        if(navAgent.velocity.sqrMagnitude == 0) {
            enemy_Anim.Walk(false);
        } else {
            enemy_Anim.Walk(true);
        }

        // GUARDIAN Transition: Teather distance check
        if (enemyType == EnemyArchetype.GUARDIAN) {
            if (Vector3.Distance(guardPosition, transform.position) > guardMaxDistance) {
                enemy_State = EnemyState.RETURN;
                return;
            }
        }

        // Transition: If close enough, attack
        if(Vector3.Distance(transform.position, playerTarget.position) <= attack_Distance) {
            enemy_State = EnemyState.ATTACK;
            return;
        }

        // Transition: Losing Line of Sight
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

        // Turn to face player
        Vector3 direction = (playerTarget.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero) {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        attack_Timer += Time.deltaTime;

        if(attack_Timer > wait_Before_Attack_Time) {
            if(Random.Range(0, 2) > 0) {
                enemy_Anim.Attack_1();
                if(soundFX != null) soundFX.Attack_1();
            } else {
                enemy_Anim.Attack_2();
                if(soundFX != null) soundFX.Attack_2();
            }

            attack_Timer = 0f;
        }

        // Transition: switch back to Chase
        if(Vector3.Distance(transform.position, playerTarget.position) > attack_Distance + chase_Player_After_Attack_Distance) {
            navAgent.isStopped = false;
            enemy_State = EnemyState.CHASE;
        }
    }

    void ReturnToGuardPoint() {
        navAgent.isStopped = false;
        navAgent.speed = move_Speed;
        navAgent.SetDestination(guardPosition);
        
        if(navAgent.velocity.sqrMagnitude == 0) {
            enemy_Anim.Walk(false);
        } else {
            enemy_Anim.Walk(true);
        }

        // Arrived at guarding point
        if (navAgent.remainingDistance <= navAgent.stoppingDistance && !navAgent.pathPending) {
            enemy_State = EnemyState.IDLE;
        }
    }

    void FleeFromPlayer() {
        navAgent.isStopped = false;
        navAgent.speed = flee_Speed;
        enemy_Anim.Walk(true);

        // Calcula el vector desde el jugador hacia el enemigo
        Vector3 dirToPlayer = transform.position - playerTarget.position;
        
        // El destino será en la dirección contraria al jugador
        Vector3 newPos = transform.position + dirToPlayer.normalized * 5f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPos, out hit, 5f, NavMesh.AllAreas)) {
            navAgent.SetDestination(hit.position);
        }

        // Si ya te alejaste lo suficiente (y perdiste línea de visión) volvé a IDLE
        if (Vector3.Distance(transform.position, playerTarget.position) > fleeDistance + 2f) {
            enemy_State = EnemyState.IDLE;
        }
    }

    void Activate_AttackPoint() {
        if(attackPoint != null) {
            attackPoint.SetActive(true);
        }
    }

    void Deactivate_AttackPoint() {
        if(attackPoint != null && attackPoint.activeInHierarchy) {
            attackPoint.SetActive(false);
        }
    }

}









































































