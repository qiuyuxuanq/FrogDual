using UnityEngine;

public class BugSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject flyPrefab;
    public GameObject beePrefab;
    public float spawnRadius = 3f;
    public float flySpawnChance = 0.7f;

    [Header("Movement Settings")]
    public float flySpeed = 2f;
    public TargetZone targetZone;
    public bool autoSpawnTest = false;  // 测试模式
    public float autoSpawnInterval = 3f;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;     // 可以设置多个生成点
    public bool useFixedSpawnPoints = false;  // 使用固定生成点还是随机边缘
    
    [Header("References")]
    public Transform spawnCenter;

    private Bug currentBug;
    private float nextSpawnTime;

    void Start()
    {
        if (spawnCenter == null)
            spawnCenter = transform;

        if (autoSpawnTest)
        {
            nextSpawnTime = Time.time + autoSpawnInterval;
        }
    }

    void Update()
    {
        // 测试模式：自动生成苍蝇
        if (autoSpawnTest && Time.time >= nextSpawnTime)
        {
            SpawnFlyingBug();
            nextSpawnTime = Time.time + autoSpawnInterval;
        }

        // 按键测试：空格键生成苍蝇
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnFlyingBug();
        }
    }

    public void SpawnFlyingBug()
    {
        // 不再销毁之前的bug，让它们自然飞出屏幕

        // 选择生成位置
        Vector3 spawnPosition = GetSpawnPosition();
        Vector3 targetPosition = targetZone != null ? targetZone.transform.position : Vector3.zero;
        Vector3 finalPosition = spawnPosition;
        
        Debug.Log($"🎯 开始生成虫子");

        GameObject bugObject;
        BugType selectedType = Random.Range(0f, 1f) < flySpawnChance ? BugType.Fly : BugType.Bee;

        // 根据类型选择对应的prefab
        GameObject selectedPrefab = selectedType == BugType.Fly ? flyPrefab : beePrefab;

        if (selectedPrefab != null)
        {
            // 使用对应的prefab
            bugObject = Instantiate(selectedPrefab);
            finalPosition.z = selectedPrefab.transform.position.z;
            bugObject.transform.position = finalPosition;

            // 确保有Bug组件
            Bug bugComponent = bugObject.GetComponent<Bug>();
            if (bugComponent == null)
            {
                bugComponent = bugObject.AddComponent<Bug>();
            }
            bugComponent.bugType = selectedType;

            currentBug = bugComponent;
        }
        else
        {
            // 创建简单的颜色方块作为备用
            if (selectedType == BugType.Fly)
            {
                bugObject = CreateSquareFly(spawnPosition);
            }
            else
            {
                bugObject = CreateSquareBee(spawnPosition);
            }
            
            finalPosition = bugObject.transform.position;
            currentBug = bugObject.GetComponent<Bug>();
        }

        // 现在给所有虫子都添加飞行组件，蜜蜂和苍蝇都会飞行
        FlyMovement flyMovement = bugObject.AddComponent<FlyMovement>();
        flyMovement.Initialize(targetPosition, flySpeed);
        
        Debug.Log($"🐛 {selectedType}开始智能追踪飞行");
    }

    GameObject CreateSquareFly(Vector3 position)
    {
        GameObject bugObject = new GameObject("Fly");
        bugObject.transform.position = position;
        bugObject.transform.localScale = Vector3.one * 0.3f;

        // 创建黑色方块
        SpriteRenderer renderer = bugObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = Color.black;
        renderer.sortingOrder = 20;

        // 添加碰撞器
        BoxCollider2D collider = bugObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        // 添加Bug组件
        Bug bugComponent = bugObject.AddComponent<Bug>();
        bugComponent.bugType = BugType.Fly;
        
        Debug.Log($"🐛 创建苍蝇：颜色=黑色, BugType=Fly");

        return bugObject;
    }

    GameObject CreateSquareBee(Vector3 position)
    {
        GameObject bugObject = new GameObject("Bee");
        bugObject.transform.position = position;
        bugObject.transform.localScale = Vector3.one * 0.3f;

        // 创建黄色方块
        SpriteRenderer renderer = bugObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSquareSprite();
        renderer.color = Color.yellow;
        renderer.sortingOrder = 20;

        // 添加碰撞器
        BoxCollider2D collider = bugObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        // 添加Bug组件
        Bug bugComponent = bugObject.AddComponent<Bug>();
        bugComponent.bugType = BugType.Bee;
        
        Debug.Log($"🐝 创建蜜蜂：颜色=黄色, BugType=Bee");

        return bugObject;
    }

    Sprite CreateSquareSprite()
    {
        // 创建简单的方形精灵
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

    Vector3 GetSpawnPosition()
    {
        if (useFixedSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
        {
            // 使用固定生成点
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 spawnPos = randomSpawnPoint.position;
            return spawnPos;
        }
        else
        {
            // 使用随机边缘生成
            return GetEdgeSpawnPosition();
        }
    }

    Vector3 GetEdgeSpawnPosition()
    {
        // 从屏幕四个边之一生成 (相对于PlayerFrog的位置调整)
        int edge = Random.Range(0, 4);
        Vector3 spawnPos = Vector3.zero;
        
        switch (edge)
        {
            case 0: // 左边
                spawnPos = new Vector3(-250f, Random.Range(-150f, -50f), -1f);
                break;
            case 1: // 右边
                spawnPos = new Vector3(50f, Random.Range(-150f, -50f), -1f);
                break;
            case 2: // 上边
                spawnPos = new Vector3(Random.Range(-250f, 50f), 50f, -1f);
                break;
            case 3: // 下边
                spawnPos = new Vector3(Random.Range(-250f, 50f), -200f, -1f);
                break;
        }
        
        return spawnPos;
    }

    void CreateSimpleBug(Vector3 position, BugType bugType)
    {
        GameObject bugObject = new GameObject($"Bug_{bugType}");
        bugObject.transform.position = position;

        SpriteRenderer renderer = bugObject.AddComponent<SpriteRenderer>();
        renderer.color = bugType == BugType.Fly ? Color.black : Color.yellow;

        CircleCollider2D collider = bugObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.2f;

        currentBug = bugObject.AddComponent<Bug>();
        currentBug.bugType = bugType;
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return spawnCenter.position + new Vector3(randomCircle.x, randomCircle.y, 0f);
    }

    public Bug GetCurrentBug()
    {
        return currentBug;
    }
}