using UnityEngine;
using UnityEngine.AI;

public enum ZombieState
{
    IDLE,
    WALKING,
    Death
}

public class StateMachine_Zombie : MonoBehaviour
{
    
    private NavMeshAgent _navMeshAgent;
    private Animator animator;
    private Transform _target;
    public ZombieState state;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        switch (state)
        {
            case ZombieState.IDLE:
                animator.SetBool("Walking", false);
                break;
            case ZombieState.WALKING:
                animator.SetBool("Walking", true);
                break;
            case ZombieState.Death:
                break;
        }
    }
}
