using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AlgorithmOfDelivery.Core;
using AlgorithmOfDelivery.Game;
using AlgorithmOfDelivery.Maze;
using AlgorithmOfDelivery.UI;

public class GameSetup : MonoBehaviour
{
    private void Awake()
    {
        CreateGameSystems();
        CreateUI();
    }

    private void CreateGameSystems()
    {
        if (FindObjectOfType<DeliveryManager>() == null)
        {
            Debug.LogError("[GameSetup] DeliveryManager not found in scene!");
            return;
        }

        if (FindObjectOfType<CourierManager>() == null)
        {
            var cm = new GameObject("CourierManager").AddComponent<CourierManager>();
            Debug.Log("[GameSetup] Created CourierManager");
        }

        if (FindObjectOfType<DeliveryQueue>() == null)
        {
            var dq = new GameObject("DeliveryQueue").AddComponent<DeliveryQueue>();
            Debug.Log("[GameSetup] Created DeliveryQueue");
        }

        if (FindObjectOfType<GameManager>() == null)
        {
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            Debug.Log("[GameSetup] Created GameManager");
        }
    }

    private void CreateUI()
    {
        DestroyExisting<DashboardUI>();
        DestroyExisting<DayPrepUI>();
        DestroyExisting<InGameBottomBar>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            foreach (Transform child in canvas.transform)
            {
                Destroy(child.gameObject);
            }
        }

