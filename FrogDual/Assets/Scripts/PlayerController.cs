using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public TargetZone targetZone;
    public Camera gameCamera;
    public PlayerFrog playerFrog;  // 新增：玩家青蛙的引用
    
    private bool inputEnabled = false;
    private bool hasClicked = false;
    
    void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }
    
    void Update()
    {
        if (inputEnabled && Input.GetMouseButtonDown(0) && !hasClicked)
        {
            HandleMouseClick();
        }
    }
    
    void HandleMouseClick()
    {
        hasClicked = true;
        
        // 触发青蛙射击动画
        if (playerFrog != null)
        {
            playerFrog.PlayShootAnimation();
        }
        
        Vector3 mouseWorldPos = gameCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        
        Debug.Log($"Player clicked at: {mouseWorldPos}");
        
        bool hitFly = false;
        bool inTargetZone = targetZone.IsPositionInZone(mouseWorldPos);
        
        Bug clickedBug = GetBugAtPosition(mouseWorldPos);
        if (clickedBug != null)
        {
            hitFly = clickedBug.bugType == BugType.Fly;
            Debug.Log($"Clicked on {clickedBug.bugType} - inTargetZone: {inTargetZone}");
        }
        else
        {
            Debug.Log($"Clicked on empty space - inTargetZone: {inTargetZone}");
        }
        
        gameManager.OnPlayerClick(hitFly, inTargetZone);
    }
    
    Bug GetBugAtPosition(Vector3 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        if (hit != null)
        {
            return hit.GetComponent<Bug>();
        }
        return null;
    }
    
    public void EnableInput()
    {
        inputEnabled = true;
        hasClicked = false;
        
        // 重置青蛙状态
        if (playerFrog != null)
        {
            playerFrog.ResetToNormal();
        }
        
        Debug.Log("Player input enabled");
    }
    
    public void DisableInput()
    {
        inputEnabled = false;
        Debug.Log("Player input disabled");
    }
    
    public bool HasClicked()
    {
        return hasClicked;
    }
    
    // 处理游戏结果
    public void OnGameWin()
    {
        if (playerFrog != null)
        {
            playerFrog.PlayVictoryAnimation();
        }
    }
    
    public void OnGameLose()
    {
        if (playerFrog != null)
        {
            playerFrog.PlayDefeatAnimation();
        }
    }
}