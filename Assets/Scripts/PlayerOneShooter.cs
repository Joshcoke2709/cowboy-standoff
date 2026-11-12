using UnityEngine;
using UnityEngine.UIElements;

public class Shooter : MonoBehaviour, IFighterShooter
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 10f;
    public float fireRate = 0.5f;

    public GameObject muzzleFlashPrefab;
    public float muzzleFlashDuration = 0.5f;
    private PlayerMovement movement;

    private float lastShotTime;
    private bool isFacingRight = true;
    private Animator animator;
    private AmmoMagazine ammo;

    void Update()
    {
        if (movement.UseAI || Time.timeScale == 0f) return;

        bool shootPressed = Input.GetKeyDown(KeyCode.Space)
                            || (GameSettings.SinglePlayer && Input.GetMouseButtonDown(0));
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
        isFacingRight = movement.IsFacingRight;

        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);


        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            flash.transform.SetParent(firePoint);
            Destroy(flash, muzzleFlashDuration);
        }

        Vector2 shootDir = isFacingRight ? Vector2.right : Vector2.left;

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDir * bulletSpeed;
        }

        Vector3 bulletScale = bullet.transform.localScale;
        bulletScale.x = Mathf.Abs(bulletScale.x) * (isFacingRight ? 1 : -1);
        bullet.transform.localScale = bulletScale;

        bullet.transform.position = new Vector3(firePoint.position.x, firePoint.position.y, 0);

        Destroy(bullet, 3f);

    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<PlayerMovement>();
        ammo = GetComponent<AmmoMagazine>();
    }
    public void SetShotTuning(float speed, float rate)
    {
        bulletSpeed = speed;
        fireRate = rate;
    }
}
