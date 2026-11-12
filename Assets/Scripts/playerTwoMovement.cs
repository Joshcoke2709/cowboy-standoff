using UnityEngine;

public class Player2Movement : MonoBehaviour, IFighterMovement
{
    public float moveSpeed = 5f;
    public float jumpForce = 9f;
    public float slideSpeed = 6f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [SerializeField] private Transform visualTransform;

    private Rigidbody2D rb;
    public Animator animator;
    public Collider2D hitbox;
    private bool isGrounded;
    private bool isFacingRight = true;
    [SerializeField] private bool useAI = false;

    private float aiHorizontal;
    private bool aiDown;
    private bool aiJump;
    private bool previousAIDown;

    private float slideTimer;
    private float slideDuration = 0.2f;

    private float duckTimer;
    private float duckDuration = 2f;

    public float duckHeight = 0.9f;
    private CapsuleCollider2D capsule;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    private bool hitboxIsLow;
    public bool IsFacingRight => visualTransform != null && visualTransform.localScale.x < 0;
    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
    public bool IsDucking => animator != null && animator.GetBool("isDucking");
    public bool UseAI => useAI;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        visualTransform = transform.Find("VisualP2");

        capsule = GetComponent<CapsuleCollider2D>();
        standingSize = capsule.size;
        standingOffset = capsule.offset;
        if (GameSettings.FromMenu) useAI = GameSettings.Player2IsAI;
    }

    private void Update()
    {


        // Movement input for arrow keys
        float horizontal;
        bool downPressed;
        bool downPressedThisFrame;
        bool upPressed;
        bool isJumpPressed;

        if (useAI)
        {
            horizontal = aiHorizontal;
            downPressed = aiDown;
            downPressedThisFrame = aiDown && !previousAIDown;
            upPressed = aiJump;
            isJumpPressed = aiJump;

            previousAIDown = aiDown;
        }
        else
        {
            horizontal = 0f;

            if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
            else if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;

            downPressed = Input.GetKey(KeyCode.DownArrow);
            downPressedThisFrame = Input.GetKeyDown(KeyCode.DownArrow);
            upPressed = Input.GetKeyDown(KeyCode.UpArrow);
            isJumpPressed = Input.GetKey(KeyCode.UpArrow);
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        // Movement
        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        // Damping
        rb.linearDamping = (isGrounded && horizontal == 0) ? 5f : 0f;

        // Jump
        if (isJumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger("jumpTakeoff");
        }

        // Walking animation
        animator.SetBool("isWalking", horizontal != 0 && isGrounded && !animator.GetBool("isSliding") && !animator.GetBool("isDucking"));
        animator.SetBool("isJumping", !isGrounded);

        // Flip sprite
        if (horizontal != 0)
        {
            Vector3 scale = visualTransform.localScale;
            scale.x = horizontal > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            visualTransform.localScale = scale;
            isFacingRight = horizontal > 0;
        }

        // Slide
        if (downPressedThisFrame && horizontal != 0 && isGrounded && !animator.GetBool("isSliding"))
        {
            animator.SetBool("isSliding", true);
            animator.SetBool("isDucking", false);
            slideTimer = slideDuration;
            rb.linearVelocity = new Vector2(horizontal * slideSpeed, rb.linearVelocity.y);
        }

        if (animator.GetBool("isSliding"))
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                animator.SetBool("isSliding", false);
                animator.SetBool("isDucking", true);
                duckTimer = duckDuration;
                rb.linearVelocity = Vector2.zero;
            }
        }

        // Duck
        if (downPressed && horizontal == 0 && isGrounded && !animator.GetBool("isSliding"))
        {
            if (!animator.GetBool("isDucking"))
            {
                animator.SetBool("isDucking", true);
                duckTimer = duckDuration;
            }
        }

        if (animator.GetBool("isDucking"))
        {
            duckTimer -= Time.deltaTime;
            if (!downPressed || duckTimer <= 0f || upPressed)
            {
                animator.SetBool("isDucking", false);
            }
        }
        UpdateHitbox();
    }

    void UpdateHitbox()
    {
        bool ducking = animator.GetBool("isDucking");
        if (ducking == hitboxIsLow) return;
        hitboxIsLow = ducking;

        if (hitboxIsLow)
        {
            capsule.size = new Vector2(standingSize.x, duckHeight);
            capsule.offset = new Vector2(standingOffset.x,
                standingOffset.y - (standingSize.y - duckHeight) / 2f);
        }
        else
        {
            capsule.size = standingSize;
            capsule.offset = standingOffset;
        }
    }
    public void SetAIInput(float horizontal, bool down, bool jump)
    {
        aiHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        aiDown = down;
        aiJump = jump;
    }

    public void ResetState()
    {
        // 1. Snap the animator back to your idle state
        animator.Play("Idle");              // replace "Idle" with your exact state name

        // 2. Reset any motion
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // 3. If you�ve disabled colliders or controls on death, re-enable them:
        var coll = GetComponent<Collider2D>();
        if (coll != null)
            coll.enabled = true;

        // 4. Reset any flags you use for jumping, dashing, etc.
        //    e.g. isGrounded = true; canDash = true; etc.
    }
}
