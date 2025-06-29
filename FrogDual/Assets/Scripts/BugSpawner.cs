using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BugSpawner : MonoBehaviour
{
    [Header(" 虫子预制体")]
    public GameObject flyPrefab;
    public GameObject beePrefab;
    public float spawnRadius = 3f;

    [Header("移动设置")]
    public float flySpeed = 2f;
    [Range(0.5f, 10f)]
    public float speedVariation = 0.5f; // 速度随机变化范围
    public TargetZone targetZone;

    [Header(" 游戏规则 - 苍蝇")]
    [Range(1, 5)]
    public int flyCount = 1;               // 苍蝇数量
    [Range(0f, 10f)]
    public float flySpawnDelay = 0f;       // 苍蝇延迟生成时间

    [Header(" 游戏规则 - 蜜蜂")]
    [Range(0, 10)]
    public int initialBeeCount = 3;        // 初始蜜蜂数量
    [Range(1, 15)]
    public int maxBeeCount = 5;            // 最大蜜蜂数量
    [Range(0.5f, 10f)]
    public float beeSpawnInterval = 3f;    // 蜜蜂生成间隔
    [Range(0f, 5f)]
    public float firstBeeDelay = 0f;       // 第一只额外蜜蜂延迟时间

    [Header(" 生成点设置")]
    public Transform[] spawnPoints;
    public bool useFixedSpawnPoints = false;
    [Range(0.5f, 5f)]
    public float edgeOffset = 1f;          // 屏幕边缘偏移

    [Header(" 引用")]
    public Transform spawnCenter;

    [Header(" 生命周期")]
    [Range(5f, 30f)]
    public float bugLifetime = 15f;        // 虫子生存时间

    [Header("调试")]
    public bool showDebugInfo = false;
    [Space]
    [Header(" 运行时状态 (只读)")]
    [SerializeField, Tooltip("当前蜜蜂数量")]
    private int currentBeeCount = 0;
    [SerializeField, Tooltip("当前苍蝇数量")]
    private int currentFlyCount = 0;
    [SerializeField, Tooltip("下次蜜蜂生成时间")]
    private float nextBeeSpawnTime = 0f;

    // 虫子管理
    private List<GameObject> activeBees = new List<GameObject>();
    private List<GameObject> activeFlies = new List<GameObject>();
    private bool gameStarted = false;

    void Start()
    {
        if (spawnCenter == null)
            spawnCenter = transform;
    }

    void Update()
    {
        // 检查是否需要生成新蜜蜂
        if (gameStarted && Time.time >= nextBeeSpawnTime && activeBees.Count < maxBeeCount)
        {
            SpawnSingleBee();
            nextBeeSpawnTime = Time.time + beeSpawnInterval;
        }

        // 清理已销毁的虫子
        CleanupDestroyedBugs();

        // 更新运行时状态显示
        UpdateRuntimeStats();

        // 测试按键
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGameSpawning();
        }
    }

    /// <summary>
    /// 更新运行时状态显示
    /// </summary>
    void UpdateRuntimeStats()
    {
        currentBeeCount = activeBees.Count;
        currentFlyCount = activeFlies.Count;
    }

    /// <summary>
    /// 开始游戏时调用 - 生成初始虫子群
    /// </summary>
    public void StartGameSpawning()
    {
        StopAllCoroutines(); // 停止之前的生成
        ClearAllBugs();      // 清除现有虫子

        gameStarted = true;
        nextBeeSpawnTime = Time.time + firstBeeDelay + beeSpawnInterval;

        // 生成初始虫子群
        StartCoroutine(SpawnInitialBugsWithDelay());

        if (showDebugInfo)
        {
            Debug.Log($"🎮 游戏开始! 将生成 {initialBeeCount} 只蜜蜂 + {flyCount} 只苍蝇");
        }
    }

    /// <summary>
    /// 停止游戏生成
    /// </summary>
    public void StopGameSpawning()
    {
        gameStarted = false;
        StopAllCoroutines();

        if (showDebugInfo)
        {
            Debug.Log("🛑 停止虫子生成");
        }
    }

    /// <summary>
    /// 带延迟的生成初始虫子
    /// </summary>
    IEnumerator SpawnInitialBugsWithDelay()
    {
        // 生成初始蜜蜂群
        for (int i = 0; i < initialBeeCount; i++)
        {
            SpawnSingleBee();

            if (showDebugInfo)
            {
                Debug.Log($"🐝 生成初始蜜蜂 #{i + 1}");
            }

            // 在蜜蜂间添加小间隔，避免重叠
            yield return new WaitForSeconds(0.2f);
        }

        // 延迟生成苍蝇
        if (flySpawnDelay > 0)
        {
            yield return new WaitForSeconds(flySpawnDelay);
        }

        // 生成苍蝇
        for (int i = 0; i < flyCount; i++)
        {
            SpawnSingleFly();

            if (showDebugInfo)
            {
                Debug.Log($"🐛 生成苍蝇 #{i + 1}");
            }

            // 在苍蝇间添加小间隔
            if (i < flyCount - 1)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    /// <summary>
    /// 生成单只蜜蜂
    /// </summary>
    void SpawnSingleBee()
    {
        GameObject bee = CreateBug(BugType.Bee);
        if (bee != null)
        {
            activeBees.Add(bee);

            if (showDebugInfo)
            {
                Debug.Log($"🐝 生成蜜蜂，当前蜜蜂数量: {activeBees.Count}/{maxBeeCount}");
            }
        }
    }

    /// <summary>
    /// 生成单只苍蝇
    /// </summary>
    void SpawnSingleFly()
    {
        GameObject fly = CreateBug(BugType.Fly);
        if (fly != null)
        {
            activeFlies.Add(fly);

            if (showDebugInfo)
            {
                Debug.Log($"🐛 生成苍蝇，当前苍蝇数量: {activeFlies.Count}");
            }
        }
    }

    /// <summary>
    /// 创建虫子的核心方法
    /// </summary>
    GameObject CreateBug(BugType bugType)
    {
        Vector3 spawnPosition = GetSpawnPosition();
        Vector3 targetPosition = targetZone != null ? targetZone.transform.position : Vector3.zero;

        GameObject bugObject;
        GameObject selectedPrefab = bugType == BugType.Fly ? flyPrefab : beePrefab;

        if (selectedPrefab != null)
        {
            // 使用预制体
            bugObject = Instantiate(selectedPrefab);
            bugObject.transform.position = spawnPosition;

            // 确保有Bug组件
            Bug bugComponent = bugObject.GetComponent<Bug>();
            if (bugComponent == null)
            {
                bugComponent = bugObject.AddComponent<Bug>();
            }
            bugComponent.bugType = bugType;
        }
        else
        {
            // 创建简单方块
            if (bugType == BugType.Fly)
            {
                bugObject = CreateSquareFly(spawnPosition);
            }
            else
            {
                bugObject = CreateSquareBee(spawnPosition);
            }
        }

        // 添加飞行移动
        FlyMovement flyMovement = bugObject.GetComponent<FlyMovement>();
        if (flyMovement == null)
        {
            flyMovement = bugObject.AddComponent<FlyMovement>();
        }

        // 添加速度随机变化
        float randomSpeed = flySpeed + Random.Range(-speedVariation, speedVariation);
        flyMovement.Initialize(targetPosition, randomSpeed);

        // 使用协程定时销毁
        StartCoroutine(DestroyBugAfterTime(bugObject, bugLifetime));

        return bugObject;
    }

    /// <summary>
    /// 定时销毁虫子的协程
    /// </summary>
    IEnumerator DestroyBugAfterTime(GameObject bugObject, float time)
    {
        yield return new WaitForSeconds(time);

        if (bugObject != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"🗑️ 定时销毁虫子: {bugObject.name}");
            }
            Destroy(bugObject);
        }
    }

    /// <summary>
    /// 清理已销毁的虫子引用
    /// </summary>
    void CleanupDestroyedBugs()
    {
        // 清理蜜蜂列表
        for (int i = activeBees.Count - 1; i >= 0; i--)
        {
            if (activeBees[i] == null)
            {
                activeBees.RemoveAt(i);
            }
        }

        // 清理苍蝇列表
        for (int i = activeFlies.Count - 1; i >= 0; i--)
        {
            if (activeFlies[i] == null)
            {
                activeFlies.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 清除所有虫子
    /// </summary>
    void ClearAllBugs()
    {
        // 销毁所有蜜蜂
        foreach (GameObject bee in activeBees)
        {
            if (bee != null)
            {
                Destroy(bee);
            }
        }
        activeBees.Clear();

        // 销毁所有苍蝇
        foreach (GameObject fly in activeFlies)
        {
            if (fly != null)
            {
                Destroy(fly);
            }
        }
        activeFlies.Clear();

        if (showDebugInfo)
        {
            Debug.Log("🧹 清除了所有虫子");
        }
    }

    /// <summary>
    /// 获取生成位置
    /// </summary>
    Vector3 GetSpawnPosition()
    {
        if (useFixedSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return randomSpawnPoint.position;
        }
        else
        {
            return GetEdgeSpawnPosition();
        }
    }

    /// <summary>
    /// 从摄像机边缘获取生成位置
    /// </summary>
    Vector3 GetEdgeSpawnPosition()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("⚠️ 没有找到主摄像机，使用默认位置");
            return Vector3.zero;
        }

        // 获取摄像机的视口边界（世界坐标）
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));

        // 确保Z坐标正确
        bottomLeft.z = -1f;
        topRight.z = -1f;

        int edge = Random.Range(0, 4);
        Vector3 spawnPos = Vector3.zero;

        switch (edge)
        {
            case 0: // 左边
                spawnPos = new Vector3(
                    bottomLeft.x - edgeOffset,
                    Random.Range(bottomLeft.y, topRight.y),
                    -1f
                );
                break;

            case 1: // 右边
                spawnPos = new Vector3(
                    topRight.x + edgeOffset,
                    Random.Range(bottomLeft.y, topRight.y),
                    -1f
                );
                break;

            case 2: // 上边
                spawnPos = new Vector3(
                    Random.Range(bottomLeft.x, topRight.x),
                    topRight.y + edgeOffset,
                    -1f
                );
                break;

            case 3: // 下边
                spawnPos = new Vector3(
                    Random.Range(bottomLeft.x, topRight.x),
                    bottomLeft.y - edgeOffset,
                    -1f
                );
                break;
        }

        if (showDebugInfo)
        {
            Debug.Log($"📍 从边缘 {edge} 生成虫子: {spawnPos}");
        }

        return spawnPos;
    }

    #region 创建简单方块虫子的方法

    GameObject CreateSquareFly(Vector3 position)
    {
        GameObject bugObject = new GameObject("Fly");
        bugObject.transform.position = position;
        bugObject.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer renderer = bugObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = Color.black;
        renderer.sortingOrder = 20;

        BoxCollider2D collider = bugObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        Bug bugComponent = bugObject.AddComponent<Bug>();
        bugComponent.bugType = BugType.Fly;

        return bugObject;
    }

    GameObject CreateSquareBee(Vector3 position)
    {
        GameObject bugObject = new GameObject("Bee");
        bugObject.transform.position = position;
        bugObject.transform.localScale = Vector3.one * 0.3f;

        SpriteRenderer renderer = bugObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = Color.yellow;
        renderer.sortingOrder = 20;

        BoxCollider2D collider = bugObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        Bug bugComponent = bugObject.AddComponent<Bug>();
        bugComponent.bugType = BugType.Bee;

        return bugObject;
    }

    Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 获取当前虫子状态
    /// </summary>
    public (int beeCount, int flyCount) GetBugCounts()
    {
        CleanupDestroyedBugs();
        return (activeBees.Count, activeFlies.Count);
    }

    /// <summary>
    /// 检查是否还有活跃的苍蝇
    /// </summary>
    public bool HasActiveFlies()
    {
        CleanupDestroyedBugs();
        return activeFlies.Count > 0;
    }

    /// <summary>
    /// 旧接口兼容 - 现在调用新的开始方法
    /// </summary>
    public void SpawnFlyingBug()
    {
        if (!gameStarted)
        {
            StartGameSpawning();
        }
    }

    /// <summary>
    /// 获取当前虫子 (兼容性方法)
    /// </summary>
    public Bug GetCurrentBug()
    {
        // 返回第一只活跃的苍蝇
        CleanupDestroyedBugs();

        if (activeFlies.Count > 0 && activeFlies[0] != null)
        {
            return activeFlies[0].GetComponent<Bug>();
        }

        return null;
    }

    #endregion
}