        CreateDashboardUI(canvas.transform);
        CreateInGameBottomBar(canvas.transform);
        CreateDayPrepUI(canvas.transform);
    }

    private void DestroyExisting<T>() where T : MonoBehaviour
    {
        var existing = FindObjectOfType<T>();
        if (existing != null)
        {
            Destroy(existing);
            Debug.Log($"[GameSetup] Destroyed existing {typeof(T).Name} component");
        }
    }

    private void CreateDashboardUI(Transform parent)
    {
        var panel = new GameObject("DashboardPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(10, -10);
        panelRect.sizeDelta = new Vector2(320, 280);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        var dash = panel.AddComponent<DashboardUI>();

        float y = -5;
        float lineHeight = 22;
        int fontSize = 16;

        var courierNameText = MakeText("CourierName", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight;
        var traitsText = MakeText("Traits", panel.transform, y, fontSize - 2, TextAnchor.UpperLeft);
        y -= lineHeight;
        var speedText = MakeText("Speed", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight;
        var positionText = MakeText("Pos", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight;
        var moneyText = MakeText("Money", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight;
        var dayText = MakeText("Day", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight;
        var timerText = MakeText("Timer", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight + 5;

        var fatigueText = MakeText("FatigueLabel", panel.transform, y, fontSize, TextAnchor.UpperLeft);
        y -= lineHeight + 5;

        var sliderGo = new GameObject("FatigueSlider", typeof(RectTransform));
        sliderGo.transform.SetParent(panel.transform, false);
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 1);
        sliderRect.anchorMax = new Vector2(0, 1);
        sliderRect.pivot = new Vector2(0, 1);
        sliderRect.anchoredPosition = new Vector2(10, y);
        sliderRect.sizeDelta = new Vector2(290, 20);
        var slider = sliderGo.AddComponent<Slider>();

        var sliderBgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        sliderBgGo.transform.SetParent(sliderGo.transform, false);
        var sliderBgRect = sliderBgGo.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero; sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.sizeDelta = Vector2.zero;
        sliderBgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var sliderFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        sliderFillGo.transform.SetParent(sliderGo.transform, false);
        var sliderFillRect = sliderFillGo.GetComponent<RectTransform>();
        sliderFillRect.anchorMin = Vector2.zero; sliderFillRect.anchorMax = Vector2.one;
        sliderFillRect.sizeDelta = Vector2.zero;
        sliderFillGo.GetComponent<Image>().color = Color.green;
        slider.fillRect = sliderFillRect;
        slider.handleRect = sliderFillRect;
        slider.targetGraphic = sliderFillGo.GetComponent<Image>();

        var zoneText = MakeText("Zone", panel.transform, y, fontSize, TextAnchor.UpperLeft);

        var dashType = typeof(DashboardUI);

        SetPrivateField(dash, "_courierNameText", courierNameText);
        SetPrivateField(dash, "_traitsText", traitsText);
        SetPrivateField(dash, "_speedText", speedText);
        SetPrivateField(dash, "_positionText", positionText);
        SetPrivateField(dash, "_moneyText", moneyText);
        SetPrivateField(dash, "_zoneText", zoneText);
        SetPrivateField(dash, "_dayText", dayText);
        SetPrivateField(dash, "_timerText", timerText);
        SetPrivateField(dash, "_fatigueText", fatigueText);
        SetPrivateField(dash, "_fatigueSlider", slider);

        Debug.Log("[GameSetup] DashboardUI created");
    }

    private void CreateDayPrepUI(Transform parent)
    {
        var panel = new GameObject("DayPrepPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500, 420);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        var prepUI = panel.AddComponent<DayPrepUI>();

        var title = MakeText("Title", panel.transform, 190, 22, TextAnchor.UpperCenter, Color.white);
        title.text = "하루 준비";
        var titleRt = title.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -20);

        var courierLabel = MakeText("CourierLabel", panel.transform, 160, 16, TextAnchor.UpperLeft, Color.gray);
        courierLabel.text = "집배원";

        int slotSize = 80;
        int gap = 10;
        int slotsTotal = 4;
        float startX = -(slotsTotal * (slotSize + gap) - gap) / 2f + slotSize / 2f;

        var courierPortraits = new Image[slotsTotal];
        var courierNameTexts = new Text[slotsTotal];
        var courierTypeTexts = new Text[slotsTotal];

        for (int i = 0; i < slotsTotal; i++)
        {
            float x = startX + i * (slotSize + gap);
            float slotY = 120;

            var slotBg = new GameObject($"CourierSlot_{i}", typeof(RectTransform), typeof(Image));
            slotBg.transform.SetParent(panel.transform, false);
            var r = slotBg.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, slotY);
            r.sizeDelta = new Vector2(slotSize, slotSize);
            slotBg.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(slotBg.transform, false);
            var pr = portrait.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(slotSize - 8, slotSize - 8);
            pr.anchoredPosition = Vector2.zero;
            courierPortraits[i] = portrait.GetComponent<Image>();
            courierPortraits[i].color = Color.white;
            courierPortraits[i].raycastTarget = false;

            int idx = i;
            var slotBtn = slotBg.AddComponent<Button>();
            slotBtn.targetGraphic = slotBg.GetComponent<Image>();
            slotBtn.onClick.AddListener(() => prepUI.OnCourierClicked(idx));

            var nameText = MakeText($"Name_{i}", slotBg.transform, -28, 12, TextAnchor.UpperCenter, Color.white);
            nameText.rectTransform.anchoredPosition = new Vector2(0, -28);
            courierNameTexts[i] = nameText;

            var typeText = MakeText($"Type_{i}", slotBg.transform, -42, 10, TextAnchor.UpperCenter, Color.gray);
            typeText.rectTransform.anchoredPosition = new Vector2(0, -42);
            courierTypeTexts[i] = typeText;
        }

        var vehicleLabel = MakeText("VehicleLabel", panel.transform, 55, 16, TextAnchor.UpperLeft, Color.gray);
        vehicleLabel.text = "운송수단";

        int vSlots = 3;
        float vStartX = -(vSlots * (slotSize + gap) - gap) / 2f + slotSize / 2f;
        var vehicleIcons = new Image[vSlots];
        var vehicleNameTexts = new Text[vSlots];

        for (int i = 0; i < vSlots; i++)
        {
            float x = vStartX + i * (slotSize + gap);
            float slotY = 15;

            var vSlot = new GameObject($"VehicleSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            vSlot.transform.SetParent(panel.transform, false);
            var r = vSlot.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, slotY);
            r.sizeDelta = new Vector2(slotSize, slotSize);
            vSlot.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

            var vIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            vIcon.transform.SetParent(vSlot.transform, false);
            var vir = vIcon.GetComponent<RectTransform>();
            vir.anchorMin = vir.anchorMax = new Vector2(0.5f, 0.5f);
            vir.sizeDelta = new Vector2(slotSize - 8, slotSize - 8);
            vir.anchoredPosition = Vector2.zero;
            vehicleIcons[i] = vIcon.GetComponent<Image>();
            vehicleIcons[i].raycastTarget = false;

            int vidx = i;
            var vBtn = vSlot.GetComponent<Button>();
            vBtn.targetGraphic = vSlot.GetComponent<Image>();
            vBtn.onClick.AddListener(() => prepUI.OnVehicleClicked(vidx));

            var vName = MakeText($"VName_{i}", vSlot.transform, -28, 12, TextAnchor.UpperCenter, Color.white);
            vName.rectTransform.anchoredPosition = new Vector2(0, -28);
            vehicleNameTexts[i] = vName;
        }

        var recruitBtn = MakeButton("RecruitBtn", panel.transform, new Vector2(-70, -65), new Vector2(120, 36), "모집");
        var startBtn = MakeButton("StartBtn", panel.transform, new Vector2(70, -65), new Vector2(120, 36), "시작");

        var recruitCostText = MakeText("RecruitCost", panel.transform, -95, 13, TextAnchor.MiddleCenter, Color.yellow);
        recruitCostText.rectTransform.anchoredPosition = new Vector2(0, -95);

        var goldText = MakeText("GoldText", panel.transform, -120, 14, TextAnchor.MiddleCenter, Color.yellow);
        goldText.rectTransform.anchoredPosition = new Vector2(0, -120);

        SetPrivateField(prepUI, "_panel", panel);
        SetPrivateField(prepUI, "_courierPortraits", courierPortraits);
        SetPrivateField(prepUI, "_courierNameTexts", courierNameTexts);
        SetPrivateField(prepUI, "_courierTypeTexts", courierTypeTexts);
        SetPrivateField(prepUI, "_vehicleIcons", vehicleIcons);
        SetPrivateField(prepUI, "_vehicleNameTexts", vehicleNameTexts);
        SetPrivateField(prepUI, "_recruitButton", recruitBtn);
        SetPrivateField(prepUI, "_startButton", startBtn);
        SetPrivateField(prepUI, "_recruitCostText", recruitCostText);
        SetPrivateField(prepUI, "_goldText", goldText);

        prepUI.LateInit();

        panel.SetActive(false);

        Debug.Log("[GameSetup] DayPrepUI created");
    }

    private void CreateInGameBottomBar(Transform parent)
    {
        var panel = new GameObject("BottomBar", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 10);
        panelRect.sizeDelta = new Vector2(400, 100);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);

        var bar = panel.AddComponent<InGameBottomBar>();

        int slots = 4;
        int size = 70;
        int gap = 10;
        float startX = -(slots * (size + gap) - gap) / 2f + size / 2f;

        var portraitImages = new Image[slots];
        var nameTexts = new Text[slots];

        for (int i = 0; i < slots; i++)
        {
            float x = startX + i * (size + gap);

            var slotGo = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(panel.transform, false);
            var sr = slotGo.GetComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = new Vector2(x, 10);
            sr.sizeDelta = new Vector2(size, size);
            slotGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

            var portrait = new GameObject($"Portrait_{i}", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(slotGo.transform, false);
            var pr = portrait.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(size - 6, size - 6);
            pr.anchoredPosition = Vector2.zero;
            portraitImages[i] = portrait.GetComponent<Image>();
            portraitImages[i].raycastTarget = false;

            int idx = i;
            var slotBtn = slotGo.AddComponent<Button>();
            slotBtn.targetGraphic = slotGo.GetComponent<Image>();
            slotBtn.onClick.AddListener(() => bar.OnPortraitClicked(idx));

            var nameTxt = MakeText($"Name_{i}", slotGo.transform, -22, 12, TextAnchor.UpperCenter, Color.white);
            nameTxt.rectTransform.anchoredPosition = new Vector2(0, -22);
            nameTexts[i] = nameTxt;
        }

        SetPrivateField(bar, "_panel", panel);
        SetPrivateField(bar, "_portraitImages", portraitImages);
        SetPrivateField(bar, "_nameTexts", nameTexts);

        Debug.Log("[GameSetup] InGameBottomBar created");
    }

    private Font LoadBestFont(int fontSize)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) return font;

        string[] fontNames = { "NanumGothic", "Noto Sans CJK KR", "UnDotum", "Malgun Gothic", "Apple SD Gothic Neo", "Arial Unicode MS", "Arial" };
        foreach (var fn in fontNames)
        {
            font = Font.CreateDynamicFontFromOSFont(fn, fontSize);
            if (font != null) return font;
        }

        font = Resources.Load<Font>("Fonts/NanumGothic-Regular");
        if (font != null) return font;

        var allFonts = Font.GetOSInstalledFontNames();
        if (allFonts != null && allFonts.Length > 0)
            font = Font.CreateDynamicFontFromOSFont(allFonts[0], fontSize);

        return font;
    }

    private Text MakeText(string name, Transform parent, float y, int fontSize, TextAnchor anchor, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, y);
        rt.sizeDelta = new Vector2(300, fontSize + 6);

        var txt = go.AddComponent<Text>();
        txt.font = LoadBestFont(fontSize);
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = color ?? Color.white;
        txt.raycastTarget = false;

        return txt;
    }

    private Button MakeButton(string name, Transform parent, Vector2 pos, Vector2 size, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var btnImg = go.GetComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.5f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = btnImg;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.sizeDelta = Vector2.zero;
        var lbl = labelGo.AddComponent<Text>();
        lbl.text = label;
        lbl.font = LoadBestFont(18);
        lbl.fontSize = 18;
        lbl.alignment = TextAnchor.MiddleCenter;
        lbl.color = Color.white;
        lbl.raycastTarget = false;

        return btn;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"[GameSetup] Field '{fieldName}' not found on {obj.GetType().Name}");
        }
    }
}
