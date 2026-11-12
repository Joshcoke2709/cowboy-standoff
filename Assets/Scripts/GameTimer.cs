using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float roundTime = 99f;
    private float currentTime;

    public TextMeshProUGUI timerText;
    public Color startColor = Color.green;
    public Color midColor = Color.yellow;
    public Color lowColor = Color.red;
    public Color waitingColor = Color.white;
    public bool startAutomatically = true;

    private bool roundActive = false;
    private bool roundEnded = false;

    private void Start()
    {
        currentTime = roundTime;
        timerText.color = waitingColor;

        if (startAutomatically)
        {
            StartRound();
        }
        else
        {
            UpdateTimerUI();
            timerText.color = waitingColor;
        }
    }

    private void Update()
    {
        if (!roundActive || roundEnded) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            //roundEnded = true;
            EndRound();
        }

        UpdateTimerUI();
    }

    public void StartRound()
    {
        roundActive = true;
        roundEnded = false;
        timerText.color = startColor;
        UpdateTimerUI();
        Debug.Log("ROUND STARTED!");
    }

    

    public void StopRound()
{
    roundActive  = false;
    roundEnded   = true;
    timerText.color = waitingColor;
}

public void ResumeRound()
{
    //currentTime  = roundTime;     // reset the clock
    roundActive  = true;
    roundEnded   = false;
    timerText.color = startColor;
    //UpdateTimerUI();
    Debug.Log("ROUND STARTED!");
}



    public void TriggerGameOver()
    {
        roundEnded = true;
        //StopRound();
        timerText.color = waitingColor;
    }

    public void Resume()
    {
        roundActive = true;
        roundEnded = false;
    }

    public void UpdateTimerUI()
    {
        int seconds = Mathf.CeilToInt(currentTime);
        timerText.text = $"{seconds:00}";

        float percentage = currentTime / roundTime;
        if (percentage > 0.66f)
            timerText.color = startColor;
        else if (percentage > 0.33f)
            timerText.color = midColor;
        else
            timerText.color = lowColor;
    }

    private void EndRound()
    {
        StopRound();
        Debug.Log("Round ended!");
        timerText.text = "00";
    }
}
