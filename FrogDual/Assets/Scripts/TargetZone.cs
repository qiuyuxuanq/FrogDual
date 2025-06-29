using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Target Zone Settings")]
    public float radius = 10.0f;
    public bool showInGame = true;

    [Header("Visual Settings")]
    public Color zoneColor = Color.red;
    public float lineWidth = 0.1f;
    public int circleSegments = 50;
    public Material lineMaterial;

    [Header("Movement Settings")]
    public bool enableMovement = true;
    public float movementRadius = 0.6f;
    public float movementSpeed = 1f;

    [Header("✨ 超精确判定设置")]
    [Range(0f, 0.5f)]
    public float tolerance = 0.1f;              // 判定容差
    public bool useUltraPrecision = true;       // 启用超精确模式
    public bool useMultiPointCheck = true;      // 启用多点检测
    public bool usePhysicsValidation = true;    // 启用物理验证

    [Header("🎯 Trigger事件设置")]
    public bool enableTriggerEvents = true;        // 启用Trigger事件
    public bool autoDetectBugEntry = true;         // 自动检测虫子进入
    public UnityEngine.Events.UnityEvent<Bug> OnBugEnterZone;  // 虫子进入事件
    public UnityEngine.Events.UnityEvent<Bug> OnBugExitZone;   // 虫子离开事件

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool autoSyncOnStart = true;

    private LineRenderer lineRenderer;
    private CircleCollider2D circleCollider;
    private Vector3 centerPosition;
    private float movementAngle = 0f;
    private System.Collections.Generic.HashSet<Bug> bugsInZone = new System.Collections.Generic.HashSet<Bug>();

    void Awake()
    {
        centerPosition = transform.position;
        SetupComponents();
    }

    void Start()
    {
        if (autoSyncOnStart)
        {
            ForceSyncAllComponents();
        }

        ValidateSync();
    }

    void Update()
    {
        if (enableMovement)
        {
            UpdateMovement();
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ForceSyncAllComponents();
        }
#if UNITY_EDITOR
        else if (Application.isEditor)
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null)
                {
                    SetupComponents();
                    SyncCollider();
                    UpdateVisualCircle();
                }
            };
        }
