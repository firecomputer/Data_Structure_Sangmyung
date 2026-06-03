using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;
using AlgorithmOfDelivery.Maze;
using AlgorithmOfDelivery.UI;

public class GameSetup : MonoBehaviour
{
    public static GameSetup Instance { get; private set; }

    [Header("Start Screen")]
    [SerializeField] private Texture2D _seaScreenTexture;

    private readonly Dictionary<string, Sprite> _roundedSpriteCache = new Dictionary<string, Sprite>();
    private Texture2D _generatedSeaTexture;
    private bool _gameStarted;
    private bool _dayPrepReady;
    private StartMenuUI _startMenuUI;
    private IntroSequenceUI _introSequenceUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CreateGameSystems();
        CreateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool GameStarted => _gameStarted;
    public bool DayPrepReady => _dayPrepReady;

    public void BeginGame()
    {
        if (_gameStarted)
            return;

        _gameStarted = true;
        _dayPrepReady = false;

        if (_startMenuUI != null)
            _startMenuUI.Hide();

        if (_introSequenceUI != null)
        {
            _introSequenceUI.Play(OnIntroSequenceComplete);
            Debug.Log("[GameSetup] Start menu dismissed. Intro sequence started.");
        }
        else
        {
            Debug.LogWarning("[GameSetup] Intro sequence UI missing, skipping intro.");
            OnIntroSequenceComplete();
        }
    }

    public void HideIntroSequence()
    {
        if (_introSequenceUI != null)
            _introSequenceUI.Hide();
    }

    private void OnIntroSequenceComplete()
    {
        _dayPrepReady = true;
        Debug.Log("[GameSetup] Intro sequence finished. Day prep is ready.");
    }

    private void CreateGameSystems()
    {
        if (FindObjectOfType<DeliveryManager>() == null)
        {
            Debug.LogError("[GameSetup] DeliveryManager not found in scene!");
            return;
        }

        if (FindObjectOfType<CourierManager>() == null)
            new GameObject("CourierManager").AddComponent<CourierManager>();

        if (FindObjectOfType<DeliveryQueue>() == null)
            new GameObject("DeliveryQueue").AddComponent<DeliveryQueue>();

        if (FindObjectOfType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        if (FindObjectOfType<PlanningManager>() == null)
            new GameObject("PlanningManager").AddComponent<PlanningManager>();

        if (FindObjectOfType<NotificationManager>() == null)
            new GameObject("NotificationManager").AddComponent<NotificationManager>();
    }

    private void CreateUI()
    {
        EnsureEventSystem();

        Canvas canvas = EnsureCanvas();
        if (canvas == null)
            return;

        foreach (Transform child in canvas.transform)
            Destroy(child.gameObject);

        CreateTopStatusBars(canvas.transform);
        DayPrepUI prepUI = CreateDayPrepUI(canvas.transform);
        InGameBottomBar bottomBar = CreateBottomBar(canvas.transform);
        NotificationUI notificationUI = CreateNotificationPanel(canvas.transform);
        MailInboxUI mailInboxUI = CreateMailInboxPanel(canvas.transform);

        CreateBottomRightButtons(canvas.transform, notificationUI, mailInboxUI);
        _startMenuUI = CreateStartMenu(canvas.transform);
        _introSequenceUI = CreateIntroSequence(canvas.transform);

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.PrepUI = prepUI;
            Debug.Log($"[GameSetup] Injected DayPrepUI into GameManager (prepUI={gm.PrepUI != null})");
        }

        if (bottomBar != null)
        {
            var camCtrl = FindObjectOfType<CameraController>();
            bottomBar.OnPortraitDoubleClicked += idx =>
            {
                var cm = CourierManager.Instance;
                if (cm != null && idx >= 0 && idx < cm.ActiveControllers.Count)
                {
                    var ctrl = cm.ActiveControllers[idx];
                    if (ctrl != null && camCtrl != null)
                        camCtrl.ZoomTo(ctrl.CurrentPosition, 0.5f);
                }
            };
        }
    }

    private void CreateTopStatusBars(Transform parent)
    {
        CreateStatusBar(parent, new Vector2(-20f, -20f), Color.black, new Color(0.74f, 0.91f, 0.63f, 1f));
    }

