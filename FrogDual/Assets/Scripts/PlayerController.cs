using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("游戏组件引用")]
    public GameManager gameManager;
    public TargetZone targetZone;
    public Camera gameCamera;
    public PlayerFrog playerFrog;

    [Header("点击检测设置")]
    [Tooltip("点击检测的半径范围，越大越容易击中")]
    [Range(0.5f, 3f)]
    public float clickDetectionRadius = 1.5f;

    [Tooltip("目标区域的容差，越大越容易判定在区域内")]
    [Range(0.1f, 2f)]
    public float targetZoneTolerance = 0.8f;

    [Tooltip("启用智能检测模式（推荐）")]
    public bool useSmartDetection = true;

    [Tooltip("显示详细的调试信息")]
    public bool showDebugInfo = true;

    [Tooltip("显示可视化调试（Scene视图中）")]
    public bool showVisualDebug = true;

    [Header("高级设置")]
    [Tooltip("优先检测苍蝇而不是蜜蜂")]
    public bool prioritizeFlyDetection = true;

    [Tooltip("在目标区域内时扩大检测范围")]
    public bool expandDetectionInZone = true;

    [Tooltip("区域内检测范围倍数")]
    [Range(1f, 3f)]
    public float zoneDetectionMultiplier = 2f;

    // 私有变量
    private bool inputEnabled = false;
    private bool hasClicked = false;
    private Vector3 lastMousePosition;

    void Start()
    {
        InitializeComponents();
    }

    void Update()
    {
        if (inputEnabled && Input.GetMouseButtonDown(0) && !hasClicked)
        {
            HandleMouseClick();
        }

        // 更新鼠标位置用于可视化调试
        if (showVisualDebug && gameCamera != null)
        {
            lastMousePosition = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            lastMousePosition.z = 0f;
        }
    }

    #region 初始化

    void InitializeComponents()
    {
        // 自动查找组件引用
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (targetZone == null)
            targetZone = FindObjectOfType<TargetZone>();

        if (playerFrog == null)
            playerFrog = FindObjectOfType<PlayerFrog>();

        // 验证必要组件
        ValidateComponents();

        if (showDebugInfo)
        {
            Debug.Log($"🎮 PlayerController 初始化完成 - 智能检测: {useSmartDetection}");
        }
    }

    void ValidateComponents()
    {
        if (gameManager == null)
            Debug.LogWarning("⚠️ GameManager 未找到！");

        if (targetZone == null)
            Debug.LogWarning("⚠️ TargetZone 未找到！");

        if (gameCamera == null)
            Debug.LogWarning("⚠️ 游戏摄像机未找到！");
    }

    #endregion

    #region 点击处理

    void HandleMouseClick()
    {
        hasClicked = true;

        // 播放青蛙射击动画
        if (playerFrog != null)
        {
            playerFrog.PlayShootAnimation();
        }

        // 获取世界坐标中的鼠标位置
        Vector3 mouseWorldPos = gameCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (showDebugInfo)
        {
            Debug.Log($"🖱️ 玩家点击位置: {mouseWorldPos}");
        }

        bool hitFly = false;
        bool inTargetZone = false;

        // 根据设置选择检测模式
        if (useSmartDetection)
        {
            (hitFly, inTargetZone) = SmartClickDetection(mouseWorldPos);
        }
        else
        {
            (hitFly, inTargetZone) = ImprovedTraditionalDetection(mouseWorldPos);
        }

        // 输出最终结果
        if (showDebugInfo)
        {
            string result = hitFly && inTargetZone ? "🎯 成功击中!" :
                           hitFly ? "🐛 击中虫子但不在目标区域" :
                           inTargetZone ? "📍 在目标区域但未击中虫子" : "❌ 完全错过";
            Debug.Log($"🔍 最终检测结果: 击中苍蝇={hitFly}, 在目标区域={inTargetZone} - {result}");
        }

        // 通知游戏管理器
        gameManager.OnPlayerClick(hitFly, inTargetZone);
    }

    #endregion

    #region 智能检测系统

    /// <summary>
    /// 智能点击检测 - 更宽松和用户友好的检测模式
    /// </summary>
    (bool hitFly, bool inTargetZone) SmartClickDetection(Vector3 mouseWorldPos)
    {
        bool hitFly = false;
        bool inTargetZone = false;

        // 🎯 第一步：检查点击位置是否在目标区域内
        bool mouseInZone = IsPositionInTargetZone(mouseWorldPos);

        if (mouseInZone)
        {
            if (showDebugInfo)
            {
                Debug.Log("✅ 鼠标点击在目标区域内，开始搜索虫子");
            }

            // 🔍 在目标区域内寻找虫子（使用扩大的检测范围）
            float detectionRadius = expandDetectionInZone ?
                clickDetectionRadius * zoneDetectionMultiplier : clickDetectionRadius;

            Bug targetBug = FindBestBugInRadius(mouseWorldPos, detectionRadius, true);

            if (targetBug != null)
            {
                hitFly = targetBug.bugType == BugType.Fly;
                inTargetZone = true;

                if (showDebugInfo)
                {
                    float distance = Vector3.Distance(mouseWorldPos, targetBug.transform.position);
                    Debug.Log($"🎯 在目标区域内找到虫子: {targetBug.bugType}, 距离: {distance:F2}");
                }
            }
            else
            {
                // 在目标区域内但没找到虫子
                inTargetZone = true;
                hitFly = false;

                if (showDebugInfo)
                {
                    Debug.Log("📍 在目标区域内但未击中虫子");
                }
            }
        }
        else
        {
            // 🔍 不在目标区域，使用标准检测
            Bug clickedBug = FindBestBugInRadius(mouseWorldPos, clickDetectionRadius, false);

            if (clickedBug != null)
            {
                hitFly = clickedBug.bugType == BugType.Fly;
                // 检查虫子是否在目标区域内
                inTargetZone = IsPositionInTargetZone(clickedBug.transform.position);

                if (showDebugInfo)
                {
                    Debug.Log($"🐛 点击到虫子: {clickedBug.bugType}, 虫子在目标区域: {inTargetZone}");
                }
            }
            else
            {
                // 完全错过
                hitFly = false;
                inTargetZone = false;

                if (showDebugInfo)
                {
                    Debug.Log("❌ 点击空白区域，完全错过");
                }
            }
        }

        return (hitFly, inTargetZone);
    }

    /// <summary>
    /// 改进的传统检测模式（向后兼容）
    /// </summary>
    (bool hitFly, bool inTargetZone) ImprovedTraditionalDetection(Vector3 mouseWorldPos)
    {
        bool hitFly = false;
        bool inTargetZone = false;

        // 使用改进的虫子检测
        Bug clickedBug = FindBestBugInRadius(mouseWorldPos, clickDetectionRadius, false);

        if (clickedBug != null)
        {
            hitFly = clickedBug.bugType == BugType.Fly;
            // 检查虫子位置是否在目标区域中
            inTargetZone = IsPositionInTargetZone(clickedBug.transform.position);
        }
        else
        {
            // 如果没点击虫子，检查空白点击是否在目标区域
            inTargetZone = IsPositionInTargetZone(mouseWorldPos);
        }

        return (hitFly, inTargetZone);
    }

    #endregion

    #region 虫子检测算法

    /// <summary>
    /// 在指定半径内找到最佳的虫子目标
    /// </summary>
    Bug FindBestBugInRadius(Vector3 worldPosition, float radius, bool inTargetZone)
    {
        // 使用圆形区域检测
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, radius);

        Bug bestBug = null;
        float bestScore = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Bug bug = hit.GetComponent<Bug>();
            if (bug == null) continue;

            // 计算距离
            float distance = Vector3.Distance(worldPosition, hit.transform.position);

            // 计算分数（越小越好）
            float score = CalculateBugScore(bug, distance, inTargetZone);

            if (score < bestScore)
            {
                bestScore = score;
                bestBug = bug;
            }
        }

        if (bestBug != null && showDebugInfo)
        {
            Debug.Log($"🔍 最佳虫子: {bestBug.bugType}, 距离: {Vector3.Distance(worldPosition, bestBug.transform.position):F2}, 分数: {bestScore:F2}");
        }

        return bestBug;
    }

    /// <summary>
    /// 计算虫子的优先级分数
    /// </summary>
    float CalculateBugScore(Bug bug, float distance, bool inTargetZone)
    {
        float score = distance; // 基础分数是距离

        // 如果启用优先检测苍蝇
        if (prioritizeFlyDetection)
        {
            if (bug.bugType == BugType.Fly)
            {
                score *= 0.5f; // 苍蝇分数减半（优先级更高）
            }
            else if (bug.bugType == BugType.Bee)
            {
                score *= 2f; // 蜜蜂分数加倍（优先级更低）
            }
        }

        // 在目标区域内的虫子优先级更高
        if (inTargetZone && IsPositionInTargetZone(bug.transform.position))
        {
            score *= 0.7f;
        }

        return score;
    }

    /// <summary>
    /// 检查位置是否在目标区域内（带容差）
    /// </summary>
    /// <summary>
    /// 检查位置是否在目标区域内（带容差）
    /// </summary>
    bool IsPositionInTargetZone(Vector3 position)
    {
        if (targetZone == null) return false;

        try
        {
            // 尝试使用TargetZone的带容差检测方法
            return targetZone.IsPositionInZoneWithTolerance(position, targetZoneTolerance);
        }
        catch (System.Exception)
        {
            // 如果方法不存在或出错，使用备用简单距离检测
            float distance = Vector3.Distance(position, targetZone.transform.position);
            float zoneRadius = GetTargetZoneRadius();
            return distance <= (zoneRadius + targetZoneTolerance);
        }
    }


    /// <summary>
    /// 获取目标区域半径
    /// </summary>
    float GetTargetZoneRadius()
    {
        if (targetZone == null) return 2f;

        CircleCollider2D circleCollider = targetZone.GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            return circleCollider.radius * targetZone.transform.localScale.x;
        }

        return 2f; // 默认半径
    }

    #endregion

    #region 向后兼容方法

    /// <summary>
    /// 原始的GetBugAtPosition方法（保留向后兼容）
    /// </summary>
    Bug GetBugAtPosition(Vector3 worldPosition)
    {
        // 使用新的改进方法
        return FindBestBugInRadius(worldPosition, clickDetectionRadius, false);
    }

    #endregion

    #region 公共接口方法

    public void EnableInput()
    {
        inputEnabled = true;
        hasClicked = false;

        if (playerFrog != null)
        {
            playerFrog.ResetToNormal();
        }

        if (showDebugInfo)
        {
            Debug.Log("🎮 玩家输入已启用");
        }
    }

    public void DisableInput()
    {
        inputEnabled = false;

        if (showDebugInfo)
        {
            Debug.Log("🎮 玩家输入已禁用");
        }
    }

    public bool HasClicked()
    {
        return hasClicked;
    }

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

    /// <summary>
    /// 获取当前检测设置的统计信息
    /// </summary>
    public string GetDetectionStats()
    {
        return $"检测模式: {(useSmartDetection ? "智能" : "传统")}, " +
               $"检测半径: {clickDetectionRadius}, " +
               $"目标区域容差: {targetZoneTolerance}, " +
               $"优先苍蝇: {prioritizeFlyDetection}";
    }

    #endregion

    #region 可视化调试

    void OnDrawGizmos()
    {
        if (!showVisualDebug || !Application.isPlaying) return;

        // 显示鼠标位置的检测范围
        Vector3 mousePos = lastMousePosition;

        // 基础检测范围
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(mousePos, clickDetectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(mousePos, clickDetectionRadius);

        // 如果在目标区域内，显示扩大的检测范围
        if (expandDetectionInZone && IsPositionInTargetZone(mousePos))
        {
            float expandedRadius = clickDetectionRadius * zoneDetectionMultiplier;
            Gizmos.color = new Color(0f, 0f, 1f, 0.1f);
            Gizmos.DrawSphere(mousePos, expandedRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(mousePos, expandedRadius);
        }

        // 显示目标区域容差范围
        if (targetZone != null)
        {
            Vector3 zonePos = targetZone.transform.position;
            float zoneRadius = GetTargetZoneRadius();

            // 原始区域
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(zonePos, zoneRadius);

            // 带容差的区域
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(zonePos, zoneRadius + targetZoneTolerance);
        }
    }

    void OnDrawGizmosSelected()
    {
        // 当选中PlayerController时显示更详细的调试信息
        if (!showVisualDebug) return;

        // 显示所有Bug的位置
        Bug[] allBugs = FindObjectsOfType<Bug>();
        foreach (Bug bug in allBugs)
        {
            if (bug.bugType == BugType.Fly)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(bug.transform.position, Vector3.one * 0.5f);
            }
            else if (bug.bugType == BugType.Bee)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(bug.transform.position, Vector3.one * 0.5f);
            }
        }
    }

    #endregion

    #region 运行时配置

    [ContextMenu("测试点击检测")]
    void TestClickDetection()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ 请在运行时测试点击检测");
            return;
        }

        Vector3 testPos = lastMousePosition;
        Debug.Log($"🧪 测试位置: {testPos}");
        Debug.Log($"🧪 检测设置: {GetDetectionStats()}");

        Bug foundBug = FindBestBugInRadius(testPos, clickDetectionRadius, false);
        bool inZone = IsPositionInTargetZone(testPos);

        Debug.Log($"🧪 测试结果: 找到虫子={foundBug != null}, 在目标区域={inZone}");
        if (foundBug != null)
        {
            Debug.Log($"🧪 虫子类型: {foundBug.bugType}");
        }
    }

    [ContextMenu("重置为默认设置")]
    void ResetToDefaultSettings()
    {
        clickDetectionRadius = 1.5f;
        targetZoneTolerance = 0.8f;
        useSmartDetection = true;
        showDebugInfo = true;
        showVisualDebug = true;
        prioritizeFlyDetection = true;
        expandDetectionInZone = true;
        zoneDetectionMultiplier = 2f;

        Debug.Log("✅ 已重置为推荐的默认设置");
    }

    #endregion
}
