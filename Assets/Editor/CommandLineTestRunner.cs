#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// 解决部分环境下 -runTests 不产出结果文件的问题：提供可被 -executeMethod 调用的测试运行入口。
/// 用法示例：
/// Unity.exe -batchmode -nographics -projectPath <path> -executeMethod CommandLineTestRunner.RunPlayMode -testResults <xmlPath> -logFile <logPath>
/// </summary>
public static class CommandLineTestRunner
{
    private const string SessionKeyPending = "MT.CommandLineTestRunner.Pending";
    private const string SessionKeyStarted = "MT.CommandLineTestRunner.Started";
    private const string SessionKeyResultsPath = "MT.CommandLineTestRunner.ResultsPath";

    private static bool s_started;
    private static TestRunnerApi s_api;
    private static Callbacks s_callbacks;
    private static double s_watchdogStart;
    private const double WatchdogTimeoutSeconds = 8 * 60; // 8min：避免 batchmode 卡死无产物（PlayMode 全量应远小于此）

    public static void RunPlayMode()
    {
        string resultsPath = GetArgValue("-testResults");
        if (string.IsNullOrEmpty(resultsPath))
            resultsPath = Path.GetFullPath("playmode_results.xml");

        // PlayMode 测试会触发域重载：TestRunnerApi 的 callbacks 注册表会被清空。
        // 所以这里把关键信息写到 SessionState，让 afterAssemblyReload 能重新注册回调，否则 batchmode 会“跑完不回调/不退出”。
        SessionState.SetBool(SessionKeyPending, true);
        SessionState.SetBool(SessionKeyStarted, false);
        SessionState.SetString(SessionKeyResultsPath, resultsPath);

        try
        {
            string dir = Path.GetDirectoryName(resultsPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        catch
        {
            // ignore
        }

        // 注意：-executeMethod 触发时机可能早于“项目完全加载完毕”，直接 Execute 在部分环境下不会启动测试。
        // 这里用 delayCall 把执行延后到 Editor 主循环，确保 TestRunner 可用。
        Debug.Log($"[CommandLineTestRunner] Scheduled PlayMode tests. resultsPath={resultsPath}");
        EditorApplication.delayCall += () => StartPlayModeTests(resultsPath);
    }

    private static void StartPlayModeTests(string resultsPath)
    {
        if (s_started) return;
        s_started = true;
        SessionState.SetBool(SessionKeyStarted, true);

        // BatchMode 下 Test Framework 会尝试保存“已修改的场景”，并弹窗询问（不允许）。
        // 这里主动切换到一个已保存的场景，避免出现 Untitled 脏场景导致测试无法启动。
        try
        {
            if (EditorSceneManager.GetActiveScene().isDirty || string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            {
                const string mainMenuScenePath = "Assets/Scenes/MainMenu.unity";
                if (File.Exists(mainMenuScenePath))
                    EditorSceneManager.OpenScene(mainMenuScenePath, OpenSceneMode.Single);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CommandLineTestRunner] Prepare scene failed: {e}");
        }

        Debug.Log($"[CommandLineTestRunner] Start PlayMode tests. resultsPath={resultsPath}");
        // TestRunnerApi 也必须跨 PlayMode 域重载存活，否则 callbacks 可能永远不会触发（batchmode 会卡死无产物）
        if (s_api == null)
        {
            s_api = ScriptableObject.CreateInstance<TestRunnerApi>();
            s_api.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        }
        // 回调对象必须在进入/退出 PlayMode 的域重载过程中存活，否则 RunFinished 可能无法回调到这里
        if (s_callbacks == null)
        {
            s_callbacks = ScriptableObject.CreateInstance<Callbacks>();
            s_callbacks.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        }
        s_callbacks.SetResultsPath(resultsPath);

        // 注册 callbacks（注意：域重载后会丢失，需要由 InitializeOnLoad 重新注册）
        try { s_api.UnregisterCallbacks(s_callbacks); } catch { /* ignore */ }
        s_api.RegisterCallbacks(s_callbacks);

        // Watchdog：防止极端情况下测试结束但回调丢失导致进程永不退出
        s_watchdogStart = EditorApplication.timeSinceStartup;
        EditorApplication.update -= WatchdogUpdate;
        EditorApplication.update += WatchdogUpdate;

        s_api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
    }

    private static void WatchdogUpdate()
    {
        if (EditorApplication.timeSinceStartup - s_watchdogStart < WatchdogTimeoutSeconds) return;
        EditorApplication.update -= WatchdogUpdate;

        string resultsPath = null;
        try { resultsPath = s_callbacks != null ? s_callbacks.ResultsPathForWatchdog : null; } catch { /* ignore */ }
        if (string.IsNullOrEmpty(resultsPath)) resultsPath = Path.GetFullPath("playmode_results.xml");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resultsPath));
            File.WriteAllText(resultsPath, "<test-run passed=\"0\" failed=\"1\" skipped=\"0\" inconclusive=\"0\" message=\"watchdog timeout\" />\n");
            Debug.LogError($"[CommandLineTestRunner] Watchdog timeout. Wrote minimal results and exit: {resultsPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommandLineTestRunner] Watchdog timeout. Failed writing results: {e}");
        }

        EditorApplication.Exit(2);
    }

