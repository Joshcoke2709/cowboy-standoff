using System.Collections;
using TMPro;
using UnityEngine;

public class DuelCountdown : MonoBehaviour
{
    public TextMeshProUGUI countdownText;
    public PlayerHealth p1Health;
    public PlayerHealth p2Health;
    public GameTimer timer;

    public float secondsPerNumber = 1f;
    public float drawDisplayTime = 0.8f;
    public float quickDrawMin = 0.8f;
    public float quickDrawMax = 1.6f;
    public Color numberColor = Color.white;
    public Color drawColor = Color.red;

    private void Start()
    {
        StartCoroutine(RunCountdown());
    }

    private IEnumerator RunCountdown()
    {
        yield return null;   // wait one frame so every other script finishes its Start()

        p1Health.DisableControls();
        p2Health.DisableControls();
        ScoreManager.RoundLive = false;

        for (int n = 3; n >= 1; n--)
        {
            Show(n.ToString(), numberColor);
            yield return new WaitForSeconds(secondsPerNumber);
        }

        yield return StartCoroutine(Draw());
    }

    private void Show(string message, Color color)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = message;
        countdownText.color = color;
    }
    public void Begin()
    {
        StopAllCoroutines();
        StartCoroutine(RunQuickDraw());
    }

    private IEnumerator RunQuickDraw()
    {
        p1Health.DisableControls();
        p2Health.DisableControls();
        ScoreManager.RoundLive = false;

        yield return new WaitForSeconds(Random.Range(quickDrawMin, quickDrawMax));
        yield return StartCoroutine(Draw());
    }
    private IEnumerator Draw()
    {
        Show("DRAW!", drawColor);
        p1Health.EnableControls();
        p2Health.EnableControls();
        ScoreManager.RoundLive = true;

        timer.StartRound();

        yield return new WaitForSeconds(drawDisplayTime);
        countdownText.gameObject.SetActive(false);
    }
}