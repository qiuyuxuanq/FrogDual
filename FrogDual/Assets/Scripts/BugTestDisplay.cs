using UnityEngine;

public class BugTestDisplay : MonoBehaviour
{
    [Header("测试设置")]
    public bool showTestBug = true;
    public TargetZone targetZone;
    public GameObject testBugPrefab;
    
    [Header("Bug设置")]
    public float bugSize = 0.5f;
    public Color flyColor = Color.green;
    public Color beeColor = Color.yellow;
    
    private GameObject currentTestBug;
    
    void Start()
    {
        if (showTestBug)
        {
            CreateTestBug();
        }
    }
    
    void CreateTestBug()
    {
        if (targetZone == null) return;
        
        // 在目标区域中心创建测试bug
        Vector3 bugPosition = targetZone.transform.position;
        
        if (testBugPrefab != null)
        {
            currentTestBug = Instantiate(testBugPrefab, bugPosition, Quaternion.identity);
        }
        else
        {
            // 创建简单的测试bug
            currentTestBug = CreateSimpleBug(bugPosition);
        }
        
        Debug.Log($"🐛 测试bug已创建在位置: {bugPosition}");
    }
    
    GameObject CreateSimpleBug(Vector3 position)
    {
        GameObject bug = new GameObject("TestBug");
        bug.transform.position = position;
        bug.transform.localScale = Vector3.one * bugSize;
        
        // 添加SpriteRenderer
        SpriteRenderer renderer = bug.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = flyColor;
        renderer.sortingOrder = 15; // 确保在其他对象之上
        
        // 添加Collider2D用于点击检测
        CircleCollider2D collider = bug.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        // 添加Bug组件
        Bug bugComponent = bug.AddComponent<Bug>();
        bugComponent.bugType = BugType.Fly;
        
        return bug;
    }
    
    Sprite CreateCircleSprite()
    {
        // 创建简单的圆形精灵
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        Vector2 center = new Vector2(32, 32);
        float radius = 30;
        
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[x + y * 64] = Color.white;
                }
                else
                {
                    pixels[x + y * 64] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
    
    public void MoveBugToRandomPosition()
    {
        if (currentTestBug == null || targetZone == null) return;
        
        // 在目标区域内随机移动bug
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0f, targetZone.radius * 0.8f);
        
        Vector3 randomOffset = new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0
        );
        
        currentTestBug.transform.position = targetZone.transform.position + randomOffset;
        
        Debug.Log($"🐛 Bug移动到新位置: {currentTestBug.transform.position}");
    }
    
    public void ToggleBugType()
    {
        if (currentTestBug == null) return;
        
        Bug bugComponent = currentTestBug.GetComponent<Bug>();
        SpriteRenderer renderer = currentTestBug.GetComponent<SpriteRenderer>();
        
        if (bugComponent.bugType == BugType.Fly)
        {
            bugComponent.bugType = BugType.Bee;
            renderer.color = beeColor;
            Debug.Log("🐝 Bug类型改为蜜蜂");
        }
        else
        {
            bugComponent.bugType = BugType.Fly;
            renderer.color = flyColor;
            Debug.Log("🪰 Bug类型改为苍蝇");
        }
    }
    
    void Update()
    {
        // 按R键随机移动bug
        if (Input.GetKeyDown(KeyCode.R))
        {
            MoveBugToRandomPosition();
        }
        
        // 按T键切换bug类型
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleBugType();
        }
    }
    
    void OnDestroy()
    {
        if (currentTestBug != null)
        {
            Destroy(currentTestBug);
        }
    }
}