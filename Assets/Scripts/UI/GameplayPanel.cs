// 依据：GDD 7.1.2 战斗界面；程序基础知识库 5.5 监听事件刷新
using UnityEngine;
using UnityEngine.UI;

public class GameplayPanel : MonoBehaviour
{
    private Text healthText;
    private Text goldText;
    private Text expText;
    private Image expFill;
    private Text levelText;
    private Image avatarImg;
    private Button potionBtn;
    private Text potionCountText;
    private Button[] skillBtns;
    private Text[] skillCdTexts;
    private Image[] skillCooldownMasks;
    private Image hpFill;
    private float[] lastSkillTimes;
    private float[] skillCooldowns;
    private Button bagBtn;

    // UI策划案 v3.2：金币数字平滑变化（0.5s）
    private int targetGold;
    private float displayedGold;
    private float goldVel;
    private bool goldInited;

    // UI策划案 v3.2：金币图标闪光（0.3s）
    private Image goldIconImg;
    private Color goldIconBaseColor = Color.white;
    private Vector3 goldIconBaseScale = Vector3.one;
    private float goldFlashRemain;
    private const float GoldFlashDuration = 0.3f;

    // UI策划案 v3.2：经验条/经验数字缓动（条 1.5s / 数字 1.0s）
    private int targetExp;
    private float displayedExp;
    private float expVel;
    private float displayedExpFill;
    private float expFillVel;
    private bool expInited;

    // UI策划案 v3.2：升级特效（轻量级 UI 脉冲）
    private Vector3 avatarBaseScale = Vector3.one;
    private Color expFillBaseColor = new Color(1f, 0.78f, 0.2f, 1f);
    private float levelUpFlashRemain;
    private const float LevelUpFlashDuration = 0.6f;

    // UI策划案 v3.2：治疗瓶禁用态 & 使用反馈
    private Image potionBgImg;
    private Image potionIconImg;
    private Color potionBaseColor = Color.white;
    private float potionFlashRemain;
    private const float PotionFlashDuration = 0.25f;

    private void Start()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // SafeArea（上下100/左右40，避免关键UI贴边）
        var safe = CreateSafeArea(root);

        // 顶部布局改为“角落锚点 + 像素偏移”，避免不同分辨率下漂移/挤压
        const float TopPad = 10f;
        const float LeftPad = 10f;
        const float RightPad = 10f;
        const float TopBtnSize = 96f;
        const float TopBtnGap = 10f;

        // Avatar + Level（左上角玩家信息）
        avatarImg = CreateImage(safe, "UI/Common/UI_Panel_Background", new Vector2(0f, 1f), new Vector2(84f, 84f), true);
        if (avatarImg != null)
        {
            avatarImg.color = new Color(1f, 1f, 1f, 0.85f);
            avatarBaseScale = avatarImg.rectTransform.localScale;
            var rt = avatarImg.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(LeftPad, -TopPad);
        }
        levelText = CreateText(safe, "Lv.1", new Vector2(0f, 1f), new Vector2(220f, 30f));
        levelText.alignment = TextAnchor.MiddleLeft;
        levelText.fontSize = 24;
        if (levelText != null)
        {
            var rt = levelText.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(LeftPad + 92f, -TopPad - 6f);
        }

        // HP Bar
        var hpBg = CreateImage(safe, "UI/Gameplay/UI_HPBar_BG", new Vector2(0f, 1f), new Vector2(340f, 34f), false);
        if (hpBg != null)
        {
            var rt = hpBg.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(LeftPad + 92f, -TopPad - 34f);
        }
        hpFill = CreateChildFill(hpBg.rectTransform, "UI/Gameplay/UI_HPBar_Fill");
        healthText = CreateText(hpBg.rectTransform, "100/100", new Vector2(0.5f, 0.5f), new Vector2(280f, 36f));
        healthText.name = "_Text_PlayerHP";
        healthText.alignment = TextAnchor.MiddleCenter;

