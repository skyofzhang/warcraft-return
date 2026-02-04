// 弹窗简单动画：Enable 时缩放+淡入（unscaled 时间，兼容 Pause）
using System.Collections;
using UnityEngine;

public class UIPopupAnimator : MonoBehaviour
{
    public float duration = 0.14f;
    public float fromScale = 0.92f;

    private CanvasGroup cg;
    private RectTransform rt;
    private Coroutine routine;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoAnimIn());
    }

    private IEnumerator CoAnimIn()
    {
        float d = Mathf.Clamp(duration, 0.05f, 0.5f);
        float t = 0f;
        if (rt != null) rt.localScale = Vector3.one * Mathf.Clamp(fromScale, 0.6f, 1f);
        if (cg != null) cg.alpha = 0f;

        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            // easeOut
            float e = 1f - Mathf.Pow(1f - k, 3f);
            if (rt != null) rt.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, e);
            if (cg != null) cg.alpha = e;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
        if (cg != null) cg.alpha = 1f;
        routine = null;
    }
}

