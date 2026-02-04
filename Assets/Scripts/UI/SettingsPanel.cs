// UI-09 设置（一级界面）
// 依据：开发计划 v2.4、UI策划案 v3.2（首版实现：BGM/SFX）
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SettingsPanel : MonoBehaviour
{
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Text hintText;
    private ToggleGroup qualityGroup;
    private ToggleGroup fpsGroup;
    private ToggleGroup langGroup;
    private Toggle qLow, qMed, qHigh;
    private Toggle fps30, fps60;
    private Toggle langCn, langEn;

    private SettingsSaveData original;
    private float curBgm;
    private float curSfx;
    private int curQuality;
    private int curFps;
    private string curLang;

    private void Start()
    {
        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;

        var safe = CreateSafeArea(root);

        CreateText(safe, "_Text_Title", "设置", new Vector2(0.5f, 0.92f), new Vector2(500f, 70f), 42, TextAnchor.MiddleCenter);

        CreateText(safe, "_Text_BGM", "音乐音量", new Vector2(0.30f, 0.82f), new Vector2(240f, 40f), 26, TextAnchor.MiddleLeft);
        bgmSlider = CreateSlider(safe, new Vector2(0.52f, 0.76f), new Vector2(540f, 44f));

        CreateText(safe, "_Text_SFX", "音效音量", new Vector2(0.30f, 0.68f), new Vector2(240f, 40f), 26, TextAnchor.MiddleLeft);
        sfxSlider = CreateSlider(safe, new Vector2(0.52f, 0.62f), new Vector2(540f, 44f));

        CreateText(safe, "_Text_Quality", "画质", new Vector2(0.30f, 0.54f), new Vector2(240f, 40f), 26, TextAnchor.MiddleLeft);
        qualityGroup = CreateToggleGroup(safe, "_TG_Quality");
        qLow = CreateToggleButton(safe, qualityGroup, "_Toggle_Quality_Low", "低", new Vector2(0.42f, 0.48f), new Vector2(160f, 80f));
        qMed = CreateToggleButton(safe, qualityGroup, "_Toggle_Quality_Medium", "中", new Vector2(0.60f, 0.48f), new Vector2(160f, 80f));
        qHigh = CreateToggleButton(safe, qualityGroup, "_Toggle_Quality_High", "高", new Vector2(0.78f, 0.48f), new Vector2(160f, 80f));

        CreateText(safe, "_Text_FPS", "帧率", new Vector2(0.30f, 0.40f), new Vector2(240f, 40f), 26, TextAnchor.MiddleLeft);
        fpsGroup = CreateToggleGroup(safe, "_TG_FPS");
        fps30 = CreateToggleButton(safe, fpsGroup, "_Toggle_FPS_30", "30", new Vector2(0.55f, 0.34f), new Vector2(180f, 80f));
        fps60 = CreateToggleButton(safe, fpsGroup, "_Toggle_FPS_60", "60", new Vector2(0.75f, 0.34f), new Vector2(180f, 80f));

        CreateText(safe, "_Text_Language", "语言", new Vector2(0.30f, 0.26f), new Vector2(240f, 40f), 26, TextAnchor.MiddleLeft);
        langGroup = CreateToggleGroup(safe, "_TG_Language");
        langCn = CreateToggleButton(safe, langGroup, "_Toggle_Language_CN", "简体中文", new Vector2(0.55f, 0.20f), new Vector2(260f, 80f));
        langEn = CreateToggleButton(safe, langGroup, "_Toggle_Language_EN", "English", new Vector2(0.80f, 0.20f), new Vector2(220f, 80f));

        hintText = CreateText(safe, "_Text_Hint", "", new Vector2(0.5f, 0.13f), new Vector2(800f, 40f), 22, TextAnchor.MiddleCenter);
        hintText.color = new Color(1f, 1f, 1f, 0.9f);

        var quit = CreateButton(safe, "_Button_Quit", "退出游戏", new Vector2(0.5f, 0.22f), new Vector2(420f, 110f));
        quit.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        var back = CreateButton(safe, "_Button_Back", "返回", new Vector2(0.32f, 0.06f), new Vector2(320f, 110f));
        back.onClick.AddListener(() =>
        {
            // UI策划案：返回=取消（回滚到进入设置时的值）
            if (original != null)
            {
                ApplyRuntime(original);
                ApplyToUI(original);
            }
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenuHome();
        });

        var confirm = CreateButton(safe, "_Button_Confirm", "确定", new Vector2(0.72f, 0.06f), new Vector2(320f, 110f));
        confirm.onClick.AddListener(() =>
        {
            PersistSettings(true);
            if (UIManager.Instance != null) UIManager.Instance.ShowMainMenuHome();
        });

        // init values
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        original = Clone(save.settings);
        ApplyToUI(save.settings);
        ApplyRuntime(save.settings);

        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(v =>
        {
            curBgm = v;
            ApplyRuntime(false);
        });
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v =>
        {
            curSfx = v;
            ApplyRuntime(false);
        });

        if (qLow != null) qLow.onValueChanged.AddListener(on => { if (on) { curQuality = 0; ApplyRuntime(false); } });
        if (qMed != null) qMed.onValueChanged.AddListener(on => { if (on) { curQuality = 1; ApplyRuntime(false); } });
        if (qHigh != null) qHigh.onValueChanged.AddListener(on => { if (on) { curQuality = 2; ApplyRuntime(false); } });
        if (fps30 != null) fps30.onValueChanged.AddListener(on => { if (on) { curFps = 30; ApplyRuntime(false); } });
        if (fps60 != null) fps60.onValueChanged.AddListener(on => { if (on) { curFps = 60; ApplyRuntime(false); } });
        if (langCn != null) langCn.onValueChanged.AddListener(on => { if (on) { curLang = "zh-CN"; MarkDirty(); } });
        if (langEn != null) langEn.onValueChanged.AddListener(on => { if (on) { curLang = "en"; MarkDirty(); } });
    }

    private float lastSaveTime;
    private void PersistSettings(bool forceSave = true)
    {
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        if (save.settings == null) save.settings = new SettingsSaveData();
        save.settings.bgmVolume = curBgm;
        save.settings.sfxVolume = curSfx;
        save.settings.qualityLevel = curQuality;
        save.settings.targetFps = curFps;
        save.settings.language = string.IsNullOrEmpty(curLang) ? "zh-CN" : curLang;
        EventManager.TriggerEvent("SETTINGS_CHANGED", save.settings);

        float now = Time.unscaledTime;
        if (forceSave || now - lastSaveTime > 0.25f)
        {
            lastSaveTime = now;
            SaveSystem.SaveNow();
            if (hintText != null) hintText.text = "已保存";
        }
    }

    private void ApplyRuntime(bool saved)
    {
        // 直接应用到运行态（UI策划案：拖动/选择应立即生效）
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(curBgm);
            AudioManager.Instance.SetSFXVolume(curSfx);
        }

        int fps = (curFps <= 30) ? 30 : 60;
        Application.targetFrameRate = fps;
        QualitySettings.vSyncCount = 0;

        int qCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
        if (qCount > 0)
        {
            int idx = Mathf.Clamp(curQuality, 0, qCount - 1);
            if (QualitySettings.GetQualityLevel() != idx)
                QualitySettings.SetQualityLevel(idx, applyExpensiveChanges: false);
        }

        MarkDirty(saved ? "已保存" : "未保存");
    }

    private void ApplyRuntime(SettingsSaveData s)
    {
        if (s == null) return;
        curBgm = Mathf.Clamp01(s.bgmVolume);
        curSfx = Mathf.Clamp01(s.sfxVolume);
        curQuality = Mathf.Clamp(s.qualityLevel, 0, 2);
        curFps = (s.targetFps <= 30) ? 30 : 60;
        curLang = string.IsNullOrEmpty(s.language) ? "zh-CN" : s.language;
        ApplyRuntime(saved: true);
    }

    private void ApplyToUI(SettingsSaveData s)
    {
        if (s == null) return;
        curBgm = Mathf.Clamp01(s.bgmVolume);
        curSfx = Mathf.Clamp01(s.sfxVolume);
        curQuality = Mathf.Clamp(s.qualityLevel, 0, 2);
        curFps = (s.targetFps <= 30) ? 30 : 60;
        curLang = string.IsNullOrEmpty(s.language) ? "zh-CN" : s.language;

        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(curBgm);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(curSfx);

        if (curQuality == 0 && qLow != null) qLow.SetIsOnWithoutNotify(true);
        if (curQuality == 1 && qMed != null) qMed.SetIsOnWithoutNotify(true);
        if (curQuality == 2 && qHigh != null) qHigh.SetIsOnWithoutNotify(true);

        if (curFps == 30 && fps30 != null) fps30.SetIsOnWithoutNotify(true);
        if (curFps == 60 && fps60 != null) fps60.SetIsOnWithoutNotify(true);

        if (curLang == "zh-CN" && langCn != null) langCn.SetIsOnWithoutNotify(true);
        if (curLang != "zh-CN" && langEn != null) langEn.SetIsOnWithoutNotify(true);
    }

    private void MarkDirty(string msg = "未保存")
    {
        if (hintText != null) hintText.text = msg;
    }

    private static SettingsSaveData Clone(SettingsSaveData s)
    {
        if (s == null) return new SettingsSaveData();
        return new SettingsSaveData
        {
            bgmVolume = s.bgmVolume,
            sfxVolume = s.sfxVolume,
            qualityLevel = s.qualityLevel,
            targetFps = s.targetFps,
            language = s.language
        };
    }

    private static RectTransform CreateSafeArea(RectTransform parent)
    {
        var go = new GameObject("_SafeArea");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40f, 100f);
        rt.offsetMax = new Vector2(-40f, -100f);
        return rt;
    }

    private static Text CreateText(RectTransform parent, string name, string content, Vector2 anchorPos, Vector2 size, int fontSize, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var text = go.AddComponent<Text>();
        text.text = content;
        if (UITheme.DefaultFont != null) text.font = UITheme.DefaultFont;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        return text;
    }

    private static ToggleGroup CreateToggleGroup(RectTransform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<ToggleGroup>();
    }

    private static Toggle CreateToggleButton(RectTransform parent, ToggleGroup group, string name, string label, Vector2 anchorPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = Color.white;

        var t = go.AddComponent<Toggle>();
        t.group = group;
        t.targetGraphic = img;

        // 用 SpriteSwap 方案统一按钮反馈
        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();
        UITheme.ApplySpriteSwapButton(btn, "UI_Button_Small_Normal", "UI_Button_Small_Pressed", "UI_Button_Small_Disabled");

        // Toggle 的选中/未选中颜色（不依赖图片资源）
        var colors = t.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.95f);
        colors.selectedColor = new Color(1f, 0.9f, 0.3f, 1f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        t.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = UITheme.DefaultFont;
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        // 点击按钮时切 Toggle（避免 Toggle 没有显式点击区域的问题）
        btn.onClick.AddListener(() => t.isOn = true);
        return t;
    }

    private static Slider CreateSlider(RectTransform parent, Vector2 anchorPos, Vector2 size)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.7f;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(6, 6);
        fillAreaRt.offsetMax = new Vector2(-6, -6);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        slider.fillRect = fillRt;

        // Handle（简单占位，避免某些平台 Slider 不可拖动）
        var handle = new GameObject("Handle");
        handle.transform.SetParent(go.transform, false);
        var handleRt = handle.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0.5f);
        handleRt.anchorMax = new Vector2(0f, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.sizeDelta = new Vector2(20f, 60f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(1f, 1f, 1f, 0.9f);
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchorPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();
        UITheme.ApplySpriteSwapButton(btn, "UI_Button_Big_Normal", "UI_Button_Big_Pressed", "UI_Button_Big_Disabled");

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = UITheme.DefaultFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return btn;
    }
}

