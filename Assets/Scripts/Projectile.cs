// Projectile.cs
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 2f;
    public int damageAmount = 2; // Damage this projectile deals

    void Start()
    {
        // Destroy the projectile after a certain time to prevent clutter
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Move the projectile forward
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the projectile hit an object tagged as "Enemy"
        if (other.CompareTag("Enemy"))
        {
            // Get the Enemy component from the collided object
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // If an Enemy component is found, call its TakeDamage method
                enemy.TakeDamage(damageAmount);
            }
            // Destroy the projectile on impact with an enemy
            Destroy(gameObject);
        }
        // Optionally, destroy projectile if it hits boundaries or other specific objects
        // Add more collision logic here if needed (e.g., if hitting meteors)
    }
}