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
    public float bugSpawnDelay = 1f;
    public float maxWaitTime = 5f;
    
    [Header("References")]
    public PlayerController playerController;
    public AIController aiController;
    public BugSpawner bugSpawner;
    public TargetZone targetZone;
    
    public GameState currentState { get; private set; }
    
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
            Debug.Log($"Countdown: {i}");
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("Ready!");
        currentState = GameState.Ready;
        
        playerController.EnableInput();
        
        float waitTime = Random.Range(1f, maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        
        currentState = GameState.Playing;
        bugSpawner.SpawnBug();
        
        aiController.StartReaction();
    }
    
    public void OnPlayerClick(bool hitFly, bool inTargetZone)
    {
        if (currentState != GameState.Ready && currentState != GameState.Playing)
            return;
            
        playerController.DisableInput();
        
        if (currentState == GameState.Ready)
        {
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
    
    void PlayerWin(string message)
    {
        currentState = GameState.PlayerWon;
        playerController.OnGameWin(); // 通知PlayerController播放胜利动画
        Debug.Log($"PLAYER WINS: {message}");
    }
    
    void PlayerLose(string message)
    {
        currentState = GameState.PlayerLost;
        playerController.OnGameLose(); // 通知PlayerController播放失败动画
        Debug.Log($"PLAYER LOSES: {message}");
    }
}