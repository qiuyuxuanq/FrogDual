using UnityEngine;
using System.Collections;
using System.Linq;

public class AIFrog : MonoBehaviour
{
    [Header("References")]
    public AIController aiController;
    public TargetZone targetZone;

    [Header("调试信息")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool currentlyMonitoring = false;
    [SerializeField] private bool currentlyInAttackMode = false;
    [SerializeField] private string currentSpriteName = "";
    [SerializeField] private int spriteChangeCount = 0;
    [SerializeField] private string loadedIdleSprite = "";
    [SerializeField] private string loadedAttackSprite = "";

    private SpriteRenderer spriteRenderer;
    private bool isInAttackMode = false;
    private bool isMonitoring = false;
    private GameObject currentFly;
    private Coroutine monitoringCoroutine;

    // 直接通过Resources加载sprite，类似PlayerFrog的简单方法
    private Sprite aiIdleSprite;
    private Sprite aiAttackSprite;

    void Start()
    {
        // 获取SpriteRenderer组件
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("❌ AIFrog: 没有找到SpriteRenderer组件!");
            return;
        }

        // 自动查找AIController
        if (aiController == null)
        {
            aiController = FindObjectOfType<AIController>();
            if (aiController != null)
            {
                Debug.Log($"✅ 找到AIController，反应时间: {aiController.fixedReactionTime}秒");
            }
            else
            {
                Debug.LogError("❌ 没有找到AIController!");
            }
        }

        // 自动查找TargetZone
        if (targetZone == null)
        {
            targetZone = FindObjectOfType<TargetZone>();
            if (targetZone != null)
            {
                Debug.Log("✅ 找到TargetZone");
            }
            else
            {
                Debug.LogError("❌ 没有找到TargetZone!");
            }
        }

        // 直接加载sprite资源
        LoadSprites();

        // 设置初始状态为idle
        SetIdleSprite();

        Debug.Log("🤖 AI Frog 初始化完成");
    }

    /// <summary>
    /// 直接加载sprite资源
    /// </summary>
    void LoadSprites()
    {
        // 尝试从当前SpriteRenderer获取现有的sprite作为idle
        if (spriteRenderer.sprite != null && spriteRenderer.sprite.name.Contains("AIIdle"))
        {
            aiIdleSprite = spriteRenderer.sprite;
            Debug.Log($"✅ 使用当前sprite作为AI Idle: {aiIdleSprite.name}");
        }

        // 尝试从Resources加载
        if (aiIdleSprite == null)
        {
            aiIdleSprite = Resources.Load<Sprite>("AIIdle");
            if (aiIdleSprite != null)
            {
                Debug.Log($"✅ 从Resources加载AI Idle Sprite: {aiIdleSprite.name}");
            }
        }

        if (aiAttackSprite == null)
        {
            aiAttackSprite = Resources.Load<Sprite>("AIAttack");
            if (aiAttackSprite != null)
            {
                Debug.Log($"✅ 从Resources加载AI Attack Sprite: {aiAttackSprite.name}");
            }
        }

        // 如果还是没有找到，尝试通过名称查找所有sprite
        if (aiIdleSprite == null || aiAttackSprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite sprite in allSprites)
            {
                if (aiIdleSprite == null && sprite.name.Contains("AIIdle"))
                {
                    aiIdleSprite = sprite;
                    Debug.Log($"✅ 找到AI Idle Sprite: {aiIdleSprite.name}");
                }
                if (aiAttackSprite == null && sprite.name.Contains("AIAttack"))
                {
                    aiAttackSprite = sprite;
                    Debug.Log($"✅ 找到AI Attack Sprite: {aiAttackSprite.name}");
                }
            }
        }

        // 如果还是没有找到，显示警告
        if (aiIdleSprite == null)
        {
            Debug.LogWarning("⚠️ 无法找到AIIdle sprite!");
        }
        else
        {
            Debug.Log($"✅ 最终加载的AI Idle Sprite: {aiIdleSprite.name}, 路径: {GetSpritePath(aiIdleSprite)}");
        }

