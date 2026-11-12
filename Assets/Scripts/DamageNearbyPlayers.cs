using UnityEngine;

public class DamageNearbyPlayers : MonoBehaviour
{
    public GameObject explosionPrefab;
    public float explosoionRadius = 3f;
    public int explosionDamage = 1;

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Explode()
    {
        if(explosionPrefab != null)
        {
            GameObject explosionInstance = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosionInstance, 2f); // Destroy the explosion effect after 2 seconds
        }
       Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosoionRadius);


        foreach (Collider2D hit in hitColliders)
        {
            PlayerHealth player = hit.GetComponent<PlayerHealth>();
            if (player != null)
            {
                // Apply damage to the player
                player.RegisterHit(); // Assuming RegisterHit is a method in PlayerHealth that handles damage
            }
        }
        Destroy(gameObject); 
    }
}