    private TopStatusBarUI CreateStatusBar(Transform parent, Vector2 anchoredPosition, Color textColor, Color fillColor)
    {
        RectTransform panel = CreateRoundedPanel(
            "StatusBar",
            parent,
            anchoredPosition,
            new Vector2(900f, 160f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            fillColor,
            new Color(Mathf.Min(1f, fillColor.r + 0.05f), Mathf.Min(1f, fillColor.g + 0.05f), Mathf.Min(1f, fillColor.b + 0.05f), fillColor.a),
            42,
            8);

        Text fastForward = CreateText(
            "FastForwardIcon",
            panel,
            new Vector2(-360f, 0f),
            new Vector2(50f, 60f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            46,
            textColor,
            TextAnchor.MiddleCenter,
            LoadSymbolFont(46));
        fastForward.text = "≫";
        fastForward.fontStyle = FontStyle.Bold;

        Text play = CreateText(
            "PlayIcon",
            panel,
            new Vector2(-280f, 0f),
            new Vector2(50f, 60f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            46,
            textColor,
            TextAnchor.MiddleCenter,
            LoadSymbolFont(46));
        play.text = "▶";
        play.fontStyle = FontStyle.Bold;

        Text pause = CreateText(
            "PauseIcon",
            panel,
            new Vector2(-205f, 0f),
            new Vector2(50f, 60f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            46,
            textColor,
            TextAnchor.MiddleCenter,
            LoadSymbolFont(46));
        pause.text = "‖";
        pause.fontStyle = FontStyle.Bold;

        Text moneyIcon = CreateText(
            "MoneyIcon",
            panel,
            new Vector2(-130f, 0f),
            new Vector2(24f, 40f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            textColor,
            TextAnchor.MiddleCenter,
            LoadSymbolFont(30));
        moneyIcon.text = "$";
        moneyIcon.fontStyle = FontStyle.Bold;

        Text moneyValue = CreateText(
            "MoneyValue",
            panel,
            new Vector2(-55f, 0f),
            new Vector2(120f, 40f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            textColor,
            TextAnchor.MiddleLeft);
        moneyValue.fontStyle = FontStyle.Bold;

        Text timeIcon = CreateText(
            "TimeIcon",
            panel,
            new Vector2(80f, 0f),
            new Vector2(24f, 40f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            textColor,
            TextAnchor.MiddleCenter,
            LoadSymbolFont(30));
        timeIcon.text = "◷";
        timeIcon.fontStyle = FontStyle.Bold;

        Text timeValue = CreateText(
            "TimeValue",
            panel,
            new Vector2(155f, 0f),
            new Vector2(120f, 40f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            textColor,
            TextAnchor.MiddleLeft);
        timeValue.fontStyle = FontStyle.Bold;

        Text dayIcon = CreateText(
            "DayIcon",
            panel,
            new Vector2(300f, 0f),
            new Vector2(24f, 40f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            textColor,
            TextAnchor.MiddleCenter,
            LoadSymbolFont(30));
        dayIcon.text = "☑";
        dayIcon.fontStyle = FontStyle.Bold;

        Text dayValue = CreateText(
            "DayValue",
            panel,
            new Vector2(375f, 0f),
            new Vector2(120f, 40f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            textColor,
            TextAnchor.MiddleLeft);
        dayValue.fontStyle = FontStyle.Bold;

        TopStatusBarUI bar = panel.gameObject.AddComponent<TopStatusBarUI>();
        bar.Configure(moneyValue, timeValue, dayValue);
        return bar;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    private Canvas EnsureCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private DayPrepUI CreateDayPrepUI(Transform parent)
    {
        RectTransform panel = CreateRoundedPanel(
            "DayPrepPanel",
            parent,
            new Vector2(20f, -20f),
            new Vector2(760f, 960f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Color(0.96f, 0.89f, 0.56f, 1f),
            new Color(0.97f, 0.91f, 0.60f, 1f),
            44,
            10);

        DayPrepUI prepUI = panel.gameObject.AddComponent<DayPrepUI>();

        RectTransform titlePill = CreateRoundedPanel(
            "TitlePill",
            panel,
            new Vector2(0f, 395f),
            new Vector2(300f, 106f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Color(1f, 0.55f, 0.63f, 1f),
            new Color(1f, 0.62f, 0.70f, 1f),
            32,
            0);

        Text titleText = CreateText(
            "TitleText",
            titlePill,
            Vector2.zero,
            new Vector2(280f, 80f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            Color.white,
            TextAnchor.MiddleCenter);
        titleText.text = "하루준비";
        titleText.fontStyle = FontStyle.Bold;

        GameObject courierListRootGo = new GameObject("CourierListRoot", typeof(RectTransform));
        RectTransform courierListRoot = courierListRootGo.GetComponent<RectTransform>();
        courierListRoot.SetParent(panel, false);
        courierListRoot.anchorMin = new Vector2(0.5f, 0.5f);
        courierListRoot.anchorMax = new Vector2(0.5f, 0.5f);
        courierListRoot.pivot = new Vector2(0.5f, 0.5f);
        courierListRoot.anchoredPosition = new Vector2(0f, 40f);
        courierListRoot.sizeDelta = new Vector2(700f, 240f);

        Button[] courierSlotButtons = new Button[4];
        Image[] courierPortraits = new Image[4];
        Text[] courierNameTexts = new Text[4];
        Text[] courierTypeTexts = new Text[4];

        float[] courierPositions = { -240f, -80f, 80f, 240f };
        for (int i = 0; i < 4; i++)
        {
            RectTransform card = CreateRoundedPanel(
                $"CourierCard_{i}",
                courierListRoot,
                new Vector2(courierPositions[i], 0f),
                new Vector2(150f, 210f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Color(0.80f, 0.94f, 0.69f, 1f),
                new Color(0.70f, 0.89f, 0.55f, 1f),
                32,
                6);

            Button cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = card.GetComponent<Image>();
            cardButton.transition = Selectable.Transition.None;

            int idx = i;
            cardButton.onClick.AddListener(() => prepUI.OnCourierClicked(idx));

            RectTransform portraitFrame = CreateRoundedPanel(
                $"CourierPortraitFrame_{i}",
                card,
                new Vector2(0f, 32f),
                new Vector2(122f, 122f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Color.white,
                Color.white,
                26,
                0);
            portraitFrame.GetComponent<Image>().raycastTarget = false;

            GameObject portraitGo = new GameObject($"CourierPortrait_{i}", typeof(RectTransform), typeof(Image));
            RectTransform portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.SetParent(portraitFrame, false);
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitRect.sizeDelta = new Vector2(116f, 116f);

            Image portrait = portraitGo.GetComponent<Image>();
            portrait.color = Color.white;
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;

            Text nameText = CreateText(
                $"CourierName_{i}",
                card,
                new Vector2(0f, -42f),
                new Vector2(130f, 26f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                16,
                Color.black,
                TextAnchor.MiddleCenter);
            nameText.fontStyle = FontStyle.Bold;

            Text typeText = CreateText(
                $"CourierType_{i}",
                card,
                new Vector2(0f, -78f),
                new Vector2(130f, 34f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                13,
                Color.black,
                TextAnchor.MiddleCenter);
            typeText.horizontalOverflow = HorizontalWrapMode.Wrap;

            courierSlotButtons[i] = cardButton;
            courierPortraits[i] = portrait;
            courierNameTexts[i] = nameText;
            courierTypeTexts[i] = typeText;
        }

        Button recruitBtn = CreateButton(
            "RecruitBtn",
            panel,
            new Vector2(-220f, -330f),
            new Vector2(180f, 70f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "모집",
            new Color(1f, 0.37f, 0.44f, 1f),
            new Color(1f, 0.56f, 0.60f, 1f),
            30,
            4);

        Button startBtn = CreateButton(
            "StartBtn",
            panel,
            new Vector2(0f, -330f),
            new Vector2(180f, 70f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "시작",
            new Color(1f, 0.37f, 0.44f, 1f),
            new Color(1f, 0.56f, 0.60f, 1f),
            30,
            4);

        Button planBtn = CreateButton(
            "PlanBtn",
            panel,
            new Vector2(220f, -330f),
            new Vector2(180f, 70f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "계획",
            new Color(1f, 0.37f, 0.44f, 1f),
            new Color(1f, 0.56f, 0.60f, 1f),
            30,
            4);

        Button completeBtn = CreateButton(
            "CompleteBtn",
            panel,
            new Vector2(0f, -430f),
            new Vector2(170f, 70f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "완료",
            new Color(1f, 0.37f, 0.44f, 1f),
            new Color(1f, 0.56f, 0.60f, 1f),
            30,
            4);
        completeBtn.gameObject.SetActive(false);

        RectTransform dayResultPanel = CreateRoundedPanel(
            "DayResultPanel",
            panel,
            Vector2.zero,
            new Vector2(760f, 960f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Color(0f, 0f, 0f, 0.84f),
            new Color(0f, 0f, 0f, 0.84f),
            0,
            0);

        Text dayResultText = CreateText(
            "DayResultText",
            dayResultPanel,
            new Vector2(0f, 70f),
            new Vector2(420f, 160f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            26,
            Color.white,
            TextAnchor.MiddleCenter);
        dayResultText.fontStyle = FontStyle.Bold;

        Button continueBtn = CreateButton(
            "ContinueBtn",
            dayResultPanel,
            new Vector2(0f, -35f),
            new Vector2(160f, 60f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "계속",
            new Color(1f, 0.37f, 0.44f, 1f),
            new Color(1f, 0.56f, 0.60f, 1f),
            28,
            4);

        var dayResultPanelGo = dayResultPanel.gameObject;
        dayResultPanelGo.SetActive(false);

        SetPrivateField(prepUI, "_panel", panel.gameObject);
        SetPrivateField(prepUI, "_courierSlotButtons", courierSlotButtons);
        SetPrivateField(prepUI, "_courierPortraits", courierPortraits);
        SetPrivateField(prepUI, "_courierNameTexts", courierNameTexts);
        SetPrivateField(prepUI, "_courierTypeTexts", courierTypeTexts);
        SetPrivateField(prepUI, "_recruitButton", recruitBtn);
        SetPrivateField(prepUI, "_startButton", startBtn);
        SetPrivateField(prepUI, "_planButton", planBtn);
        SetPrivateField(prepUI, "_dayResultPanel", dayResultPanelGo);
        SetPrivateField(prepUI, "_dayResultText", dayResultText);
        SetPrivateField(prepUI, "_dayResultContinueButton", continueBtn);
        prepUI.LateInit();

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
            SetPrivateField(gm, "_planDoneButton", completeBtn);

        panel.gameObject.SetActive(false);
        return prepUI;
    }

    private InGameBottomBar CreateBottomBar(Transform parent)
    {
        GameObject root = new GameObject("BottomBar", typeof(RectTransform));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, -20f);
        rootRect.sizeDelta = new Vector2(1600f, 320f);

        InGameBottomBar bar = root.AddComponent<InGameBottomBar>();

        Button[] slotButtons = new Button[4];
        RectTransform[] slotRects = new RectTransform[4];
        Image[] portraitImages = new Image[4];
        RectTransform[] fatigueFillRects = new RectTransform[4];

        float[] positions = { -330f, -110f, 110f, 330f };
        for (int i = 0; i < 4; i++)
        {
            RectTransform slotRoot = CreateRoundedPanel(
                $"Slot_{i}",
                rootRect,
                new Vector2(positions[i], 0f),
                new Vector2(210f, 220f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Color(0.80f, 0.94f, 0.69f, 1f),
                new Color(0.70f, 0.89f, 0.55f, 1f),
                34,
                8);

            Button slotBtn = slotRoot.gameObject.AddComponent<Button>();
            slotBtn.targetGraphic = slotRoot.GetComponent<Image>();
            slotBtn.transition = Selectable.Transition.None;

            RectTransform portraitFrame = CreateRoundedPanel(
                $"PortraitFrame_{i}",
                slotRoot,
                new Vector2(0f, -8f),
                new Vector2(182f, 182f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Color.white,
                Color.white,
                28,
                0);

            portraitFrame.GetComponent<Image>().raycastTarget = false;

            GameObject portraitGo = new GameObject($"Portrait_{i}", typeof(RectTransform), typeof(Image));
            RectTransform portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.SetParent(portraitFrame, false);
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitRect.sizeDelta = new Vector2(174f, 174f);

            Image portrait = portraitGo.GetComponent<Image>();
            portrait.color = Color.white;
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;

            RectTransform fatigueBg = CreateRoundedPanel(
                $"FatigueBg_{i}",
                slotRoot,
                new Vector2(0f, -92f),
                new Vector2(170f, 12f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Color(0.76f, 0.85f, 0.95f, 1f),
                new Color(0.76f, 0.85f, 0.95f, 1f),
                6,
                0);
            fatigueBg.GetComponent<Image>().raycastTarget = false;

            RectTransform fatigueFill = CreateRoundedPanel(
                $"FatigueFill_{i}",
                fatigueBg,
                Vector2.zero,
                new Vector2(170f, 12f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Color(0.31f, 0.59f, 0.95f, 1f),
                new Color(0.31f, 0.59f, 0.95f, 1f),
                6,
                0);

            Image fatigueFillImage = fatigueFill.GetComponent<Image>();
            fatigueFillImage.raycastTarget = false;

            int idx = i;
            slotBtn.onClick.AddListener(() => bar.OnPortraitClicked(idx));

            slotButtons[i] = slotBtn;
            slotRects[i] = slotRoot;
            portraitImages[i] = portrait;
            fatigueFillRects[i] = fatigueFill;
        }

        RectTransform bubbleRoot = CreateRoundedPanel(
            "CourierBubble",
            rootRect,
            new Vector2(280f, 220f),
            new Vector2(390f, 170f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Color.white,
            new Color(0.59f, 0.86f, 0.42f, 1f),
            28,
            5);
        bubbleRoot.GetComponent<Image>().raycastTarget = false;

        RectTransform bubbleTailOuter = CreateRoundedPanel(
            "BubbleTailOuter",
            bubbleRoot,
            new Vector2(-146f, -74f),
            new Vector2(30f, 30f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Color(0.59f, 0.86f, 0.42f, 1f),
            new Color(0.59f, 0.86f, 0.42f, 1f),
            0,
            0);
        bubbleTailOuter.GetComponent<Image>().raycastTarget = false;
        bubbleTailOuter.localRotation = Quaternion.Euler(0f, 0f, 45f);

        RectTransform bubbleTailInner = CreateRoundedPanel(
            "BubbleTailInner",
            bubbleRoot,
            new Vector2(-146f, -74f),
            new Vector2(20f, 20f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Color.white,
            Color.white,
            0,
            0);
        bubbleTailInner.GetComponent<Image>().raycastTarget = false;
        bubbleTailInner.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Text bubbleName = CreateText(
            "BubbleName",
            bubbleRoot,
            new Vector2(0f, 44f),
            new Vector2(340f, 34f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            24,
            Color.black,
            TextAnchor.MiddleCenter);

        Text bubbleType = CreateText(
            "BubbleType",
            bubbleRoot,
            new Vector2(0f, 8f),
            new Vector2(340f, 28f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            18,
            Color.black,
            TextAnchor.MiddleCenter);

        Text bubbleTraits = CreateText(
            "BubbleTraits",
            bubbleRoot,
            new Vector2(0f, -40f),
            new Vector2(330f, 54f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            16,
            Color.black,
            TextAnchor.MiddleCenter);
        bubbleTraits.horizontalOverflow = HorizontalWrapMode.Wrap;

        bubbleRoot.gameObject.SetActive(false);

        SetPrivateField(bar, "_panelRect", rootRect);
        SetPrivateField(bar, "_slotButtons", slotButtons);
        SetPrivateField(bar, "_slotRects", slotRects);
        SetPrivateField(bar, "_portraitImages", portraitImages);
        SetPrivateField(bar, "_fatigueFillRects", fatigueFillRects);
        SetPrivateField(bar, "_bubbleRoot", bubbleRoot.gameObject);
        SetPrivateField(bar, "_bubbleRect", bubbleRoot);
        SetPrivateField(bar, "_bubbleNameText", bubbleName);
        SetPrivateField(bar, "_bubbleTypeText", bubbleType);
        SetPrivateField(bar, "_bubbleTraitsText", bubbleTraits);

        return bar;
    }

    private NotificationUI CreateNotificationPanel(Transform parent)
    {
        RectTransform panel = CreateRoundedPanel(
            "NotificationPanel",
            parent,
            new Vector2(-20f, -200f),
            new Vector2(380f, 300f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            Color.white,
            new Color(0.59f, 0.86f, 0.42f, 1f),
            28,
            5);

        Text title = CreateText(
            "NotificationTitle",
            panel,
            new Vector2(0f, 124f),
            new Vector2(220f, 34f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            22,
            Color.black,
            TextAnchor.MiddleCenter);
        title.text = "알림";
        title.fontStyle = FontStyle.Bold;

        RectTransform listRoot = new GameObject("NotificationList", typeof(RectTransform)).GetComponent<RectTransform>();
        listRoot.SetParent(panel, false);
        listRoot.anchorMin = new Vector2(0f, 0f);
        listRoot.anchorMax = new Vector2(1f, 1f);
        listRoot.offsetMin = new Vector2(12f, 12f);
        listRoot.offsetMax = new Vector2(-12f, -46f);

        NotificationUI notificationUI = panel.gameObject.AddComponent<NotificationUI>();
        notificationUI.Initialize(panel.gameObject, listRoot);
        notificationUI.SetVisible(false);
        return notificationUI;
    }

    private MailInboxUI CreateMailInboxPanel(Transform parent)
    {
        RectTransform panel = CreateRoundedPanel(
            "MailInboxPanel",
            parent,
            new Vector2(-20f, -520f),
            new Vector2(420f, 360f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            Color.white,
            new Color(0.59f, 0.86f, 0.42f, 1f),
            28,
            5);

        Text title = CreateText(
            "MailTitle",
            panel,
            new Vector2(-120f, 150f),
            new Vector2(220f, 34f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            22,
            Color.black,
            TextAnchor.MiddleLeft);
        title.text = "알림함";
        title.fontStyle = FontStyle.Bold;

        Button closeBtn = CreateButton(
            "MailClose",
            panel,
            new Vector2(170f, 150f),
            new Vector2(34f, 34f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "X",
            new Color(1f, 0.37f, 0.44f, 1f),
            Color.white,
            18,
            3);

        RectTransform listRoot = new GameObject("MailList", typeof(RectTransform)).GetComponent<RectTransform>();
        listRoot.SetParent(panel, false);
        listRoot.anchorMin = new Vector2(0f, 0f);
        listRoot.anchorMax = new Vector2(1f, 1f);
        listRoot.offsetMin = new Vector2(12f, 12f);
        listRoot.offsetMax = new Vector2(-12f, -46f);

        NotificationManager notificationManager = FindObjectOfType<NotificationManager>();
        MailInboxUI inbox = panel.gameObject.AddComponent<MailInboxUI>();
        inbox.Initialize(notificationManager, panel.gameObject, listRoot, closeBtn);
        inbox.Hide();
        return inbox;
    }

    private void CreateBottomRightButtons(Transform parent, NotificationUI notificationUI, MailInboxUI mailInboxUI)
    {
        Button alertBtn = CreateButton(
            "AlertButton",
            parent,
            new Vector2(-190f, 60f),
            new Vector2(120f, 120f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            "!",
            new Color(1f, 0.37f, 0.44f, 1f),
            Color.white,
            66,
            4);

        Button mailBtn = CreateButton(
            "MailButton",
            parent,
            new Vector2(-20f, 60f),
            new Vector2(120f, 120f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            "✉",
            new Color(1f, 0.37f, 0.44f, 1f),
            Color.white,
            56,
            4,
            LoadSymbolFont(56));

        if (notificationUI != null)
            alertBtn.onClick.AddListener(notificationUI.ToggleVisible);

        if (mailInboxUI != null)
            mailBtn.onClick.AddListener(mailInboxUI.Toggle);
    }

    private StartMenuUI CreateStartMenu(Transform parent)
    {
        GameObject root = new GameObject("StartMenu", typeof(RectTransform), typeof(RawImage), typeof(StartMenuUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        RawImage background = root.GetComponent<RawImage>();
        background.texture = GetSeaScreenTexture();
        background.color = Color.white;
        background.raycastTarget = true;

        Text title = CreateText(
            "StartTitle",
            rootRect,
            new Vector2(0f, 92f),
            new Vector2(960f, 180f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            76,
            Color.white,
            TextAnchor.MiddleCenter);
        title.text = "섬마을 집배원";
        title.fontStyle = FontStyle.Bold;

        Button startButton = CreateButton(
            "GameStartButton",
            rootRect,
            new Vector2(0f, -88f),
            new Vector2(340f, 96f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            "게임 시작",
            new Color(1f, 0.93f, 0.62f, 1f),
            new Color(0.17f, 0.18f, 0.20f, 1f),
            34,
            4);

        StartMenuUI startMenuUI = root.GetComponent<StartMenuUI>();
        startMenuUI.Initialize(this, startButton);
        startMenuUI.Show();
        return startMenuUI;
    }

    private IntroSequenceUI CreateIntroSequence(Transform parent)
    {
        GameObject root = new GameObject("IntroSequence", typeof(RectTransform), typeof(Image), typeof(IntroSequenceUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = root.GetComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        Text text = CreateText(
            "IntroText",
            rootRect,
            new Vector2(0f, 760f),
            new Vector2(1280f, 2000f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            30,
            Color.white,
            TextAnchor.UpperCenter);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.15f;

        IntroSequenceUI introSequenceUI = root.GetComponent<IntroSequenceUI>();
        introSequenceUI.Initialize(text);
        return introSequenceUI;
    }

    private Texture2D GetSeaScreenTexture()
    {
        if (_seaScreenTexture != null)
            return _seaScreenTexture;

        if (_generatedSeaTexture != null)
            return _generatedSeaTexture;

        _generatedSeaTexture = CreateFallbackSeaTexture();
        return _generatedSeaTexture;
    }

    private Texture2D CreateFallbackSeaTexture()
    {
        const int width = 512;
        const int height = 512;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color top = new Color(0.24f, 0.65f, 0.86f, 1f);
        Color bottom = new Color(0.08f, 0.28f, 0.50f, 1f);
        Color foam = new Color(0.80f, 0.95f, 0.98f, 1f);
        Color deep = new Color(0.05f, 0.18f, 0.35f, 1f);

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            Color baseColor = Color.Lerp(top, bottom, v);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float wave = Mathf.Sin(u * 18f + v * 10f) * 0.025f;
                wave += Mathf.Sin(u * 37f - v * 16f) * 0.012f;

                float sheen = Mathf.Clamp01((Mathf.Sin(u * 6f + v * 3f + 0.6f) + 1f) * 0.5f);
                Color c = Color.Lerp(baseColor, foam, sheen * 0.06f);
                c += new Color(wave, wave * 0.8f, wave * 0.5f, 0f);

                float depth = Mathf.Clamp01((v - 0.58f) * 1.7f);
                c = Color.Lerp(c, deep, depth * 0.12f);

                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private RectTransform CreateRoundedPanel(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Color fillColor,
        Color borderColor,
        int cornerRadius,
        int borderThickness)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        ApplyRoundedSprite(img, size, fillColor, borderColor, cornerRadius, borderThickness);
        return rt;
    }

    private Button CreateButton(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        string label,
        Color fillColor,
        Color textColor,
        int fontSize,
        int borderThickness,
        Font font = null)
    {
        RectTransform rt = CreateRoundedPanel(
            name,
            parent,
            anchoredPosition,
            size,
            anchorMin,
            anchorMax,
            pivot,
            fillColor,
            new Color(Mathf.Min(1f, fillColor.r + 0.12f), Mathf.Min(1f, fillColor.g + 0.12f), Mathf.Min(1f, fillColor.b + 0.12f), fillColor.a),
            Mathf.Min(Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 0.5f) - 1, fontSize > 40 ? 60 : 28),
            borderThickness);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = rt.GetComponent<Image>();
        button.transition = Selectable.Transition.None;

        Text text = CreateText(
            "Label",
            rt,
            Vector2.zero,
            size,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            fontSize,
            textColor,
            TextAnchor.MiddleCenter,
            font ?? LoadFont(fontSize));
        text.text = label;
        text.fontStyle = FontStyle.Bold;

        return button;
    }

    private Text CreateText(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        int fontSize,
        Color color,
        TextAnchor alignment,
        Font font = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Text text = go.AddComponent<Text>();
        text.font = font ?? LoadFont(fontSize);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void ApplyRoundedSprite(Image image, Vector2 size, Color fillColor, Color borderColor, int cornerRadius, int borderThickness)
    {
        int width = Mathf.Max(2, Mathf.RoundToInt(size.x));
        int height = Mathf.Max(2, Mathf.RoundToInt(size.y));
        image.sprite = GetRoundedSprite(width, height, cornerRadius, fillColor, borderColor, borderThickness);
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.raycastTarget = true;
    }

    private Sprite GetRoundedSprite(int width, int height, int cornerRadius, Color fillColor, Color borderColor, int borderThickness)
    {
        string key = $"{width}x{height}:{cornerRadius}:{borderThickness}:{fillColor}:{borderColor}";
        if (_roundedSpriteCache.TryGetValue(key, out Sprite cached))
            return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color32 fill32 = fillColor;
        Color32 border32 = borderColor;
        Color32 transparent = new Color32(0, 0, 0, 0);

        Color32[] pixels = new Color32[width * height];
        int innerWidth = Mathf.Max(1, width - borderThickness * 2);
        int innerHeight = Mathf.Max(1, height - borderThickness * 2);
        int innerRadius = Mathf.Max(0, cornerRadius - borderThickness);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool insideOuter = IsInsideRoundedRect(x, y, width, height, cornerRadius);
                if (!insideOuter)
                {
                    pixels[y * width + x] = transparent;
                    continue;
                }

                bool insideInner = true;
                if (borderThickness > 0)
                {
                    int ix = x - borderThickness;
                    int iy = y - borderThickness;
                    if (ix < 0 || iy < 0 || ix >= innerWidth || iy >= innerHeight)
                    {
                        insideInner = false;
                    }
                    else
                    {
                        insideInner = IsInsideRoundedRect(ix, iy, innerWidth, innerHeight, innerRadius);
                    }
                }

                pixels[y * width + x] = insideInner ? fill32 : border32;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _roundedSpriteCache[key] = sprite;
        return sprite;
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        if (width <= 0 || height <= 0)
            return false;

        radius = Mathf.Max(0, Mathf.Min(radius, Mathf.Min(width, height) / 2));

        int left = radius;
        int right = width - radius - 1;
        int bottom = radius;
        int top = height - radius - 1;

        if (x >= left && x <= right)
            return true;

        if (y >= bottom && y <= top)
            return true;

        int cx = x < left ? radius : width - radius - 1;
        int cy = y < bottom ? radius : height - radius - 1;
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    private Font LoadFont(int fontSize)
    {
        Font font = Resources.Load<Font>("Fonts/NanumGothic-Regular");
        if (font != null)
            return font;

        string[] names =
        {
            "NanumGothic",
            "Noto Sans CJK KR",
            "Noto Sans",
            "DejaVu Sans",
            "Malgun Gothic",
            "Apple SD Gothic Neo",
            "Arial Unicode MS",
            "Arial"
        };

        foreach (string name in names)
        {
            font = Font.CreateDynamicFontFromOSFont(name, fontSize);
            if (font != null)
                return font;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        string[] allFonts = Font.GetOSInstalledFontNames();
        if (allFonts != null && allFonts.Length > 0)
            return Font.CreateDynamicFontFromOSFont(allFonts[0], fontSize);

        return null;
    }

    private Font LoadSymbolFont(int fontSize)
    {
        string[] names =
        {
            "Segoe UI Symbol",
            "Noto Sans Symbols 2",
            "Noto Sans Symbols",
            "DejaVu Sans",
            "Symbola",
            "Arial Unicode MS",
            "Arial"
        };

        foreach (string name in names)
        {
            Font font = Font.CreateDynamicFontFromOSFont(name, fontSize);
            if (font != null)
                return font;
        }

        return LoadFont(fontSize);
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null)
            field.SetValue(obj, value);
        else
            Debug.LogWarning($"[GameSetup] Field '{fieldName}' not found on {obj.GetType().Name}");
    }
}
