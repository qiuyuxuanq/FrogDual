using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ZoneEntryRecord
{
    public float entryTime;
    public BugType bugType;
    public bool wasSolo;        // 是否单独在区域内
    public float exitTime;

    public ZoneEntryRecord(float entryTime, BugType bugType, bool wasSolo)
    {
        this.entryTime = entryTime;
        this.bugType = bugType;
        this.wasSolo = wasSolo;
        this.exitTime = -1f;
    }
}

public class TargetZoneOpportunityManager : MonoBehaviour
{
    [Header("机会保证设置")]
    [Range(1, 5)]
    public int requiredOpportunities = 2;           // 60秒内需要的机会次数
    [Range(30f, 120f)]
    public float timeWindow = 60f;                  // 时间窗口（秒）
    [Range(1f, 10f)]
    public float minSoloDuration = 2f;              // 单独停留的最短时间

    [Header("智能调整")]
    public bool enableSmartAdjustment = true;       // 启用智能调整
    [Range(0.1f, 2f)]
    public float attractionMultiplier = 1.5f;       // 吸引力倍数
    public float emergencySpawnTime = 45f;          // 紧急生成时间点

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool enableTestMode = false;

    [Space]
    [Header("运行时状态 (只读)")]
    [SerializeField] private int currentOpportunities = 0;
    [SerializeField] private float gameStartTime = 0f;
    [SerializeField] private float timeRemaining = 0f;
    [SerializeField] private bool isGuaranteeActive = false;

    // 组件引用
    private TargetZone targetZone;
    private BugSpawner bugSpawner;
    private GameManager gameManager;

    // 记录系统
    private List<ZoneEntryRecord> entryRecords = new List<ZoneEntryRecord>();
    private Dictionary<Bug, float> bugEntryTimes = new Dictionary<Bug, float>();

    // 当前状态
    private Coroutine guaranteeCoroutine;
    private bool systemActive = false;

    void Awake()
    {
        targetZone = FindObjectOfType<TargetZone>();
        bugSpawner = FindObjectOfType<BugSpawner>();
        gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        SetupTargetZoneEvents();
    }

    void Update()
    {
        if (systemActive)
        {
            UpdateTimeRemaining();

            if (enableTestMode)
            {
                HandleTestInput();
            }
        }
    }

    #region 公共接口

    /// <summary>
    /// 开始机会追踪系统
    /// </summary>
    public void StartOpportunityTracking()
    {
        systemActive = true;
        gameStartTime = Time.time;
        currentOpportunities = 0;
        entryRecords.Clear();
        bugEntryTimes.Clear();
        isGuaranteeActive = false;

        if (guaranteeCoroutine != null)
        {
            StopCoroutine(guaranteeCoroutine);
        }

        guaranteeCoroutine = StartCoroutine(OpportunityGuaranteeCoroutine());

        if (showDebugInfo)
        {
            Debug.Log($"🎯 开始机会追踪：需要在{timeWindow}秒内提供{requiredOpportunities}次单独机会");
        }
    }

    /// <summary>
    /// 停止机会追踪系统
    /// </summary>
    public void StopOpportunityTracking()
    {
        systemActive = false;

        if (guaranteeCoroutine != null)
        {
            StopCoroutine(guaranteeCoroutine);
            guaranteeCoroutine = null;
        }

        if (showDebugInfo)
        {
            Debug.Log($"🛑 停止机会追踪。最终统计：{currentOpportunities}/{requiredOpportunities}次机会");
        }
    }

    /// <summary>
    /// 获取当前机会统计
    /// </summary>
    public (int current, int required, float timeRemaining) GetOpportunityStats()
    {
        float remaining = systemActive ? (gameStartTime + timeWindow - Time.time) : 0f;
        return (currentOpportunities, requiredOpportunities, remaining);
    }

    #endregion

    #region 事件处理

    void SetupTargetZoneEvents()
    {
        if (targetZone == null)
        {
            Debug.LogError("❌ TargetZone未找到！");
            return;
        }

        // 订阅目标区域事件
        targetZone.OnBugEnterZone.AddListener(OnBugEnterZone);
        targetZone.OnBugExitZone.AddListener(OnBugExitZone);

        if (showDebugInfo)
        {
            Debug.Log("✅ 已订阅TargetZone事件");
        }
    }

    void OnBugEnterZone(Bug bug)
    {
        if (!systemActive || bug == null) return;

        float currentTime = Time.time;
        bugEntryTimes[bug] = currentTime;

        // 检查是否是单独进入
        bool isSolo = IsTargetZoneSolo(bug);

        if (showDebugInfo)
        {
            Debug.Log($"🐛 虫子进入目标区域: {bug.bugType}, 单独={isSolo}");
        }

        // 如果是苍蝇单独进入，开始监控
        if (bug.bugType == BugType.Fly && isSolo)
        {
            StartCoroutine(MonitorSoloOpportunity(bug, currentTime));
        }
    }

