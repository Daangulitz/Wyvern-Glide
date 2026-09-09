using UnityEngine;

public class SpawnZone : MonoBehaviour
{
    [SerializeField] private GameObject zombie;
    [SerializeField] private int numberToSpawn = 10;
    
    private Bounds spawnbounds;

    void Start()
    {
        spawnbounds = GetComponent<Collider>().bounds;

        for (int i = 0; i < numberToSpawn; i++)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(spawnbounds.min.x, spawnbounds.max.x),
            spawnbounds.center.y,
            Random.Range(spawnbounds.min.z, spawnbounds.max.z)
            );
        
        Instantiate(zombie, randomPosition, Quaternion.identity);
    }
}
