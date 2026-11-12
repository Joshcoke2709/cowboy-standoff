using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the Western title menu and match setup screen.
/// </summary>
public sealed class MainMenuUI : MonoBehaviour
{
    private enum DoorAnimationState { None, Opening, Closing }

    [System.Serializable]
    private sealed class PreviewLayer
    {
        public Sprite sprite;
        public Vector2 localPosition;
        public Vector2 localScale = Vector2.one;
        public Vector3 localEulerAngles;
        public bool flipX;
    }

    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private Sprite characterPreviewSprite;
    [SerializeField] private Sprite titleDecorationSprite;
    [SerializeField] private Sprite saloonDoorLeftSprite;
    [SerializeField] private Sprite saloonDoorRightSprite;
    [SerializeField] private PreviewLayer[] playerOnePreviewLayers;
    [SerializeField] private PreviewLayer[] playerTwoPreviewLayers;
    [SerializeField] private string gameplaySceneName = "MainScene";

    private const float PreviewBodyWidth = 170f;
    private const float PreviewBodyWorldWidth = 1.72f;

    private RectTransform root;
    private GameObject mainPage;
    private GameObject[] setupPages;
    private int setupStep = -1;
    private Image previewImage;
    private TextMeshProUGUI previewPlayerLabel;
    private GameObject playerOneOverlayRoot;
    private GameObject playerTwoOverlayRoot;
    private Button playerOneButton;
    private Button playerTwoButton;
    private Button[] colorButtons;
    private TMP_Text[] colorButtonLabels;
    private Button[] difficultyButtons;
    private TMP_Text[] difficultyButtonLabels;
    private bool editingPlayerOne = true;
    private bool singlePlayerMode = true;
    private bool playingAsPlayerOne = true;
    private Button singlePlayerModeButton;
    private Button twoPlayerModeButton;
    private Transform controlsPaper;
    private GameObject controlsDisplayRoot;
    private Button playerChoiceOneButton;
    private Button playerChoiceTwoButton;
    private Image playerChoiceOneImage;
    private Image playerChoiceTwoImage;
    private GameObject saloonDoorsRoot;
    private RectTransform leftDoorRect;
    private RectTransform rightDoorRect;
    private Coroutine saloonDoorRoutine;
    private Coroutine titleDoorDelayRoutine;
    private DoorAnimationState doorAnimationState;
    private bool saloonDoorsAreOpen;
    private float saloonDoorScale = 1f;
    private const float OpenDoorScale = 0.22f;

    private void Start()
    {
        EnsureEventSystem();
        BuildMenu();
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
        // ENTER is handled as one screen advance in Update, so don't also submit
        // the currently selected button through the UI module on that same press.
        inputModule.submit = null;
    }

    private void BuildMenu()
    {
        GameObject canvasObject = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        CreatePanel("Backdrop", root, Vector2.zero, Vector2.zero, new Color32(110, 70, 45, 255), stretch: true);
        CreatePanel("Top trim", root, new Vector2(0f, 8f), new Vector2(0f, -18f), new Color32(170, 126, 66, 255), fullWidth: true);
        CreatePanel("Bottom trim", root, new Vector2(0f, 8f), new Vector2(0f, 18f), new Color32(170, 126, 66, 255), fullWidth: true);

        mainPage = CreatePage("Main Menu Page", out Transform mainContent);
        BuildMainPage(mainContent);
        BuildSetupPages();
        SetActiveSetupStep(-1);
        BuildSaloonDoors();
        titleDoorDelayRoutine = StartCoroutine(OpenTitleDoorsAfterDelay());

        SelectFirstButton(mainPage.transform);
    }

    private void BuildSaloonDoors()
    {
        saloonDoorsRoot = new GameObject("Saloon Door Frame", typeof(RectTransform));
        saloonDoorsRoot.transform.SetParent(root, false);
        RectTransform doorsRect = saloonDoorsRoot.GetComponent<RectTransform>();
        doorsRect.anchorMin = Vector2.zero;
        doorsRect.anchorMax = Vector2.one;
        doorsRect.offsetMin = Vector2.zero;
        doorsRect.offsetMax = Vector2.zero;

        leftDoorRect = CreateSaloonDoor(doorsRect, true, saloonDoorLeftSprite);
        rightDoorRect = CreateSaloonDoor(doorsRect, false, saloonDoorRightSprite);
        SetSaloonDoorScale(1f);
    }

    private RectTransform CreateSaloonDoor(Transform parent, bool leftSide, Sprite sprite)
    {
        GameObject doorObject = new GameObject(leftSide ? "Left Saloon Door" : "Right Saloon Door",
            typeof(RectTransform), typeof(Image));
        doorObject.transform.SetParent(parent, false);
        RectTransform door = doorObject.GetComponent<RectTransform>();
        door.anchorMin = door.anchorMax = new Vector2(0.5f, 0.5f);
        door.pivot = new Vector2(leftSide ? 0f : 1f, 0.5f);
        door.sizeDelta = new Vector2(755f, 944f);
        door.anchoredPosition = new Vector2(leftSide ? -755f : 755f, 0f);

        Image image = doorObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return door;
    }

