using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    public float fixedReactionTime = 2f; // 固定2秒反应时间

    [Header("References")]
    public GameManager gameManager;
    public TargetZone targetZone;

    private bool hasReacted = false;
    private bool isMonitoring = false;
    private bool hasStartedReaction = false;
    private Coroutine reactionCoroutine;
    private GameObject currentFly;

    public void StartReaction()
    {
        hasReacted = false;
        hasStartedReaction = false;
        isMonitoring = true;

        Debug.Log("🤖 AI开始监控苍蝇位置...");

        // 开始监控苍蝇位置
        StartCoroutine(MonitorFlyPosition());
    }

    IEnumerator MonitorFlyPosition()
    {
        while (isMonitoring && gameManager.currentState == GameState.Playing)
        {
            // 查找当前场景中的苍蝇（只找苍蝇，不找蜜蜂）
            FindCurrentFly();

            // ✅ 关键修改：只有苍蝇在TargetZone内，才开始反应计时
            if (currentFly != null && !hasStartedReaction && IsFlyInTargetZone())
            {
                // ✅ 确认这是苍蝇而不是蜜蜂
                Bug bugComponent = currentFly.GetComponent<Bug>();
                if (bugComponent != null && bugComponent.bugType == BugType.Fly)
                {
                    hasStartedReaction = true;
                    Debug.Log($"🎯 苍蝇进入目标区域! AI开始{fixedReactionTime}秒反应计时...");
                    reactionCoroutine = StartCoroutine(ReactAfterDelay(fixedReactionTime));
                    break; // 开始计时后停止监控
                }
                else if (bugComponent != null && bugComponent.bugType == BugType.Bee)
                {
                    Debug.Log("🐝 蜜蜂进入目标区域，但AI不会因此反应");
                    // 蜜蜂进入不触发AI反应，继续监控
                }
            }

            yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次
        }
    }

    void FindCurrentFly()
    {
        // ✅ 修改：查找所有Bug组件，优先找苍蝇
        Bug[] allBugs = FindObjectsOfType<Bug>();
        currentFly = null;

        // 优先查找苍蝇
        foreach (Bug bug in allBugs)
        {
            if (bug.bugType == BugType.Fly)
            {
                currentFly = bug.gameObject;
                break; // 找到第一只苍蝇就停止
            }
        }

        // 如果没有苍蝇，也可以用FlyMovement组件作为备选（向后兼容）
        if (currentFly == null)
        {
            FlyMovement flyMovement = FindObjectOfType<FlyMovement>();
            if (flyMovement != null)
            {
                Bug bugComponent = flyMovement.GetComponent<Bug>();
                if (bugComponent != null && bugComponent.bugType == BugType.Fly)
                {
                    currentFly = flyMovement.gameObject;
                }
            }
        }
    }

    bool IsFlyInTargetZone()
    {
        if (currentFly == null || targetZone == null) return false;

        return targetZone.IsPositionInZone(currentFly.transform.position);
    }

    IEnumerator ReactAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (gameManager.currentState == GameState.Playing && !hasReacted)
        {
            hasReacted = true;
            isMonitoring = false;
            Debug.Log("🚨 AI反应了! 苍蝇逃脱失败!");

            // ✅ 明确说明是因为苍蝇逃脱
            gameManager.OnFlyEscape(); // 使用专门的苍蝇逃脱方法
        }
    }

    public bool HasReacted()
    {
        return hasReacted;
    }

    public void StopReaction()
    {
        isMonitoring = false;
        hasStartedReaction = false;

        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
        }

        Debug.Log("🛑 AI停止监控");
    }
}
