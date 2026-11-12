using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, IFighterMovement
{
    public float moveSpeed = 5f;
    public float jumpForce = 9f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [SerializeField] private Transform visualTransform;

    private Rigidbody2D rb;
    public Animator animator;
    public Collider2D hitbox;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;

    public bool IsDucking => animator != null && animator.GetBool("isDucking");
    private bool isFacingRight = true;

    private float slideTimer;
    private float slideDuration = 0.2f;
    public float slideSpeed = 6f;
    public bool IsFacingRight => visualTransform != null && visualTransform.localScale.x > 0;

    private float duckTimer;
    private float duckDuration = 2f;

    private float walkTime;
    public float walkThreshold = 0.1f;

    public float duckHeight = 0.9f;
    private CapsuleCollider2D capsule;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    private bool hitboxIsLow;

    [SerializeField] private bool useAI = false;
    private float aiHorizontal;
    private bool aiDown;
    private bool aiJump;
    private bool previousAIDown;
    public bool UseAI => useAI;
    private KeyCode leftKey = KeyCode.A;
    private KeyCode rightKey = KeyCode.D;
    private KeyCode downKey = KeyCode.S;
    private KeyCode upKey = KeyCode.W;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        visualTransform = transform.Find("Visual");

        capsule = GetComponent<CapsuleCollider2D>();
        standingSize = capsule.size;
        standingOffset = capsule.offset;
        if (GameSettings.FromMenu) useAI = GameSettings.Player1IsAI;
        if (GameSettings.SinglePlayer)
        {
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            downKey = KeyCode.DownArrow;
            upKey = KeyCode.UpArrow;
        }
    }

    void Update()
    {
        float move = 0f;
        bool downPressed;
        bool downPressedThisFrame;
        bool upPressed;

        if (useAI)
        {
            move = aiHorizontal;
            downPressed = aiDown;
            downPressedThisFrame = aiDown && !previousAIDown;
            upPressed = aiJump;
            previousAIDown = aiDown;
        }
        else
        {
            if (Input.GetKey(leftKey)) move = -1f;
            else if (Input.GetKey(rightKey)) move = 1f;

            downPressed = Input.GetKey(downKey);
            downPressedThisFrame = Input.GetKeyDown(downKey);
            upPressed = Input.GetKeyDown(upKey);
        }

        bool movePressed = move != 0;
        bool isHoldingDown = downPressed;

        bool wasGrounded = isGrounded;

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            0.1f,
            groundLayer
        );

        animator.SetBool("isJumping", !isGrounded);

        if (wasGrounded != isGrounded)
        {
            Debug.Log("GROUND STATE CHANGED: " + isGrounded +
                      " | Velocity Y: " + rb.linearVelocity.y);
        }

        animator.SetBool(
            "isWalking",
            move != 0 &&
            isGrounded &&
            !animator.GetBool("isSliding") &&
            !animator.GetBool("isDucking")
        );


        // Movement
        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);

        if (move > 0)
            visualTransform.localScale = new Vector3(1, 1, 1);
        else if (move < 0)
            visualTransform.localScale = new Vector3(-1, 1, 1);

        walkTime = (move != 0) ? walkTime + Time.deltaTime : 0f;

        // Damping
        rb.linearDamping = (isGrounded && move == 0) ? 5f : 0f;

        // Jumping (using W)
        if (upPressed && isGrounded)
        {
            Debug.Log("JUMP FIRED | Grounded: " + isGrounded +
             " | Velocity Y: " + rb.linearVelocity.y);

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger("jumpTakeoff");

            /*float jumpAnimSpeed = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / moveSpeed);
            animator.speed = Mathf.Lerp(1.3f, 0.7f, jumpAnimSpeed);*/
        }

        /*if (isGrounded && animator.speed != 1f)
        {
            animator.speed = 1f;
        }*/

        // Sliding
        if (downPressedThisFrame && movePressed && isGrounded && !animator.GetBool("isSliding"))
        {
            animator.SetBool("isSliding", true);
            animator.SetBool("isDucking", false);
            slideTimer = slideDuration;
            rb.linearVelocity = new Vector2(move * slideSpeed, rb.linearVelocity.y);
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

        // Ducking
        if (upPressed && animator.GetBool("isDucking"))
        {
            animator.SetBool("isDucking", false);
        }

        if (isHoldingDown && move == 0 && isGrounded && !animator.GetBool("isSliding"))
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
            if (!isHoldingDown || duckTimer <= 0f || upPressed)
            {
                animator.SetBool("isDucking", false);
            }
        }

        animator.SetBool("isJumping", !isGrounded);



        // Update facing direction
        if (move != 0)
        {
            Vector3 visualScale = visualTransform.localScale;
            visualScale.x = Mathf.Sign(move);
            Mathf.Abs(visualScale.x); // Ensure scale is always positive
            visualTransform.localScale = visualScale;

            isFacingRight = move > 0;
        }

        UpdateHitbox();
    }

    void UpdateHitbox()
    {
        if (IsDucking == hitboxIsLow) return;
        hitboxIsLow = IsDucking;

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

        // 3. If you’ve disabled colliders or controls on death, re-enable them:
        var coll = GetComponent<Collider2D>();
        if (coll != null)
            coll.enabled = true;

        // 4. Reset any flags you use for jumping, dashing, etc.
        //    e.g. isGrounded = true; canDash = true; etc.
    }
}
