using UnityEngine;

public class BulletExplosion : MonoBehaviour
{
    public GameObject explosionPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Explode();
        
    }

    private void Explode()
    {
        if (explosionPrefab != null)
        {
           GameObject explosionInstance =  Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            Destroy(explosionInstance, 0.5f); // Destroy the explosion after 0.5 seconds
        }

        Destroy(gameObject); // destroy the bullet
        //Destroy(explosionPrefab);
    }

}