        // EXP Bar（需求知识库 v2.2：战斗界面必须包含经验条）
        var expBg = CreateImage(safe, "UI/Gameplay/UI_HPBar_BG", new Vector2(0f, 1f), new Vector2(340f, 22f), false);
        if (expBg != null)
        {
            var rt = expBg.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(LeftPad + 92f, -TopPad - 70f);
        }
        expFill = CreateChildFill(expBg.rectTransform, "UI/Gameplay/UI_HPBar_Fill");
        if (expFill != null)
        {
            expFillBaseColor = new Color(1f, 0.78f, 0.2f, 1f);
            expFill.color = expFillBaseColor;
        }
        expText = CreateText(expBg.rectTransform, "0/100", new Vector2(0.5f, 0.5f), new Vector2(280f, 22f));
        expText.alignment = TextAnchor.MiddleCenter;
        expText.fontSize = 18;

        // Gold (icon + text) —— 放在“右上三按钮组”左侧，避免被按钮遮挡
        const float GoldIconSize = 44f;
        const float GoldTextWidth = 160f;
        const float GoldGroupGap = 48f; // 金币组与右侧按钮组间距（避免右上重叠）
        float btnStep = TopBtnSize + TopBtnGap;
        float goldTextX = -RightPad - 3f * btnStep - GoldGroupGap; // 预留给：pause/settings/bag

        goldIconImg = CreateIcon(safe, "UI/Common/UI_Icon_Gold", new Vector2(GoldIconSize, GoldIconSize), new Vector2(1f, 1f));
        if (goldIconImg != null)
        {
            goldIconBaseColor = goldIconImg.color;
            goldIconBaseScale = goldIconImg.rectTransform.localScale;
            var rt = goldIconImg.rectTransform;
            // icon 在文字左侧（用左上 pivot，计算更直观）
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(goldTextX - GoldTextWidth - GoldIconSize - 10f, -TopPad - 10f);
        }
        goldText = CreateText(safe, "0", new Vector2(1f, 1f), new Vector2(160f, 34f));
        goldText.name = "_Text_Gold";
        goldText.alignment = TextAnchor.MiddleLeft;
        goldText.fontSize = 24;
        if (goldText != null)
        {
            var rt = goldText.rectTransform;
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(GoldTextWidth, 34f);
            rt.anchoredPosition = new Vector2(goldTextX, -TopPad - 14f);
        }

