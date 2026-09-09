using UnityEngine;

public class RagdollActivator : MonoBehaviour
{
    public Animator animator;
    public Rigidbody[] ragdollBodies;

    void Start()
    {
        SetRagdollState(false); // disable ragdoll at start
    }

    public void ActivateRagdoll()
    {
        animator.enabled = false;
        SetRagdollState(true);
    }

    private void SetRagdollState(bool state)
    {
        foreach (Rigidbody rb in ragdollBodies)
        {
            rb.isKinematic = !state;
            if (rb.TryGetComponent<Collider>(out var col))
                col.enabled = state;
        }
    }
}
