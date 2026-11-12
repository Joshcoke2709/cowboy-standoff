using UnityEngine;
using UnityEngine.Serialization;

public class FighterAI : MonoBehaviour
{
    [FormerlySerializedAs("player1")]
    public Transform opponent;
    public float stoppingDistance = 1f;
    private IFighterMovement movement;
    public Transform hayBale;
    public LayerMask obstacleLayer;
    public Transform aiShotOrigin;
    public float minJumpDelay = 2f;
    public float maxJumpDelay = 4f;
    private float nextJumpTime;
    private IFighterMovement opponentMovement;
    private string opponentState;
    private IFighterShooter shooter;
    private Collider2D opponentCollider;
    public float reactionMin = 0.15f;
    public float reactionMax = 0.4f;
    [Range(0f, 1f)] public float duckChance = 0.8f;
    [Range(0f, 1f)] public float counterJumpChance = 0.5f;
    private bool willCounterJump;
    private bool counterJumpDone;
    private bool opponentWasAirborne;
    private float reactTime;
    private bool willDuck;
    private bool shotTuningApplied;
    public Difficulty difficulty = Difficulty.Medium;
    public bool useGameSettings = false;

    void Start()
    {
        if (useGameSettings || GameSettings.FromMenu) difficulty = GameSettings.AIDifficulty;
        ApplyDifficulty();
        movement = GetComponent<IFighterMovement>();

        if (opponent != null)
        {
            opponentMovement = opponent.GetComponent<IFighterMovement>();
            opponentCollider = opponent.GetComponent<Collider2D>();
        }

        if (opponentMovement == null)
        {
            Debug.LogError("Opponent movement component not found! Is Opponent assigned?");
        }

        shooter = GetComponent<IFighterShooter>();
        nextJumpTime = Time.time + Random.Range(minJumpDelay, maxJumpDelay);
    }

    void Update()
    {
        if (movement == null || !movement.UseAI) return;

        if (!shotTuningApplied && shooter != null)
        {
            ApplyShotTuning();
            shotTuningApplied = true;
        }

        if (opponent == null || hayBale == null || aiShotOrigin == null) return;

        if (opponentMovement == null) return;

        if (opponentMovement.IsGrounded)
        {
            opponentState = opponentMovement.IsDucking ? "Ducking" : "Standing";
        }
        else if (opponentMovement.VerticalVelocity > 0.5f)
        {
            opponentState = "Rising";
        }
        else if (opponentMovement.VerticalVelocity < -0.5f)
        {
            opponentState = "Falling";
        }
        else
        {
            opponentState = "Jump Peak";
        }

        //Debug.Log("Player 1 State: " + opponentState);

        bool jump = false;
        if (Time.time >= nextJumpTime)
        {
            jump = true;
            nextJumpTime = Time.time + Random.Range(minJumpDelay, maxJumpDelay);
        }

        bool opponentAirborne = !opponentMovement.IsGrounded;

        if (opponentAirborne && !opponentWasAirborne)
        {
            // The opponent has just left the ground: decide how to react
            reactTime = Time.time + Random.Range(reactionMin, reactionMax);
            willDuck = Random.value <= duckChance;
            willCounterJump = !willDuck && Random.value <= counterJumpChance;
            counterJumpDone = false;
        }
        opponentWasAirborne = opponentAirborne;

        if (opponentAirborne && willCounterJump && !counterJumpDone
            && Time.time >= reactTime && movement.IsGrounded)
        {
            jump = true;
            counterJumpDone = true;
        }

        bool duck = opponentAirborne && willDuck && Time.time >= reactTime
                    && movement.IsGrounded && !jump;

        float distance = opponent.position.x - transform.position.x;

        // Check whether the hay bale blocks the shot
        Vector2 origin = aiShotOrigin.position;
        float shotDistance = Mathf.Abs(distance);
        Vector2 shotDirection = new Vector2(Mathf.Sign(distance), 0f);

        RaycastHit2D hit = Physics2D.Raycast(origin, shotDirection, shotDistance, obstacleLayer);
        Debug.DrawLine(origin, origin + shotDirection * shotDistance, Color.red);

        bool isBlocked = hit.collider != null;

        Bounds p1Bounds = opponentCollider.bounds;
        bool isAligned = origin.y >= p1Bounds.min.y && origin.y <= p1Bounds.max.y;

        /*if (isBlocked)
        {
            Debug.Log("Blocked by: " + hit.collider.gameObject.name);
        }
        else
        {
            Debug.Log("Clear shot");
        }*/

        if (isBlocked)
        {
            movement.SetAIInput(0f, duck, jump);
            return;
        }

        // Clear shot: stay put and fire
        movement.SetAIInput(0f, duck, jump);
        if (isAligned) shooter.TryShoot();
    }
    [ContextMenu("Apply Difficulty Now")]
    [ContextMenu("Apply Difficulty Now")]

    private void ApplyShotTuning()
    {
        float speed = 10f;
        float rate = 0.5f;

        switch (difficulty)
        {
            case Difficulty.Easy:
                speed = 8f;
                rate = 0.7f;
                break;

            case Difficulty.Medium:
                speed = 10f;
                rate = 0.5f;
                break;

            case Difficulty.Hard:
                speed = 15f;
                rate = 0.35f;
                break;
        }

        shooter.SetShotTuning(speed, rate);
    }
    public void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                reactionMin = 0.4f; reactionMax = 0.8f; duckChance = 0.4f; counterJumpChance = 0.1f;
                minJumpDelay = 3f; maxJumpDelay = 6f;
                break;
            case Difficulty.Medium:
                reactionMin = 0.15f; reactionMax = 0.4f; duckChance = 0.6f; counterJumpChance = 0.4f;
                minJumpDelay = 2f; maxJumpDelay = 4f;
                break;
            case Difficulty.Hard:
                reactionMin = 0.05f; reactionMax = 0.2f; duckChance = 0.5f; counterJumpChance = 0.8f;
                minJumpDelay = 1f; maxJumpDelay = 2.5f;
                break;
        }
    }
}