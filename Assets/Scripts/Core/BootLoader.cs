// Boot 场景启动器：创建核心 Managers，并切到 MainMenu
// 依据：程序知识库（Unity）v2.1.1 1.2 必需场景；程序基础知识库 5.1 必需场景
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        EnsureManagers();

        // 进入主界面场景（Boot 仅负责初始化）
        if (!string.IsNullOrEmpty(mainMenuSceneName) && SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError($"[BootLoader] 无法加载主界面场景（未加入 Build Settings?）: {mainMenuSceneName}");
                return;
            }
            // 规则：场景切换统一通过 GameManager，避免普通脚本直接 LoadScene
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
            else SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private static void EnsureManagers()
    {
        // 注意：各 Manager 自身 Awake 会 DontDestroyOnLoad 并处理重复实例。
        if (EventManager.Instance == null) new GameObject(nameof(EventManager)).AddComponent<EventManager>();
        if (ConfigManager.Instance == null) new GameObject(nameof(ConfigManager)).AddComponent<ConfigManager>();
        if (AudioManager.Instance == null) new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
        if (GameManager.Instance == null) new GameObject(nameof(GameManager)).AddComponent<GameManager>();
        if (UIManager.Instance == null) new GameObject(nameof(UIManager)).AddComponent<UIManager>();
    }
}