    private static string GetArgValue(string name)
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private class Callbacks : ScriptableObject, ICallbacks
    {
        [SerializeField] private string resultsPath;

        public string ResultsPathForWatchdog => resultsPath;

        public void SetResultsPath(string resultsPath)
        {
            this.resultsPath = resultsPath;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log("[CommandLineTestRunner] RunStarted");
            try
            {
                if (!string.IsNullOrEmpty(resultsPath))
                    File.WriteAllText(resultsPath + ".started", DateTime.Now.ToString("s"));
            }
            catch { /* ignore */ }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            EditorApplication.update -= WatchdogUpdate;
            int fail = GetIntProp(result, "FailCount");
            int pass = GetIntProp(result, "PassCount");
            int skip = GetIntProp(result, "SkipCount");
            int inconclusive = GetIntProp(result, "InconclusiveCount");

            string xml = TryGetXml(result);
            if (string.IsNullOrEmpty(xml))
            {
                // 兜底：写一个最小 XML，保证命令行有产物
                xml =
                    $"<test-run passed=\"{pass}\" failed=\"{fail}\" skipped=\"{skip}\" inconclusive=\"{inconclusive}\" />\n";
            }

            try
            {
                File.WriteAllText(resultsPath, xml);
                Debug.Log($"[CommandLineTestRunner] Wrote test results: {resultsPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CommandLineTestRunner] Failed writing results to {resultsPath}: {e}");
                // 写文件失败也要退出，避免 CI 卡住
            }

            try
            {
                if (!string.IsNullOrEmpty(resultsPath))
                    File.WriteAllText(resultsPath + ".finished", DateTime.Now.ToString("s"));
            }
            catch { /* ignore */ }

            // 清理 SessionState（避免后续域重载重复注册/重复退出）
            SessionState.SetBool(SessionKeyPending, false);
            SessionState.SetBool(SessionKeyStarted, false);
            SessionState.EraseString(SessionKeyResultsPath);

            // 非 0 表示失败，便于脚本化检查
            EditorApplication.Exit(fail > 0 ? 1 : 0);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        private static int GetIntProp(object obj, string name)
        {
            if (obj == null) return 0;
            try
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                if (p != null && p.PropertyType == typeof(int))
                    return (int)p.GetValue(obj);
            }
            catch
            {
                // ignore
            }
            return 0;
        }

        private static string TryGetXml(ITestResultAdaptor result)
        {
            if (result == null) return null;
            try
            {
                // 反射调用：不同版本 Test Framework 的 API 形态略有差异
                // 尝试：ToXml(bool) -> XmlNode
                var m = result.GetType().GetMethod("ToXml", BindingFlags.Instance | BindingFlags.Public);
                if (m != null)
                {
                    object xmlObj = null;
                    var ps = m.GetParameters();
                    if (ps != null && ps.Length == 1 && ps[0].ParameterType == typeof(bool))
                        xmlObj = m.Invoke(result, new object[] { true });
                    else if (ps == null || ps.Length == 0)
                        xmlObj = m.Invoke(result, null);

                    if (xmlObj is XmlNode node) return node.OuterXml;
                    if (xmlObj is XmlDocument doc) return doc.OuterXml;
                    // NUnit TNode：有 OuterXml 属性，但 ToString() 只会返回类型名
                    if (xmlObj != null)
                    {
                        var pOuter = xmlObj.GetType().GetProperty("OuterXml", BindingFlags.Instance | BindingFlags.Public);
                        if (pOuter != null && pOuter.PropertyType == typeof(string))
                            return (string)pOuter.GetValue(xmlObj);
                    }
                    if (xmlObj != null) return xmlObj.ToString();
                }
            }
            catch
            {
                // ignore
            }
            return null;
        }
    }
}

