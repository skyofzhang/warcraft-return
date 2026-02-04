// 依据：GDD 7.1.3 结算界面；程序基础知识库 5.5
using UnityEngine;
using UnityEngine.UI;

public class SettlementPanel : MonoBehaviour
{
    private Text titleText;
    private Text goldText;
    private Text expText;
    /// <summary>失败时展示的保留收益文案（GDD 7.1.3、开发计划 4.3）。</summary>
    private Text retainedHintText;
    private Image expBarFill;
    private RectTransform lootRoot;
    private Text lootTitle;

    private int lastRewardGold;
    private int lastRewardExp;
    private Coroutine rewardAnimRoutine;

    private Image panelImage;
    private Image titleImage;

    private void Start()
    {
        RectTransform root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = CreateImage(root, new Color(0f, 0f, 0f, 0.7f));
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;
        bg.raycastTarget = true;

        // 中心面板
        panelImage = CreateImage(root, Color.white);
        panelImage.name = "_Panel_BG";
        panelImage.raycastTarget = false;
        panelImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        panelImage.rectTransform.anchoredPosition = Vector2.zero;
        // v2.0 切图规格：800x600
        panelImage.rectTransform.sizeDelta = new Vector2(800f, 600f);
        UITheme.ApplyImageSprite(panelImage, "UI/Settlement/UI_ResultPanel_BG", preserveAspect: true);

        // v2.0 标题规格：600x200
        titleImage = CreateChildImage(panelImage.rectTransform, "UI/Settlement/UI_Text_Victory", new Vector2(0.5f, 0.84f), new Vector2(600f, 200f));
        titleImage.name = "_Image_Title";

        titleText = CreateText(panelImage.rectTransform, "胜利", new Vector2(0.5f, 0.84f), new Vector2(400f, 80f));
        titleText.fontSize = 48;
        titleText.name = "_Text_Title";

        CreateChildImage(panelImage.rectTransform, "UI/Common/UI_Icon_Gold", new Vector2(0.32f, 0.62f), new Vector2(64f, 64f));
        goldText = CreateText(panelImage.rectTransform, "0", new Vector2(0.55f, 0.62f), new Vector2(300f, 50f));
        goldText.name = "_Text_Gold";
        goldText.alignment = TextAnchor.MiddleLeft;

        CreateChildImage(panelImage.rectTransform, "UI/Common/UI_Icon_Exp", new Vector2(0.32f, 0.54f), new Vector2(64f, 64f));
        expText = CreateText(panelImage.rectTransform, "0", new Vector2(0.55f, 0.54f), new Vector2(300f, 50f));
        expText.name = "_Text_Exp";
        expText.alignment = TextAnchor.MiddleLeft;

        // 经验条（首版：固定 0~100 简化）
        var expBg = CreateChildImage(panelImage.rectTransform, "UI/Gameplay/UI_HPBar_BG", new Vector2(0.5f, 0.44f), new Vector2(620f, 40f));
        expBg.name = "_Image_ExpBar_BG";
        expBarFill = CreateChildFill(expBg.rectTransform, "UI/Gameplay/UI_HPBar_Fill");
        expBarFill.name = "_Image_ExpBar_Fill";

        // 掉落展示（UI-05：展示装备）
        lootTitle = CreateText(panelImage.rectTransform, "掉落：", new Vector2(0.18f, 0.36f), new Vector2(220f, 40f));
        lootTitle.name = "_Text_LootTitle";
        lootTitle.fontSize = 22;
        lootTitle.alignment = TextAnchor.MiddleLeft;

        var lootGo = new GameObject("_LootRoot");
        lootGo.transform.SetParent(panelImage.rectTransform, false);
        lootRoot = lootGo.AddComponent<RectTransform>();
        lootRoot.anchorMin = new Vector2(0.18f, 0.28f);
        lootRoot.anchorMax = new Vector2(0.82f, 0.28f);
        lootRoot.pivot = new Vector2(0.5f, 0.5f);
        lootRoot.anchoredPosition = Vector2.zero;
        lootRoot.sizeDelta = new Vector2(0f, 120f);

        retainedHintText = CreateText(panelImage.rectTransform, "", new Vector2(0.5f, 0.30f), new Vector2(620f, 40f));
        retainedHintText.name = "_Text_RetainedHint";
        retainedHintText.fontSize = 22;
        retainedHintText.gameObject.SetActive(false);

        var continueBtn = CreateButton(panelImage.rectTransform, "返回主菜单", new Vector2(0.5f, 0.22f), new Vector2(400f, 120f));
        continueBtn.onClick.AddListener(OnContinue);

        var retryBtn = CreateButton(panelImage.rectTransform, "重试", new Vector2(0.5f, 0.10f), new Vector2(400f, 120f));
        retryBtn.onClick.AddListener(OnRetry);

        EventManager.AddListener("LEVEL_COMPLETED", OnLevelCompleted);
        EventManager.AddListener("LEVEL_FAILED", OnLevelFailed);
    }

