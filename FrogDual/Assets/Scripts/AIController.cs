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
            // 查找当前场景中的苍蝇
            FindCurrentFly();
            
            // 如果找到苍蝇且苍蝇在TargetZone内，开始反应计时
            if (currentFly != null && !hasStartedReaction && IsFlyInTargetZone())
            {
                hasStartedReaction = true;
                Debug.Log($"🎯 苍蝇进入目标区域! AI开始{fixedReactionTime}秒反应计时...");
                reactionCoroutine = StartCoroutine(ReactAfterDelay(fixedReactionTime));
                break; // 开始计时后停止监控
            }
            
            yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次
        }
    }
    
    void FindCurrentFly()
    {
        // 查找带有FlyMovement组件的GameObject（苍蝇）
        FlyMovement flyMovement = FindObjectOfType<FlyMovement>();
        if (flyMovement != null)
        {
            currentFly = flyMovement.gameObject;
        }
        else
        {
            currentFly = null;
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
            Debug.Log("🚨 AI反应了!");
            gameManager.OnAIReact();
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
    }
}