#endif
    }

    #region 🎯 超精确判定系统

    /// <summary>
    /// 超精确的位置判定 - 多重验证
    /// </summary>
    public bool IsPositionInZoneUltraPrecise(Vector3 worldPosition)
    {
        if (!useUltraPrecision)
        {
            return IsPositionInZone(worldPosition); // 回退到标准模式
        }

        // ✅ 第一层：基础距离判定
        bool basicCheck = IsPositionInZoneBasic(worldPosition);

        // ✅ 第二层：物理重叠检测
        bool physicsCheck = usePhysicsValidation ? IsPositionInZonePhysics(worldPosition) : basicCheck;

        // ✅ 第三层：像素级精确检测
        bool pixelCheck = IsPositionInZonePixelPerfect(worldPosition);

        // 组合判定：需要至少2层验证通过
        int passedChecks = (basicCheck ? 1 : 0) + (physicsCheck ? 1 : 0) + (pixelCheck ? 1 : 0);
        bool finalResult = passedChecks >= 2;

        if (showDebugInfo)
        {
            Debug.Log($"🔍 超精确判定结果:");
            Debug.Log($"   - 基础距离: {basicCheck}");
            Debug.Log($"   - 物理重叠: {physicsCheck}");
            Debug.Log($"   - 像素精确: {pixelCheck}");
            Debug.Log($"   - 通过检查: {passedChecks}/3");
            Debug.Log($"   - 最终结果: {finalResult}");
        }

        return finalResult;
    }

    /// <summary>
    /// 基础距离判定 - 高精度计算
    /// </summary>
    bool IsPositionInZoneBasic(Vector3 worldPosition)
    {
        Vector3 targetPos = transform.position;
        Vector3 checkPos = worldPosition;

        // 严格的2D计算，Z轴归零
        targetPos.z = 0f;
        checkPos.z = 0f;

        // 使用双精度计算提高精度
        double deltaX = (double)checkPos.x - (double)targetPos.x;
        double deltaY = (double)checkPos.y - (double)targetPos.y;
        double sqrDistance = deltaX * deltaX + deltaY * deltaY;
        double sqrRadius = (double)radius * (double)radius;

        return sqrDistance <= sqrRadius;
    }

    /// <summary>
    /// 物理重叠检测 - 使用Unity物理系统
    /// </summary>
    bool IsPositionInZonePhysics(Vector3 worldPosition)
    {
        if (circleCollider == null) return false;

        // 使用Unity的物理系统进行精确检测
        Vector2 point2D = new Vector2(worldPosition.x, worldPosition.y);
        bool overlap = circleCollider.OverlapPoint(point2D);

        return overlap;
    }

    /// <summary>
    /// 像素级精确检测 - 屏幕空间精度
    /// </summary>
    bool IsPositionInZonePixelPerfect(Vector3 worldPosition)
    {
        Vector3 targetPos = transform.position;
        Vector3 checkPos = worldPosition;

        // 转换为像素坐标进行计算
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return IsPositionInZoneBasic(worldPosition);

        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(targetPos);
        Vector3 checkScreenPos = mainCamera.WorldToScreenPoint(checkPos);

        // 计算屏幕空间的半径
        Vector3 edgeWorldPos = targetPos + Vector3.right * radius;
        Vector3 edgeScreenPos = mainCamera.WorldToScreenPoint(edgeWorldPos);
        float screenRadius = Vector3.Distance(targetScreenPos, edgeScreenPos);

        // 屏幕空间距离计算
        float screenDistance = Vector2.Distance(
            new Vector2(targetScreenPos.x, targetScreenPos.y),
            new Vector2(checkScreenPos.x, checkScreenPos.y)
        );

        return screenDistance <= screenRadius;
    }

    /// <summary>
    /// 虫子进入区域的超精确检测
    /// </summary>
    public bool IsBugInZoneUltraPrecise(GameObject bugObject)
    {
        if (bugObject == null) return false;

        if (!useUltraPrecision)
        {
            return IsBugInZone(bugObject); // 回退到标准模式
        }

        // ✅ 方法1：碰撞体边界检测
        bool boundsCheck = IsBugInZoneBounds(bugObject);

        // ✅ 方法2：中心点检测
        bool centerCheck = IsPositionInZoneUltraPrecise(bugObject.transform.position);

        // ✅ 方法3：多点采样检测（检测虫子的关键点）
        bool multiPointCheck = useMultiPointCheck ? IsBugInZoneMultiPoint(bugObject) : centerCheck;

        // 需要至少2种方法验证通过
        int passedChecks = (boundsCheck ? 1 : 0) + (centerCheck ? 1 : 0) + (multiPointCheck ? 1 : 0);
        bool finalResult = passedChecks >= 2;

        if (showDebugInfo)
        {
            Debug.Log($"🐛 虫子超精确检测: {bugObject.name}");
            Debug.Log($"   - 边界检测: {boundsCheck}");
            Debug.Log($"   - 中心检测: {centerCheck}");
            Debug.Log($"   - 多点检测: {multiPointCheck}");
            Debug.Log($"   - 最终结果: {finalResult}");
        }

        return finalResult;
    }

    /// <summary>
    /// 虫子边界检测
    /// </summary>
    bool IsBugInZoneBounds(GameObject bugObject)
    {
        Collider2D bugCollider = bugObject.GetComponent<Collider2D>();
        if (bugCollider == null || circleCollider == null) return false;

        return circleCollider.bounds.Intersects(bugCollider.bounds);
    }

    /// <summary>
    /// 虫子多点采样检测
    /// </summary>
    bool IsBugInZoneMultiPoint(GameObject bugObject)
    {
        Collider2D bugCollider = bugObject.GetComponent<Collider2D>();
        if (bugCollider == null) return false;

        Bounds bounds = bugCollider.bounds;

        // 检测虫子的关键点
        Vector3[] checkPoints = new Vector3[]
        {
            bounds.center,                    // 中心
            bounds.min,                       // 左下角
            bounds.max,                       // 右上角
            new Vector3(bounds.min.x, bounds.max.y, bounds.center.z), // 左上角
            new Vector3(bounds.max.x, bounds.min.y, bounds.center.z), // 右下角
        };

        int pointsInZone = 0;
        foreach (Vector3 point in checkPoints)
        {
            if (IsPositionInZoneBasic(point))
            {
                pointsInZone++;
            }
        }

        // 如果虫子的大部分点都在区域内，则认为虫子在区域内
        return pointsInZone >= 3; // 5个点中至少3个在区域内
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 增强的位置检测方法 - 更精确的判定
    /// </summary>
    public bool IsPositionInZone(Vector3 worldPosition)
    {
        // ✅ 增强1: 确保Z轴不影响2D距离计算
        Vector3 targetPos = transform.position;
        Vector3 checkPos = worldPosition;

        // 将Z坐标统一为0进行2D距离计算
        targetPos.z = 0f;
        checkPos.z = 0f;

        // ✅ 增强2: 使用平方距离比较，避免开方运算提高精度
        float sqrDistance = (checkPos - targetPos).sqrMagnitude;
        float sqrRadius = radius * radius;

        bool inZone = sqrDistance <= sqrRadius;

        // ✅ 增强3: 额外的边界容差检测
        bool inZoneWithTolerance = sqrDistance <= (radius + tolerance) * (radius + tolerance);

        if (showDebugInfo)
        {
            float actualDistance = Mathf.Sqrt(sqrDistance);
            Debug.Log($"🔍 精确位置检测:");
            Debug.Log($"   - 目标位置: {targetPos}");
            Debug.Log($"   - 检测位置: {checkPos}");
            Debug.Log($"   - 实际距离: {actualDistance:F3}");
            Debug.Log($"   - 区域半径: {radius:F3}");
            Debug.Log($"   - 标准判定: {inZone}");
            Debug.Log($"   - 容差判定: {inZoneWithTolerance}");
        }

        return inZone;
    }

    /// <summary>
    /// 带容差的位置检测（用于更宽松的判定）
    /// </summary>
    public bool IsPositionInZoneWithTolerance(Vector3 worldPosition, float customTolerance = -1f)
    {
        Vector3 targetPos = transform.position;
        Vector3 checkPos = worldPosition;

        targetPos.z = 0f;
        checkPos.z = 0f;

        float useTolerance = customTolerance >= 0f ? customTolerance : tolerance;
        float sqrDistance = (checkPos - targetPos).sqrMagnitude;
        float sqrRadiusWithTolerance = (radius + useTolerance) * (radius + useTolerance);

        return sqrDistance <= sqrRadiusWithTolerance;
    }

    /// <summary>
    /// 检测虫子碰撞体是否与目标区域重叠
    /// </summary>
    public bool IsBugInZone(GameObject bugObject)
    {
        if (bugObject == null) return false;

        Collider2D bugCollider = bugObject.GetComponent<Collider2D>();
        if (bugCollider == null) return false;

        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider == null) return false;

        // ✅ 使用物理碰撞检测，更准确
        bool overlapping = bugCollider.bounds.Intersects(zoneCollider.bounds);

        if (showDebugInfo)
        {
            Debug.Log($"🔍 碰撞体检测: {bugObject.name} 重叠状态={overlapping}");
        }

        return overlapping;
    }

    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0.1f, newRadius);
        ForceSyncAllComponents();

        Debug.Log($"🎯 TargetZone半径已更新为: {radius}");
    }

    public void SetMovementRadius(float newMovementRadius)
    {
        movementRadius = Mathf.Max(0f, newMovementRadius);
        Debug.Log($"🔄 移动轨道半径已更新为: {movementRadius}");
    }

    public void SetMovement(bool enabled)
    {
        enableMovement = enabled;
        if (!enabled)
        {
            transform.position = centerPosition;
        }
    }

    public void SetVisibility(bool visible)
    {
        showInGame = visible;
        UpdateVisualCircle();
    }

    /// <summary>
    /// 设置超精确模式
    /// </summary>
    public void SetUltraPrecisionMode(bool enabled)
    {
        useUltraPrecision = enabled;
        Debug.Log($"🎯 超精确模式: {(enabled ? "启用" : "禁用")}");
    }

    [ContextMenu("Force Sync All Components")]
    public void ForceSyncAllComponents()
    {
        SetupComponents();
        SyncCollider();
        UpdateVisualCircle();

        Debug.Log($"🔄 强制同步完成 - 半径: {radius}");

        ValidateSync();
    }

    [ContextMenu("Validate Sync Status")]
    public void ValidateSync()
    {
        if (circleCollider == null)
        {
            Debug.LogWarning("⚠️ CircleCollider2D 组件缺失!");
            return;
        }

        bool isSynced = Mathf.Approximately(circleCollider.radius, radius);

        if (isSynced)
        {
            Debug.Log($"✅ 同步状态正常:");
            Debug.Log($"   - 代码半径: {radius}");
            Debug.Log($"   - 碰撞器半径: {circleCollider.radius}");
        }
        else
        {
            Debug.LogError($"❌ 同步失败!");
            Debug.LogError($"   - 代码半径: {radius}");
            Debug.LogError($"   - 碰撞器半径: {circleCollider.radius}");
            Debug.LogError($"   - 差值: {Mathf.Abs(circleCollider.radius - radius)}");

            Debug.Log("🔧 尝试自动修复...");
            SyncCollider();
        }
    }

    #endregion

    #region Private Methods

    void SetupComponents()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            Debug.Log("🆕 创建了新的CircleCollider2D组件");
        }
        circleCollider.isTrigger = true;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log("🆕 创建了新的LineRenderer组件");
        }

        SetupLineRenderer();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.sortingOrder = 10;

        SetLineRendererColor(zoneColor);

        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
    }

    void SetLineRendererColor(Color color)
    {
        if (lineRenderer == null) return;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0.0f), new GradientAlphaKey(color.a, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
    }

    void SyncCollider()
    {
        if (circleCollider != null)
        {
            float oldRadius = circleCollider.radius;
            circleCollider.radius = radius;

            if (showDebugInfo)
            {
                Debug.Log($"🔄 碰撞器半径同步: {oldRadius} → {radius}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ SyncCollider: CircleCollider2D组件为空!");
        }
    }

    void UpdateVisualCircle()
    {
        if (lineRenderer == null)
        {
            SetupComponents();
            return;
        }

        if (!showInGame)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = circleSegments;

        Vector3[] points = new Vector3[circleSegments];
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / circleSegments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            points[i] = new Vector3(x, y, 0f);
        }

        lineRenderer.SetPositions(points);
        SetLineRendererColor(zoneColor);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    void UpdateMovement()
    {
        if (movementRadius <= 0f) return;

        movementAngle += movementSpeed * Time.deltaTime;
        if (movementAngle >= Mathf.PI * 2f)
        {
            movementAngle -= Mathf.PI * 2f;
        }

        float x = centerPosition.x + Mathf.Cos(movementAngle) * movementRadius;
        float y = centerPosition.y + Mathf.Sin(movementAngle) * movementRadius;

        transform.position = new Vector3(x, y, centerPosition.z);
    }

    #endregion

    #region Debug and Gizmos

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);

        Gizmos.color = zoneColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (enableMovement && movementRadius > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(centerPosition, movementRadius);
        }

        // ✅ 显示精确判定边界
        if (useUltraPrecision && tolerance > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, radius + tolerance);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);

        if (enableMovement)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(centerPosition, Vector3.one * 0.15f);
        }
    }

    #endregion

    #region 🔥 增强的Trigger事件系统

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!enableTriggerEvents) return;

        Bug bug = other.GetComponent<Bug>();
        if (bug != null && autoDetectBugEntry)
        {
            // 添加到追踪列表
            bugsInZone.Add(bug);

            // 触发事件
            OnBugEnterZone?.Invoke(bug);

            if (showDebugInfo)
            {
                Debug.Log($"🎯 虫子进入目标区域: {bug.bugType} - 当前区域内虫子数: {bugsInZone.Count}");
            }

            // 通知GameManager（如果需要）
            GameManager gameManager = FindObjectOfType<GameManager>();
            
        }
        else if (showDebugInfo)
        {
            Debug.Log($"🎯 非虫子对象进入区域: {other.name}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!enableTriggerEvents) return;

        Bug bug = other.GetComponent<Bug>();
        if (bug != null && autoDetectBugEntry)
        {
            // 从追踪列表移除
            bugsInZone.Remove(bug);

            // 触发事件
            OnBugExitZone?.Invoke(bug);

            if (showDebugInfo)
            {
                Debug.Log($"🎯 虫子离开目标区域: {bug.bugType} - 当前区域内虫子数: {bugsInZone.Count}");
            }

            // 通知GameManager（如果需要）
            GameManager gameManager = FindObjectOfType<GameManager>();
          
        }
        else if (showDebugInfo)
        {
            Debug.Log($"🎯 非虫子对象离开区域: {other.name}");
        }
    }

    /// <summary>
    /// 获取当前在区域内的所有虫子
    /// </summary>
    public System.Collections.Generic.HashSet<Bug> GetBugsInZone()
    {
        // 清理已销毁的虫子
        bugsInZone.RemoveWhere(bug => bug == null);
        return bugsInZone;
    }

    /// <summary>
    /// 检查指定类型的虫子是否在区域内
    /// </summary>
    public bool HasBugTypeInZone(BugType bugType)
    {
        foreach (Bug bug in bugsInZone)
        {
            if (bug != null && bug.bugType == bugType)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取区域内指定类型虫子的数量
    /// </summary>
    public int GetBugCountInZone(BugType bugType)
    {
        int count = 0;
        foreach (Bug bug in bugsInZone)
        {
            if (bug != null && bug.bugType == bugType)
            {
                count++;
            }
        }
        return count;
    }

    #endregion
}
