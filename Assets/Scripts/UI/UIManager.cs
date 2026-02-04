// 依据：程序基础知识库 5.2、5.5、5.9 第五层；GDD 7.1；事件驱动刷新
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private enum MainMenuPage
    {
        Home = 0,
        Inventory = 1,
        Shop = 2,
        Settings = 3,
        Character = 4,
        Skills = 5
    }

    private Canvas mainCanvas;
    private GameObject globalRoot;
    private TopStatusBar topStatusBar;
    private BottomNavigationBar bottomNav;

    private GameObject rootMainMenu;
    private GameObject rootGameplay;
    private GameObject rootSettlement;
    private GameObject rootPopup;
    private GameObject rootTip;

    private GameObject pageHome;
    private GameObject pageInventory;
    private GameObject pageShop;
    private GameObject pageSettings;
    private GameObject pageCharacter;
    private GameObject pageSkills;

    private MainMenuPanel mainMenuPanel;
    private InventoryPanel inventoryPanel;
    private ShopPanel shopPanel;
    private SettingsPanel settingsPanel;
    private CharacterAttributesPanel characterAttributesPanel;
    private SkillsPanel skillsPanel;

    private GameplayPanel gameplayPanel;
    private SettlementPanel settlementPanel;
    private LevelConfirmationPopup levelConfirmPopup;
    private PauseMenuPopup pauseMenuPopup;
    private FailurePopup failurePopup;
    private ItemDetailsPopup itemDetailsPopup;
    private SellConfirmationPopup sellConfirmationPopup;
    private EquipmentDetailsPopup equipmentDetailsPopup;
    private SkillUpgradeConfirmationPopup skillUpgradeConfirmationPopup;
    private ConfirmPopup confirmPopup;
    private SettingsPopup settingsPopup;
    private InventoryPanel inventoryPopupInGame;

    private ToastManager toastManager;
    private CombatTextManager combatTextManager;

    private MainMenuPage currentMainMenuPage = MainMenuPage.Home;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
    }

    private void OnEnable()
    {
        EventManager.AddListener("GAME_STATE_CHANGED", OnGameStateChanged);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("GAME_STATE_CHANGED", OnGameStateChanged);
    }

    private void BuildCanvas()
    {
        GameObject canvasGo = CreateUIRectGO("UICanvas", transform);

        mainCanvas = canvasGo.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 0;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGo.AddComponent<GraphicRaycaster>();

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(transform, false);
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 全局组件（COMP-02/COMP-01）：仅在主菜单相关界面显示（Gameplay/Settlement 隐藏）
        globalRoot = CreateUIRectGO("Global", canvasGo.transform);
        var globalCanvas = globalRoot.AddComponent<Canvas>();
        globalCanvas.overrideSorting = true;
        globalCanvas.sortingOrder = 150;
        globalRoot.AddComponent<GraphicRaycaster>();

        topStatusBar = new GameObject("TopStatusBar").AddComponent<TopStatusBar>();
        topStatusBar.transform.SetParent(globalRoot.transform, false);

        bottomNav = new GameObject("BottomNavigationBar").AddComponent<BottomNavigationBar>();
        bottomNav.transform.SetParent(globalRoot.transform, false);

        rootMainMenu = CreateUIRectGO("MainMenu", canvasGo.transform);
        var mainLayerCanvas = rootMainMenu.AddComponent<Canvas>();
        mainLayerCanvas.overrideSorting = true;
        mainLayerCanvas.sortingOrder = (int)UILayer.Main;
        // 交互必须有 GraphicRaycaster：否则 Button 点击不会触发
        rootMainMenu.AddComponent<GraphicRaycaster>();

        // 一级界面页（M2）：UI-01/04/06/07/08/09
        pageHome = CreateUIRectGO("Page_Home", rootMainMenu.transform);
        mainMenuPanel = pageHome.AddComponent<MainMenuPanel>();

        pageInventory = CreateUIRectGO("Page_Inventory", rootMainMenu.transform);
        inventoryPanel = pageInventory.AddComponent<InventoryPanel>();
        inventoryPanel.OnClose = () => ShowMainMenuHome();

        pageShop = CreateUIRectGO("Page_Shop", rootMainMenu.transform);
        shopPanel = pageShop.AddComponent<ShopPanel>();

        pageSettings = CreateUIRectGO("Page_Settings", rootMainMenu.transform);
        settingsPanel = pageSettings.AddComponent<SettingsPanel>();

        pageCharacter = CreateUIRectGO("Page_CharacterAttributes", rootMainMenu.transform);
        characterAttributesPanel = pageCharacter.AddComponent<CharacterAttributesPanel>();

        pageSkills = CreateUIRectGO("Page_Skills", rootMainMenu.transform);
        skillsPanel = pageSkills.AddComponent<SkillsPanel>();

        rootGameplay = CreateUIRectGO("Gameplay", canvasGo.transform);
        var gameplayCanvas = rootGameplay.AddComponent<Canvas>();
        gameplayCanvas.overrideSorting = true;
        gameplayCanvas.sortingOrder = (int)UILayer.Main;
        rootGameplay.AddComponent<GraphicRaycaster>();
        gameplayPanel = rootGameplay.AddComponent<GameplayPanel>();

        rootSettlement = CreateUIRectGO("Settlement", canvasGo.transform);
        var settlementCanvas = rootSettlement.AddComponent<Canvas>();
        settlementCanvas.overrideSorting = true;
        settlementCanvas.sortingOrder = (int)UILayer.Main;
        rootSettlement.AddComponent<GraphicRaycaster>();
        settlementPanel = rootSettlement.AddComponent<SettlementPanel>();

        // 弹窗层（UI-10/UI-15/UI-16 等）
        rootPopup = CreateUIRectGO("Popup", canvasGo.transform);
        var popupCanvas = rootPopup.AddComponent<Canvas>();
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = (int)UILayer.Popup;
        rootPopup.AddComponent<GraphicRaycaster>();

        levelConfirmPopup = new GameObject("LevelConfirmationPopup").AddComponent<LevelConfirmationPopup>();
        levelConfirmPopup.transform.SetParent(rootPopup.transform, false);
        levelConfirmPopup.OnCancel = () => { if (mainMenuPanel != null) mainMenuPanel.ShowMainControlsOnly(); };

        pauseMenuPopup = new GameObject("PauseMenuPopup").AddComponent<PauseMenuPopup>();
        pauseMenuPopup.transform.SetParent(rootPopup.transform, false);

        failurePopup = new GameObject("FailurePopup").AddComponent<FailurePopup>();
        failurePopup.transform.SetParent(rootPopup.transform, false);

        settingsPopup = new GameObject("SettingsPopup").AddComponent<SettingsPopup>();
        settingsPopup.transform.SetParent(rootPopup.transform, false);
        settingsPopup.gameObject.SetActive(false);

        // 战斗背包弹窗（UI-03 需要背包入口；首版用 InventoryPanel 复用实现）
        inventoryPopupInGame = new GameObject("InventoryPopupInGame").AddComponent<InventoryPanel>();
        inventoryPopupInGame.transform.SetParent(rootPopup.transform, false);
        inventoryPopupInGame.OnClose = () =>
        {
            inventoryPopupInGame.gameObject.SetActive(false);
            // 默认：从战斗打开背包后，关闭即继续游戏
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
                GameManager.Instance.ResumeGame();
        };
        inventoryPopupInGame.gameObject.SetActive(false);

        // 二级弹窗（M2）：UI-11/12/13/14
        itemDetailsPopup = new GameObject("ItemDetailsPopup").AddComponent<ItemDetailsPopup>();
        itemDetailsPopup.transform.SetParent(rootPopup.transform, false);
        sellConfirmationPopup = new GameObject("SellConfirmationPopup").AddComponent<SellConfirmationPopup>();
        sellConfirmationPopup.transform.SetParent(rootPopup.transform, false);
        equipmentDetailsPopup = new GameObject("EquipmentDetailsPopup").AddComponent<EquipmentDetailsPopup>();
        equipmentDetailsPopup.transform.SetParent(rootPopup.transform, false);
        skillUpgradeConfirmationPopup = new GameObject("SkillUpgradeConfirmationPopup").AddComponent<SkillUpgradeConfirmationPopup>();
        skillUpgradeConfirmationPopup.transform.SetParent(rootPopup.transform, false);
        confirmPopup = new GameObject("ConfirmPopup").AddComponent<ConfirmPopup>();
        confirmPopup.transform.SetParent(rootPopup.transform, false);

        // Tip 层：Toast + 飘字
        rootTip = CreateUIRectGO("Tip", canvasGo.transform);
        var tipCanvas = rootTip.AddComponent<Canvas>();
        tipCanvas.overrideSorting = true;
        tipCanvas.sortingOrder = (int)UILayer.Tip;
        rootTip.AddComponent<GraphicRaycaster>();

        toastManager = new GameObject("ToastManager").AddComponent<ToastManager>();
        toastManager.transform.SetParent(rootTip.transform, false);

        combatTextManager = new GameObject("CombatTextManager").AddComponent<CombatTextManager>();
        combatTextManager.transform.SetParent(rootTip.transform, false);

        // 给所有弹窗挂上基础动画（不破坏逻辑）
        levelConfirmPopup.gameObject.AddComponent<UIPopupAnimator>();
        pauseMenuPopup.gameObject.AddComponent<UIPopupAnimator>();
        failurePopup.gameObject.AddComponent<UIPopupAnimator>();
        settingsPopup.gameObject.AddComponent<UIPopupAnimator>();
        inventoryPopupInGame.gameObject.AddComponent<UIPopupAnimator>();
        itemDetailsPopup.gameObject.AddComponent<UIPopupAnimator>();
        sellConfirmationPopup.gameObject.AddComponent<UIPopupAnimator>();
        equipmentDetailsPopup.gameObject.AddComponent<UIPopupAnimator>();
        skillUpgradeConfirmationPopup.gameObject.AddComponent<UIPopupAnimator>();
        confirmPopup.gameObject.AddComponent<UIPopupAnimator>();

        RefreshPanelVisibility();
    }

    private static RectTransform EnsureStretchRect(GameObject go)
    {
        if (go == null) return null;
        // RectTransform 是 Transform 的替代品，不能对已有 Transform 的 GO 直接 AddComponent。
        var rt = go.transform as RectTransform;
        if (rt == null) return null;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private static GameObject CreateUIRectGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        EnsureStretchRect(go);
        return go;
    }

    private void OnGameStateChanged(object stateObj)
    {
        RefreshPanelVisibility();
    }

    private void RefreshPanelVisibility()
    {
        GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.MainMenu;
        rootMainMenu.SetActive(state == GameState.MainMenu);
        rootGameplay.SetActive(state == GameState.InGame || state == GameState.Paused);
        rootSettlement.SetActive(state == GameState.Settlement);

        // 全局导航/顶部条：主菜单体系显示
        if (globalRoot != null)
            globalRoot.SetActive(state == GameState.MainMenu);

        // 主菜单页切换
        if (state == GameState.MainMenu)
        {
            SetMainMenuPageActive(currentMainMenuPage);
        }
        else
        {
            SetAllMainMenuPagesInactive();
        }

        // 弹窗：按状态显示/隐藏（不阻塞推进，但提供可用交互）
        if (pauseMenuPopup != null)
        {
            if (state == GameState.Paused) pauseMenuPopup.Show();
            else pauseMenuPopup.gameObject.SetActive(false);
        }

        // 失败弹窗：当进入结算且失败时弹出（结算面板仍可显示失败收益）
        if (failurePopup != null)
        {
            bool shouldShowFailure = (state == GameState.Settlement) && (GameManager.Instance != null) && !GameManager.Instance.LastVictory;
            if (shouldShowFailure) failurePopup.Show();
            else failurePopup.gameObject.SetActive(false);
        }

        // 顶部状态栏刷新一次，避免从战斗返回主菜单时显示旧值
        if (topStatusBar != null) topStatusBar.RefreshFromRuntimeOrSave();
    }

    private void SetAllMainMenuPagesInactive()
    {
        if (pageHome != null) pageHome.SetActive(false);
        if (pageInventory != null) pageInventory.SetActive(false);
        if (pageShop != null) pageShop.SetActive(false);
        if (pageSettings != null) pageSettings.SetActive(false);
        if (pageCharacter != null) pageCharacter.SetActive(false);
        if (pageSkills != null) pageSkills.SetActive(false);
    }

    private void SetMainMenuPageActive(MainMenuPage page)
    {
        SetAllMainMenuPagesInactive();
        if (page == MainMenuPage.Home && pageHome != null) pageHome.SetActive(true);
        else if (page == MainMenuPage.Inventory && pageInventory != null) pageInventory.SetActive(true);
        else if (page == MainMenuPage.Shop && pageShop != null) pageShop.SetActive(true);
        else if (page == MainMenuPage.Settings && pageSettings != null) pageSettings.SetActive(true);
        else if (page == MainMenuPage.Character && pageCharacter != null) pageCharacter.SetActive(true);
        else if (page == MainMenuPage.Skills && pageSkills != null) pageSkills.SetActive(true);
    }

    private void SwitchMainMenuPage(MainMenuPage page)
    {
        currentMainMenuPage = page;
        SetMainMenuPageActive(page);
        if (topStatusBar != null) topStatusBar.RefreshFromRuntimeOrSave();
    }

    /// <summary>UI-10：关卡确认弹窗。</summary>
    public void ShowLevelConfirmation(int levelId)
    {
        if (levelConfirmPopup != null)
            levelConfirmPopup.Show(levelId);
    }

    public void ShowItemDetails(int equipmentId, int countInBag)
    {
        if (itemDetailsPopup != null)
            itemDetailsPopup.ShowEquipment(equipmentId, countInBag);
    }

    public void ShowSellConfirmation(int equipmentId, int count, int unitSellPrice)
    {
        if (sellConfirmationPopup != null)
            sellConfirmationPopup.Show(equipmentId, count, unitSellPrice);
    }

    public void ShowEquipmentDetails(string slot, int equipmentId)
    {
        if (equipmentDetailsPopup != null)
            equipmentDetailsPopup.Show(slot, equipmentId);
    }

    public void ShowSkillUpgradeConfirmation(string skillId, string skillName, int currentLv, int nextLv, int cost, bool canAfford, System.Action confirm)
    {
        if (skillUpgradeConfirmationPopup != null)
            skillUpgradeConfirmationPopup.Show(skillId, skillName, currentLv, nextLv, cost, canAfford, confirm);
    }

    public void ShowConfirm(string title, string desc, string confirmLabel, string cancelLabel, System.Action onConfirm)
    {
        if (confirmPopup != null)
            confirmPopup.Show(title, desc, confirmLabel, cancelLabel, onConfirm);
    }

    public void ShowToast(string message, float seconds = 1.2f)
    {
        if (toastManager != null) toastManager.Show(message, seconds);
    }

    public void ShowSettingsPopup()
    {
        if (settingsPopup != null) settingsPopup.Show();
    }

    public void ShowInventoryPopupInGame()
    {
        if (inventoryPopupInGame == null) return;
        inventoryPopupInGame.gameObject.SetActive(true);
    }

    public void ShowMainMenuHome() => SwitchMainMenuPage(MainMenuPage.Home);
    public void ShowInventoryPage() => SwitchMainMenuPage(MainMenuPage.Inventory);
    public void ShowShopPage() => SwitchMainMenuPage(MainMenuPage.Shop);
    public void ShowSettingsPage() => SwitchMainMenuPage(MainMenuPage.Settings);
    public void ShowCharacterAttributesPage() => SwitchMainMenuPage(MainMenuPage.Character);
    public void ShowSkillsPage() => SwitchMainMenuPage(MainMenuPage.Skills);
}
