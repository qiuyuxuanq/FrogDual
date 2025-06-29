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
    public float flySpawnInterval = 2f;

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
            Debug.Log($"⏱️ 倒数: {i}");
            yield return new WaitForSeconds(1f);
        }

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

        // ❌ 删除这行：bugSpawner.StopGameSpawning();

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
                // 点击了空白区域，不算失败，继续游戏
                Debug.Log("🎯 点击了目标区域内的空白位置，继续游戏");
                playerController.EnableInput(); // 重新启用输入
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

        // 游戏结束，不再重启
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

        // 游戏结束，不再重启
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

}
