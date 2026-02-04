using UnityEngine;

/// <summary>
/// Listens to combat events and triggers a small camera shake.
/// </summary>
public class CameraShakeOnDamage : MonoBehaviour
{
    private static CameraShakeOnDamage s_instance;
    private System.Action<object> onDamageDealt;
    private System.Action<object> onCrit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (s_instance != null) return;
        var go = new GameObject("__CameraShake");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<CameraShakeOnDamage>();
    }

    private void OnEnable()
    {
        onDamageDealt = OnDamageDealt;
        onCrit = OnCrit;
        EventManager.AddListener("DAMAGE_DEALT", onDamageDealt);
        EventManager.AddListener("CRITICAL_HIT", onCrit);
    }

    private void OnDisable()
    {
        if (onDamageDealt != null) EventManager.RemoveListener("DAMAGE_DEALT", onDamageDealt);
        if (onCrit != null) EventManager.RemoveListener("CRITICAL_HIT", onCrit);
    }

    private void OnDamageDealt(object data)
    {
        // payload: [damage(float), isCrit(bool), targetInstanceId(int)]
        if (!(data is object[] arr) || arr.Length < 3) return;
        int id = arr[2] is int iid ? iid : 0;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || id != player.GetInstanceID()) return;
        Shake(0.10f, 0.10f);
    }

    private void OnCrit(object data)
    {
        // payload: [damage(float), targetInstanceId(int)]
        if (!(data is object[] arr) || arr.Length < 2) return;
        int id = arr[1] is int iid ? iid : 0;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || id != player.GetInstanceID()) return;
        Shake(0.16f, 0.14f);
    }

    private static void Shake(float intensity, float duration)
    {
        var follow = Object.FindObjectOfType<ThirdPersonFollowCamera>();
        if (follow != null) follow.Shake(intensity, duration);
    }
}