    void OnBugExitZone(Bug bug)
    {
        if (!systemActive || bug == null) return;

        if (bugEntryTimes.ContainsKey(bug))
        {
            bugEntryTimes.Remove(bug);
        }

        if (showDebugInfo)
        {
            Debug.Log($"🐛 虫子离开目标区域: {bug.bugType}");
        }
    }

    #endregion

    #region 机会检测和记录

    /// <summary>
    /// 检查目标区域是否只有指定虫子
    /// </summary>
    bool IsTargetZoneSolo(Bug targetBug)
    {
        if (targetZone == null) return false;

        var bugsInZone = targetZone.GetBugsInZone();

        // 只有1只虫子且是目标虫子
        return bugsInZone.Count == 1 && bugsInZone.Contains(targetBug);
    }

    /// <summary>
    /// 监控单独机会
    /// </summary>
    IEnumerator MonitorSoloOpportunity(Bug bug, float startTime)
    {
        float soloStartTime = startTime;
        bool opportunityRecorded = false;

        while (bug != null && bugEntryTimes.ContainsKey(bug))
        {
            // 检查是否仍然单独
            bool stillSolo = IsTargetZoneSolo(bug);

            if (!stillSolo)
            {
                // 不再单独，重置计时
                soloStartTime = Time.time;
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // 检查单独时间是否足够
            float soloDuration = Time.time - soloStartTime;

            if (soloDuration >= minSoloDuration && !opportunityRecorded)
            {
                // 记录有效机会
                RecordOpportunity(bug, soloStartTime);
                opportunityRecorded = true;

                if (showDebugInfo)
                {
                    Debug.Log($"✅ 记录有效机会：{bug.bugType} 单独停留 {soloDuration:F1}秒");
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 记录有效机会
    /// </summary>
    void RecordOpportunity(Bug bug, float entryTime)
    {
        currentOpportunities++;

        ZoneEntryRecord record = new ZoneEntryRecord(entryTime, bug.bugType, true);
        entryRecords.Add(record);

        if (showDebugInfo)
        {
            Debug.Log($"📊 机会统计：{currentOpportunities}/{requiredOpportunities}");
        }
    }

    #endregion

    #region 机会保证系统

    /// <summary>
    /// 机会保证协程
    /// </summary>
    IEnumerator OpportunityGuaranteeCoroutine()
    {
        while (systemActive)
        {
            float elapsed = Time.time - gameStartTime;
            float remaining = timeWindow - elapsed;

            // 检查是否需要紧急干预
            if (remaining <= emergencySpawnTime && currentOpportunities < requiredOpportunities)
            {
                int neededOpportunities = requiredOpportunities - currentOpportunities;

                if (showDebugInfo)
                {
                    Debug.Log($"🚨 紧急干预：剩余{remaining:F1}秒，需要{neededOpportunities}次机会");
                }

                yield return StartCoroutine(EmergencyOpportunityMode(neededOpportunities, remaining));
            }

            // 时间窗口结束
            if (remaining <= 0)
            {
                break;
            }

            yield return new WaitForSeconds(1f);
        }

        // 最终统计
        LogFinalStats();
    }

    /// <summary>
    /// 紧急机会模式
    /// </summary>
    IEnumerator EmergencyOpportunityMode(int neededOpportunities, float timeRemaining)
    {
        isGuaranteeActive = true;

        if (showDebugInfo)
        {
            Debug.Log($"🚨 启动紧急机会模式：{neededOpportunities}次机会 in {timeRemaining:F1}秒");
        }

        // 增强苍蝇生成和吸引力
        if (enableSmartAdjustment && bugSpawner != null)
        {
            yield return StartCoroutine(BoostFlyGeneration(neededOpportunities, timeRemaining));
        }

        isGuaranteeActive = false;
    }

    /// <summary>
    /// 增强苍蝇生成
    /// </summary>
    IEnumerator BoostFlyGeneration(int neededOpportunities, float timeAvailable)
    {
        float intervalBetweenFlies = timeAvailable / (neededOpportunities + 1);

        for (int i = 0; i < neededOpportunities; i++)
        {
            if (!systemActive) break;

            // 清理目标区域的蜜蜂
            ClearBeesFromTargetZone();

            yield return new WaitForSeconds(0.5f);

            // 生成一只特殊的"保证机会"苍蝇
            SpawnGuaranteedOpportunityFly();

            if (showDebugInfo)
            {
                Debug.Log($"🎯 生成保证机会苍蝇 #{i + 1}");
            }

            yield return new WaitForSeconds(intervalBetweenFlies);
        }
    }

    /// <summary>
    /// 清理目标区域的蜜蜂
    /// </summary>
    void ClearBeesFromTargetZone()
    {
        if (targetZone == null) return;

        var bugsInZone = targetZone.GetBugsInZone();
        foreach (Bug bug in bugsInZone)
        {
            if (bug != null && bug.bugType == BugType.Bee)
            {
                // 给蜜蜂一个远离目标区域的推力
                ApplyRepulsionForce(bug.gameObject);

                if (showDebugInfo)
                {
                    Debug.Log($"🐝 推离蜜蜂: {bug.name}");
                }
            }
        }
    }

    /// <summary>
    /// 生成保证机会的苍蝇
    /// </summary>
    void SpawnGuaranteedOpportunityFly()
    {
        if (bugSpawner == null || targetZone == null) return;

        // 创建一只特殊的苍蝇，它会直接飞向目标区域
        GameObject guaranteedFly = CreateGuaranteedFly();

        if (guaranteedFly != null)
        {
            // 添加特殊的飞行行为
            GuaranteedFlyMovement guaranteedMovement = guaranteedFly.AddComponent<GuaranteedFlyMovement>();
            guaranteedMovement.Initialize(targetZone.transform.position, minSoloDuration + 1f);
        }
    }

    /// <summary>
    /// 创建保证机会的苍蝇
    /// </summary>
    GameObject CreateGuaranteedFly()
    {
        // 在屏幕边缘随机位置生成
        Vector3 spawnPosition = GetRandomEdgePosition();

        GameObject flyPrefab = bugSpawner.flyPrefab;
        GameObject guaranteedFly;

        if (flyPrefab != null)
        {
            guaranteedFly = Instantiate(flyPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            // 创建简单的方块苍蝇
            guaranteedFly = CreateSimpleFly(spawnPosition);
        }

        // 确保有Bug组件
        Bug bugComponent = guaranteedFly.GetComponent<Bug>();
        if (bugComponent == null)
        {
            bugComponent = guaranteedFly.AddComponent<Bug>();
        }
        bugComponent.bugType = BugType.Fly;

        return guaranteedFly;
    }

    #endregion

    #region 辅助方法

    void UpdateTimeRemaining()
    {
        timeRemaining = Mathf.Max(0f, gameStartTime + timeWindow - Time.time);
    }

    void ApplyRepulsionForce(GameObject bugObject)
    {
        if (targetZone == null) return;

        Vector3 awayDirection = (bugObject.transform.position - targetZone.transform.position).normalized;
        float repulsionForce = 5f;

        // 如果虫子有FlyMovement组件，修改其目标
        FlyMovement flyMovement = bugObject.GetComponent<FlyMovement>();
        if (flyMovement != null)
        {
            Vector3 newTarget = bugObject.transform.position + awayDirection * repulsionForce;
            flyMovement.Initialize(newTarget, 3f);
        }
        else
        {
            // 直接移动
            bugObject.transform.position += awayDirection * repulsionForce * Time.deltaTime;
        }
    }

    Vector3 GetRandomEdgePosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.zero;

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 10));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 10));

        int edge = Random.Range(0, 4);
        Vector3 spawnPos = Vector3.zero;

        switch (edge)
        {
            case 0: // 左边
                spawnPos = new Vector3(bottomLeft.x - 2f, Random.Range(bottomLeft.y, topRight.y), 0f);
                break;
            case 1: // 右边
                spawnPos = new Vector3(topRight.x + 2f, Random.Range(bottomLeft.y, topRight.y), 0f);
                break;
            case 2: // 上边
                spawnPos = new Vector3(Random.Range(bottomLeft.x, topRight.x), topRight.y + 2f, 0f);
                break;
            case 3: // 下边
                spawnPos = new Vector3(Random.Range(bottomLeft.x, topRight.x), bottomLeft.y - 2f, 0f);
                break;
        }

        return spawnPos;
    }

    GameObject CreateSimpleFly(Vector3 position)
    {
        GameObject fly = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fly.name = "GuaranteedFly";
        fly.transform.position = position;
        fly.transform.localScale = Vector3.one * 0.3f;

        // 移除默认碰撞器，添加2D碰撞器
        DestroyImmediate(fly.GetComponent<BoxCollider>());
        fly.AddComponent<BoxCollider2D>();

        // 设置颜色
        Renderer renderer = fly.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red; // 红色表示保证机会苍蝇
        }

        return fly;
    }

    void LogFinalStats()
    {
        if (!showDebugInfo) return;

        Debug.Log($"📊 最终机会统计报告：");
        Debug.Log($"   - 时间窗口：{timeWindow}秒");
        Debug.Log($"   - 要求机会：{requiredOpportunities}次");
        Debug.Log($"   - 实际机会：{currentOpportunities}次");
        Debug.Log($"   - 成功率：{(currentOpportunities >= requiredOpportunities ? "✅ 达标" : "❌ 未达标")}");

        for (int i = 0; i < entryRecords.Count; i++)
        {
            var record = entryRecords[i];
            Debug.Log($"   机会#{i + 1}: {record.bugType} at {record.entryTime - gameStartTime:F1}s");
        }
    }

    void HandleTestInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("🧪 测试：手动触发紧急模式");
            StartCoroutine(EmergencyOpportunityMode(1, 10f));
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("🧪 测试：重置机会计数");
            currentOpportunities = 0;
        }
    }

    #endregion
}
