using UnityEngine;

/// <summary>
/// Simple UV scroller for runtime-generated materials (works for URP Lit and Standard as long as the shader supports tiling/offset).
/// </summary>
[DisallowMultipleComponent]
public class TextureScroll : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.02f, 0.00f);
    public string propertyName = "_BaseMap"; // URP Lit
    public Vector2 secondarySpeed = new Vector2(0.00f, 0.00f);
    public string secondaryPropertyName = "_BumpMap"; // optional

    private Renderer rr;
    private Material mat;
    private Vector2 offset;
    private Vector2 offset2;

    private void Awake()
    {
        rr = GetComponent<Renderer>();
        if (rr == null) rr = GetComponentInChildren<Renderer>();
        if (rr == null) enabled = false;
    }

    private void Start()
    {
        if (rr == null) return;
        mat = rr.material; // instance
        if (mat == null) { enabled = false; return; }

        // fallback to Standard "_MainTex"
        if (!mat.HasProperty(propertyName) && mat.HasProperty("_MainTex"))
            propertyName = "_MainTex";

        if (!mat.HasProperty(propertyName)) enabled = false;

        if (!string.IsNullOrEmpty(secondaryPropertyName) && !mat.HasProperty(secondaryPropertyName))
        {
            secondaryPropertyName = null;
        }
    }

    private void Update()
    {
        if (mat == null) return;
        offset += speed * Time.deltaTime;
        mat.SetTextureOffset(propertyName, offset);

        if (!string.IsNullOrEmpty(secondaryPropertyName) && (secondarySpeed.sqrMagnitude > 0.000001f))
        {
            offset2 += secondarySpeed * Time.deltaTime;
            mat.SetTextureOffset(secondaryPropertyName, offset2);
        }
    }
}

