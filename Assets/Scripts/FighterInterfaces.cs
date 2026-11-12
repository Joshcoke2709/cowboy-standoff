public interface IFighterMovement
{
    bool IsGrounded { get; }
    float VerticalVelocity { get; }
    bool IsDucking { get; }
    bool IsFacingRight { get; }
    void SetAIInput(float horizontal, bool down, bool jump);
    bool UseAI { get; }
}

public interface IFighterShooter
{
    void TryShoot();
    void SetShotTuning(float speed, float rate);
}