// 战斗飘字：监听伤害事件，生成屏幕空间飘字（unscaled 时间，兼容 Pause）
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatTextManager : MonoBehaviour
{
    private class PooledText
    {
        public GameObject go;
        public RectTransform rt;
        public CanvasGroup cg;
        public Text text;
    }

    private readonly Queue<PooledText> pool = new Queue<PooledText>();
    private RectTransform root;
    // instanceId -> Transform 缓存，避免每次伤害事件全场 FindObjectsOfType 扫描
    private static readonly Dictionary<int, Transform> instanceIdToTransform = new Dictionary<int, Transform>();

    private System.Action<object> onDamageDealt;
    private System.Action<object> onCrit;

    private void Start()
    {
        root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
    }

    private void OnEnable()
    {
        onDamageDealt = OnDamageEvent;
        EventManager.AddListener("DAMAGE_DEALT", onDamageDealt);
        onCrit = OnCritEvent;
        EventManager.AddListener("CRITICAL_HIT", onCrit);
    }

    private void OnDisable()
    {
        if (onDamageDealt != null) EventManager.RemoveListener("DAMAGE_DEALT", onDamageDealt);
        if (onCrit != null) EventManager.RemoveListener("CRITICAL_HIT", onCrit);
    }

    private void OnDamageEvent(object data)
    {
        // DAMAGE_* payload: [damage(float), isCrit(bool), targetInstanceId(int)]
        if (!(data is object[] arr) || arr.Length < 3) return;
        float dmg = arr[0] is float f ? f : 0f;
        bool isCrit = arr[1] is bool b && b;
        int targetId = arr[2] is int id ? id : 0;
        if (dmg <= 0f) return;

        var worldPos = ResolveWorldPos(targetId);
        ShowText(Mathf.RoundToInt(dmg).ToString(), worldPos, isCrit);
    }

    private void OnCritEvent(object data)
    {
        // CRITICAL_HIT payload: [damage(float), targetInstanceId(int)]
        if (!(data is object[] arr) || arr.Length < 2) return;
        float dmg = arr[0] is float f ? f : 0f;
        int targetId = arr[1] is int id ? id : 0;
        if (dmg <= 0f) return;
        var worldPos = ResolveWorldPos(targetId);
        // 强化暴击飘字（再飘一次，作为“额外强调”）
        ShowText("暴击 " + Mathf.RoundToInt(dmg), worldPos, crit: true);
    }

    private static Vector3 ResolveWorldPos(int instanceId)
    {
        if (instanceId != 0)
        {
            if (instanceIdToTransform.TryGetValue(instanceId, out var cached) && cached != null &&
                cached.gameObject != null && cached.gameObject.GetInstanceID() == instanceId)
                return cached.position + Vector3.up * 1.4f;

            instanceIdToTransform.Remove(instanceId);

            // 兜底：首次命中时扫描一次并缓存（避免每帧/每次伤害扫描全场景）
            var all = Object.FindObjectsOfType<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t != null && t.gameObject != null && t.gameObject.GetInstanceID() == instanceId)
                {
                    instanceIdToTransform[instanceId] = t;
                    return t.position + Vector3.up * 1.4f;
                }
            }
        }
        return Vector3.zero;
    }

    public void ShowText(string content, Vector3 worldPos, bool crit)
    {
        if (string.IsNullOrEmpty(content) || root == null) return;

        var p = GetOrCreate();
        p.text.text = content;
        p.text.color = crit ? new Color(1f, 0.82f, 0.1f, 1f) : Color.white;
        p.text.fontSize = crit ? 40 : 32;
        p.cg.alpha = 1f;
        p.go.SetActive(true);

        Vector3 screen = (Camera.main != null && worldPos != Vector3.zero) ? Camera.main.WorldToScreenPoint(worldPos) : new Vector3(Screen.width * 0.5f, Screen.height * 0.55f, 0f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
        p.rt.anchoredPosition = local;

        StartCoroutine(CoAnimate(p, crit));
    }

    private IEnumerator CoAnimate(PooledText p, bool crit)
    {
        float t = 0f;
        float dur = 0.85f;
        Vector2 start = p.rt.anchoredPosition;
        Vector2 end = start + new Vector2(0f, crit ? 160f : 120f);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            p.rt.anchoredPosition = Vector2.Lerp(start, end, k);
            p.cg.alpha = 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
            yield return null;
        }
        Recycle(p);
    }

    private PooledText GetOrCreate()
    {
        if (pool.Count > 0) return pool.Dequeue();

        var go = new GameObject("_CombatText");
        go.transform.SetParent(transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 80f);

        var cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var text = go.AddComponent<Text>();
        text.font = UITheme.DefaultFont;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        return new PooledText { go = go, rt = rt, cg = cg, text = text };
    }

    private void Recycle(PooledText p)
    {
        if (p == null) return;
        if (p.go != null) p.go.SetActive(false);
        pool.Enqueue(p);
    }
}

