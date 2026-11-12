using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates the gameplay pause button, pause menu, and difficulty submenu.
/// Difficulty values are passed to the existing GameSettings and FighterAI APIs.
/// </summary>
public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private Sprite ammoBulletSprite;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private const int AmmoIconCount = 6;
    private static readonly Color ReadyAmmoColor = new Color32(255, 229, 184, 255);
    private static readonly Color SpentAmmoColor = new Color32(105, 78, 55, 105);

    private static bool hasMenuDifficultySelection;

    private Canvas menuCanvas;
    private Button pauseButton;
    private GameObject overlayRoot;
    private GameObject pausePage;
    private GameObject difficultyPage;
    private GameObject controlsPage;
    private GameObject winnerOverlayRoot;
    private GameObject winnerPosterRoot;
    private RectTransform winnerPosterRect;
    private TMP_Text winnerTitleText;
    private TMP_Text winnerScoreText;
    private Coroutine winnerEntranceRoutine;
    private Material countdownOutlineMaterial;
    private TMP_Text currentDifficultyText;
    private Image[] playerOneAmmoIcons;
    private Image[] playerTwoAmmoIcons;
    private AmmoMagazine playerOneAmmo;
    private AmmoMagazine playerTwoAmmo;
    private float nextAmmoSearchTime;
    private readonly Dictionary<Difficulty, Button> difficultyButtons = new Dictionary<Difficulty, Button>();
    private readonly Dictionary<Difficulty, TMP_Text> difficultyLabels = new Dictionary<Difficulty, TMP_Text>();

    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Start()
    {
        BuildUI();
        FindAmmoMagazines();
        UpdateAmmoDisplay();
        ApplyCountdownTextStyle();
        if (hasMenuDifficultySelection)
        {
            StartCoroutine(ApplySavedDifficultyAfterSceneStarts());
        }
    }

    private void Update()
    {
        if ((playerOneAmmo == null || playerTwoAmmo == null) && Time.unscaledTime >= nextAmmoSearchTime)
        {
            FindAmmoMagazines();
            nextAmmoSearchTime = Time.unscaledTime + 1f;
        }

        UpdateAmmoDisplay();
    }

    private void OnEnable()
    {
        ScoreManager.MatchEnded += ShowWinnerScreen;
    }

    private void OnDisable()
    {
        ScoreManager.MatchEnded -= ShowWinnerScreen;
        RestoreTimeScale();
    }

    private IEnumerator ApplySavedDifficultyAfterSceneStarts()
    {
        // Let FighterAI.Start apply its scene defaults first, then restore the menu choice.
        yield return null;
        ApplyDifficultyToExistingAI(GameSettings.AIDifficulty);
        UpdateDifficultySelection();
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        menuCanvas = canvasObject.GetComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        pauseButton = CreateButton(canvasRect, "Pause Button", "II  PAUSE", new Vector2(-38f, -34f), new Vector2(166f, 64f), OpenPauseMenu);
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = pauseRect.anchorMax = Vector2.one;
        pauseRect.pivot = Vector2.one;
        pauseRect.anchoredPosition = new Vector2(-38f, -34f);

        playerOneAmmoIcons = BuildAmmoIconRow(canvasRect, true);
        playerTwoAmmoIcons = BuildAmmoIconRow(canvasRect, false);

        overlayRoot = new GameObject("Pause Overlay", typeof(RectTransform));
        overlayRoot.transform.SetParent(canvasRect, false);
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image dimBackdrop = CreatePanel("Dim backdrop", overlayRect, Vector2.zero, Vector2.zero, new Color32(17, 12, 9, 190), stretch: true).GetComponent<Image>();
        dimBackdrop.raycastTarget = true;
        CreatePanel("Menu shadow", overlayRect, new Vector2(770f, 790f), new Vector2(12f, -14f), new Color32(12, 8, 5, 190));
        CreatePanel("Menu frame", overlayRect, new Vector2(770f, 790f), Vector2.zero, new Color32(143, 94, 45, 255));
        RectTransform paper = CreatePanel("Menu paper", overlayRect, new Vector2(744f, 764f), Vector2.zero, new Color32(238, 220, 180, 255));
        RectTransform innerBorder = CreatePanel("Menu inner border", paper, new Vector2(710f, 730f), Vector2.zero, new Color32(183, 139, 77, 255));
        RectTransform innerPaper = CreatePanel("Menu inner paper", paper, new Vector2(702f, 722f), Vector2.zero, new Color32(238, 220, 180, 255));

        pausePage = new GameObject("Pause Options", typeof(RectTransform));
        pausePage.transform.SetParent(innerPaper, false);
        SetCenteredRect(pausePage.GetComponent<RectTransform>(), new Vector2(702f, 722f), Vector2.zero);

        difficultyPage = new GameObject("Difficulty Options", typeof(RectTransform));
        difficultyPage.transform.SetParent(innerPaper, false);
        SetCenteredRect(difficultyPage.GetComponent<RectTransform>(), new Vector2(702f, 722f), Vector2.zero);

        controlsPage = new GameObject("Controls Options", typeof(RectTransform));
        controlsPage.transform.SetParent(innerPaper, false);
        SetCenteredRect(controlsPage.GetComponent<RectTransform>(), new Vector2(702f, 722f), Vector2.zero);

        BuildPausePage(pausePage.transform);
        BuildDifficultyPage(difficultyPage.transform);
        BuildControlsPage(controlsPage.transform);
        BuildWinnerOverlay(canvasRect);
        difficultyPage.SetActive(false);
        controlsPage.SetActive(false);
        overlayRoot.SetActive(false);
        winnerOverlayRoot.SetActive(false);
    }

    private Image[] BuildAmmoIconRow(Transform parent, bool playerOne)
    {
        Image[] icons = new Image[AmmoIconCount];
        const float iconWidth = 34f;
        const float iconHeight = 22f;
        const float spacing = 8f;
        const float rowY = 188f;
        float rowWidth = AmmoIconCount * iconWidth + (AmmoIconCount - 1) * spacing;
        float healthBarCenterX = playerOne ? -420f : 410f;
        float firstIconX = healthBarCenterX - rowWidth * 0.5f + iconWidth * 0.5f;

        for (int i = 0; i < icons.Length; i++)
        {
            string playerLabel = playerOne ? "Player 1" : "Player 2";
            GameObject iconObject = new GameObject($"{playerLabel} Ammo {i + 1}", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = ammoBulletSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.rectTransform.localScale = new Vector3(playerOne ? 1f : -1f, 1f, 1f);
            SetCenteredRect(icon.rectTransform, new Vector2(iconWidth, iconHeight),
                new Vector2(firstIconX + i * (iconWidth + spacing), rowY));
            icons[i] = icon;
        }

        return icons;
    }

    private void FindAmmoMagazines()
    {
        AmmoMagazine[] magazines = FindObjectsByType<AmmoMagazine>(FindObjectsSortMode.None);
        foreach (AmmoMagazine magazine in magazines)
        {
            if (magazine == null) continue;
            if (magazine.isPlayerOne)
            {
                playerOneAmmo = magazine;
            }
            else
            {
                playerTwoAmmo = magazine;
            }
        }
    }

    private void UpdateAmmoDisplay()
    {
        UpdateAmmoIconRow(playerOneAmmo, playerOneAmmoIcons);
        UpdateAmmoIconRow(playerTwoAmmo, playerTwoAmmoIcons);
    }

    private static void UpdateAmmoIconRow(AmmoMagazine magazine, Image[] icons)
    {
        if (icons == null) return;

        int filledCount = 0;
        if (magazine != null)
        {
            filledCount = magazine.IsReloading
                ? Mathf.FloorToInt(magazine.ReloadProgress * magazine.maxAmmo)
                : magazine.Current;
        }
        filledCount = Mathf.Clamp(filledCount, 0, icons.Length);

        for (int i = 0; i < icons.Length; i++)
            icons[i].color = i < filledCount ? ReadyAmmoColor : SpentAmmoColor;
    }

    private void BuildWinnerOverlay(Transform canvasRect)
    {
        winnerOverlayRoot = new GameObject("Winner Overlay", typeof(RectTransform));
        winnerOverlayRoot.transform.SetParent(canvasRect, false);
        RectTransform overlayRect = winnerOverlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image dimBackdrop = CreatePanel("Winner Dim Backdrop", overlayRect, Vector2.zero, Vector2.zero,
            new Color32(17, 12, 9, 210), stretch: true).GetComponent<Image>();
        dimBackdrop.raycastTarget = true;
        winnerPosterRoot = new GameObject("Winner Poster", typeof(RectTransform));
        winnerPosterRoot.transform.SetParent(overlayRect, false);
        winnerPosterRect = winnerPosterRoot.GetComponent<RectTransform>();
        SetCenteredRect(winnerPosterRect, new Vector2(770f, 790f), Vector2.zero);

        CreatePanel("Winner Poster Shadow", winnerPosterRect, new Vector2(770f, 790f), new Vector2(12f, -14f), new Color32(12, 8, 5, 210));
        CreatePanel("Winner Poster Frame", winnerPosterRect, new Vector2(770f, 790f), Vector2.zero, new Color32(143, 94, 45, 255));
        RectTransform paper = CreatePanel("Winner Poster Paper", winnerPosterRect, new Vector2(744f, 764f), Vector2.zero, new Color32(238, 220, 180, 255));
        CreatePanel("Winner Inner Border", paper, new Vector2(710f, 730f), Vector2.zero, new Color32(183, 139, 77, 255));
        RectTransform innerPaper = CreatePanel("Winner Inner Paper", paper, new Vector2(702f, 722f), Vector2.zero, new Color32(238, 220, 180, 255));

        CreateText(innerPaper, "Winner Caption", "THE DUST HAS SETTLED", bodyFont, 22f, FontStyles.Bold,
            new Vector2(620f, 42f), new Vector2(0f, 250f), new Color32(117, 77, 43, 255), 1.5f);
        winnerTitleText = CreateText(innerPaper, "Winner Title", "PLAYER 1 WINS!", titleFont, 58f, FontStyles.Bold,
            new Vector2(640f, 110f), new Vector2(0f, 142f), new Color32(74, 43, 28, 255), 1f);
        CreateText(innerPaper, "Final Score Caption", "FINAL SCORE", bodyFont, 24f, FontStyles.Bold,
            new Vector2(600f, 42f), new Vector2(0f, 45f), new Color32(117, 77, 43, 255), 1f);
        winnerScoreText = CreateText(innerPaper, "Final Score", "3 - 0", titleFont, 54f, FontStyles.Bold,
            new Vector2(600f, 90f), new Vector2(0f, -20f), new Color32(74, 43, 28, 255), 1f);

        CreateButton(innerPaper, "Play Again Button", "PLAY AGAIN", new Vector2(0f, -125f), new Vector2(450f, 76f), PlayAgain);
        CreateButton(innerPaper, "Winner Main Menu Button", "MAIN MENU", new Vector2(0f, -225f), new Vector2(450f, 76f), ReturnToMainMenu);
    }

    private void BuildPausePage(Transform parent)
    {
        CreateText(parent, "Pause title", "PAUSED", titleFont, 64f, FontStyles.Bold,
            new Vector2(620f, 86f), new Vector2(0f, 285f), new Color32(74, 43, 28, 255), 1f);

        CreateButton(parent, "Resume Button", "RESUME", new Vector2(0f, 170f), new Vector2(470f, 76f), ResumeGame);
        CreateButton(parent, "Controls Button", "CONTROLS", new Vector2(0f, 78f), new Vector2(470f, 76f), OpenControlsPage);
        CreateButton(parent, "Difficulty Button", "DIFFICULTY", new Vector2(0f, -14f), new Vector2(470f, 76f), OpenDifficultyPage);
        CreateButton(parent, "Restart Button", "RESTART", new Vector2(0f, -106f), new Vector2(470f, 76f), RestartGame);
        CreateButton(parent, "Quit Button", "MAIN MENU", new Vector2(0f, -198f), new Vector2(470f, 76f), ReturnToMainMenu);
    }

    private void BuildControlsPage(Transform parent)
    {
        CreateText(parent, "Controls title", "CONTROLS", titleFont, 56f, FontStyles.Bold,
            new Vector2(640f, 82f), new Vector2(0f, 276f), new Color32(74, 43, 28, 255), 1f);
        BuildControlDisplay(parent, GameSettings.Player1IsAI ^ GameSettings.Player2IsAI);
        CreateButton(parent, "Controls Back Button", "BACK", new Vector2(0f, -270f), new Vector2(350f, 70f), BackFromControls, compact: true);
    }

    private void BuildControlDisplay(Transform parent, bool singlePlayer)
    {
        if (singlePlayer)
        {
            CreateText(parent, "Movement Keys Caption", "MOVE  /  JUMP  /  DUCK", bodyFont, 23f, FontStyles.Bold,
                new Vector2(640f, 38f), new Vector2(-145f, 175f), new Color32(74, 43, 28, 255), 0.2f);
            CreateArrowCluster(parent, new Vector2(-145f, 52f), 56f);
            CreateText(parent, "Arrow Mapping", "UP JUMPS   LEFT / RIGHT MOVE   DOWN DUCK", bodyFont, 15f, FontStyles.Bold,
                new Vector2(650f, 34f), new Vector2(-145f, -58f), new Color32(74, 43, 28, 255), 0.1f);
            CreateText(parent, "Shoot Keys Caption", "SHOOT", bodyFont, 23f, FontStyles.Bold,
                new Vector2(270f, 38f), new Vector2(205f, 125f), new Color32(74, 43, 28, 255), 0.2f);
            CreateKeycap(parent, "SPACE", new Vector2(205f, 36f), new Vector2(185f, 64f));
            CreateText(parent, "Shoot Alternative", "OR", bodyFont, 20f, FontStyles.Bold,
                new Vector2(100f, 32f), new Vector2(205f, -20f), new Color32(117, 77, 43, 255), 0.2f);
            CreateKeycap(parent, "MOUSE CLICK", new Vector2(205f, -92f), new Vector2(240f, 64f));
        }
        else
        {
            CreateText(parent, "P1 Controls Caption", "PLAYER 1", titleFont, 30f, FontStyles.Bold,
                new Vector2(300f, 42f), new Vector2(-175f, 185f), new Color32(74, 43, 28, 255), 0.2f);
            CreateText(parent, "P2 Controls Caption", "PLAYER 2", titleFont, 30f, FontStyles.Bold,
                new Vector2(300f, 42f), new Vector2(175f, 185f), new Color32(74, 43, 28, 255), 0.2f);
            CreateWasdCluster(parent, new Vector2(-175f, 42f), 52f);
            CreateArrowCluster(parent, new Vector2(175f, 42f), 52f);
            CreateText(parent, "P1 Key Mapping", "W JUMP   A / D MOVE   S DUCK", bodyFont, 13f, FontStyles.Bold,
                new Vector2(340f, 28f), new Vector2(-175f, -35f), new Color32(74, 43, 28, 255), 0.1f);
            CreateText(parent, "P2 Key Mapping", "UP JUMP   LEFT / RIGHT MOVE   DOWN DUCK", bodyFont, 12f, FontStyles.Bold,
                new Vector2(350f, 28f), new Vector2(175f, -35f), new Color32(74, 43, 28, 255), 0.1f);
            CreateKeycap(parent, "SPACE", new Vector2(-175f, -112f), new Vector2(160f, 58f));
            CreateKeycap(parent, "MOUSE CLICK", new Vector2(175f, -112f), new Vector2(200f, 58f));
        }
    }

    private void CreateWasdCluster(Transform parent, Vector2 center, float size)
    {
        CreateKeycap(parent, "W", center + new Vector2(0f, size + 6f), Vector2.one * size);
        CreateKeycap(parent, "A", center + new Vector2(-size - 6f, 0f), Vector2.one * size);
        CreateKeycap(parent, "S", center, Vector2.one * size);
        CreateKeycap(parent, "D", center + new Vector2(size + 6f, 0f), Vector2.one * size);
    }

    private void CreateArrowCluster(Transform parent, Vector2 center, float size)
    {
        CreateArrowKeycap(parent, center + new Vector2(0f, size + 6f), size, 0);
        CreateArrowKeycap(parent, center + new Vector2(-size - 6f, 0f), size, 1);
        CreateArrowKeycap(parent, center, size, 2);
        CreateArrowKeycap(parent, center + new Vector2(size + 6f, 0f), size, 3);
    }

    private void CreateKeycap(Transform parent, string label, Vector2 position, Vector2 size)
    {
        RectTransform cap = CreatePanel("Keycap " + label, parent, size, position, new Color32(70, 47, 34, 255));
        CreatePanel("Keycap Rim", cap, size - new Vector2(8f, 8f), Vector2.zero, new Color32(183, 139, 77, 255)).SetAsFirstSibling();
        CreatePanel("Keycap Face", cap, size - new Vector2(14f, 14f), Vector2.zero, new Color32(70, 47, 34, 255));
        CreateText(cap, "Key Label", label, bodyFont, label.Length > 4 ? 17f : 28f, FontStyles.Bold,
            size - new Vector2(12f, 12f), Vector2.zero, new Color32(250, 229, 186, 255), 0.1f);
    }

    private void CreateArrowKeycap(Transform parent, Vector2 position, float size, int direction)
    {
        RectTransform cap = CreatePanel("Arrow Keycap", parent, Vector2.one * size, position, new Color32(70, 47, 34, 255));
        CreatePanel("Keycap Rim", cap, Vector2.one * (size - 8f), Vector2.zero, new Color32(183, 139, 77, 255)).SetAsFirstSibling();
        CreatePanel("Keycap Face", cap, Vector2.one * (size - 14f), Vector2.zero, new Color32(70, 47, 34, 255));
        Transform arrow = new GameObject("Arrow Shape", typeof(RectTransform)).transform;
        arrow.SetParent(cap, false);
        arrow.localRotation = Quaternion.Euler(0f, 0f, direction * 90f);
        Color color = new Color32(250, 229, 186, 255);
        CreatePanel("Arrow Shaft", arrow, new Vector2(8f, size * 0.42f), new Vector2(0f, -size * 0.04f), color);
        RectTransform wingA = CreatePanel("Arrow Head", arrow, new Vector2(7f, size * 0.26f), new Vector2(-size * 0.11f, size * 0.14f), color);
        wingA.localRotation = Quaternion.Euler(0f, 0f, -42f);
        RectTransform wingB = CreatePanel("Arrow Head", arrow, new Vector2(7f, size * 0.26f), new Vector2(size * 0.11f, size * 0.14f), color);
        wingB.localRotation = Quaternion.Euler(0f, 0f, 42f);
    }

    private void BuildDifficultyPage(Transform parent)
    {
        CreateText(parent, "Difficulty title", "DIFFICULTY", titleFont, 56f, FontStyles.Bold,
            new Vector2(640f, 82f), new Vector2(0f, 276f), new Color32(74, 43, 28, 255), 1.5f);
        CreateText(parent, "Difficulty subtitle", "CHOOSE YOUR OPPONENT'S TEMPO", bodyFont, 21f, FontStyles.Bold,
            new Vector2(640f, 40f), new Vector2(0f, 224f), new Color32(117, 77, 43, 255), 1f);

        CreateDifficultyButton(parent, Difficulty.Easy, "EASY", 110f);
        CreateDifficultyButton(parent, Difficulty.Medium, "MEDIUM", 16f);
        CreateDifficultyButton(parent, Difficulty.Hard, "HARD", -78f);

        currentDifficultyText = CreateText(parent, "Current difficulty", "CURRENT: MEDIUM", bodyFont, 20f, FontStyles.Bold,
            new Vector2(620f, 38f), new Vector2(0f, -158f), new Color32(117, 77, 43, 255), 1f);
        CreateButton(parent, "Back Button", "BACK", new Vector2(0f, -250f), new Vector2(350f, 70f), BackToPausePage, compact: true);
        UpdateDifficultySelection();
    }

    private void CreateDifficultyButton(Transform parent, Difficulty difficulty, string label, float y)
    {
        Button button = CreateButton(parent, label + " Button", label, new Vector2(0f, y), new Vector2(450f, 76f),
            () => SelectDifficulty(difficulty));
        difficultyButtons.Add(difficulty, button);
        difficultyLabels.Add(difficulty, button.GetComponentInChildren<TMP_Text>());
    }

    private Button CreateButton(Transform parent, string objectName, string label, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction action, bool compact = false)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, size, position);

        Image image = buttonObject.GetComponent<Image>();
        image.color = compact ? new Color32(117, 77, 43, 255) : new Color32(106, 56, 34, 255);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color32(145, 91, 51, 255);
        colors.pressedColor = new Color32(73, 39, 27, 255);
        colors.selectedColor = new Color32(145, 91, 51, 255);
        colors.disabledColor = new Color32(91, 72, 51, 180);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        CreatePanel("Gold edge", rect, new Vector2(6f, size.y - 16f), new Vector2(-size.x / 2f + 10f, 0f), new Color32(220, 176, 91, 255));
        CreateText(rect, "Button label", label, compact ? bodyFont : titleFont, compact ? 27f : 34f,
            FontStyles.Bold, new Vector2(size.x - 70f, size.y - 8f), Vector2.zero,
            new Color32(250, 229, 186, 255), 1.5f);
        return button;
    }

    private void OpenPauseMenu()
    {
        if (isPaused) return;

        previousTimeScale = Time.timeScale;
        isPaused = true;
        Time.timeScale = 0f;
        pauseButton.gameObject.SetActive(false);
        pausePage.SetActive(true);
        difficultyPage.SetActive(false);
        controlsPage.SetActive(false);
        overlayRoot.SetActive(true);
        SelectFirstButton(pausePage.transform);
    }

    private void ResumeGame()
    {
        overlayRoot.SetActive(false);
        pauseButton.gameObject.SetActive(true);
        RestoreTimeScale();
        EventSystem.current?.SetSelectedGameObject(pauseButton.gameObject);
    }

    private void OpenDifficultyPage()
    {
        pausePage.SetActive(false);
        difficultyPage.SetActive(true);
        controlsPage.SetActive(false);
        UpdateDifficultySelection();
        SelectFirstButton(difficultyPage.transform);
    }

    private void OpenControlsPage()
    {
        pausePage.SetActive(false);
        difficultyPage.SetActive(false);
        controlsPage.SetActive(true);
        SelectFirstButton(controlsPage.transform);
    }

    private void BackFromControls()
    {
        controlsPage.SetActive(false);
        pausePage.SetActive(true);
        SelectFirstButton(pausePage.transform);
    }

    private void BackToPausePage()
    {
        difficultyPage.SetActive(false);
        pausePage.SetActive(true);
        SelectFirstButton(pausePage.transform);
    }

    private void SelectDifficulty(Difficulty difficulty)
    {
        RememberMenuDifficulty(difficulty);
        ApplyDifficultyToExistingAI(difficulty);
        UpdateDifficultySelection();
    }

    /// <summary>
    /// Records a difficulty chosen before MainScene loads. The existing delayed
    /// apply path then overrides scene defaults after FighterAI.Start runs.
    /// </summary>
    public static void RememberMenuDifficulty(Difficulty difficulty)
    {
        GameSettings.AIDifficulty = difficulty;
        hasMenuDifficultySelection = true;
    }

    private void ApplyDifficultyToExistingAI(Difficulty difficulty)
    {
        FighterAI[] fighters = FindObjectsByType<FighterAI>(FindObjectsSortMode.None);
        foreach (FighterAI fighter in fighters)
        {
            fighter.difficulty = difficulty;
            fighter.ApplyDifficulty();
        }
    }

    private void UpdateDifficultySelection()
    {
        if (currentDifficultyText != null)
        {
            currentDifficultyText.text = "CURRENT: " + GameSettings.AIDifficulty.ToString().ToUpperInvariant();
        }

        foreach (KeyValuePair<Difficulty, Button> entry in difficultyButtons)
        {
            bool selected = entry.Key == GameSettings.AIDifficulty;
            ColorBlock colors = entry.Value.colors;
            colors.normalColor = selected ? new Color32(183, 139, 77, 255) : new Color32(106, 56, 34, 255);
            colors.highlightedColor = selected ? new Color32(202, 158, 91, 255) : new Color32(145, 91, 51, 255);
            entry.Value.colors = colors;
            difficultyLabels[entry.Key].color = selected ? new Color32(65, 42, 27, 255) : new Color32(250, 229, 186, 255);
        }
    }

    private void ShowWinnerScreen(int winner, int p1Score, int p2Score)
    {
        if (winnerOverlayRoot == null)
            return;

        if (winnerTitleText != null)
            winnerTitleText.text = "PLAYER " + winner + " WINS!";
        if (winnerScoreText != null)
            winnerScoreText.text = p1Score + " - " + p2Score;

        if (!isPaused)
            previousTimeScale = Time.timeScale;
        isPaused = true;
        Time.timeScale = 0f;
        pauseButton.gameObject.SetActive(false);
        overlayRoot.SetActive(false);
        if (winnerEntranceRoutine != null)
        {
            StopCoroutine(winnerEntranceRoutine);
            winnerEntranceRoutine = null;
        }
        RectTransform overlayRect = winnerOverlayRoot.GetComponent<RectTransform>();
        float startY = overlayRect.rect.height * 0.5f + winnerPosterRect.rect.height * 0.5f + 24f;
        winnerPosterRect.anchoredPosition = new Vector2(0f, startY);
        winnerOverlayRoot.SetActive(true);
        SelectFirstButton(winnerOverlayRoot.transform);
        winnerEntranceRoutine = StartCoroutine(AnimateWinnerPosterEntrance(startY));
    }

    private IEnumerator AnimateWinnerPosterEntrance(float startY)
    {
        const float duration = 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            winnerPosterRect.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(startY, 0f, EaseOutBack(t, 1.12f)));
            yield return null;
        }

        winnerPosterRect.anchoredPosition = Vector2.zero;
        winnerEntranceRoutine = null;
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        float c3 = overshoot + 1f;
        float shifted = t - 1f;
        return 1f + c3 * shifted * shifted * shifted + overshoot * shifted * shifted;
    }

    private void PlayAgain()
    {
        LoadSceneWithTimeReset(SceneManager.GetActiveScene().name);
    }

    private void RestartGame()
    {
        LoadSceneWithTimeReset(SceneManager.GetActiveScene().name);
    }

    private void ReturnToMainMenu()
    {
        LoadSceneWithTimeReset(mainMenuSceneName);
    }

    private void LoadSceneWithTimeReset(string sceneName)
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(sceneName);
    }

    private void ApplyCountdownTextStyle()
    {
        GameObject countdownObject = GameObject.Find("CountdownText");
        if (countdownObject == null)
            return;

        TextMeshProUGUI countdownText = countdownObject.GetComponent<TextMeshProUGUI>();
        if (countdownText == null)
            return;

        if (titleFont != null)
            countdownText.font = titleFont;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.fontSize = 120f;
        countdownText.enableAutoSizing = false;
        countdownText.rectTransform.sizeDelta = new Vector2(650f, 180f);

        Material sharedFontMaterial = countdownText.fontSharedMaterial;
        if (sharedFontMaterial != null && sharedFontMaterial.HasProperty("_OutlineColor") && sharedFontMaterial.HasProperty("_OutlineWidth"))
        {
            countdownOutlineMaterial = new Material(sharedFontMaterial);
            countdownOutlineMaterial.SetColor("_OutlineColor", new Color32(38, 24, 18, 255));
            countdownOutlineMaterial.SetFloat("_OutlineWidth", 0.25f);
            countdownText.fontMaterial = countdownOutlineMaterial;
        }
    }

    private void SelectFirstButton(Transform page)
    {
        Button firstButton = page.GetComponentInChildren<Button>();
        if (firstButton != null) EventSystem.current?.SetSelectedGameObject(firstButton.gameObject);
    }

    private void RestoreTimeScale()
    {
        if (!isPaused) return;
        Time.timeScale = previousTimeScale;
        isPaused = false;
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
        if (countdownOutlineMaterial != null)
            Destroy(countdownOutlineMaterial);
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, Vector2 size, Vector2 position, Color color, bool stretch = false)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            SetCenteredRect(rect, size, position);
        }
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string objectName, string value, TMP_FontAsset font,
        float fontSize, FontStyles style, Vector2 size, Vector2 position, Color color, float characterSpacing)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetCenteredRect(textObject.GetComponent<RectTransform>(), size, position);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.72f;
        text.fontSizeMax = fontSize;
        text.characterSpacing = characterSpacing;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
