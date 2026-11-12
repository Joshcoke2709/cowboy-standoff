using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public int p1Score, p2Score;
    public TextMeshProUGUI p1ScoreText, p2ScoreText;
    public static event System.Action<int, int, int> MatchEnded;   // winner, p1 score, p2 score
    public GameTimer timer;
    public PlayerHealth p1Health, p2Health;
    public PlayerMovement p1Movement;
    public Player2Movement p2Movement;
    public DuelCountdown countdown;
    public static bool RoundLive = true;
    private bool gameEnded;
    private bool roundResolving;
    public float slowMotionScale = 0.25f;
    public float slowMotionSeconds = 1.2f;
    public float winnerScreenDelay = 1f;
    private float defaultFixedDelta;

    public void PlayerDied(string playerTag)
    {

        Debug.Log($"ScoreManager.PlayerDied({playerTag})");
        // …

        if (gameEnded || roundResolving) return;
        roundResolving = true;
        RoundLive = false;

        // 1. Increment the *winner’s* score
        if (playerTag == "Player1") p2Score++;
        else if (playerTag == "Player2") p1Score++;

        UpdateScoreUI();

        // 2. Stop timer & schedule round reset
        timer.StopRound();
        if (p1Score >= 3 || p2Score >= 3)
        {
            GameOver();
            return;
        }
        Invoke(nameof(ResetRound), 2f);
    }

    private void UpdateScoreUI()
    {
        p1ScoreText.text = p1Score.ToString();
        p2ScoreText.text = p2Score.ToString();
    }

    private void ResetRound()
    {
        // 1. Refill health
        p1Health.ResetHealth();
        p2Health.ResetHealth();

        // 2. Reset visual state
        p1Movement.ResetState();
        p2Movement.ResetState();

        // 3. Check for overall game end
        roundResolving = false;

        foreach (GameObject bullet in GameObject.FindGameObjectsWithTag("Bullet"))
            Destroy(bullet);

        // 4. Start next round
        countdown.Begin();
        Debug.Log("Next Round!");


    }
    private void Awake()
    {
        defaultFixedDelta = Time.fixedDeltaTime;
        RoundLive = true;
    }

    private void GameOver()
    {
        if (gameEnded) return;
        gameEnded = true;

        timer.StopRound();

        p1Health.DisableControls();
        p2Health.DisableControls();

        Debug.Log("Game Over! Final Scores: P1: " + p1Score + ", P2: " + p2Score);
        StartCoroutine(EndMatchSequence());
    }
    private IEnumerator EndMatchSequence()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = defaultFixedDelta * slowMotionScale;
        yield return new WaitForSecondsRealtime(slowMotionSeconds);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        yield return new WaitForSecondsRealtime(winnerScreenDelay);

        MatchEnded?.Invoke(p1Score >= 3 ? 1 : 2, p1Score, p2Score);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
    }
}