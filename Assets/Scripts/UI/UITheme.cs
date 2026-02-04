// UI 主题：从 Resources/UI/ 加载切图并应用到动态生成的 UGUI。
// 资源来自：根目录 WarcraftReturn_UI（已复制到 Assets/Resources/UI/...）
using UnityEngine;
using UnityEngine.UI;

public static class UITheme
{
    private static Font _defaultFont;

    /// <summary>用于 UGUI Text 的默认字体，保证不返回 null（避免 NullReferenceException）。</summary>
    public static Font DefaultFont
    {
        get
        {
            if (_defaultFont != null) return _defaultFont;
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null) _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_defaultFont == null) _defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            return _defaultFont;
        }
    }

    /// <summary>
    /// 从 Resources 加载 Sprite。
    /// - 传入带 "/" 的路径：按原样作为 Resources 路径（不含扩展名）
    /// - 传入不带 "/" 的文件名：自动在 UI/Common|MainMenu|Gameplay|Settlement 下尝试
    /// </summary>
    public static Sprite LoadSprite(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return null;

        if (nameOrPath.Contains("/"))
            return Resources.Load<Sprite>(nameOrPath);

        // 统一在 Resources/UI/* 下查找
        string[] candidates =
        {
            "UI/Common/" + nameOrPath,
            "UI/MainMenu/" + nameOrPath,
            "UI/Gameplay/" + nameOrPath,
            "UI/Settlement/" + nameOrPath,
            "UI/" + nameOrPath
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            var s = Resources.Load<Sprite>(candidates[i]);
            if (s != null) return s;
        }
        return null;
    }

    public static void ApplySpriteSwapButton(Button btn, string normal, string pressed, string disabled)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img == null) img = btn.gameObject.AddComponent<Image>();

        var sNormal = LoadSprite(normal);
        var sPressed = LoadSprite(pressed);
        var sDisabled = LoadSprite(disabled);

        if (sNormal != null)
        {
            img.sprite = sNormal;
            img.color = Color.white;
        }

        // 只有当至少有 normal/pressed 时才启用 SpriteSwap，否则保持 ColorTint/默认
        if (sNormal != null || sPressed != null || sDisabled != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.pressedSprite = sPressed;
            ss.disabledSprite = sDisabled;
            btn.spriteState = ss;
        }
    }

    public static void ApplyImageSprite(Image img, string spriteNameOrPath, bool preserveAspect = true)
    {
        if (img == null) return;
        var s = LoadSprite(spriteNameOrPath);
        if (s == null) return;
        img.sprite = s;
        img.color = Color.white;
        img.preserveAspect = preserveAspect;
    }
}

