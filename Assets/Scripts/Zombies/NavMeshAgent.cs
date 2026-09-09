using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFollow : MonoBehaviour
{
    public float followRange = 10f;
    private Transform player;                    
    private NavMeshAgent agent;
    [SerializeField] private StateMachine_Zombie zombie;

    private void Awake()
    {
        zombie = GetComponent<StateMachine_Zombie>();
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure the player is tagged as 'Player'.");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= followRange)
        {
            agent.SetDestination(player.position);
            zombie.state = ZombieState.WALKING;
        }
        else
        {
            agent.ResetPath();
            zombie.state = ZombieState.IDLE;
        }
    }

    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, followRange);
    }
}