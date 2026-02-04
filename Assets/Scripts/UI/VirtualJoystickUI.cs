// 依据：GDD 7.1.2 战斗界面 _Joystick_Move；程序指南 1.7 摇杆驱动 PlayerController
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 虚拟摇杆 UI：拖拽区域驱动 VirtualJoystick.JoystickInput，供 PlayerController 使用。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("摇杆区域")]
    public RectTransform stickArea;
    [Header("摇杆手柄（可选）")]
    public RectTransform stickHandle;
    [Header("最大偏移（像素）")]
    public float maxOffset = 80f;

    private Vector2 centerPos;

    private void Awake()
    {
        if (stickArea == null) stickArea = GetComponent<RectTransform>();
        centerPos = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateStick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateStick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        VirtualJoystick.JoystickInput = Vector2.zero;
        if (stickHandle != null)
            stickHandle.anchoredPosition = Vector2.zero;
    }

    private void UpdateStick(PointerEventData eventData)
    {
        if (stickArea == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(stickArea, eventData.position, eventData.pressEventCamera, out Vector2 local);
        Vector2 dir = local - centerPos;
        if (maxOffset > 0 && dir.magnitude > maxOffset)
            dir = dir.normalized * maxOffset;
        VirtualJoystick.JoystickInput = maxOffset > 0 ? dir / maxOffset : dir.normalized;
        if (stickHandle != null)
            stickHandle.anchoredPosition = dir;
    }
}
