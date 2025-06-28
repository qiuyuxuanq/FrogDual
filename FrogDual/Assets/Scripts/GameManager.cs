using UnityEngine;
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
    public float flySpawnInterval = 2f; // 每2秒生成一个苍蝇
    
    [Header("References")]
    public PlayerController playerController;
    public AIController aiController;
    public BugSpawner bugSpawner;
    public TargetZone targetZone;
    
    public GameState currentState { get; private set; }
    private Coroutine flySpawningCoroutine;
    
    void Start()
    {
        StartGame();
    }
    
    public void StartGame()
    {
        currentState = GameState.Waiting;
        StartCoroutine(GameSequence());
    }
    
    IEnumerator GameSequence()
    {
        for (int i = 3; i >= 1; i--)
        {
            Debug.Log($"⏱️ 倒数: {i}");
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("🟡 Ready状态开始!");
        currentState = GameState.Ready;
        
        playerController.EnableInput();
        Debug.Log("✅ Player input enabled");
        
        Debug.Log("⏰ Ready状态持续1秒后开始游戏...");
        yield return new WaitForSeconds(1f);
        
        Debug.Log("🟢 Playing状态开始! (持续生成苍蝇模式)");
        currentState = GameState.Playing;
        
        // 开始持续生成苍蝇
        flySpawningCoroutine = StartCoroutine(ContinuousFlySpawning());
        
        aiController.StartReaction();
    }
    
    IEnumerator ContinuousFlySpawning()
    {
        while (currentState == GameState.Playing)
        {
            bugSpawner.SpawnFlyingBug();
            Debug.Log($"🐛 生成新苍蝇 (每{flySpawnInterval}秒一个)");
            yield return new WaitForSeconds(flySpawnInterval);
        }
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
        else if (!hitFly)
        {
            PlayerLose("Hit a bee! Game over!");
        }
        else if (aiController.HasReacted())
        {
            PlayerLose("AI was faster!");
        }
        else
        {
            PlayerWin("You caught the fly!");
        }
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
        Debug.Log($"PLAYER WINS: {message}");
        
        // 停止生成苍蝇和AI反应
        StopFlySpawning();
        aiController.StopReaction();
        
        // 3秒后重新开始游戏
        StartCoroutine(RestartGameAfterDelay(3f));
    }
    
    void PlayerLose(string message)
    {
        currentState = GameState.PlayerLost;
        playerController.OnGameLose();
        Debug.Log($"PLAYER LOSES: {message}");
        
        // 停止生成苍蝇和AI反应
        StopFlySpawning();
        aiController.StopReaction();
        
        // 3秒后重新开始游戏
        StartCoroutine(RestartGameAfterDelay(3f));
    }
    
    void StopFlySpawning()
    {
        if (flySpawningCoroutine != null)
        {
            StopCoroutine(flySpawningCoroutine);
            flySpawningCoroutine = null;
        }
    }
    
    IEnumerator RestartGameAfterDelay(float delay)
    {
        Debug.Log($"🔄 游戏将在{delay}秒后重新开始...");
        yield return new WaitForSeconds(delay);
        
        // 清理场景中的所有苍蝇
        FlyMovement[] flies = FindObjectsOfType<FlyMovement>();
        foreach (FlyMovement fly in flies)
        {
            if (fly != null)
                Destroy(fly.gameObject);
        }
        
        // 重新开始游戏
        StartGame();
    }
}