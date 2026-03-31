using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float damageToPlayer = 20f;
    public int scoreValue = 10;
    public AudioClip destroySound;

    void Start()
    {
        // Force Z-position to -1 for 100% collision reliability!
        Vector3 pos = transform.position;
        pos.z = -1f;
        transform.position = pos;

        // [VISUAL] Enforce layering: Higher than Obstacles (5) but below Player (10)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 7;

        // [FAIR PLAY] Shrink the collider to 80% of the sprite size.
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        if (bc == null) bc = gameObject.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        if (sr != null) bc.size = sr.sprite.bounds.size * 0.8f;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.isGameOver) return;
        transform.Translate(Vector3.left * GameManager.Instance.globalSpeed * Time.deltaTime);
        if (transform.position.x < -12f) Destroy(gameObject);
    }

    public void TakeDamageFromProjectile()
    {
        // Shooting an enemy gives 10 marks as requested!
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);
        Die();
    }

    public void TakeDamageFromCollision()
    {
        // Colliding with an enemy does NOT give marks as requested!
        Die();
    }

    private void Die()
    {
        if (destroySound != null) AudioSource.PlayClipAtPoint(destroySound, transform.position);
        Destroy(gameObject);
    }
}
