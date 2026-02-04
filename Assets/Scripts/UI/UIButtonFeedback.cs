// 依据：策划知识库 v1.6 9.4 UI 反馈；程序基础知识库 5.5
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 按钮点击视觉反馈：按下时缩放至 scaleOnPress，松开恢复。可选音效。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("按下缩放比例")]
    [Range(0.8f, 1f)] public float scaleOnPress = 0.95f;
    [Header("动画时长")]
    public float duration = 0.08f;

    private RectTransform rt;
    private Vector3 normalScale;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (rt != null) normalScale = rt.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (rt != null) rt.localScale = normalScale * scaleOnPress;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX_ButtonClick();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (rt != null) rt.localScale = normalScale;
    }
}
