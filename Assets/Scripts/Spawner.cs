using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject obstaclePrefab;
    public GameObject enemyPrefab;
    public GameObject healthPackPrefab;
    
    [Header("Spawn Settings")]
    public float baseObstacleSpawnRate = 3f; 
    public float baseItemSpawnRate = 2f; 
    
    private float nextObstacleTime;
    private float nextItemTime;

    void Start()
    {
        nextObstacleTime = Time.time + baseObstacleSpawnRate;
        nextItemTime = Time.time + baseItemSpawnRate;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver) 
        {
            // Keep pushing the spawn time forward while game holds so they don't instantly pop out
            if (GameManager.Instance != null && !GameManager.Instance.isGameStarted)
            {
                nextObstacleTime = Time.time + baseObstacleSpawnRate;
                nextItemTime = Time.time + baseItemSpawnRate;
            }
            return;
        }
        
        float speedMultiplier = GameManager.Instance.globalSpeed / 5f; 
        
        if (Time.time >= nextObstacleTime)
        {
            SpawnObstacle();
            nextObstacleTime = Time.time + (baseObstacleSpawnRate / speedMultiplier);
        }
        
        if (Time.time >= nextItemTime)
        {
            SpawnItem();
            nextItemTime = Time.time + (baseItemSpawnRate / speedMultiplier);
        }
    }
    
    void SpawnObstacle()
    {
        if (obstaclePrefab == null) return;

        float randomY = Random.Range(-2.5f, 2.5f);
        Vector3 spawnPos = new Vector3(12f, randomY, -1f); 
        
        Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
    }
    
    void SpawnItem()
    {
        // [ANTI-OVERLAP] Ensure physics is up to date before checking!
        Physics2D.SyncTransforms();

        Vector3 spawnPos = Vector3.zero;
        bool foundSafeSpot = false;
        
        // Try up to 5 times to find a gap in the obstacles
        for (int i = 0; i < 5; i++)
        {
            float randomY = Random.Range(-4f, 4f);
            spawnPos = new Vector3(12f, randomY, -1f);
            
            // Check if ANY collider is already at this spot
            // Increased radius slightly to 0.7f for better "buffer" space
            Collider2D hit = Physics2D.OverlapCircle(spawnPos, 0.7f);
            
            if (hit == null)
            {
                foundSafeSpot = true;
                break;
            }
        }

        // If we didn't find a safe spot after 5 tries, we skip the spawn to be safe!
        if (!foundSafeSpot) return;
        
        // 20% chance HealthPack, 80% chance Enemy
        float randomVal = Random.value;
        GameObject prefab = (randomVal > 0.8f && healthPackPrefab != null) ? healthPackPrefab : enemyPrefab;
        
        if (prefab != null)
        {
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}
