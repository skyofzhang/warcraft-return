// UI-14 技能升级确认弹窗
using System;
using UnityEngine;
using UnityEngine.UI;

public class SkillUpgradeConfirmationPopup : MonoBehaviour
{
    private Text titleText;
    private Text descText;
    private Button confirmBtn;
    private Button cancelBtn;

    private Action onConfirm;

    public void Show(string skillId, string skillName, int currentLv, int nextLv, int cost, bool canAfford, Action confirm)
    {
        onConfirm = confirm;
        if (titleText != null) titleText.text = "技能升级确认";
        if (descText != null)
        {
            descText.text = $"{skillName} ({skillId})\nLv.{currentLv} → Lv.{nextLv}\n消耗金币：{cost}" + (canAfford ? "" : "\n（金币不足）");
        }

        if (confirmBtn != null)
        {
            confirmBtn.interactable = canAfford;
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() =>
            {
                onConfirm?.Invoke();
                Hide();
            });
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(Hide);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        var root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = true;

        var panelGo = new GameObject("_Panel");
        panelGo.transform.SetParent(transform, false);
        var prt = panelGo.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(780f, 520f);
        var pimg = panelGo.AddComponent<Image>();
        pimg.color = Color.white;
        pimg.raycastTarget = false;
        UITheme.ApplyImageSprite(pimg, "UI/Common/UI_Panel_Background", preserveAspect: true);

        titleText = CreateText(prt, "_Text_Title", "技能升级确认", new Vector2(0.5f, 0.82f), new Vector2(720f, 70f), 40, TextAnchor.MiddleCenter);
        descText = CreateText(prt, "_Text_Desc", "", new Vector2(0.5f, 0.58f), new Vector2(720f, 170f), 24, TextAnchor.MiddleCenter);

        confirmBtn = CreateButton(prt, "_Button_Confirm", "确认", new Vector2(0.5f, 0.22f), new Vector2(420f, 120f));
        cancelBtn = CreateButton(prt, "_Button_Cancel", "取消", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));

        gameObject.SetActive(false);
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
        text.font = UITheme.DefaultFont;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        return text;
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

