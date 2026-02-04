// COMP-02 顶部状态栏：显示头像/等级/金币（钻石/体力预留）。
// 依据：需求知识库 v2.2 0.6.3、策划知识库 v2.2 0.6、开发计划 v2.4
using System;
using UnityEngine;
using UnityEngine.UI;

public class TopStatusBar : MonoBehaviour
{
    private Text levelText;
    private Text goldText;
    private Image avatarImage;

    private Action<object> onGoldChanged;
    private Action<object> onLevelUp;

    // UI策划案 v3.2：金币数字平滑变化（0.5s）
    private int targetGold;
    private float displayedGold;
    private float goldVel;
    private bool goldInited;

    private void Start()
    {
        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(0f, 140f);

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;

        // 左侧：头像 + 等级（拉开间距，避免头像与「Lv.x」重叠）
        avatarImage = CreateImage(root, "_Image_Avatar", "UI/MainMenu/UI_MainMenu_Logo", new Vector2(0.08f, 0.5f), new Vector2(96f, 96f), true);
        if (avatarImage != null) avatarImage.raycastTarget = false;

        levelText = CreateText(root, "_Text_Level", "Lv.1", new Vector2(0.26f, 0.5f), new Vector2(200f, 60f));
        levelText.alignment = TextAnchor.MiddleLeft;
        levelText.fontSize = 30;
        var levelRt = levelText.rectTransform;
        levelRt.pivot = new Vector2(0f, 0.5f);
        levelRt.anchorMin = new Vector2(0.26f, 0.5f);
        levelRt.anchorMax = new Vector2(0.26f, 0.5f);
        levelRt.anchoredPosition = Vector2.zero;

        // 右侧：金币图标 + 数字（拉开间距，避免数字叠在图标上）
        CreateImage(root, "_Image_GoldIcon", "UI/Common/UI_Icon_Gold", new Vector2(0.68f, 0.5f), new Vector2(64f, 64f), true);
        goldText = CreateText(root, "_Text_Gold", "0", new Vector2(0.88f, 0.5f), new Vector2(220f, 60f));
        goldText.alignment = TextAnchor.MiddleLeft;
        goldText.fontSize = 30;
        var goldRt = goldText.rectTransform;
        goldRt.pivot = new Vector2(0f, 0.5f);
        goldRt.anchorMin = new Vector2(0.88f, 0.5f);
        goldRt.anchorMax = new Vector2(0.88f, 0.5f);
        goldRt.anchoredPosition = Vector2.zero;

        RefreshFromRuntimeOrSave();
    }

    private void OnEnable()
    {
        onGoldChanged = _ => RefreshFromRuntimeOrSave();
        onLevelUp = _ => RefreshFromRuntimeOrSave();
        EventManager.AddListener("GOLD_CHANGED", onGoldChanged);
        EventManager.AddListener("LEVEL_UP", onLevelUp);
        RefreshFromRuntimeOrSave();
    }

    private void OnDisable()
    {
        if (onGoldChanged != null) EventManager.RemoveListener("GOLD_CHANGED", onGoldChanged);
        if (onLevelUp != null) EventManager.RemoveListener("LEVEL_UP", onLevelUp);
    }

    private void Update()
    {
        if (goldText == null) return;
        float dt = Time.unscaledDeltaTime;
        displayedGold = Mathf.SmoothDamp(displayedGold, targetGold, ref goldVel, 0.5f, Mathf.Infinity, dt);
        goldText.text = Mathf.RoundToInt(displayedGold).ToString();
    }

    public void RefreshFromRuntimeOrSave()
    {
        int level = 1;
        int gold = 0;

        // 优先运行态玩家（InGame）
        var player = GameObject.FindGameObjectWithTag("Player");
        var ps = player != null ? player.GetComponent<PlayerStats>() : null;
        if (ps != null)
        {
            level = ps.Level;
            gold = ps.Gold;
        }
        else
        {
            // 主菜单等无 Player：用存档
            SaveSystem.EnsureLoaded();
            var save = SaveSystem.GetCached();
            if (save != null && save.player != null)
            {
                level = Mathf.Max(1, save.player.level);
                gold = Mathf.Max(0, save.player.gold);
            }
        }

        if (levelText != null) levelText.text = $"Lv.{level}";
        targetGold = gold;
        if (!goldInited)
        {
            goldInited = true;
            displayedGold = targetGold;
        }
    }

    private static Image CreateImage(RectTransform parent, string name, string spritePath, Vector2 anchorPos, Vector2 size, bool preserveAspect)
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
        img.raycastTarget = false;
        UITheme.ApplyImageSprite(img, spritePath, preserveAspect);
        return img;
    }

    private static Text CreateText(RectTransform parent, string name, string content, Vector2 anchorPos, Vector2 size)
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
        text.font = UITheme.DefaultFont;
        text.color = Color.white;
        return text;
    }
}

