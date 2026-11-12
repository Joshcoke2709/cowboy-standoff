using UnityEngine;

public class Player2Shooter : MonoBehaviour, IFighterShooter
{
    public GameObject bulletPrefab;             // Bullet prefab to instantiate
    public Transform firePoint;                 // Where the bullet spawns from
    private Animator animator;
    public float bulletSpeed = 10f;             // Speed of bullet
    public float fireRate = 0.5f;               // Cooldown between shots

    public GameObject muzzleFlashPrefab;        // Muzzle flash prefab
    public float muzzleFlashDuration = 0.5f;    // Duration of flash

    private float lastShotTime;
    private bool isFacingRight = false;         // Starts facing LEFT since Player 2 is on the right
    private Player2Movement movement;
    private AmmoMagazine ammo;

    void Update()
    {
        if (movement.UseAI || Time.timeScale == 0f) return;

        bool shootPressed = Input.GetMouseButtonDown(0)
                            || (GameSettings.SinglePlayer && Input.GetKeyDown(KeyCode.Space));
        if (shootPressed) TryShoot();
    }
    public void TryShoot()
    {
        if (!enabled) return;

        if (Time.time - lastShotTime >= fireRate && (ammo == null || ammo.TryConsume()))
        {
            Shoot();
            lastShotTime = Time.time;
        }
    }

    void Shoot()
    {
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }

        // Create bullet and flash at firePoint
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
        flash.transform.SetParent(firePoint);
        Destroy(flash, muzzleFlashDuration);

        // Set direction based on facing
        Vector2 shootDir = movement.IsFacingRight ? Vector2.right : Vector2.left;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Prevent falling
        rb.linearVelocity = shootDir * bulletSpeed;

        Vector3 bulletScale = bullet.transform.localScale;
        bulletScale.x = Mathf.Abs(bulletScale.x) * (movement.IsFacingRight ? -1 : 1);
        bullet.transform.localScale = bulletScale;

        // Auto-destroy bullet after time
        Destroy(bullet, 3f);
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<Player2Movement>();
        ammo = GetComponent<AmmoMagazine>();
    }
    public void SetShotTuning(float speed, float rate)
    {
        bulletSpeed = speed;
        fireRate = rate;
    }
}
