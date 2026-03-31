using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public float healAmount = 20f; // Each pack is exactly 1/5th of 100 max health
    public AudioClip collectSound; // Sound to play on collection
    
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
}
