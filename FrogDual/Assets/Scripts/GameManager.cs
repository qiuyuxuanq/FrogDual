using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum GameState
{
    Waiting,
    Ready,
    Playing,
    PlayerWon,
    PlayerLost
}

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float countdownTime = 3f;
    public float flySpawnInterval = 2f;

    [Header("References")]
    public PlayerController playerController;
    public AIController aiController;
    public BugSpawner bugSpawner;
    public TargetZone targetZone;
    
    [Header("UI References")]
    public Canvas menuCanvas;
    public Canvas gameCanvas;
    public Canvas countDownCanvas;
    public Canvas tutorialCanvas;
    public Canvas winCanvas;
    public Canvas loseCanvas;
    public Button startButton;
    public Button exitButton;
    
    [Header("Win/Lose UI")]
    public Image winImage;
    public Image loseImage;
    
    [Header("倒计时显示 (使用Canvas Image)")]
    public Image countdownImage1;    // 数字1的Image组件
    public Image countdownImage2;    // 数字2的Image组件  
    public Image countdownImage3;    // 数字3的Image组件
    public float countdownDisplayTime = 1f; // 每个数字显示的时间

    public GameState currentState { get; private set; }

    void Start()
    {
        // 初始化UI状态
        InitializeUI();
        
        // 确保所有倒计时Image初始状态为隐藏
        HideAllCountdownImages();
        
        // 设置按钮事件
        SetupButtonEvents();
        
        // 等待玩家点击开始，不自动开始游戏
    }

    public void StartGame()
    {
        currentState = GameState.Waiting;
        StartCoroutine(GameSequence());
    }

    IEnumerator GameSequence()
    {
        // 3、2、1倒计时显示
        for (int i = 3; i >= 1; i--)
        {
            Debug.Log($"⏱️ 倒数: {i}");
            ShowCountdownNumber(i);
            yield return new WaitForSeconds(countdownDisplayTime);
        }
        
        // 隐藏倒计时数字
        HideAllCountdownImages();

        Debug.Log("🟡 Ready状态开始!");
        currentState = GameState.Ready;

        playerController.EnableInput();
        Debug.Log("✅ Player input enabled");

        Debug.Log("⏰ Ready状态持续1秒后开始游戏...");
        yield return new WaitForSeconds(1f);

        Debug.Log("🟢 Playing状态开始!");
        currentState = GameState.Playing;

        // ✅ 修复：只在游戏开始时启动虫子生成
        bugSpawner.StartGameSpawning();

        aiController.StartReaction();
    }


    public void OnPlayerClick(bool hitFly, bool inTargetZone)
    {
        if (currentState != GameState.Ready && currentState != GameState.Playing)
            return;

        playerController.DisableInput();

        Debug.Log($"🔍 点击检测: 当前状态={currentState}, 击中苍蝇={hitFly}, 在目标区域={inTargetZone}");

        if (currentState == GameState.Ready)
        {
            Debug.Log("❌ 点击过早! 需要等到Playing状态");
            PlayerLose("Clicked too early!");
        }
        else if (!inTargetZone)
        {
            PlayerLose("Missed the target zone!");
        }
        else if (hitFly)
        {
            // ✅ 击中苍蝇且在目标区域内
            if (aiController.HasReacted())
            {
                PlayerLose("AI was faster!");
            }
            else
            {
                PlayerWin("You caught the fly!");
            }
        }
        else
        {
            // ✅ 修复：在目标区域内但没击中苍蝇（可能击中蜜蜂或空白）
            // 检查是否点击了蜜蜂
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;

            Bug clickedBug = GetBugAtPosition(mouseWorldPos);
            if (clickedBug != null && clickedBug.bugType == BugType.Bee)
            {
                PlayerLose("Hit a bee! Game over!");
            }
            else
            {
                // 点击了空白区域，不算失败，但不允许再次点击
                Debug.Log("🎯 点击了目标区域内的空白位置，继续游戏（但不能再次点击）");
                // 不重新启用输入，保持玩家只能点击一次的限制
            }
        }
    }

    // ✅ 添加辅助方法获取点击的虫子
    Bug GetBugAtPosition(Vector3 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        if (hit != null)
        {
            return hit.GetComponent<Bug>();
        }
        return null;
    }


    public void OnAIReact()
    {
        if (currentState == GameState.Playing && !playerController.HasClicked())
        {
            PlayerLose("AI reacted first!");
        }
    }

    public void OnFlyEscape()
    {
        if (currentState == GameState.Playing && !playerController.HasClicked())
        {
            PlayerLose("The fly escaped!");
        }
    }

    void PlayerWin(string message)
    {
        currentState = GameState.PlayerWon;
        playerController.OnGameWin();
        Debug.Log($"🎉 PLAYER WINS: {message}");

        // ✅ 修复：首先停止BugSpawner，防止继续生成
        if (bugSpawner != null)
        {
            bugSpawner.StopGameSpawning();
        }

        // 停止AI反应
        if (aiController != null)
        {
            aiController.StopReaction();
        }

        // 立即销毁所有虫子
        DestroyAllBugs();

        // 显示胜利UI
        ShowWinUI();
        
        Debug.Log("🏆 游戏胜利结束！");
    }

    void PlayerLose(string message)
    {
        currentState = GameState.PlayerLost;
        playerController.OnGameLose();
        Debug.Log($"💀 PLAYER LOSES: {message}");

        // ✅ 修复：首先停止BugSpawner，防止继续生成
        if (bugSpawner != null)
        {
            bugSpawner.StopGameSpawning();
        }

        // 停止AI反应
        if (aiController != null)
        {
            aiController.StopReaction();
        }

        // 立即销毁所有虫子
        DestroyAllBugs();

        // 显示失败UI
        ShowLoseUI();
        
        Debug.Log("😵 游戏失败结束！");
    }


    void DestroyAllBugs()
    {
        // 查找并销毁所有虫子GameObject
        Bug[] allBugs = FindObjectsOfType<Bug>();
        foreach (Bug bug in allBugs)
        {
            if (bug != null && bug.gameObject != null)
            {
                Debug.Log($"🧹 销毁虫子: {bug.gameObject.name}");
                Destroy(bug.gameObject);
            }
        }

        // 也可以通过名字查找并销毁
        GameObject[] flies = GameObject.FindGameObjectsWithTag("Fly");
        foreach (GameObject fly in flies)
        {
            Debug.Log($"🧹 销毁苍蝇: {fly.name}");
            Destroy(fly);
        }

        GameObject[] bees = GameObject.FindGameObjectsWithTag("Bee");
        foreach (GameObject bee in bees)
        {
            Debug.Log($"🧹 销毁蜜蜂: {bee.name}");
            Destroy(bee);
        }

        // 通用方法：销毁所有名字包含"Fly"或"Bee"的GameObject
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Fly") || obj.name.Contains("Bee"))
            {
                // 确保它们有Bug组件或FlyMovement组件
                if (obj.GetComponent<Bug>() != null || obj.GetComponent<FlyMovement>() != null)
                {
                    Debug.Log($"🧹 销毁虫子对象: {obj.name}");
                    Destroy(obj);
                }
            }
        }

        Debug.Log("🧹 所有虫子已清理完毕！");

    }
    
    /// <summary>
    /// 显示倒计时数字 - 使用Canvas Image
    /// </summary>
    void ShowCountdownNumber(int number)
    {
        // 首先隐藏所有倒计时数字
        HideAllCountdownImages();
        
        // 根据数字激活对应的Image
        Image imageToShow = null;
        switch (number)
        {
            case 1:
                imageToShow = countdownImage1;
                break;
            case 2:
                imageToShow = countdownImage2;
                break;
            case 3:
                imageToShow = countdownImage3;
                break;
        }
        
        // 激活对应的Image
        if (imageToShow != null)
        {
            imageToShow.gameObject.SetActive(true);
            Debug.Log($"📱 显示倒计时数字: {number} (激活 {imageToShow.name})");
        }
        else
        {
            Debug.LogWarning($"⚠️ 没有找到数字 {number} 对应的Image组件! 请在Inspector中设置countdownImage{number}");
        }
    }
    
    /// <summary>
    /// 隐藏所有倒计时数字
    /// </summary>
    void HideAllCountdownImages()
    {
        if (countdownImage1 != null) countdownImage1.gameObject.SetActive(false);
        if (countdownImage2 != null) countdownImage2.gameObject.SetActive(false);
        if (countdownImage3 != null) countdownImage3.gameObject.SetActive(false);
        
        Debug.Log("🙈 隐藏所有倒计时数字");
    }

    /// <summary>
    /// 手动测试倒计时显示（右键菜单）
    /// </summary>
    [ContextMenu("测试显示倒计时3")]
    public void TestShowCountdown3()
    {
        Debug.Log("🧪 手动测试：显示倒计时3");
        ShowCountdownNumber(3);
    }

    /// <summary>
    /// 手动测试倒计时显示（右键菜单）
    /// </summary>
    [ContextMenu("测试显示倒计时2")]
    public void TestShowCountdown2()
    {
        Debug.Log("🧪 手动测试：显示倒计时2");
        ShowCountdownNumber(2);
    }

    /// <summary>
    /// 手动测试倒计时显示（右键菜单）
    /// </summary>
    [ContextMenu("测试显示倒计时1")]
    public void TestShowCountdown1()
    {
        Debug.Log("🧪 手动测试：显示倒计时1");
        ShowCountdownNumber(1);
    }

    /// <summary>
    /// 手动隐藏倒计时（右键菜单）
    /// </summary>
    [ContextMenu("隐藏所有倒计时")]
    public void TestHideAllCountdown()
    {
        Debug.Log("🧪 手动测试：隐藏所有倒计时");
        HideAllCountdownImages();
    }
    
    /// <summary>
    /// 初始化UI状态
    /// </summary>
    void InitializeUI()
    {
        // 显示菜单Canvas，隐藏游戏Canvas
        if (menuCanvas != null) menuCanvas.gameObject.SetActive(true);
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(false);
        if (countDownCanvas != null) countDownCanvas.gameObject.SetActive(false);
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
        if (winCanvas != null) winCanvas.gameObject.SetActive(false);
        if (loseCanvas != null) loseCanvas.gameObject.SetActive(false);
        
        Debug.Log("📱 UI初始化完成：显示MenuCanvas，隐藏其他所有Canvas");
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtonEvents()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
            Debug.Log("✅ Start按钮事件已设置");
        }
        else
        {
            Debug.LogWarning("⚠️ Start Button引用未设置！");
        }
        
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButtonClicked);
            Debug.Log("✅ Exit按钮事件已设置");
        }
        else
        {
            Debug.LogWarning("⚠️ Exit Button引用未设置！");
        }
    }
    
    /// <summary>
    /// 开始按钮点击事件
    /// </summary>
    public void OnStartButtonClicked()
    {
        Debug.Log("🎮 玩家点击了开始按钮");
        
        // 切换BGM：停止菜单BGM，播放游戏BGM
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.SwitchToGameBGM();
        }
        
        // 隐藏菜单Canvas，同时显示教程Canvas和游戏Canvas
        if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(true);
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(true);
        if (countDownCanvas != null) countDownCanvas.gameObject.SetActive(true);
        
        Debug.Log("📱 切换UI：隐藏MenuCanvas，同时显示TutorialCanvas、GameCanvas和CountDownCanvas");
        
        // 开始教程序列
        StartCoroutine(ShowTutorialSequence());
    }
    
    /// <summary>
    /// 退出按钮点击事件
    /// </summary>
    public void OnExitButtonClicked()
    {
        Debug.Log("🚪 玩家点击了退出按钮");
        
        // 在编辑器中停止播放
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 在构建的游戏中退出应用程序
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 延迟返回菜单
    /// </summary>
    IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToMenu();
    }
    
    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMenu()
    {
        Debug.Log("🔄 返回主菜单");
        
        // 重置游戏状态
        currentState = GameState.Waiting;
        
        // 停止所有游戏活动
        if (bugSpawner != null) bugSpawner.StopGameSpawning();
        if (aiController != null) aiController.StopReaction();
        if (playerController != null) playerController.DisableInput();
        
        // 清理场景
        DestroyAllBugs();
        HideAllCountdownImages();
        
        // 切换BGM：播放菜单BGM
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.SwitchToMenuBGM();
        }
        
        // 切换UI
        if (menuCanvas != null) menuCanvas.gameObject.SetActive(true);
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(false);
        if (countDownCanvas != null) countDownCanvas.gameObject.SetActive(false);
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
        if (winCanvas != null) winCanvas.gameObject.SetActive(false);
        if (loseCanvas != null) loseCanvas.gameObject.SetActive(false);
        
        Debug.Log("📱 已返回主菜单");
    }
    
    /// <summary>
    /// 显示教程序列：显示TutorialCanvas 2秒后自动隐藏并开始游戏
    /// </summary>
    IEnumerator ShowTutorialSequence()
    {
        Debug.Log("📚 显示教程Canvas，2秒后自动隐藏");
        
        // 等待2秒
        yield return new WaitForSeconds(2f);
        
        Debug.Log("📚 教程时间结束，隐藏TutorialCanvas");
        
        // 只隐藏教程Canvas，游戏Canvas保持显示
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
        
        Debug.Log("📱 教程结束：隐藏TutorialCanvas，GameCanvas和CountDownCanvas保持显示");
        Debug.Log("🎮 教程结束，开始正式游戏");
        
        // 开始游戏
        StartGame();
    }
    
    /// <summary>
    /// 显示胜利UI，3秒后自动返回菜单
    /// </summary>
    void ShowWinUI()
    {
        Debug.Log("🎉 显示胜利UI");
        
        // 保持GameCanvas显示，只隐藏倒计时Canvas，显示胜利Canvas
        if (countDownCanvas != null) countDownCanvas.gameObject.SetActive(false);
        if (winCanvas != null) winCanvas.gameObject.SetActive(true);
        
        Debug.Log("📱 胜利UI显示：GameCanvas保持显示，WinCanvas作为覆盖层");
        
        // 3秒后返回菜单
        StartCoroutine(ReturnToMenuAfterDelay(3f));
    }
    
    /// <summary>
    /// 显示失败UI，3秒后自动返回菜单
    /// </summary>
    void ShowLoseUI()
    {
        Debug.Log("💀 显示失败UI");
        
        // 保持GameCanvas显示，只隐藏倒计时Canvas，显示失败Canvas
        if (countDownCanvas != null) countDownCanvas.gameObject.SetActive(false);
        if (loseCanvas != null) loseCanvas.gameObject.SetActive(true);
        
        Debug.Log("📱 失败UI显示：GameCanvas保持显示，LoseCanvas作为覆盖层");
        
        // 3秒后返回菜单
        StartCoroutine(ReturnToMenuAfterDelay(3f));
    }
}