/// <summary>
/// 域重载后重新注册 TestRunner callbacks（Test Framework 的 callbacks 注册表是静态单例，会在重载时被清空）。
/// </summary>
[InitializeOnLoad]
public static class CommandLineTestRunnerBootstrap
{
    static CommandLineTestRunnerBootstrap()
    {
        if (!Application.isBatchMode) return;
        if (!SessionState.GetBool("MT.CommandLineTestRunner.Pending", false)) return;
        if (!SessionState.GetBool("MT.CommandLineTestRunner.Started", false)) return;
        var resultsPath = SessionState.GetString("MT.CommandLineTestRunner.ResultsPath", null);
        if (string.IsNullOrEmpty(resultsPath)) return;

        // 触发一次“空注册”：确保 callbacks 在当前域存在并被注册到 CallbacksHolder.instance
        try
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            var cb = ScriptableObject.CreateInstance<CommandLineTestRunner_CallbacksProxy>();
            cb.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
            cb.SetResultsPath(resultsPath);
            api.RegisterCallbacks(cb);
            Debug.Log("[CommandLineTestRunner] Bootstrap re-registered callbacks after domain reload.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CommandLineTestRunner] Bootstrap callback register failed: {e}");
        }
    }
}

/// <summary>
/// 独立的 callbacks 类型（避免访问 CommandLineTestRunner 的私有嵌套类）。
/// 只负责 RunFinished 写结果并退出；RunStarted 可选写标记文件。
/// </summary>
public class CommandLineTestRunner_CallbacksProxy : ScriptableObject, ICallbacks
{
    [SerializeField] private string resultsPath;

    public void SetResultsPath(string resultsPath) => this.resultsPath = resultsPath;

    public void RunStarted(ITestAdaptor testsToRun)
    {
        Debug.Log("[CommandLineTestRunner] RunStarted (proxy)");
        try
        {
            if (!string.IsNullOrEmpty(resultsPath))
                File.WriteAllText(resultsPath + ".started", DateTime.Now.ToString("s"));
        }
        catch { /* ignore */ }
    }

    public void RunFinished(ITestResultAdaptor result)
    {
        int fail = 0, pass = 0, skip = 0, inconclusive = 0;
        try
        {
            if (result != null)
            {
                fail = GetIntProp(result, "FailCount");
                pass = GetIntProp(result, "PassCount");
                skip = GetIntProp(result, "SkipCount");
                inconclusive = GetIntProp(result, "InconclusiveCount");
            }
        }
        catch { /* ignore */ }

        string xml = TryGetXml(result);
        if (string.IsNullOrEmpty(xml))
            xml = $"<test-run passed=\"{pass}\" failed=\"{fail}\" skipped=\"{skip}\" inconclusive=\"{inconclusive}\" />\n";

        try
        {
            if (string.IsNullOrEmpty(resultsPath)) resultsPath = Path.GetFullPath("playmode_results.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(resultsPath));
            File.WriteAllText(resultsPath, xml);
            File.WriteAllText(resultsPath + ".finished", DateTime.Now.ToString("s"));
            Debug.Log($"[CommandLineTestRunner] Wrote test results (proxy): {resultsPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommandLineTestRunner] Failed writing results (proxy): {e}");
        }

        SessionState.SetBool("MT.CommandLineTestRunner.Pending", false);
        SessionState.SetBool("MT.CommandLineTestRunner.Started", false);
        SessionState.EraseString("MT.CommandLineTestRunner.ResultsPath");

        EditorApplication.Exit(fail > 0 ? 1 : 0);
    }

    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }

    private static int GetIntProp(object obj, string name)
    {
        if (obj == null) return 0;
        try
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.PropertyType == typeof(int))
                return (int)p.GetValue(obj);
        }
        catch { /* ignore */ }
        return 0;
    }

    private static string TryGetXml(ITestResultAdaptor result)
    {
        if (result == null) return null;
        try
        {
            var m = result.GetType().GetMethod("ToXml", BindingFlags.Instance | BindingFlags.Public);
            if (m != null)
            {
                object xmlObj = null;
                var ps = m.GetParameters();
                if (ps != null && ps.Length == 1 && ps[0].ParameterType == typeof(bool))
                    xmlObj = m.Invoke(result, new object[] { true });
                else if (ps == null || ps.Length == 0)
                    xmlObj = m.Invoke(result, null);

                if (xmlObj is XmlNode node) return node.OuterXml;
                if (xmlObj is XmlDocument doc) return doc.OuterXml;
                // NUnit TNode：有 OuterXml 属性，但 ToString() 只会返回类型名
                if (xmlObj != null)
                {
                    var pOuter = xmlObj.GetType().GetProperty("OuterXml", BindingFlags.Instance | BindingFlags.Public);
                    if (pOuter != null && pOuter.PropertyType == typeof(string))
                        return (string)pOuter.GetValue(xmlObj);
                }
                if (xmlObj != null) return xmlObj.ToString();
            }
        }
        catch { /* ignore */ }
        return null;
    }
}
#endif

