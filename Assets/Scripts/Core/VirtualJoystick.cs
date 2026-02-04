// 依据：AI程序工作指南 1.7 输入接口约定；Phase 4 摇杆驱动 PlayerController
using UnityEngine;

public static class VirtualJoystick
{
    /// <summary>
    /// UI 摇杆组件写入的输入；无 UI 或未拖拽时为 Vector2.zero。
    /// </summary>
    public static Vector2 JoystickInput { get; set; }

    /// <summary>
    /// 返回移动方向，归一化 -1～1；x 水平，y 垂直。优先使用 UI 摇杆，否则键盘。
    /// </summary>
    public static Vector2 GetInput()
    {
        if (JoystickInput.sqrMagnitude > 0.01f)
            return JoystickInput.normalized;
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}