        // Bag / Settings / Pause（右上角）
        bagBtn = CreateButton(safe, "", new Vector2(1f, 1f), new Vector2(TopBtnSize, TopBtnSize));
        bagBtn.onClick.AddListener(OnBag);
        CreateIcon(bagBtn.transform, "UI/Common/UI_Icon_Bag", new Vector2(80f, 80f));
        if (bagBtn != null)
        {
            var rt = bagBtn.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-RightPad - 2f * btnStep, -TopPad);
        }

        var settingsBtn = CreateButton(safe, "", new Vector2(1f, 1f), new Vector2(TopBtnSize, TopBtnSize));
        settingsBtn.onClick.AddListener(OnSettings);
        CreateIcon(settingsBtn.transform, "UI/Common/UI_Icon_Settings", new Vector2(80f, 80f));
        if (settingsBtn != null)
        {
            var rt = settingsBtn.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-RightPad - 1f * btnStep, -TopPad);
        }

        var pauseBtn = CreateButton(safe, "", new Vector2(1f, 1f), new Vector2(TopBtnSize, TopBtnSize));
        pauseBtn.onClick.AddListener(OnPause);
        CreateIcon(pauseBtn.transform, "UI/Gameplay/UI_Icon_Pause", new Vector2(80f, 80f));
        if (pauseBtn != null)
        {
            var rt = pauseBtn.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-RightPad, -TopPad);
        }

        // Skill buttons（2个技能 + 1个闪避）
        skillBtns = new Button[3];
        skillCdTexts = new Text[3];
        skillCooldownMasks = new Image[3];
        lastSkillTimes = new float[3];
        skillCooldowns = new float[3] { 8f, 5f, 6f };

        // Potion（治疗瓶）
        // UI策划案 v3.2：治疗瓶在左下（摇杆上方）
        potionBtn = CreateButton(safe, "", new Vector2(0.18f, 0.30f), new Vector2(120f, 120f));
        potionBtn.onClick.AddListener(OnPotion);
        potionBgImg = potionBtn.GetComponent<Image>();
        if (potionBgImg != null) UITheme.ApplyImageSprite(potionBgImg, "UI/Gameplay/UI_SkillBtn_BG", preserveAspect: true);
        potionIconImg = CreateIcon(potionBtn.transform, "UI/Gameplay/UI_Icon_Skill_Heal", new Vector2(90f, 90f));
        if (potionIconImg != null) potionBaseColor = potionIconImg.color;
        potionCountText = potionBtn.GetComponentInChildren<Text>();
        if (potionCountText != null)
        {
            potionCountText.text = "";
            potionCountText.fontSize = 22;
            potionCountText.alignment = TextAnchor.LowerRight;
        }

        // 闪避（更贴近 UI 效果图：左侧一个小技能）
        skillBtns[2] = CreateSkillButton(safe, new Vector2(0.70f, 0.12f), "UI/Gameplay/UI_Icon_Skill_Dodge", 2);
        skillBtns[0] = CreateSkillButton(safe, new Vector2(0.82f, 0.12f), "UI/Gameplay/UI_Icon_Skill_MultiArrow", 0);
        // SK002=穿透箭：目前资源集中无专用穿透箭图标，先用箭类占位图标
        skillBtns[1] = CreateSkillButton(safe, new Vector2(0.94f, 0.12f), "UI/Gameplay/UI_Icon_Skill_MultiArrow", 1);

        CreateJoystickArea(safe);

        EventManager.AddListener("HEALTH_CHANGED", OnHealthChanged);
        EventManager.AddListener("GOLD_CHANGED", OnGoldChanged);
        EventManager.AddListener("EXP_GAINED", OnExpGained);
        EventManager.AddListener("LEVEL_UP", OnLevelUp);
        EventManager.AddListener("SKILL_USED", OnSkillUsed);
        EventManager.AddListener("POTION_CHANGED", OnPotionChanged);

        RefreshFromPlayer();
    }

    /// <summary>仅在进行中或暂停时刷新，避免结算界面时血条/金币等仍被事件驱动更新。</summary>
    private bool IsGameplayActive()
    {
        return GameManager.Instance != null &&
               (GameManager.Instance.CurrentState == GameState.InGame || GameManager.Instance.CurrentState == GameState.Paused);
    }

    private void RefreshFromPlayer()
    {
        if (!IsGameplayActive()) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || healthText == null || goldText == null) return;
        var ps = player.GetComponent<PlayerStats>();
        if (ps == null) return;
        healthText.text = $"{ps.CurrentHp:F0}/{ps.MaxHp:F0}";
        if (hpFill != null) hpFill.fillAmount = ps.MaxHp > 0 ? Mathf.Clamp01(ps.CurrentHp / ps.MaxHp) : 0f;
        targetGold = ps.Gold;
        if (!goldInited)
        {
            goldInited = true;
            displayedGold = targetGold;
        }
        if (levelText != null) levelText.text = $"Lv.{ps.Level}";
        targetExp = Mathf.Clamp(ps.ExpInCurrentLevel, 0, 100);
        if (!expInited)
        {
            expInited = true;
            displayedExp = targetExp;
            displayedExpFill = targetExp / 100f;
        }
        if (potionCountText != null) potionCountText.text = ps.PotionCount > 0 ? ("×" + ps.PotionCount) : "";
        // 为了满足“数量=0 仍可点击播放提示音效”的交互，这里不直接禁用 Button，而是做视觉禁用态
        if (potionBtn != null) potionBtn.interactable = true;
        ApplyPotionVisualState(ps.PotionCount > 0);
    }

    private void OnEnable()
    {
        if (IsGameplayActive() && healthText != null) RefreshFromPlayer();
    }

    private void OnDestroy()
    {
        EventManager.RemoveListener("HEALTH_CHANGED", OnHealthChanged);
        EventManager.RemoveListener("GOLD_CHANGED", OnGoldChanged);
        EventManager.RemoveListener("EXP_GAINED", OnExpGained);
        EventManager.RemoveListener("LEVEL_UP", OnLevelUp);
        EventManager.RemoveListener("SKILL_USED", OnSkillUsed);
        EventManager.RemoveListener("POTION_CHANGED", OnPotionChanged);
    }

    private void Update()
    {
        if (!IsGameplayActive()) return;
        float udt = Time.unscaledDeltaTime;

        // 金币缓动（不受 timeScale 影响）
        if (goldText != null)
        {
            displayedGold = Mathf.SmoothDamp(displayedGold, targetGold, ref goldVel, 0.5f, Mathf.Infinity, udt);
            int shown = Mathf.RoundToInt(displayedGold);
            goldText.text = shown.ToString();
        }

        // 金币图标闪光（不受 timeScale 影响）
        if (goldIconImg != null && goldFlashRemain > 0f)
        {
            goldFlashRemain = Mathf.Max(0f, goldFlashRemain - udt);
            float t01 = 1f - (goldFlashRemain / GoldFlashDuration);
            float pulse = Mathf.Sin(t01 * Mathf.PI); // 0->1->0
            var flashColor = new Color(1f, 1f, 0.35f, 1f);
            goldIconImg.color = Color.Lerp(goldIconBaseColor, flashColor, pulse);
            float scale = Mathf.Lerp(1f, 1.12f, pulse);
            goldIconImg.rectTransform.localScale = goldIconBaseScale * scale;
            if (goldFlashRemain <= 0f)
            {
                goldIconImg.color = goldIconBaseColor;
                goldIconImg.rectTransform.localScale = goldIconBaseScale;
            }
        }

        // 经验条/数字缓动（不受 timeScale 影响）
        if (expText != null || expFill != null)
        {
            displayedExp = Mathf.SmoothDamp(displayedExp, targetExp, ref expVel, 1.0f, Mathf.Infinity, udt);
            int shownExp = Mathf.Clamp(Mathf.RoundToInt(displayedExp), 0, 100);
            if (expText != null) expText.text = $"{shownExp}/100";

            float targetFill = Mathf.Clamp01(targetExp / 100f);
            displayedExpFill = Mathf.SmoothDamp(displayedExpFill, targetFill, ref expFillVel, 1.5f, Mathf.Infinity, udt);
            if (expFill != null) expFill.fillAmount = Mathf.Clamp01(displayedExpFill);
        }

        // 升级特效：头像脉冲 + 经验条闪光（不受 timeScale 影响）
        if (levelUpFlashRemain > 0f)
        {
            levelUpFlashRemain = Mathf.Max(0f, levelUpFlashRemain - udt);
            float t01 = 1f - (levelUpFlashRemain / LevelUpFlashDuration);
            float pulse = Mathf.Sin(t01 * Mathf.PI); // 0->1->0

            if (avatarImg != null)
            {
                float s = Mathf.Lerp(1f, 1.18f, pulse);
                avatarImg.rectTransform.localScale = avatarBaseScale * s;
            }
            if (expFill != null)
            {
                var flashColor = Color.Lerp(expFillBaseColor, Color.white, pulse * 0.75f);
                expFill.color = flashColor;
                if (levelUpFlashRemain <= 0f) expFill.color = expFillBaseColor;
            }
        }

        // 治疗瓶使用反馈（闪光）
        if (potionIconImg != null && potionFlashRemain > 0f)
        {
            potionFlashRemain = Mathf.Max(0f, potionFlashRemain - udt);
            float t01 = 1f - (potionFlashRemain / PotionFlashDuration);
            float pulse = Mathf.Sin(t01 * Mathf.PI);
            var flash = new Color(0.3f, 1f, 0.35f, 1f);
            potionIconImg.color = Color.Lerp(potionBaseColor, flash, pulse);
            if (potionFlashRemain <= 0f) potionIconImg.color = potionBaseColor;
        }

        UpdateSkillCooldownUI(0);
        UpdateSkillCooldownUI(1);
    }

    private void CreateJoystickArea(RectTransform parent)
    {
        // 固定尺寸摇杆：避免用百分比锚点导致在不同分辨率/安全区下过大
        GameObject joystickGo = new GameObject("_Joystick_Move", typeof(RectTransform));
        joystickGo.transform.SetParent(parent, false);
        RectTransform rt = joystickGo.transform as RectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        // parent 已是 SafeArea，所以这里用小偏移即可
        rt.anchoredPosition = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(320f, 320f);
        var img = joystickGo.AddComponent<Image>();
        img.color = Color.white;
        UITheme.ApplyImageSprite(img, "UI/Gameplay/UI_Joystick_BG", preserveAspect: true);
        img.raycastTarget = true;

        // 手柄
        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(joystickGo.transform, false);
        var handleRt = handleGo.AddComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0.5f, 0.5f);
        handleRt.anchorMax = new Vector2(0.5f, 0.5f);
        handleRt.pivot = new Vector2(0.5f, 0.5f);
        handleRt.anchoredPosition = Vector2.zero;
        handleRt.sizeDelta = new Vector2(120f, 120f);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = Color.white;
        handleImg.raycastTarget = false;
        UITheme.ApplyImageSprite(handleImg, "UI/Gameplay/UI_Joystick_Knob", preserveAspect: true);

        var joystick = joystickGo.AddComponent<VirtualJoystickUI>();
        joystick.stickArea = rt;
        joystick.stickHandle = handleRt;
        joystick.maxOffset = 80f;
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

        // 默认按小按钮样式（Pause/Skill）
        UITheme.ApplySpriteSwapButton(btn, "UI_Button_Small_Normal", "UI_Button_Small_Pressed", "UI_Button_Small_Disabled");

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = UITheme.DefaultFont;
        text.fontSize = 22;
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

    private Image CreateImage(RectTransform parent, string spritePath, Vector2 anchorPos, Vector2 size, bool preserveAspect)
    {
        var go = new GameObject("Image");
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
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect);
        return img;
    }

    private Image CreateChildFill(RectTransform parent, string spritePath)
    {
        var go = new GameObject("Fill");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect: false);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = 0;
        img.fillAmount = 1f;
        return img;
    }

    private Image CreateCooldownMask(Transform parent, string spritePath, Vector2 size)
    {
        var go = new GameObject("CooldownMask");
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
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = 2; // Top
        img.fillClockwise = false;
        img.fillAmount = 0f;
        return img;
    }

    private Image CreateIcon(RectTransform parent, string spritePath, Vector2 size, Vector2 anchorPos)
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
        return img;
    }

    private Image CreateIcon(Transform parent, string spritePath, Vector2 size)
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
        return img;
    }

    private void OnHealthChanged(object data)
    {
        if (!IsGameplayActive() || healthText == null || !(data is object[] arr) || arr.Length < 2) return;
        float current = arr[0] is float c ? c : 0f;
        float max = arr[1] is float m ? m : 1f;
        healthText.text = $"{current:F0}/{max:F0}";
        if (hpFill != null) hpFill.fillAmount = max > 0 ? Mathf.Clamp01(current / max) : 0f;
    }

    private void OnGoldChanged(object data)
    {
        if (!IsGameplayActive() || goldText == null || !(data is object[] arr) || arr.Length < 1) return;
        int gold = arr[0] is int g ? g : 0;
        targetGold = gold;
        if (!goldInited)
        {
            goldInited = true;
            displayedGold = targetGold;
        }

        // delta > 0 才闪光（“获得金币”的反馈）
        int delta = 0;
        if (arr.Length > 1 && arr[1] is int d) delta = d;
        if (delta > 0 && goldIconImg != null)
        {
            goldFlashRemain = GoldFlashDuration;
            goldIconImg.color = new Color(1f, 1f, 0.35f, 1f);
        }
    }

    private void OnExpGained(object data)
    {
        if (!IsGameplayActive()) return;
        RefreshFromPlayer();
    }

    private void OnLevelUp(object data)
    {
        if (!IsGameplayActive()) return;
        RefreshFromPlayer();
        levelUpFlashRemain = LevelUpFlashDuration;
        if (UIManager.Instance != null) UIManager.Instance.ShowToast("升级！");
    }

    private void OnSkillUsed(object data)
    {
        if (!IsGameplayActive()) return;
        int skillId = 0;
        if (data is object[] arr && arr.Length > 0 && arr[0] is int sid) skillId = sid;
        if (lastSkillTimes == null || skillId < 0 || skillId >= lastSkillTimes.Length) return;

        lastSkillTimes[skillId] = Time.time;

        // 与 PlayerController 当前技能冷却保持一致（配置化后可能变化）
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null && skillCooldowns != null)
        {
            if (skillCooldowns.Length > 0) skillCooldowns[0] = pc.GetSkillCooldownSeconds(0);
            if (skillCooldowns.Length > 1) skillCooldowns[1] = pc.GetSkillCooldownSeconds(1);
            if (skillCooldowns.Length > 2) skillCooldowns[2] = pc.GetDodgeCooldownSeconds();
        }
    }

    private void OnSkill(int skillId)
    {
        if (lastSkillTimes == null || skillCooldowns == null) return;
        if (skillId < 0 || skillId >= lastSkillTimes.Length || skillId >= skillCooldowns.Length) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null)
            skillCooldowns[skillId] = (skillId == 2) ? pc.GetDodgeCooldownSeconds() : pc.GetSkillCooldownSeconds(skillId);
        if (Time.time < lastSkillTimes[skillId] + skillCooldowns[skillId])
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowToast("技能冷却中");
            return;
        }
        if (pc != null) pc.UseSkill(skillId);
        lastSkillTimes[skillId] = Time.time;
    }

    private void OnPotion()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null)
        {
            bool ok = pc.TryUsePotion();
            if (!ok)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX_UI_Error();
                if (UIManager.Instance != null) UIManager.Instance.ShowToast("治疗瓶不足");
                return;
            }
        }
        // 成功使用：图标闪光
        potionFlashRemain = PotionFlashDuration;
        RefreshFromPlayer();
    }

    private void OnPotionChanged(object data)
    {
        if (!IsGameplayActive()) return;
        RefreshFromPlayer();
    }

    private void ApplyPotionVisualState(bool hasPotion)
    {
        // 禁用态：半透明、略灰
        if (potionBgImg != null)
            potionBgImg.color = hasPotion ? Color.white : new Color(1f, 1f, 1f, 0.5f);

        if (potionIconImg != null)
            potionIconImg.color = hasPotion ? potionBaseColor : new Color(0.7f, 0.7f, 0.7f, 0.7f);

        if (potionCountText != null)
            potionCountText.color = hasPotion ? Color.white : new Color(1f, 1f, 1f, 0.7f);
    }

    private void OnPause()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();
    }

    private void OnSettings()
    {
        // UI策划案 v3.2：右上角设置按钮。首版实现：先暂停再打开设置弹窗。
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSettingsPopup();
    }

    private void OnBag()
    {
        // 需求知识库 v2.2：战斗界面必须包含背包入口
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowInventoryPopupInGame();
    }

    private void UpdateSkillCooldownUI(int skillId)
    {
        if (skillCdTexts == null || skillCdTexts.Length <= skillId) return;
        var cdText = skillCdTexts[skillId];
        var mask = skillCooldownMasks != null && skillCooldownMasks.Length > skillId ? skillCooldownMasks[skillId] : null;

        float cd = (skillCooldowns != null && skillCooldowns.Length > skillId) ? skillCooldowns[skillId] : 0f;
        float last = (lastSkillTimes != null && lastSkillTimes.Length > skillId) ? lastSkillTimes[skillId] : 0f;
        if (cd <= 0.01f)
        {
            if (cdText != null) cdText.text = "";
            if (mask != null) mask.fillAmount = 0f;
            return;
        }

        if (cdText != null && Time.time < last + cd)
        {
            float remain = last + cd - Time.time;
            cdText.text = Mathf.CeilToInt(remain) + "s";
            if (mask != null) mask.fillAmount = Mathf.Clamp01(remain / cd);
        }
        else
        {
            if (cdText != null) cdText.text = "";
            if (mask != null) mask.fillAmount = 0f;
        }
    }

    private Button CreateSkillButton(RectTransform parent, Vector2 anchorPos, string iconSpritePath, int skillId)
    {
        var btn = CreateButton(parent, "", anchorPos, new Vector2(120f, 120f));
        btn.onClick.AddListener(() => OnSkill(skillId));
        var img = btn.GetComponent<Image>();
        if (img != null) UITheme.ApplyImageSprite(img, "UI/Gameplay/UI_SkillBtn_BG", preserveAspect: true);
        CreateIcon(btn.transform, iconSpritePath, new Vector2(90f, 90f));
        skillCooldownMasks[skillId] = CreateCooldownMask(btn.transform, "UI/Gameplay/UI_Skill_CooldownMask", new Vector2(120f, 120f));
        skillCdTexts[skillId] = btn.GetComponentInChildren<Text>();
        if (skillCdTexts[skillId] != null) skillCdTexts[skillId].text = "";
        return btn;
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
}
