using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement")]
    public float moveSpeed = 8f;
    
    [Header("Health System")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Audio")]
    public AudioSource engineSource;
    
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private System.Collections.Generic.HashSet<int> hitObjects = new System.Collections.Generic.HashSet<int>();
    
    [Header("Shake Settings")]
    public float hitShakeDuration = 0.1f; // Reduced from 0.2f
    public float hitShakeMagnitude = 0.1f; // Reduced from 0.3f
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Force Z-position to -1 to align with obstacles and enemies!
        Vector3 playerPos = transform.position;
        playerPos.z = -1f;
        transform.position = playerPos;

        // FORCE RELIABILITY: Use Dynamic but with Gravity 0
        rb.isKinematic = false;
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // FORCE it to never stop checking for hits!
        
        // Start at full 100 health (5 packs) as requested!
        currentHealth = maxHealth;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        }

        // Enforce visual layering: Player (10) should be above Obstacles (5)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 10;

        // TAG CHECK: Without "Player" tag, ScoreZone will not work!
        if (!CompareTag("Player"))
        {
            Debug.LogError("PLAYER CAR IS NOT TAGGED 'Player'! ScoreZones will not work until you fix this in the Unity Inspector.");
        }
    }
    
    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)) 
        {
            rb.velocity = Vector2.zero; 
            if (engineSource != null && engineSource.isPlaying) engineSource.Stop();
            return; 
        }

        if (engineSource != null && !engineSource.isPlaying) engineSource.Play();
        
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -8.5f, 8.5f);
        pos.y = Mathf.Clamp(pos.y, -4.5f, 4.5f);
        transform.position = pos;
        
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }
    
    void FixedUpdate()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)) return;
        rb.MovePosition(rb.position + movementInput.normalized * moveSpeed * Time.fixedDeltaTime);
    }
    
    void Shoot()
    {
        if (projectilePrefab == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 fireDir = (mousePos - spawnPos).normalized;
        
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile pScript = proj.GetComponent<Projectile>();
        if (pScript != null) pScript.Initialize(fireDir);
    }
    
    public void TakeDamage(float amount)
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)) return;
        
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        // [FIXED] Removed the -10 score penalty for hitting obstacles!
        // The score should only increase when passing through gaps.
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        }
        
        if (CameraController.Instance != null)
        {
            StartCoroutine(CameraController.Instance.Shake(hitShakeDuration, hitShakeMagnitude));
        }
        
        if (currentHealth == 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver)) return;
        if (currentHealth <= 0) return;
        
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        }
    }
    
    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        gameObject.SetActive(false);
    }
    void OnTriggerEnter2D(Collider2D col) 
    { 
        HandleCollision(col); 
    }
    void OnTriggerStay2D(Collider2D col) { HandleCollision(col); }

    void HandleCollision(Collider2D col)
    {
        int id = col.gameObject.GetInstanceID();
        if (hitObjects.Contains(id)) return;

        // Skip safe paths (gaps) - Do this first!
        if (col.GetComponent<ScoreZone>() != null) return;

        bool handled = false;

        // 1. Check for Enemy
        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Loss of 20 health for hitting enemy - confirmed!
            TakeDamage(20);
            enemy.TakeDamageFromCollision(); // Use the collision method which does NOT give points!
            handled = true;
        }

        // 2. Check for Obstacle (Parent)
        Obstacle obstacle = col.GetComponentInParent<Obstacle>();
        // [RELIABILITY] Also check for ObstacleWall script directly on the collider!
        ObstacleWall wall = col.GetComponent<ObstacleWall>();

        if (obstacle != null || wall != null)
        {
            // [MARK AS HIT] If the player hits ANY part of the obstacle, they lose the score for it!
            if (obstacle != null) obstacle.hasPlayerHitWall = true;
            else if (wall != null)
            {
                Obstacle parentObstacle = wall.GetComponentInParent<Obstacle>();
                if (parentObstacle != null) parentObstacle.hasPlayerHitWall = true;
            }

            // If it's a wall hit, trigger damage!
            float damageAmount = (obstacle != null) ? obstacle.damage : 20f;
            TakeDamage(damageAmount);

            // Play the obstacle's specific hit sound if it has one!
            if (obstacle != null && obstacle.hitSound != null) AudioSource.PlayClipAtPoint(obstacle.hitSound, transform.position);
            
            // Score penalty removed as requested
            handled = true;
        }

        // 3. Check for HealthPack
        HealthPack healthPack = col.GetComponent<HealthPack>();
        if (healthPack != null)
        {
            if (currentHealth < maxHealth)
            {
                Heal(healthPack.healAmount);
                if (healthPack.collectSound != null) AudioSource.PlayClipAtPoint(healthPack.collectSound, transform.position);
                Destroy(col.gameObject);
                handled = true;
            }
        }

        // 4. Mark as handled so we don't hit the same instance again this frame
        if (handled) hitObjects.Add(id);
    }
}