    private void SetSaloonDoorScale(float scale)
    {
        saloonDoorScale = scale;
        if (leftDoorRect != null)
            leftDoorRect.localScale = new Vector3(scale, 1f, 1f);
        if (rightDoorRect != null)
            rightDoorRect.localScale = new Vector3(scale, 1f, 1f);
    }

    private IEnumerator OpenTitleDoorsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        titleDoorDelayRoutine = null;
        if (saloonDoorsRoot != null && saloonDoorsRoot.activeSelf && !saloonDoorsAreOpen)
            StartSaloonDoorAnimation(true, false);
    }

    private void StartSaloonDoorAnimation(bool opening, bool loadGameplayWhenClosed)
    {
        if (titleDoorDelayRoutine != null)
        {
            StopCoroutine(titleDoorDelayRoutine);
            titleDoorDelayRoutine = null;
        }
        StopSaloonDoorAnimation();
        saloonDoorsRoot.SetActive(true);
        doorAnimationState = opening ? DoorAnimationState.Opening : DoorAnimationState.Closing;
        saloonDoorRoutine = StartCoroutine(AnimateSaloonDoors(opening, loadGameplayWhenClosed));
    }

    private IEnumerator AnimateSaloonDoors(bool opening, bool loadGameplayWhenClosed)
    {
        float startScale = saloonDoorScale;
        float endScale = opening ? OpenDoorScale : 1f;
        float duration = opening ? 1.6f : 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = opening
            ? EaseOutBack(progress, 1.4f)
            : progress * progress * (3f - 2f * progress);
            SetSaloonDoorScale(Mathf.LerpUnclamped(startScale, endScale, eased));
            yield return null;
        }

        SetSaloonDoorScale(endScale);
        saloonDoorRoutine = null;
        doorAnimationState = DoorAnimationState.None;
        saloonDoorsAreOpen = opening;
        saloonDoorsRoot.SetActive(opening && mainPage.activeSelf);
        if (!opening && loadGameplayWhenClosed)
            LoadGameplayScene();
    }

    private void StopSaloonDoorAnimation()
    {
        if (saloonDoorRoutine != null)
        {
            StopCoroutine(saloonDoorRoutine);
            saloonDoorRoutine = null;
        }
        doorAnimationState = DoorAnimationState.None;
    }

    private void ResetSaloonDoorsClosed()
    {
        StopSaloonDoorAnimation();
        if (titleDoorDelayRoutine != null)
        {
            StopCoroutine(titleDoorDelayRoutine);
            titleDoorDelayRoutine = null;
        }
        saloonDoorsAreOpen = false;
        SetSaloonDoorScale(1f);
        saloonDoorsRoot.SetActive(true);
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        float c3 = overshoot + 1f;
        float shifted = t - 1f;
        return 1f + c3 * shifted * shifted * shifted + overshoot * shifted * shifted;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private GameObject CreatePage(string pageName, out Transform content)
    {
        GameObject page = new GameObject(pageName, typeof(RectTransform));
        page.transform.SetParent(root, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CreatePanel("Poster shadow", rect, new Vector2(1510f, 970f), new Vector2(14f, -14f), new Color32(20, 13, 9, 150));
        CreatePanel("Poster frame", rect, new Vector2(1510f, 970f), Vector2.zero, new Color32(143, 94, 45, 255));
        RectTransform paper = CreatePanel("Poster paper", rect, new Vector2(1482f, 942f), Vector2.zero, new Color32(228, 205, 157, 255));
        CreatePanel("Inner border", paper, new Vector2(1440f, 900f), Vector2.zero, new Color32(183, 139, 77, 255));
        RectTransform innerPaper = CreatePanel("Inner paper", paper, new Vector2(1432f, 892f), Vector2.zero, new Color32(238, 220, 180, 255));
        content = innerPaper;
        return page;
    }

    private void BuildMainPage(Transform paper)
    {
        CreateText(paper, "Game title", "COWBOY\nSTANDOFF", titleFont, 132f, FontStyles.Bold,
            new Vector2(1160f, 310f), new Vector2(0f, 236f), new Color32(74, 43, 28, 255), 0f);
        if (titleDecorationSprite != null)
        {
            GameObject artObject = new GameObject("Cowboy Hat and Revolver Artwork", typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(paper, false);
            Image art = artObject.GetComponent<Image>();
            art.sprite = titleDecorationSprite;
            art.preserveAspect = true;
            art.raycastTarget = false;
            SetCenteredRect(art.rectTransform, new Vector2(300f, 160f), new Vector2(0f, -360f));
        }
        CreateWesternButton(paper, "Start Button", "START", new Vector2(500f, 116f),
            new Vector2(0f, -202f), ShowMatchSetup, true);
    }

    private void BuildSetupPages()
    {
        setupPages = new GameObject[5];
        setupPages[0] = CreateSetupPage("Mode Page", "MODE", out Transform modePaper);
        BuildModePage(modePaper);
        setupPages[1] = CreateSetupPage("Player Select Page", "PLAYER SELECT", out Transform playerPaper);
        BuildPlayerSelectPage(playerPaper);
        setupPages[2] = CreateSetupPage("Character Setup Page", "CHARACTER SETUP", out Transform colorPaper);
        BuildCharacterSetupPage(colorPaper);
        setupPages[3] = CreateSetupPage("Difficulty Page", "DIFFICULTY", out Transform difficultyPaper);
        BuildDifficultyPage(difficultyPaper);
        setupPages[4] = CreateSetupPage("Controls Page", "CONTROLS", out Transform controlsContent);
        BuildControlsPage(controlsContent);
    }

    private GameObject CreateSetupPage(string pageName, string heading, out Transform paper)
    {
        GameObject page = CreatePage(pageName, out paper);
        CreateText(paper, pageName + " Heading", heading, titleFont, 74f, FontStyles.Bold,
            new Vector2(1200f, 100f), new Vector2(0f, 340f), new Color32(74, 43, 28, 255), 1f);
        return page;
    }

    private void BuildModePage(Transform paper)
    {
        singlePlayerModeButton = CreateWesternButton(paper, "One Player Mode", "1 PLAYER (VS AI)", new Vector2(560f, 150f), new Vector2(-310f, 30f),
            () => SetSinglePlayerMode(true), true);
        twoPlayerModeButton = CreateWesternButton(paper, "Two Player Mode", "2 PLAYERS", new Vector2(560f, 150f), new Vector2(310f, 30f),
            () => SetSinglePlayerMode(false), true);
        CreateBottomCornerButton(paper, "Back Button", "BACK", new Vector2(280f, 76f), ShowMainMenu, false, false);
        CreateBottomCornerButton(paper, "Continue Button", "CONTINUE", new Vector2(380f, 82f), ContinueFromMode, true, true);
        SetSinglePlayerMode(true);
    }

    private void BuildPlayerSelectPage(Transform paper)
    {
        CreatePlayerChoiceCard(paper, true, new Vector2(-320f, 10f));
        CreatePlayerChoiceCard(paper, false, new Vector2(320f, 10f));
        CreateBottomCornerButton(paper, "Back Button", "BACK", new Vector2(280f, 76f), BackFromPlayerSelect, false, false);
        CreateBottomCornerButton(paper, "Continue Button", "CONTINUE", new Vector2(380f, 82f), ContinueFromPlayerSelect, true, true);
        SelectHumanPlayer(true);
    }

    private void CreatePlayerChoiceCard(Transform parent, bool playerOne, Vector2 position)
    {
        RectTransform card = CreatePanel(playerOne ? "Player One Choice Card" : "Player Two Choice Card", parent,
            new Vector2(540f, 490f), position, new Color32(183, 139, 77, 255));
        CreatePanel("Choice Card Paper", card, new Vector2(520f, 470f), Vector2.zero, new Color32(250, 238, 208, 255));
        CreateText(card, "Choice Player Name", playerOne ? "PLAYER 1" : "PLAYER 2", titleFont, 46f, FontStyles.Bold,
            new Vector2(480f, 64f), new Vector2(0f, 180f), new Color32(74, 43, 28, 255), 1f);
        AddPreviewFigure(card, playerOne, new Vector2(0f, -10f), new Vector2(220f, 300f));
        Button chooseButton = CreateWesternButton(card, "Choose Player", "CHOOSE", new Vector2(340f, 76f), new Vector2(0f, -185f),
            () => SelectHumanPlayer(playerOne), true);
        if (playerOne) playerChoiceOneButton = chooseButton; else playerChoiceTwoButton = chooseButton;
    }

    private void AddPreviewFigure(Transform parent, bool playerOne, Vector2 position, Vector2 size)
    {
        GameObject figure = new GameObject("Character Figure", typeof(RectTransform), typeof(Image));
        figure.transform.SetParent(parent, false);
        Image image = figure.GetComponent<Image>();
        image.sprite = characterPreviewSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = CharacterColorSettings.GetColor(playerOne);
        if (playerOne) playerChoiceOneImage = image; else playerChoiceTwoImage = image;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position + new Vector2(-14f, -22f);
        float previewScale = size.x / Mathf.Max(1f,
            characterPreviewSprite != null ? characterPreviewSprite.rect.width : PreviewBodyWidth);
        if (characterPreviewSprite != null)
        {
            image.SetNativeSize();
            rect.pivot = Vector2.Scale(characterPreviewSprite.pivot,
                new Vector2(1f / characterPreviewSprite.rect.width, 1f / characterPreviewSprite.rect.height));
        }
        else
        {
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
        }
        rect.localScale = new Vector3((playerOne ? 1f : -1f) * previewScale, previewScale, 1f);
        GameObject layers = CreatePreviewLayers(parent, playerOne ? playerOnePreviewLayers : playerTwoPreviewLayers,
            previewScale, size.x);
        layers.GetComponent<RectTransform>().anchoredPosition = position + new Vector2(-14f, -22f);
    }

    private void BuildCharacterSetupPage(Transform paper)
    {
        playerOneButton = CreateWesternButton(paper, "Player One Tab", "PLAYER 1", new Vector2(280f, 66f), new Vector2(-180f, 256f),
            () => SetEditingPlayer(true), false);
        playerTwoButton = CreateWesternButton(paper, "Player Two Tab", "PLAYER 2", new Vector2(280f, 66f), new Vector2(180f, 256f),
            () => SetEditingPlayer(false), false);

        RectTransform previewFrame = CreatePanel("Character Preview Frame", paper, new Vector2(340f, 450f), new Vector2(-390f, -10f), new Color32(143, 94, 45, 255));
        CreatePanel("Character Preview Paper", previewFrame, new Vector2(326f, 436f), Vector2.zero, new Color32(250, 238, 208, 255));
        GameObject preview = new GameObject("Character Preview", typeof(RectTransform), typeof(Image));
        preview.transform.SetParent(previewFrame, false);
        previewImage = preview.GetComponent<Image>();
        previewImage.sprite = characterPreviewSprite;
        previewImage.raycastTarget = false;
        previewImage.preserveAspect = true;
        RectTransform previewRect = preview.GetComponent<RectTransform>();
        previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(-14f, -22f);
        previewImage.SetNativeSize();
        if (characterPreviewSprite != null)
            previewRect.pivot = Vector2.Scale(characterPreviewSprite.pivot, new Vector2(1f / characterPreviewSprite.rect.width, 1f / characterPreviewSprite.rect.height));
        float previewScale = PreviewBodyWidth / Mathf.Max(1f, characterPreviewSprite != null ? characterPreviewSprite.rect.width : PreviewBodyWidth);
        previewRect.localScale = new Vector3(previewScale, previewScale, 1f);
        playerOneOverlayRoot = CreatePreviewLayers(previewFrame, playerOnePreviewLayers, previewScale);
        playerTwoOverlayRoot = CreatePreviewLayers(previewFrame, playerTwoPreviewLayers, previewScale);
        playerOneOverlayRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(-14f, -22f);
        playerTwoOverlayRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(-14f, -22f);

        previewPlayerLabel = CreateText(paper, "Preview Selected Player", "PLAYER 1 - DUSTY BLUE", bodyFont, 20f, FontStyles.Bold,
            new Vector2(440f, 42f), new Vector2(-390f, -260f), new Color32(91, 57, 37, 255), 0.2f);

        colorButtons = new Button[CharacterColorSettings.PaletteCount];
        colorButtonLabels = new TMP_Text[colorButtons.Length];
        float[] colorX = { 0f, 245f, 490f };
        float[] colorY = { 100f, -100f };
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int colorIndex = i;
            colorButtons[i] = CreateColorButton(paper, colorIndex,
                new Vector2(colorX[i % 3] + 80f, colorY[i / 3]));
            colorButtonLabels[i] = colorButtons[i].GetComponentInChildren<TMP_Text>();
        }
        CreateBottomCornerButton(paper, "Back Button", "BACK", new Vector2(280f, 76f), BackFromColors, false, false);
        CreateBottomCornerButton(paper, "Continue Button", "CONTINUE", new Vector2(380f, 82f), ContinueFromColors, true, true);
        UpdatePlayerSelection();
        UpdateColorSelection();
    }

    private void BuildDifficultyPage(Transform paper)
    {
        difficultyButtons = new Button[3];
        difficultyButtonLabels = new TMP_Text[3];
        string[] names = { "EASY", "MEDIUM", "HARD" };
        Difficulty[] values = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            difficultyButtons[i] = CreateWesternButton(paper, names[i] + " Difficulty", names[i], new Vector2(520f, 105f),
                new Vector2(0f, 150f - i * 140f), () => SelectDifficulty(values[index]), true);
            difficultyButtonLabels[i] = difficultyButtons[i].GetComponentInChildren<TMP_Text>();
        }
        CreateBottomCornerButton(paper, "Back Button", "BACK", new Vector2(280f, 76f), BackFromDifficulty, false, false);
        CreateBottomCornerButton(paper, "Continue Button", "CONTINUE", new Vector2(380f, 82f), () => GoToStep(4), true, true);
    }

    private void BuildControlsPage(Transform paper)
    {
        controlsPaper = paper;
        CreateBottomCornerButton(paper, "Back Button", "BACK", new Vector2(280f, 76f), BackFromControls, false, false);
        CreateBottomCornerButton(paper, "Begin Duel Button", "BEGIN DUEL", new Vector2(430f, 92f), BeginDuel, true, true);
    }

    private Button CreateBottomCornerButton(Transform parent, string objectName, string label, Vector2 size,
        UnityEngine.Events.UnityAction action, bool prominent, bool rightSide)
    {
        Button button = CreateWesternButton(parent, objectName, label, size, Vector2.zero, action, prominent);
        RectTransform rect = button.GetComponent<RectTransform>();
        Vector2 corner = rightSide ? new Vector2(1f, 0f) : Vector2.zero;
        rect.anchorMin = rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = rightSide ? new Vector2(-56f, 56f) : new Vector2(56f, 56f);
        return button;
    }

    private static GameObject CreateGroup(string groupName, Transform parent)
    {
        GameObject group = new GameObject(groupName, typeof(RectTransform));
        group.transform.SetParent(parent, false);
        RectTransform rect = group.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return group;
    }

    private GameObject CreatePreviewLayers(Transform parent, PreviewLayer[] layers, float previewScale,
        float previewBodyWidth = PreviewBodyWidth)
    {
        GameObject layerRoot = new GameObject("Outfit Preview Layers", typeof(RectTransform));
        layerRoot.transform.SetParent(parent, false);
        RectTransform rootRect = layerRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;

        if (layers == null)
            return layerRoot;

        float uiUnitsPerWorldUnit = previewBodyWidth / PreviewBodyWorldWidth;
        foreach (PreviewLayer layer in layers)
        {
            if (layer == null || layer.sprite == null)
                continue;

            GameObject layerObject = new GameObject(layer.sprite.name + " Preview", typeof(RectTransform), typeof(Image));
            layerObject.transform.SetParent(layerRoot.transform, false);
            Image image = layerObject.GetComponent<Image>();
            image.sprite = layer.sprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.color = Color.white;
            image.SetNativeSize();

            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.Scale(layer.sprite.pivot, new Vector2(1f / layer.sprite.rect.width, 1f / layer.sprite.rect.height));
            rect.anchoredPosition = layer.localPosition * uiUnitsPerWorldUnit;
            float xSign = layer.flipX ? -1f : 1f;
            rect.localScale = new Vector3(layer.localScale.x * previewScale * xSign, layer.localScale.y * previewScale, 1f);
            rect.localRotation = Quaternion.Euler(layer.localEulerAngles);
        }

        return layerRoot;
    }

    private Button CreateColorButton(Transform parent, int index, Vector2 position)
    {
        RectTransform rect = CreatePanel("Color Swatch " + index, parent, new Vector2(220f, 104f), position, new Color32(115, 72, 43, 255));
        Image background = rect.GetComponent<Image>();
        background.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => SelectColor(index));
        ConfigureButtonColors(button, new Color32(115, 72, 43, 255), new Color32(183, 139, 77, 255));

        RectTransform swatch = CreatePanel("Preset Colour", rect, new Vector2(202f, 56f), new Vector2(0f, 16f), CharacterColorSettings.GetColorAt(index));
        swatch.GetComponent<Image>().raycastTarget = false;
        CreateText(rect, "Color Name", CharacterColorSettings.GetColorNameAt(index), bodyFont, 15f, FontStyles.Bold,
            new Vector2(208f, 32f), new Vector2(0f, -34f), new Color32(250, 229, 186, 255), 0f);
        return button;
    }

    private Button CreateWesternButton(Transform parent, string objectName, string label,
        Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action, bool prominent)
    {
        Color32 brown = prominent ? new Color32(106, 56, 34, 255) : new Color32(115, 72, 43, 255);
        RectTransform rect = CreatePanel(objectName, parent, size, position, brown);
        Image background = rect.GetComponent<Image>();
        background.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        ConfigureButtonColors(button, brown, new Color32(183, 139, 77, 255));

        RectTransform keyline = CreatePanel("Button keyline", rect, size - new Vector2(16f, 16f), Vector2.zero, new Color32(220, 176, 91, 255));
        keyline.SetAsFirstSibling();
        RectTransform face = CreatePanel("Button face", rect, size - new Vector2(26f, 26f), Vector2.zero, brown);
        face.SetAsFirstSibling();
        face.GetComponent<Image>().raycastTarget = true;
        CreateText(rect, "Button label", label, prominent ? titleFont : bodyFont, prominent ? 38f : 24f,
            FontStyles.Bold, size - new Vector2(34f, 28f), Vector2.zero, new Color32(250, 229, 186, 255), prominent ? 1f : 0.5f);
        button.targetGraphic = face.GetComponent<Image>();
        return button;
    }

    private static void ConfigureButtonColors(Button button, Color normal, Color selected)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = selected;
        colors.pressedColor = new Color32(73, 39, 27, 255);
        colors.selectedColor = selected;
        colors.disabledColor = new Color32(90, 75, 60, 180);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
    }

    private void SetEditingPlayer(bool playerOne)
    {
        editingPlayerOne = playerOne;
        UpdatePlayerSelection();
        UpdateColorSelection();
    }

    private void SetSinglePlayerMode(bool singlePlayer)
    {
        singlePlayerMode = singlePlayer;
        SetSelectedStyle(singlePlayerModeButton, singlePlayerMode, new Color32(115, 72, 43, 255));
        SetSelectedStyle(twoPlayerModeButton, !singlePlayerMode, new Color32(115, 72, 43, 255));
    }

    private void SelectHumanPlayer(bool playerOne)
    {
        playingAsPlayerOne = playerOne;
        SetSelectedStyle(playerChoiceOneButton, playingAsPlayerOne, new Color32(115, 72, 43, 255));
        SetSelectedStyle(playerChoiceTwoButton, !playingAsPlayerOne, new Color32(115, 72, 43, 255));
    }

    private void UpdatePlayerSelection()
    {
        SetSelectedStyle(playerOneButton, editingPlayerOne, new Color32(115, 72, 43, 255));
        SetSelectedStyle(playerTwoButton, !editingPlayerOne, new Color32(115, 72, 43, 255));
    }

    private void SelectColor(int index)
    {
        CharacterColorSettings.SetColor(editingPlayerOne, index);
        UpdateColorSelection();
    }

    private void UpdateColorSelection()
    {
        int selectedIndex = CharacterColorSettings.GetColorIndex(editingPlayerOne);
        if (previewImage != null)
        {
            previewImage.color = CharacterColorSettings.GetColor(editingPlayerOne);
            float previewScale = PreviewBodyWidth / Mathf.Max(1f, characterPreviewSprite != null ? characterPreviewSprite.rect.width : PreviewBodyWidth);
            float xSign = editingPlayerOne ? 1f : -1f;
            previewImage.rectTransform.localScale = new Vector3(previewScale * xSign, previewScale, 1f);
        }
        if (previewPlayerLabel != null)
        {
            string playerName = editingPlayerOne ? "PLAYER 1" : "PLAYER 2";
            previewPlayerLabel.text = playerName + " - " + CharacterColorSettings.GetColorNameAt(selectedIndex);
        }
        if (playerOneOverlayRoot != null)
            playerOneOverlayRoot.SetActive(editingPlayerOne);
        if (playerTwoOverlayRoot != null)
            playerTwoOverlayRoot.SetActive(!editingPlayerOne);

        if (colorButtons == null)
            return;
        for (int i = 0; i < colorButtons.Length; i++)
        {
            bool selected = i == selectedIndex;
            SetSelectedStyle(colorButtons[i], selected, new Color32(115, 72, 43, 255));
            if (colorButtonLabels[i] != null)
                colorButtonLabels[i].color = selected ? new Color32(31, 69, 76, 255) : new Color32(250, 229, 186, 255);
        }
        if (playerChoiceOneImage != null) playerChoiceOneImage.color = CharacterColorSettings.GetColor(true);
        if (playerChoiceTwoImage != null) playerChoiceTwoImage.color = CharacterColorSettings.GetColor(false);
    }

    private void SelectDifficulty(Difficulty difficulty)
    {
        PauseMenuUI.RememberMenuDifficulty(difficulty);
        UpdateDifficultySelection();
    }

    private void UpdateDifficultySelection()
    {
        if (difficultyButtons == null)
            return;

        Difficulty[] values = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        for (int i = 0; i < values.Length; i++)
        {
            bool selected = values[i] == GameSettings.AIDifficulty;
            SetSelectedStyle(difficultyButtons[i], selected, new Color32(115, 72, 43, 255));
            if (difficultyButtonLabels[i] != null)
                difficultyButtonLabels[i].color = selected ? new Color32(65, 42, 27, 255) : new Color32(250, 229, 186, 255);
        }
    }

    private void SetSelectedStyle(Button button, bool selected, Color32 normal)
    {
        if (button == null)
            return;
        Color highlighted = selected ? new Color32(202, 158, 91, 255) : new Color32(149, 94, 49, 255);
        ColorBlock colors = button.colors;
        colors.normalColor = selected ? new Color32(183, 139, 77, 255) : normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        button.colors = colors;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.color = selected ? new Color32(65, 42, 27, 255) : new Color32(250, 229, 186, 255);
    }

    private void ShowMatchSetup()
    {
        mainPage.SetActive(false);
        SetActiveSetupStep(0);
        UpdatePlayerSelection();
        UpdateColorSelection();
        UpdateDifficultySelection();
        saloonDoorsRoot.SetActive(false);
    }

    private void ShowMainMenu()
    {
        SetActiveSetupStep(-1);
        mainPage.SetActive(true);
        ResetSaloonDoorsClosed();
        titleDoorDelayRoutine = StartCoroutine(OpenTitleDoorsAfterDelay());
        SelectFirstButton(mainPage.transform);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.enterKey.wasPressedThisFrame)
            return;
        if (mainPage.activeSelf)
            ShowMatchSetup();
        else
            AdvanceSetupStep();
    }

    private void SetActiveSetupStep(int step)
    {
        setupStep = step;
        if (setupPages == null) return;
        for (int i = 0; i < setupPages.Length; i++)
            setupPages[i].SetActive(i == step);
        if (step >= 0)
        {
            if (step == 4) RefreshControlsDisplay(singlePlayerMode);
            SelectFirstButton(setupPages[step].transform);
        }
    }

    private void GoToStep(int step)
    {
        if (doorAnimationState == DoorAnimationState.Closing)
        {
            StopSaloonDoorAnimation();
            saloonDoorsAreOpen = true;
            saloonDoorsRoot.SetActive(false);
        }
        if (step == 3 && !singlePlayerMode) step = 4;
        SetActiveSetupStep(step);
    }

    private void ContinueFromMode() => GoToStep(singlePlayerMode ? 1 : 2);
    private void ContinueFromPlayerSelect()
    {
        SetEditingPlayer(playingAsPlayerOne);
        GoToStep(2);
    }
    private void ContinueFromColors() => GoToStep(singlePlayerMode ? 3 : 4);
    private void BackFromPlayerSelect() => GoToStep(0);
    private void BackFromColors() => GoToStep(singlePlayerMode ? 1 : 0);
    private void BackFromDifficulty() => GoToStep(2);
    private void BackFromControls() => GoToStep(singlePlayerMode ? 3 : 2);

    private void AdvanceSetupStep()
    {
        switch (setupStep)
        {
            case 0: ContinueFromMode(); break;
            case 1: ContinueFromPlayerSelect(); break;
            case 2: ContinueFromColors(); break;
            case 3: GoToStep(4); break;
            case 4: BeginDuel(); break;
        }
    }

    private void RefreshControlsDisplay(bool singlePlayer)
    {
        if (controlsDisplayRoot != null) Destroy(controlsDisplayRoot);
        controlsDisplayRoot = new GameObject("Controls Display", typeof(RectTransform));
        controlsDisplayRoot.transform.SetParent(controlsPaper, false);
        RectTransform rootRect = controlsDisplayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;
        if (singlePlayer)
            BuildSinglePlayerControls(controlsDisplayRoot.transform);
        else
            BuildTwoPlayerControls(controlsDisplayRoot.transform);
    }

    private void BuildSinglePlayerControls(Transform parent)
    {
        CreateText(parent, "Movement Label", "MOVE  •  JUMP  •  DUCK", bodyFont, 30f, FontStyles.Bold,
            new Vector2(680f, 48f), new Vector2(-300f, 210f), new Color32(74, 43, 28, 255), 0.5f);
        CreateArrowCluster(parent, new Vector2(-300f, 60f));
        CreateText(parent, "Arrow Control Legend", "UP = JUMP     LEFT / RIGHT = MOVE     DOWN = DUCK", bodyFont, 18f, FontStyles.Bold,
            new Vector2(800f, 40f), new Vector2(-300f, -95f), new Color32(74, 43, 28, 255), 0.1f);
        CreateText(parent, "Shoot Label", "SHOOT", bodyFont, 30f, FontStyles.Bold,
            new Vector2(680f, 48f), new Vector2(300f, 210f), new Color32(74, 43, 28, 255), 0.5f);
        CreateKeycap(parent, "SPACE", new Vector2(300f, 80f), new Vector2(220f, 90f));
        CreateText(parent, "Or Label", "OR", bodyFont, 24f, FontStyles.Bold,
            new Vector2(100f, 38f), new Vector2(300f, -25f), new Color32(117, 77, 43, 255), 0.5f);
        CreateKeycap(parent, "MOUSE CLICK", new Vector2(300f, -130f), new Vector2(300f, 90f));
    }

    private void BuildTwoPlayerControls(Transform parent)
    {
        CreateText(parent, "Player One Controls", "PLAYER 1", titleFont, 44f, FontStyles.Bold,
            new Vector2(560f, 60f), new Vector2(-360f, 220f), new Color32(74, 43, 28, 255), 0.5f);
        CreateText(parent, "Player Two Controls", "PLAYER 2", titleFont, 44f, FontStyles.Bold,
            new Vector2(560f, 60f), new Vector2(360f, 220f), new Color32(74, 43, 28, 255), 0.5f);
        CreateWasdCluster(parent, new Vector2(-360f, 45f));
        CreateArrowCluster(parent, new Vector2(360f, 45f));
        CreateText(parent, "Player One Key Legend", "W JUMPS   A / D MOVE   S DUCK", bodyFont, 19f, FontStyles.Bold,
            new Vector2(600f, 38f), new Vector2(-360f, -70f), new Color32(74, 43, 28, 255), 0.1f);
        CreateText(parent, "Player Two Key Legend", "UP JUMPS   LEFT / RIGHT MOVE   DOWN DUCK", bodyFont, 17f, FontStyles.Bold,
            new Vector2(660f, 38f), new Vector2(360f, -70f), new Color32(74, 43, 28, 255), 0.1f);
        CreateKeycap(parent, "SPACE", new Vector2(-360f, -165f), new Vector2(230f, 84f));
        CreateKeycap(parent, "MOUSE CLICK", new Vector2(360f, -165f), new Vector2(270f, 84f));
    }

    private void CreateWasdCluster(Transform parent, Vector2 center)
    {
        CreateKeycap(parent, "W", center + new Vector2(0f, 88f), new Vector2(76f, 76f));
        CreateKeycap(parent, "A", center + new Vector2(-84f, 0f), new Vector2(76f, 76f));
        CreateKeycap(parent, "S", center + new Vector2(0f, 0f), new Vector2(76f, 76f));
        CreateKeycap(parent, "D", center + new Vector2(84f, 0f), new Vector2(76f, 76f));
    }

    private void CreateArrowCluster(Transform parent, Vector2 center)
    {
        CreateArrowKeycap(parent, center + new Vector2(0f, 88f), 0);
        CreateArrowKeycap(parent, center + new Vector2(-84f, 0f), 1);
        CreateArrowKeycap(parent, center + new Vector2(0f, 0f), 2);
        CreateArrowKeycap(parent, center + new Vector2(84f, 0f), 3);
    }

    private void CreateKeycap(Transform parent, string label, Vector2 position, Vector2 size)
    {
        RectTransform cap = CreatePanel("Keycap " + label, parent, size, position, new Color32(70, 47, 34, 255));
        CreatePanel("Keycap Rim", cap, size - new Vector2(10f, 10f), Vector2.zero, new Color32(183, 139, 77, 255)).SetAsFirstSibling();
        CreatePanel("Keycap Face", cap, size - new Vector2(18f, 18f), Vector2.zero, new Color32(70, 47, 34, 255));
        CreateText(cap, "Key Label", label, bodyFont, label.Length > 4 ? 22f : 38f, FontStyles.Bold,
            size - new Vector2(16f, 16f), Vector2.zero, new Color32(250, 229, 186, 255), 0.2f);
    }

    private void CreateArrowKeycap(Transform parent, Vector2 position, int direction)
    {
        RectTransform cap = CreatePanel("Arrow Keycap", parent, new Vector2(76f, 76f), position, new Color32(70, 47, 34, 255));
        CreatePanel("Keycap Rim", cap, new Vector2(66f, 66f), Vector2.zero, new Color32(183, 139, 77, 255)).SetAsFirstSibling();
        CreatePanel("Keycap Face", cap, new Vector2(58f, 58f), Vector2.zero, new Color32(70, 47, 34, 255));
        Transform arrow = new GameObject("Arrow Shape", typeof(RectTransform)).transform;
        arrow.SetParent(cap, false);
        arrow.localRotation = Quaternion.Euler(0f, 0f, direction * 90f);
        Color arrowColor = new Color32(250, 229, 186, 255);
        CreatePanel("Arrow Shaft", arrow, new Vector2(12f, 32f), new Vector2(0f, -2f), arrowColor);
        RectTransform leftWing = CreatePanel("Arrow Head Left", arrow, new Vector2(10f, 20f), new Vector2(-7f, 10f), arrowColor);
        leftWing.localRotation = Quaternion.Euler(0f, 0f, -42f);
        RectTransform rightWing = CreatePanel("Arrow Head Right", arrow, new Vector2(10f, 20f), new Vector2(7f, 10f), arrowColor);
        rightWing.localRotation = Quaternion.Euler(0f, 0f, 42f);
    }

    private void BeginDuel()
    {
        if (doorAnimationState == DoorAnimationState.Closing)
        {
            StopSaloonDoorAnimation();
            LoadGameplayScene();
            return;
        }

        GameSettings.FromMenu = true;
        if (singlePlayerMode)
        {
            GameSettings.Player1IsAI = !playingAsPlayerOne;
            GameSettings.Player2IsAI = playingAsPlayerOne;
        }
        else
        {
            GameSettings.Player1IsAI = false;
            GameSettings.Player2IsAI = false;
        }

        Time.timeScale = 1f;
        CharacterColorSettings.Save();
        PauseMenuUI.RememberMenuDifficulty(GameSettings.AIDifficulty);

        if (doorAnimationState == DoorAnimationState.Opening)
            StopSaloonDoorAnimation();

        if (saloonDoorsAreOpen || saloonDoorsRoot.activeSelf)
            StartSaloonDoorAnimation(false, true);
        else
            LoadGameplayScene();
    }

    private void LoadGameplayScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    private static void SelectFirstButton(Transform page)
    {
        Button firstButton = page.GetComponentInChildren<Button>();
        if (firstButton != null)
            EventSystem.current?.SetSelectedGameObject(firstButton.gameObject);
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, Vector2 size, Vector2 position, Color color,
        bool stretch = false, bool fullWidth = false)
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
        else if (fullWidth)
        {
            float anchor = position.y < 0f ? 1f : 0f;
            rect.anchorMin = new Vector2(0f, anchor);
            rect.anchorMax = new Vector2(1f, anchor);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
        return rect;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, string value, TMP_FontAsset font,
        float fontSize, FontStyles style, Vector2 size, Vector2 position, Color color, float characterSpacing)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

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

    private static void CreateDiamond(Transform parent, Vector2 position, float size, Color color)
    {
        RectTransform diamond = CreatePanel("Poster ornament", parent, new Vector2(size, size), position, color);
        diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }
}
