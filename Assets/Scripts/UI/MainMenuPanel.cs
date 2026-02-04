// 依据：GDD 7.1.1 主界面；程序基础知识库 5.5
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuPanel : MonoBehaviour
{
    private GameObject mainControlsRoot;
    private GameObject levelSelectRoot;
    private RectTransform levelSelectListRoot;
    private Text levelSelectHintText;
    private GameObject settingsRoot;
    private GameObject inventoryRoot;
    private InventoryPanel inventoryPanel;
    private Text goldText;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private float lastSettingsSaveTime;
    private Image backgroundImage;
    private Image logoImage;
    private RectTransform safeRoot;

    private void Start()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // 背景与Logo（来自 Assets/Resources/UI/...）
        backgroundImage = CreateFullScreenImage(root, "_BG", "UI/MainMenu/UI_MainMenu_Background");
        if (backgroundImage != null) backgroundImage.raycastTarget = false;

        logoImage = CreateImage(root, "_Logo", "UI/MainMenu/UI_MainMenu_Logo", new Vector2(0.5f, 0.78f), new Vector2(800f, 200f), true);
        if (logoImage != null) logoImage.raycastTarget = false;

        // SafeArea（上下100/左右40）
        safeRoot = CreateSafeArea(root);

        mainControlsRoot = new GameObject("MainControls");
        mainControlsRoot.transform.SetParent(safeRoot != null ? safeRoot : root, false);
        var mainRt = mainControlsRoot.AddComponent<RectTransform>();
        mainRt.anchorMin = Vector2.zero;
        mainRt.anchorMax = Vector2.one;
        mainRt.offsetMin = Vector2.zero;
        mainRt.offsetMax = Vector2.zero;

        // 主按钮（开始/继续/退出）；背包入口仅保留底部导航栏 COMP-01，避免与底部「背包」重叠
        var startBtn = CreateButton(mainRt, "开始游戏", new Vector2(0.5f, 0.30f), new Vector2(400f, 120f));
        startBtn.onClick.AddListener(OnStartGame);

        var continueBtn = CreateButton(mainRt, "继续游戏", new Vector2(0.5f, 0.21f), new Vector2(400f, 120f));
        continueBtn.onClick.AddListener(OnContinue);

        var exitBtn = CreateButton(mainRt, "退出游戏", new Vector2(0.5f, 0.12f), new Vector2(400f, 100f));
        exitBtn.onClick.AddListener(OnExit);

        // 角色/技能入口（小按钮行）；底部导航栏已有 主页/背包/商城/设置
        var characterBtn = CreateButton(mainRt, "角色", new Vector2(0.38f, 0.04f), new Vector2(180f, 64f));
        characterBtn.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowCharacterAttributesPage();
        });
        var skillsBtn = CreateButton(mainRt, "技能", new Vector2(0.62f, 0.04f), new Vector2(180f, 64f));
        skillsBtn.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowSkillsPage();
        });

        BuildLevelSelectPanel(safeRoot != null ? safeRoot : root);
        // 不在主界面内再创建“背包/设置”弹窗（统一走 UIManager 的页面系统）
    }

    private void OnEnable()
    {
        // UIManager 通过 SetActive 切换主菜单显示，这里确保每次回到主菜单 UI 状态与存档显示正确
        if (mainControlsRoot != null) mainControlsRoot.SetActive(true);
        if (levelSelectRoot != null) levelSelectRoot.SetActive(false);
        if (settingsRoot != null) settingsRoot.SetActive(false);
        if (inventoryRoot != null) inventoryRoot.SetActive(false);
        if (goldText != null) RefreshGoldText(goldText);
    }

    private void BuildLevelSelectPanel(RectTransform root)
    {
        levelSelectRoot = new GameObject("LevelSelectPanel");
        levelSelectRoot.transform.SetParent(root, false);
        var rt = levelSelectRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var bg = levelSelectRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        CreateText(rt, "选择关卡", new Vector2(0.5f, 0.88f), new Vector2(400f, 60f));

        levelSelectHintText = CreateText(rt, "未加载到关卡配置", new Vector2(0.5f, 0.55f), new Vector2(600f, 50f));
        levelSelectHintText.alignment = TextAnchor.MiddleCenter;
        levelSelectHintText.color = new Color(1f, 1f, 1f, 0.9f);

        var listGo = new GameObject("LevelButtons");
        listGo.transform.SetParent(levelSelectRoot.transform, false);
        levelSelectListRoot = listGo.AddComponent<RectTransform>();
        levelSelectListRoot.anchorMin = Vector2.zero;
        levelSelectListRoot.anchorMax = Vector2.one;
        levelSelectListRoot.offsetMin = Vector2.zero;
        levelSelectListRoot.offsetMax = Vector2.zero;

        var closeBtn = CreateButton(rt, "关闭", new Vector2(0.5f, 0.08f), new Vector2(200f, 80f));
        closeBtn.onClick.AddListener(() =>
        {
            if (levelSelectRoot != null) levelSelectRoot.SetActive(false);
            if (mainControlsRoot != null) mainControlsRoot.SetActive(true);
        });

        levelSelectRoot.SetActive(false);
    }

    private void PopulateLevelSelect()
    {
        if (levelSelectListRoot == null) return;

        // 确保配置已加载（MainMenuPanel.Start 可能早于 GameManager.Initialize）
        if (ConfigManager.Instance != null && (ConfigManager.Instance.LevelConfigs == null || ConfigManager.Instance.LevelConfigs.Count == 0))
            ConfigManager.Instance.LoadAllConfigs();

        // 清空旧按钮
        for (int i = levelSelectListRoot.childCount - 1; i >= 0; i--)
            Destroy(levelSelectListRoot.GetChild(i).gameObject);

        // M3：展示 1~10 关，按存档的 unlocked_level_id 解锁
        SaveSystem.EnsureLoaded();
        int unlockedUpTo = SaveSystem.GetCached()?.player != null ? Mathf.Max(1, SaveSystem.GetCached().player.unlocked_level_id) : 1;

        if (levelSelectHintText != null)
        {
            bool hasAnyCfg = ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs != null && ConfigManager.Instance.LevelConfigs.Count > 0;
            levelSelectHintText.gameObject.SetActive(!hasAnyCfg);
            if (!hasAnyCfg) levelSelectHintText.text = "未加载到关卡配置（将以占位方式进入）";
        }

        // 一屏展示 10 关（无滚动）：缩小步进与按钮高度
        float y = 0.82f;
        float step = 0.075f;
        for (int id = 1; id <= 10; id++)
        {
            bool locked = id > unlockedUpTo;
            bool hasCfg = ConfigManager.Instance != null && ConfigManager.Instance.LevelConfigs != null && ConfigManager.Instance.LevelConfigs.ContainsKey(id);
            string name = hasCfg ? ConfigManager.Instance.LevelConfigs[id].level_name : $"第{id}关";
            if (string.IsNullOrEmpty(name)) name = $"第{id}关";

            string label = locked ? $"{name}（锁定）" : (hasCfg ? name : $"{name}（占位）");
            var btn = CreateButton(levelSelectListRoot, label, new Vector2(0.5f, y), new Vector2(520f, 80f));
            btn.interactable = !locked;
            int capturedId = id;
            btn.onClick.AddListener(() => OnLevelSelected(capturedId));
            y -= step;
        }
    }

    private void OnLevelSelected(int levelId)
    {
        // 隐藏选关面板；主按钮保持隐藏，避免「开始游戏」等与弹窗内「开始」重叠
        if (levelSelectRoot != null) levelSelectRoot.SetActive(false);
        // 不恢复 mainControlsRoot，等用户点「取消」时由 LevelConfirmationPopup.OnCancel 恢复

        // UI-10：关卡确认弹窗（由 UIManager 统一管理弹窗层）
        if (UIManager.Instance != null)
            UIManager.Instance.ShowLevelConfirmation(levelId);
        else if (GameManager.Instance != null)
            GameManager.Instance.StartGame(levelId);
    }

    /// <summary>关卡确认弹窗「取消」时恢复主按钮，由 UIManager 设置 LevelConfirmationPopup.OnCancel 调用。</summary>
    public void ShowMainControlsOnly()
    {
        if (mainControlsRoot != null) mainControlsRoot.SetActive(true);
        if (levelSelectRoot != null) levelSelectRoot.SetActive(false);
    }

    private void BuildSettingsPanel(RectTransform root)
    {
        settingsRoot = new GameObject("SettingsPanel");
        settingsRoot.transform.SetParent(root, false);
        var rt = settingsRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var bg = settingsRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);

        CreateText(rt, "设置", new Vector2(0.5f, 0.88f), new Vector2(300f, 60f));

        CreateText(rt, "背景音乐", new Vector2(0.35f, 0.65f), new Vector2(200f, 40f));
        bgmSlider = CreateSlider(rt, new Vector2(0.5f, 0.58f), new Vector2(500f, 40f));
        if (AudioManager.Instance != null) bgmSlider.SetValueWithoutNotify(AudioManager.Instance.bgmVolume);
        bgmSlider.onValueChanged.AddListener(v =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(v);
            PersistSettings(false);
        });

        CreateText(rt, "音效", new Vector2(0.35f, 0.45f), new Vector2(200f, 40f));
        sfxSlider = CreateSlider(rt, new Vector2(0.5f, 0.38f), new Vector2(500f, 40f));
        if (AudioManager.Instance != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
        sfxSlider.onValueChanged.AddListener(v =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
            PersistSettings(false);
        });

        var closeBtn = CreateButton(rt, "关闭", new Vector2(0.5f, 0.12f), new Vector2(200f, 80f));
        closeBtn.onClick.AddListener(() =>
        {
            if (settingsRoot != null) settingsRoot.SetActive(false);
            if (mainControlsRoot != null) mainControlsRoot.SetActive(true);
            PersistSettings(true);
        });

        settingsRoot.SetActive(false);
    }

    private void BuildInventoryPanel(RectTransform root)
    {
        inventoryRoot = new GameObject("InventoryPanel");
        inventoryRoot.transform.SetParent(root, false);
        inventoryPanel = inventoryRoot.AddComponent<InventoryPanel>();
        inventoryPanel.OnClose = () =>
        {
            if (inventoryRoot != null) inventoryRoot.SetActive(false);
            if (mainControlsRoot != null) mainControlsRoot.SetActive(true);
        };
        inventoryRoot.SetActive(false);
    }

    private void PersistSettings(bool forceSave)
    {
        SaveSystem.EnsureLoaded();
        if (AudioManager.Instance != null)
        {
            var save = SaveSystem.GetCached();
            save.settings.bgmVolume = AudioManager.Instance.bgmVolume;
            save.settings.sfxVolume = AudioManager.Instance.sfxVolume;
            // 通知其他系统（可选监听）
            EventManager.TriggerEvent("SETTINGS_CHANGED", save.settings);
        }

        float now = Time.unscaledTime;
        if (forceSave || now - lastSettingsSaveTime > 0.25f)
        {
            lastSettingsSaveTime = now;
            SaveSystem.SaveNow();
        }
    }

    private Slider CreateSlider(RectTransform parent, Vector2 anchorPos, Vector2 size)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.7f;
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(5, 5);
        fillAreaRt.offsetMax = new Vector2(-5, -5);
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
        return slider;
    }

    private void RefreshGoldText(Text goldText)
    {
        if (goldText == null) return;
        // 主菜单一般没有 Player 对象，显示存档中的金币
        SaveSystem.EnsureLoaded();
        var save = SaveSystem.GetCached();
        int gold = save != null && save.player != null ? save.player.gold : 0;
        goldText.text = "金币:" + gold;
    }

    private void OnContinue()
    {
        if (GameManager.Instance == null) return;
        // 继续：默认进入已解锁的最高关卡（主菜单无“上次关卡”字段时的可用兜底）
        SaveSystem.EnsureLoaded();
        int levelId = SaveSystem.GetCached()?.player != null ? Mathf.Max(1, SaveSystem.GetCached().player.unlocked_level_id) : 1;
        GameManager.Instance.StartGame(levelId);
    }

    private void OnExit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private Button CreateButton(RectTransform parent, string label, Vector2 anchorPos, Vector2 size)
    {
        GameObject go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var btn = go.AddComponent<Button>();
        go.AddComponent<UIButtonFeedback>();

        // 大/小按钮皮肤
        if (size.x >= 180f && size.y >= 70f)
            UITheme.ApplySpriteSwapButton(btn, "UI_Button_Big_Normal", "UI_Button_Big_Pressed", "UI_Button_Big_Disabled");
        else
            UITheme.ApplySpriteSwapButton(btn, "UI_Button_Small_Normal", "UI_Button_Small_Pressed", "UI_Button_Small_Disabled");

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = UITheme.DefaultFont;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return btn;
    }

    private Text CreateText(RectTransform parent, string content, Vector2 anchorPos, Vector2 size)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = UITheme.DefaultFont;
        text.fontSize = 24;
        text.color = Color.white;
        return text;
    }

    private Image CreateFullScreenImage(RectTransform parent, string name, string spritePath)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect: false);
        return img;
    }

    private Image CreateImage(RectTransform parent, string name, string spritePath, Vector2 anchorPos, Vector2 size, bool preserveAspect)
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
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect);
        return img;
    }

    private void CreateIcon(Transform parent, string spritePath, Vector2 size)
    {
        var go = new GameObject("_Icon");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect: true);
    }

    private void CreateIcon(RectTransform parent, string spritePath, Vector2 size, Vector2 anchorPos)
    {
        var go = new GameObject("_Icon");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect: true);
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

    private void OnStartGame()
    {
        if (levelSelectRoot != null)
            levelSelectRoot.SetActive(true);
        if (mainControlsRoot != null)
            mainControlsRoot.SetActive(false);
        PopulateLevelSelect();
    }

    private void OnSettings()
    {
        if (settingsRoot != null)
            settingsRoot.SetActive(true);
        if (mainControlsRoot != null)
            mainControlsRoot.SetActive(false);

        // 打开时刷新滑块为当前值（避免存档/场景切换后显示旧值）
        if (AudioManager.Instance != null)
        {
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(AudioManager.Instance.bgmVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
        }
    }

    private void OnInventory()
    {
        if (inventoryRoot != null)
            inventoryRoot.SetActive(true);
        if (mainControlsRoot != null)
            mainControlsRoot.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.Refresh();
    }

    /// <summary>供 COMP-01 导航栏调用：打开背包（当前版本背包仍作为主菜单内弹窗实现）。</summary>
    public void OpenInventoryFromNav()
    {
        OnInventory();
    }

    /// <summary>供 COMP-01 导航栏调用：打开设置（当前版本设置仍作为主菜单内弹窗实现）。</summary>
    public void OpenSettingsFromNav()
    {
        OnSettings();
    }
}
