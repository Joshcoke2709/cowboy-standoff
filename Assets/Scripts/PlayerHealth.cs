
using System.Collections;
using UnityEngine;
using UnityEngine.UI;



public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 90;
    private int currentHealth;
    //private int currentHits = 0;

    public Image healthFillImage;
    public GameObject explosionPrefab;

    private Animator animator;

    private bool isDead = false;

    public ScoreManager scoreManager;


    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();

        UpdateHealthBar();

    }
    public void RegisterHit()
    {
        if (!ScoreManager.RoundLive) return;
        if (isDead) return; // Ignore hits if already dead

        currentHealth--;
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Hit");

        UpdateHealthBar();

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Die();
            DisableControls();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage == null) return;
        float healthPercentage = (float)currentHealth / maxHealth;

        StartCoroutine(AnimateHealthBar(healthFillImage.fillAmount, healthPercentage));
        //change the color based on health percentage
        if (healthPercentage > 0.66f)
        {
            healthFillImage.color = Color.green;
        }
        else if (healthPercentage > 0.33f)
        {
            healthFillImage.color = Color.yellow;
        }
        else
        {
            healthFillImage.color = Color.red;
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Debug.Log("Player hit by bullet!");
            RegisterHit();
            Destroy(collision.gameObject); // Destroy the bullet after hit
        }
    }

    private IEnumerator AnimateHealthBar(float from, float to)
    {
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            healthFillImage.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }

        healthFillImage.fillAmount = to; // Ensure it ends at the exact value
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthBar();
        EnableControls();
        animator.ResetTrigger("Dead");
        animator.Play("Idle");

        AmmoMagazine ammo = GetComponent<AmmoMagazine>();
        if (ammo != null) ammo.Refill();
    }


    private void Die()
    {


        Debug.Log($"{gameObject.name} died – calling PlayerDied()");



        animator.SetTrigger("Dead");
        scoreManager.PlayerDied(gameObject.tag);

        // [... play death animation, disable controls, etc. ...]
        //FindObjectOfType<ScoreManager>().PlayerDied(this.gameObject.tag);



        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        var p1Move = GetComponent<PlayerMovement>();
        if (p1Move != null) p1Move.enabled = false;
        var p2Move = GetComponent<Player2Movement>();
        if (p2Move != null) p2Move.enabled = false;
        var p1shooter = GetComponent<Shooter>();
        if (p1shooter != null) p1shooter.enabled = false;
        var p2shooter = GetComponent<Player2Shooter>();
        if (p2shooter != null) p2shooter.enabled = false;

    }

    public void EnableControls()
    {

        var p1Move = GetComponent<PlayerMovement>();
        if (p1Move != null) p1Move.enabled = true;
        var p2Move = GetComponent<Player2Movement>();
        if (p2Move != null) p2Move.enabled = true;
        var p1shooter = GetComponent<Shooter>();
        if (p1shooter != null) p1shooter.enabled = true;
        var p2shooter = GetComponent<Player2Shooter>();
        if (p2shooter != null) p2shooter.enabled = true;
    }

    public void DisableControls()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        var p1Move = GetComponent<PlayerMovement>();
        var p2Move = GetComponent<Player2Movement>();
        var p1shooter = GetComponent<Shooter>();
        var p2shooter = GetComponent<Player2Shooter>();
        if (p1Move != null) p1Move.enabled = false;
        if (p2Move != null) p2Move.enabled = false;
        if (p1shooter != null) p1shooter.enabled = false;
        if (p2shooter != null) p2shooter.enabled = false;
    }

}