    private void OnDestroy()
    {
        EventManager.RemoveListener("LEVEL_COMPLETED", OnLevelCompleted);
        EventManager.RemoveListener("LEVEL_FAILED", OnLevelFailed);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            lastRewardGold = GameManager.Instance.LastRewardGold;
            lastRewardExp = GameManager.Instance.LastRewardExp;
            SetResult(GameManager.Instance.LastVictory);
        }
        else
            RefreshReward(true);
    }

    private void OnLevelCompleted(object data)
    {
        ReadRewardFromEvent(data);
        SetResult(true);
    }

    private void OnLevelFailed(object data)
    {
        ReadRewardFromEvent(data);
        SetResult(false);
    }

    private void ReadRewardFromEvent(object data)
    {
        int g = 0, e = 0;
        if (data is object[] arr)
        {
            if (arr.Length > 0 && arr[0] is int gg) g = gg;
            if (arr.Length > 1 && arr[1] is int ee) e = ee;
        }
        lastRewardGold = Mathf.Max(0, g);
        lastRewardExp = Mathf.Max(0, e);
    }

    private void SetResult(bool victory)
    {
        if (titleText != null)
            titleText.text = victory ? "胜利" : "失败";

        if (titleImage != null)
        {
            UITheme.ApplyImageSprite(titleImage, victory ? "UI/Settlement/UI_Text_Victory" : "UI/Settlement/UI_Text_Defeat", preserveAspect: true);
            // 如果图能加载，优先用图
            if (titleImage.sprite != null && titleText != null) titleText.gameObject.SetActive(false);
            else if (titleText != null) titleText.gameObject.SetActive(true);
        }
        if (retainedHintText != null)
            retainedHintText.gameObject.SetActive(false);
        RefreshReward(victory);
        RefreshLoot(victory);
    }

    private void RefreshReward(bool victory)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || goldText == null || expText == null) return;
        var ps = player.GetComponent<PlayerStats>();
        if (ps == null) return;

        // UI策划案 v3.2：界面打开时数字递增动画（金币/经验 1s，经验条 1.5s）
        if (retainedHintText != null)
        {
            retainedHintText.gameObject.SetActive(!victory && (lastRewardGold > 0 || lastRewardExp > 0));
            if (!victory && retainedHintText.gameObject.activeSelf)
                retainedHintText.text = string.Format("本局保留: 金币 +{0}, 经验 +{1}", lastRewardGold, lastRewardExp);
        }

        float expFillTarget = Mathf.Clamp01(ps.ExpInCurrentLevel / 100f);
        if (rewardAnimRoutine != null) StopCoroutine(rewardAnimRoutine);
        rewardAnimRoutine = StartCoroutine(CoAnimateRewards(lastRewardGold, lastRewardExp, expFillTarget, victory));
    }

    private System.Collections.IEnumerator CoAnimateRewards(int gold, int exp, float expFillTarget, bool victory)
    {
        float goldDur = 1.0f;
        float expDur = 1.0f;
        float barDur = 1.5f;

        int g0 = 0;
        int e0 = 0;
        float b0 = 0f;

        float t = 0f;
        while (t < barDur)
        {
            t += Time.unscaledDeltaTime;

            float kg = goldDur > 0f ? Mathf.Clamp01(t / goldDur) : 1f;
            float ke = expDur > 0f ? Mathf.Clamp01(t / expDur) : 1f;
            float kb = barDur > 0f ? Mathf.Clamp01(t / barDur) : 1f;

            int cg = Mathf.RoundToInt(Mathf.Lerp(g0, gold, kg));
            int ce = Mathf.RoundToInt(Mathf.Lerp(e0, exp, ke));
            if (goldText != null) goldText.text = victory ? $"+{cg}" : $"+{cg} (保留)";
            if (expText != null) expText.text = victory ? $"+{ce}" : $"+{ce} (保留)";
            if (expBarFill != null) expBarFill.fillAmount = Mathf.Lerp(b0, expFillTarget, kb);

            yield return null;
        }

        if (goldText != null) goldText.text = victory ? $"+{gold}" : $"+{gold} (保留)";
        if (expText != null) expText.text = victory ? $"+{exp}" : $"+{exp} (保留)";
        if (expBarFill != null) expBarFill.fillAmount = expFillTarget;
        rewardAnimRoutine = null;
    }

    private void RefreshLoot(bool victory)
    {
        if (lootRoot == null) return;

        // 清空旧展示
        for (int i = lootRoot.childCount - 1; i >= 0; i--)
            Destroy(lootRoot.GetChild(i).gameObject);

        if (lootTitle != null) lootTitle.gameObject.SetActive(victory);
        if (!victory) return; // 首版：失败展示走 UI-16；UI-05 主要用于胜利结算

        var gm = GameManager.Instance;
        if (gm == null) return;
        var dict = gm.GetLastLootEquipSnapshot();
        if (dict == null || dict.Count == 0)
        {
            // 无掉落也要有反馈
            var t = CreateText(lootRoot, "（无）", new Vector2(0.0f, 0.5f), new Vector2(300f, 40f));
            t.alignment = TextAnchor.MiddleLeft;
            t.fontSize = 22;
            return;
        }

        // 最多展示 5 个（首版简化）
        var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>>(dict);
        list.Sort((a, b) =>
        {
            int ar = GetQualityRank(a.Key);
            int br = GetQualityRank(b.Key);
            if (ar != br) return br.CompareTo(ar); // 高品质优先
            return a.Key.CompareTo(b.Key);
        });

        int shown = 0;
        float startX = 0f;
        float dx = 140f;
        for (int i = 0; i < list.Count; i++)
        {
            int equipmentId = list[i].Key;
            int count = list[i].Value;
            if (equipmentId <= 0 || count <= 0) continue;

            float x = startX + shown * dx;
            CreateLootSlot(lootRoot, equipmentId, count, new Vector2(x, 0f));
            shown++;
            if (shown >= 5) break;
        }

        // 超出显示数量提示
        int remaining = Mathf.Max(0, list.Count - shown);
        if (remaining > 0)
        {
            var t = CreateText(lootRoot, $"+{remaining}", new Vector2(0f, 0.5f), new Vector2(260f, 44f));
            t.alignment = TextAnchor.MiddleLeft;
            t.fontSize = 22;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + shown * dx + 10f, 0f);
        }
    }

    private static int GetQualityRank(int equipmentId)
    {
        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) && cfg != null)
        {
            return cfg.quality switch
            {
                "legendary" => 4,
                "epic" => 3,
                "rare" => 2,
                "uncommon" => 1,
                _ => 0
            };
        }
        return 0;
    }

    private void CreateLootSlot(RectTransform parent, int equipmentId, int count, Vector2 localPos)
    {
        var go = new GameObject($"Loot_{equipmentId}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(120f, 120f);

        var bg = go.AddComponent<Image>();
        bg.color = Color.white;
        UITheme.ApplyImageSprite(bg, "UI/Settlement/UI_LootSlot_BG", preserveAspect: true);

        // 品质框
        string qPath = "UI/Settlement/UI_Quality_Common";
        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var cfg) && cfg != null)
        {
            qPath = cfg.quality switch
            {
                "rare" => "UI/Settlement/UI_Quality_Rare",
                "epic" => "UI/Settlement/UI_Quality_Epic",
                "legendary" => "UI/Settlement/UI_Quality_Legend",
                _ => "UI/Settlement/UI_Quality_Common"
            };
        }
        var q = new GameObject("Quality");
        q.transform.SetParent(go.transform, false);
        var qrt = q.AddComponent<RectTransform>();
        qrt.anchorMin = Vector2.zero;
        qrt.anchorMax = Vector2.one;
        qrt.offsetMin = Vector2.zero;
        qrt.offsetMax = Vector2.zero;
        var qImg = q.AddComponent<Image>();
        qImg.color = Color.white;
        qImg.raycastTarget = false;
        UITheme.ApplyImageSprite(qImg, qPath, preserveAspect: true);

        // 图标（缺失则用背包图标兜底）
        Sprite iconSprite = null;
        if (ConfigManager.Instance != null && ConfigManager.Instance.EquipmentConfigs != null &&
            ConfigManager.Instance.EquipmentConfigs.TryGetValue(equipmentId, out var e) && e != null &&
            !string.IsNullOrEmpty(e.icon_path))
        {
            iconSprite = Resources.Load<Sprite>(e.icon_path);
        }
        if (iconSprite == null) iconSprite = Resources.Load<Sprite>("UI/Common/UI_Icon_Bag");

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.5f);
        irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = Vector2.zero;
        irt.sizeDelta = new Vector2(90f, 90f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        // 数量
        if (count > 1)
        {
            var t = CreateText(rt, "x" + count, new Vector2(0.82f, 0.18f), new Vector2(90f, 40f));
            t.alignment = TextAnchor.MiddleRight;
            t.fontSize = 22;
        }
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

    private Image CreateImage(RectTransform parent, Color color)
    {
        GameObject go = new GameObject("Image");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
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

        UITheme.ApplySpriteSwapButton(btn, "UI_Button_Big_Normal", "UI_Button_Big_Pressed", "UI_Button_Big_Disabled");

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
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private Image CreateChildImage(RectTransform parent, string spritePath, Vector2 anchorPos, Vector2 size)
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
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect: true);
        return img;
    }

    private void OnContinue()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadMainMenu();
    }

    private void OnRetry()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RetryLevel();
    }
}
