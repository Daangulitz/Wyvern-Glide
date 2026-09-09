using UnityEngine;

public class DealDamage : MonoBehaviour
{
    private ZombieHealth zombie;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            zombie = collision.gameObject.GetComponent<ZombieHealth>();
            zombie.TakeDamage(zombie.currentHealth);
        }
    }
}