        if (aiAttackSprite == null)
        {
            Debug.LogWarning("⚠️ 无法找到AIAttack sprite!");
        }
        else
        {
            Debug.Log($"✅ 最终加载的AI Attack Sprite: {aiAttackSprite.name}, 路径: {GetSpritePath(aiAttackSprite)}");
        }
    }

    /// <summary>
    /// 获取sprite的路径信息（用于调试）
    /// </summary>
    string GetSpritePath(Sprite sprite)
    {
        if (sprite == null) return "null";
        return $"Texture: {sprite.texture?.name ?? "unknown"}";
    }

    void Update()
    {
        // 更新调试信息
        UpdateDebugInfo();

        // 如果AIController存在且游戏正在进行，开始监控
        if (aiController != null && !isMonitoring)
        {
            StartMonitoring();
        }
    }

    /// <summary>
    /// 更新Inspector中的调试信息
    /// </summary>
    void UpdateDebugInfo()
    {
        currentlyMonitoring = isMonitoring;
        currentlyInAttackMode = isInAttackMode;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            currentSpriteName = spriteRenderer.sprite.name;
        }
    }

    /// <summary>
    /// 开始监控苍蝇位置和AI反应
    /// </summary>
    public void StartMonitoring()
    {
        if (isMonitoring) return;

        isMonitoring = true;
        isInAttackMode = false;
        SetIdleSprite();

        Debug.Log("🔍 AI Frog 开始监控苍蝇位置...");

        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
        }

        monitoringCoroutine = StartCoroutine(MonitorFlyAndReact());
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void StopMonitoring()
    {
        isMonitoring = false;

        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
            monitoringCoroutine = null;
        }

        // 返回到idle状态
        SetIdleSprite();

        Debug.Log("🛑 AI Frog 停止监控");
    }

    /// <summary>
    /// 监控苍蝇位置并根据AI反应时间切换sprite
    /// </summary>
    IEnumerator MonitorFlyAndReact()
    {
        float monitoringTime = 0f;
        bool flyInTargetZone = false;
        float timeInTargetZone = 0f;

        Debug.Log("🔍 开始监控协程...");

        while (isMonitoring)
        {
            // 查找当前苍蝇
            FindCurrentFly();

            if (currentFly == null)
            {
                // Debug.Log("🔍 没有找到苍蝇，继续监控...");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // 检查苍蝇是否在目标区域内
            bool currentlyInZone = IsFlyInTargetZone();

            if (currentlyInZone)
            {
                if (!flyInTargetZone)
                {
                    // 苍蝇刚进入目标区域
                    flyInTargetZone = true;
                    timeInTargetZone = 0f;
                    Debug.Log("🎯 AI Frog 检测到苍蝇进入目标区域，开始计时...");
                }
                else
                {
                    // 苍蝇持续在目标区域内
                    timeInTargetZone += Time.deltaTime;

                    // 每秒显示一次计时进度
                    if (Mathf.FloorToInt(timeInTargetZone) != Mathf.FloorToInt(timeInTargetZone - Time.deltaTime))
                    {
                        Debug.Log($"⏰ 苍蝇在目标区域内 {Mathf.FloorToInt(timeInTargetZone)}秒, 反应时间: {aiController.fixedReactionTime}秒");
                    }

                    // ✅ 修改：只切换sprite，不结束游戏
                    // 游戏结束由AIController通过TriggerAttackAndEndGame触发
                    if (timeInTargetZone >= aiController.fixedReactionTime && !isInAttackMode)
                    {
                        Debug.Log($"⚡ AI Frog 准备攻击！苍蝇在目标区域内超过 {aiController.fixedReactionTime} 秒");

                        // 强调调试信息
                        if (debugMode)
                        {
                            Debug.Log("🚨🚨🚨 AI FROG 切换到攻击模式！！！ 🚨🚨🚨");
                            Debug.Log($"🔍 当前sprite: {spriteRenderer.sprite?.name ?? "null"}");
                            Debug.Log($"🔍 目标attack sprite: {aiAttackSprite?.name ?? "null"}");
                        }

                        SetAttackSprite();

                        // 注意：这里只是切换sprite，游戏结束由AIController触发
                        Debug.Log("🎯 AI Frog已准备就绪，等待AIController信号执行最终攻击...");
                    }
                }
            }
            else
            {
                if (flyInTargetZone)
                {
                    // 苍蝇离开了目标区域
                    flyInTargetZone = false;
                    timeInTargetZone = 0f;

                    // 如果AI已经在攻击模式，返回idle状态
                    if (isInAttackMode)
                    {
                        Debug.Log("🔄 苍蝇离开目标区域，AI Frog 返回待机状态");
                        SetIdleSprite();
                    }
                    else
                    {
                        Debug.Log("🔄 苍蝇离开目标区域，重置计时");
                    }
                }
            }

            monitoringTime += Time.deltaTime;
            yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次，与AIController保持一致
        }

        Debug.Log("🛑 监控协程结束");
    }

    /// <summary>
    /// 查找当前场景中的苍蝇
    /// </summary>
    void FindCurrentFly()
    {
        // 查找所有Bug组件，优先找苍蝇
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

        // 如果没有苍蝇，也可以用FlyMovement组件作为备选
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

    /// <summary>
    /// 检查苍蝇是否在目标区域内
    /// </summary>
    bool IsFlyInTargetZone()
    {
        if (currentFly == null || targetZone == null) return false;

        return targetZone.IsPositionInZone(currentFly.transform.position);
    }

    /// <summary>
    /// 设置为待机图片 - 类似PlayerFrog的简单方法
    /// </summary>
    public void SetIdleSprite()
    {
        if (spriteRenderer != null && aiIdleSprite != null)
        {
            Sprite previousSprite = spriteRenderer.sprite;
            spriteRenderer.sprite = aiIdleSprite;
            isInAttackMode = false;
            spriteChangeCount++;

            if (debugMode)
            {
                Debug.Log($"🤖 AI Frog 切换到待机状态 - {aiIdleSprite.name} (切换次数: {spriteChangeCount})");
                Debug.Log($"🔄 从 {previousSprite?.name ?? "null"} 切换到 {aiIdleSprite.name}");
            }
        }
        else if (aiIdleSprite == null)
        {
            Debug.LogWarning("⚠️ AI Idle Sprite 为空，无法切换");
        }
    }

    /// <summary>
    /// 设置为攻击图片 - 类似PlayerFrog的简单方法
    /// </summary>
    public void SetAttackSprite()
    {
        if (spriteRenderer != null && aiAttackSprite != null)
        {
            Sprite previousSprite = spriteRenderer.sprite;
            spriteRenderer.sprite = aiAttackSprite;
            isInAttackMode = true;
            spriteChangeCount++;

            if (debugMode)
            {
                Debug.Log($"⚔️ AI Frog 切换到攻击状态 - {aiAttackSprite.name} (切换次数: {spriteChangeCount})");
                Debug.Log($"🔄 从 {previousSprite?.name ?? "null"} 切换到 {aiAttackSprite.name}");
                Debug.Log($"🔍 SpriteRenderer.sprite 现在是: {spriteRenderer.sprite?.name ?? "null"}");
            }
        }
        else if (aiAttackSprite == null)
        {
            Debug.LogWarning("⚠️ AI Attack Sprite 为空，无法切换");
        }
    }

    /// <summary>
    /// 强制测试方法：手动切换到攻击状态（右键菜单）
    /// </summary>
    [ContextMenu("强制测试切换到Attack")]
    public void ForceTestAttack()
    {
        Debug.Log("🧪 强制测试：切换到Attack");
        
        // 直接尝试加载并切换sprite
        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        Sprite testAttackSprite = null;
        
        foreach (Sprite sprite in allSprites)
        {
            if (sprite.name.Contains("AIAttack"))
            {
                testAttackSprite = sprite;
                break;
            }
        }
        
        if (testAttackSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = testAttackSprite;
            isInAttackMode = true;
            Debug.Log($"🧪 强制切换成功: {testAttackSprite.name}");
        }
        else
        {
            Debug.LogError($"🧪 强制切换失败: sprite={testAttackSprite?.name}, renderer={spriteRenderer}");
        }
    }

    /// <summary>
    /// 测试方法：显示所有加载的sprites
    /// </summary>
    [ContextMenu("显示所有sprites")]
    public void ShowAllSprites()
    {
        Debug.Log("🔍 显示所有找到的sprites:");
        Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (Sprite sprite in allSprites)
        {
            if (sprite.name.Contains("AI"))
            {
                Debug.Log($"🔍 AI相关sprite: {sprite.name}, 纹理: {sprite.texture?.name}");
            }
        }
    }

    /// <summary>
    /// 测试方法：手动切换到攻击状态（右键菜单）
    /// </summary>
    [ContextMenu("测试切换到Attack")]
    public void TestSwitchToAttack()
    {
        Debug.Log("🧪 手动测试：切换到Attack");
        SetAttackSprite();
    }

    /// <summary>
    /// 测试方法：手动切换到待机状态（右键菜单）
    /// </summary>
    [ContextMenu("测试切换到Idle")]
    public void TestSwitchToIdle()
    {
        Debug.Log("🧪 手动测试：切换到Idle");
        SetIdleSprite();
    }

    /// <summary>
    /// 重置AI Frog到初始状态
    /// </summary>
    public void ResetToIdle()
    {
        StopMonitoring();
        SetIdleSprite();
        Debug.Log("🔄 AI Frog 重置为待机状态");
    }

    /// <summary>
    /// 获取当前是否处于攻击模式
    /// </summary>
    public bool IsInAttackMode()
    {
        return isInAttackMode;
    }

    /// <summary>
    /// 由AIController触发：执行最终攻击并结束游戏
    /// </summary>
    public void TriggerAttackAndEndGame()
    {
        Debug.Log("🚨🚨🚨 AI FROG 执行最终攻击！！！ 🚨🚨🚨");

        // 确保切换到攻击sprite
        SetAttackSprite();

        // 停止监控
        StopMonitoring();

        // 可以在这里添加攻击动画效果
        StartCoroutine(ExecuteAttackAnimation());
    }

    /// <summary>
    /// 执行攻击动画并结束游戏
    /// </summary>
    IEnumerator ExecuteAttackAnimation()
    {
        // 可以在这里添加一些攻击效果
        Debug.Log("⚔️ AI Frog 执行攻击动画...");

        // 等待一小段时间让玩家看到攻击动作
        yield return new WaitForSeconds(0.5f);

        // 结束游戏
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("🏁 AI Frog攻击完成，游戏结束！");
            gameManager.OnFlyEscape(); // 苍蝇被AI抓住，玩家失败
        }
        else
        {
            Debug.LogError("❌ 无法找到GameManager!");
        }
    }

    void OnDestroy()
    {
        StopMonitoring();
    }
}