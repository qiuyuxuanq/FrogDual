using UnityEngine;

public class FlyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    public float randomOffset = 0.5f;
    public float pathUpdateInterval = 0.2f; // 每0.2秒更新一次路径
    
    [Header("References")]
    public TargetZone targetZone;
    
    private Vector3 startPosition;
    private bool isFlying = false;
    private bool hasPassedTargetZone = false;
    private float pathUpdateTimer = 0f;
    private Vector3 currentDirection;
    
    public void Initialize(Vector3 target, float flySpeed)
    {
        speed = flySpeed;
        startPosition = transform.position;
        isFlying = true;
        
        // 查找TargetZone引用
        if (targetZone == null)
            targetZone = FindObjectOfType<TargetZone>();
        
        // 计算初始飞行方向
        UpdateFlightDirection();
        
        Debug.Log($"🐛 苍蝇开始智能追踪飞行：从 {startPosition}");
    }
    
    void UpdateFlightDirection()
    {
        if (targetZone == null) return;
        
        Vector3 currentTargetPos = targetZone.transform.position;
        Vector3 directionToTarget = (currentTargetPos - transform.position).normalized;
        
        // 如果还没经过TargetZone，朝向当前TargetZone位置
        if (!hasPassedTargetZone)
        {
            currentDirection = directionToTarget;
        }
        else
        {
            // 经过TargetZone后，继续朝屏幕边缘飞行
            currentDirection = CalculateExitDirection(currentTargetPos);
        }
        
        // 添加轻微随机偏移
        Vector2 randomOffset = Random.insideUnitCircle * this.randomOffset * 0.1f;
        currentDirection += new Vector3(randomOffset.x, randomOffset.y, 0f);
        currentDirection = currentDirection.normalized;
    }
    
    Vector3 CalculateExitDirection(Vector3 targetPos)
    {
        Vector3 exitDirection = currentDirection;
        
        // 确定朝哪个屏幕边缘飞行
        if (Mathf.Abs(exitDirection.x) > Mathf.Abs(exitDirection.y))
        {
            // 主要沿X轴移动
            exitDirection = exitDirection.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            // 主要沿Y轴移动
            exitDirection = exitDirection.y > 0 ? Vector3.up : Vector3.down;
        }
        
        return exitDirection;
    }
    
    void Update()
    {
        if (!isFlying) return;
        
        // 定期更新飞行路径以追踪移动的TargetZone
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval && !hasPassedTargetZone)
        {
            UpdateFlightDirection();
            pathUpdateTimer = 0f;
        }
        
        // 移动苍蝇
        transform.position += currentDirection * speed * Time.deltaTime;
        
        // 检查是否经过TargetZone
        CheckTargetZonePassage();
        
        // 添加轻微的上下飘动效果
        float bobAmount = Mathf.Sin(Time.time * 5f) * 0.1f;
        transform.position += Vector3.up * bobAmount * Time.deltaTime;
        
        // 检查是否飞出屏幕
        CheckScreenBounds();
    }
    
    void CheckTargetZonePassage()
    {
        if (hasPassedTargetZone || targetZone == null) return;
        
        // 检查是否在TargetZone内或附近
        float distanceToTarget = Vector3.Distance(transform.position, targetZone.transform.position);
        
        if (distanceToTarget < 15f) // 减小距离，更精确的检测
        {
            hasPassedTargetZone = true;
            OnPassTargetZone();
            
            // 经过TargetZone后，重新计算飞行方向
            UpdateFlightDirection();
        }
    }
    
    void OnPassTargetZone()
    {
        Debug.Log("🎯 苍蝇成功穿过目标区域！");
    }
    
    void CheckScreenBounds()
    {
        Vector3 pos = transform.position;
        
        // 检查是否超出屏幕边界 (根据你的camera size 140调整)
        if (pos.x > 250f || pos.x < -300f || pos.y > 150f || pos.y < -200f)
        {
            OnReachScreenEdge();
        }
    }
    
    void OnReachScreenEdge()
    {
        isFlying = false;
        Debug.Log("🚀 苍蝇飞出屏幕边缘，自动清理");
        
        // 苍蝇飞出屏幕，直接销毁（不影响游戏胜负）
        Destroy(gameObject);
    }
    
    public bool IsInTargetZone(TargetZone targetZone)
    {
        if (targetZone == null) return false;
        return targetZone.IsPositionInZone(transform.position);
    }
}