using System.Collections;
using UnityEngine;

/// <summary>
/// Simple hit flash using MaterialPropertyBlock (no material instancing).
/// Works for URP Lit/SimpleLit and Standard by writing _BaseColor/_Color.
/// </summary>
public class HitFlash : MonoBehaviour
{
    public float duration = 0.08f;
    public Color flashColor = Color.white;

    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;
    private Coroutine co;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    public void Play()
    {
        if (renderers == null || renderers.Length == 0) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoFlash());
    }

    private IEnumerator CoFlash()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
            var c = Color.Lerp(Color.white, flashColor, k);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }
            yield return null;
        }
        // Clear block
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            r.SetPropertyBlock(null);
        }
        co = null;
    }
}

