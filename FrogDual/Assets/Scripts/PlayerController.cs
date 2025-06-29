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

        if (playerFrog != null)
        {
            playerFrog.PlayShootAnimation();
        }

        Vector3 mouseWorldPos = gameCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Debug.Log($"Player clicked at: {mouseWorldPos}");

        bool hitFly = false;
        bool inTargetZone = false;

        // 首先检查是否点击了虫子
        Bug clickedBug = GetBugAtPosition(mouseWorldPos);

        if (clickedBug != null)
        {
            hitFly = clickedBug.bugType == BugType.Fly;

            // ✅ 关键修复：检查虫子位置是否在TargetZone中，而不是鼠标位置
            inTargetZone = targetZone.IsPositionInZone(clickedBug.transform.position);

            Debug.Log($"Clicked on {clickedBug.bugType} at {clickedBug.transform.position}");
            Debug.Log($"Bug in target zone: {inTargetZone}");
        }
        else
        {
            // 如果没点击虫子，检查空白点击是否在目标区域
            inTargetZone = targetZone.IsPositionInZone(mouseWorldPos);
            Debug.Log($"Clicked on empty space - inTargetZone: {inTargetZone}");
        }

        gameManager.OnPlayerClick(hitFly, inTargetZone);
    }


    Bug GetBugAtPosition(Vector3 worldPosition)
    {
        // 获取所有重叠的碰撞器
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);

        foreach (Collider2D hit in hits)
        {
            Bug bug = hit.GetComponent<Bug>();
            if (bug != null)
            {
                Debug.Log($"🔍 检测到虫子: {bug.bugType} 在位置 {hit.transform.position}");
                return bug; // 返回第一个找到的Bug
            }
        }

        Debug.Log($"🔍 位置 {worldPosition} 没有检测到虫子");
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