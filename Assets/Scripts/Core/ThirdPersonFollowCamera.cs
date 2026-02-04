// 依据：程序基础知识库 5.2、GDD 1.3、程序指南 任务 3.7（无 Cinemachine 时的简易实现）
using UnityEngine;

/// <summary>
/// 第三人称跟随相机，平滑跟随玩家。建议有 Cinemachine 时改用 Cinemachine Virtual Camera。
/// </summary>
public class ThirdPersonFollowCamera : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("跟随参数")]
    public float distance = 8f;
    public float height = 3f;
    public float smoothTime = 0.2f;

    private Vector3 velocity = Vector3.zero;
    private float lastFindTime = -1f;
    private const float FindPlayerInterval = 0.5f;

    [Header("镜头震动（战斗表现力）")]
    public float shakeIntensity = 0.12f;
    public float shakeDuration = 0.12f;
    private float shakeUntil;
    private float shakeAmp;

    public void Shake(float intensity, float duration)
    {
        shakeAmp = Mathf.Max(shakeAmp, Mathf.Max(0f, intensity));
        shakeUntil = Mathf.Max(shakeUntil, Time.unscaledTime + Mathf.Max(0f, duration));
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (Time.time - lastFindTime >= FindPlayerInterval)
            {
                lastFindTime = Time.time;
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            return;
        }

        Vector3 desiredPosition = target.position - target.forward * distance + Vector3.up * height;
        desiredPosition.y = target.position.y + height;

        // Base follow
        var pos = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // Shake (unscaled, so it still works when timeScale changes)
        if (Time.unscaledTime < shakeUntil && shakeAmp > 0.0001f)
        {
            float k = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / Mathf.Max(0.001f, shakeDuration));
            float amp = shakeAmp * k;
            Vector3 r = Random.insideUnitSphere;
            r.z = 0f; // don't change distance too much
            pos += r * amp;
        }
        else
        {
            shakeAmp = 0f;
        }

        transform.position = pos;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
