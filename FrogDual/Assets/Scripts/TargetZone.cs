using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Target Zone Settings")]
    public float radius = 2f;
    public Color zoneColor = Color.red;
    public bool showGizmo = true;
    public bool showInGame = true;
    
    [Header("Movement Settings")]
    public bool enableMovement = true;
    public float movementRadius = 1.5f;  // 移动轨道半径
    public float movementSpeed = 30f;    // 移动速度（度/秒）
    
    [Header("Visual Settings")]
    public float lineWidth = 1.0f;  // 增加线条宽度
    public int circleSegments = 100; // 增加平滑度
    
    private LineRenderer lineRenderer;
    private Vector3 centerPosition;     // 固定的中心点
    private float currentAngle = 0f;    // 当前角度
    
    void Start()
    {
        // 保存初始位置作为中心点
        centerPosition = transform.position;
        
        SetupCollider();
        SetupVisualCircle();
    }
    
    void Update()
    {
        if (enableMovement)
        {
            UpdateMovement();
        }
    }
    
    void UpdateMovement()
    {
        // 更新角度
        currentAngle += movementSpeed * Time.deltaTime;
        if (currentAngle >= 360f)
        {
            currentAngle -= 360f;
        }
        
        // 计算新位置（围绕中心点做圆周运动）
        float angleRad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleRad) * movementRadius,
            Mathf.Sin(angleRad) * movementRadius,
            0f
        );
        
        transform.position = centerPosition + offset;
    }
    
    void SetupCollider()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        collider.radius = radius;
        collider.isTrigger = true;
        
        Debug.Log($"Target zone setup with radius: {radius}");
    }
    
    void SetupVisualCircle()
    {
        if (!showInGame) return;
        
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // 配置LineRenderer - 改进的设置
        lineRenderer.material = CreateLineMaterial();
        lineRenderer.startColor = zoneColor;
        lineRenderer.endColor = zoneColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = circleSegments + 1;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.sortingOrder = 10; // 提高渲染优先级
        
        // 创建圆形
        UpdateCircleVisual();
    }
    
    Material CreateLineMaterial()
    {
        // 创建更好的线条材质
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = zoneColor;
        return lineMat;
    }
    
    void UpdateCircleVisual()
    {
        if (lineRenderer == null || !showInGame) return;
        
        float angleStep = 360f / circleSegments;
        
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            lineRenderer.SetPosition(i, point);
        }
    }
    
    public bool IsPositionInZone(Vector3 worldPosition)
    {
        float distance = Vector3.Distance(transform.position, worldPosition);
        bool inZone = distance <= radius;
        
        // 删除调试日志
        
        Debug.Log($"Position {worldPosition} is {(inZone ? "IN" : "OUT")} target zone (distance: {distance:F2})");
        
        return inZone;
    }
    
    public bool IsBugInZone(Bug bug)
    {
        if (bug == null) return false;
        return IsPositionInZone(bug.transform.position);
    }
    
    // 运行时调整半径
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        
        // 更新碰撞器
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            collider.radius = radius;
        }
        
        // 更新可视化
        UpdateCircleVisual();
    }
    
    void OnValidate()
    {
        // 在编辑器中实时更新
        if (Application.isPlaying)
        {
            UpdateCircleVisual();
        }
    }
    
    void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = zoneColor;
            DrawWireCircle(transform.position, radius);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        DrawWireCircle(transform.position, radius);
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, radius);
    }
    
    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}