using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    public float fixedReactionTime = 2f; // 固定2秒反应时间

    [Header("🎬 精灵动画设置")]
    public Sprite[] idleSprites;        // 待机状态精灵序列
    public Sprite[] attackSprites;      // 攻击状态精灵序列
    public float animationSpeed = 0.2f; // 动画帧率（秒/帧）
    public bool loopIdleAnimation = true;  // 是否循环待机动画
    public bool loopAttackAnimation = false; // 是否循环攻击动画

    [Header("References")]
    public GameManager gameManager;
    public TargetZone targetZone;
    public SpriteRenderer spriteRenderer; // AI青蛙的精灵渲染器

    // 动画状态
    public enum AIAnimationState { Idle, Preparing, Attacking, Completed }
    private AIAnimationState currentAnimationState = AIAnimationState.Idle;

    // 原有变量
    private bool hasReacted = false;
    private bool isMonitoring = false;
    private bool hasStartedReaction = false;
    private Coroutine reactionCoroutine;
    private Coroutine animationCoroutine;
    private GameObject currentFly;

    // 动画相关变量
    private int currentSpriteIndex = 0;
    private bool isAnimating = false;

    #region 🎯 初始化系统

    void Start()
    {
        InitializeComponents();
        LoadSprites();
        SetIdleAnimation();
    }

    void InitializeComponents()
    {
        // 自动查找组件
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (targetZone == null) targetZone = FindObjectOfType<TargetZone>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // 如果没有SpriteRenderer，尝试在子对象中查找
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // 如果还是没有，尝试查找AI Frog GameObject
        if (spriteRenderer == null)
        {
            GameObject aiFrog = GameObject.Find("AI Frog");
            if (aiFrog != null)
            {
                spriteRenderer = aiFrog.GetComponent<SpriteRenderer>();
            }
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("⚠️ AIController: 未找到SpriteRenderer组件！精灵动画将无法工作。");
        }
    }

    void LoadSprites()
    {
        // 如果数组为空，尝试从Resources加载
        if (idleSprites == null || idleSprites.Length == 0)
        {
            idleSprites = LoadSpriteArray("AIIdle");
        }

        if (attackSprites == null || attackSprites.Length == 0)
        {
            attackSprites = LoadSpriteArray("AIAttack");
        }

        Debug.Log($"🎬 加载精灵: 待机={idleSprites?.Length ?? 0}帧, 攻击={attackSprites?.Length ?? 0}帧");
    }

    Sprite[] LoadSpriteArray(string baseName)
    {
        // 尝试加载多个精灵帧
        var spriteList = new System.Collections.Generic.List<Sprite>();

        // 尝试加载单个精灵
        Sprite singleSprite = Resources.Load<Sprite>(baseName);
        if (singleSprite != null)
        {
            spriteList.Add(singleSprite);
        }

        // 尝试加载编号精灵 (AIIdle1, AIIdle2, etc.)
        for (int i = 1; i <= 10; i++)
        {
            Sprite sprite = Resources.Load<Sprite>($"{baseName}{i}");
            if (sprite != null)
            {
                spriteList.Add(sprite);
            }
            else
            {
                break; // 没有更多帧了
            }
        }

        // 如果没有找到编号的，尝试查找项目中的精灵
        if (spriteList.Count == 0)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite sprite in allSprites)
            {
                if (sprite.name.Contains(baseName))
                {
                    spriteList.Add(sprite);
                }
            }
        }

        return spriteList.ToArray();
    }

    #endregion

    #region 🎬 动画系统

    public void SetIdleAnimation()
    {
        currentAnimationState = AIAnimationState.Idle;
        StartSpriteAnimation(idleSprites, loopIdleAnimation);

        Debug.Log("🐸 AI切换到待机动画");
    }

    public void SetPreparingAnimation()
    {
        currentAnimationState = AIAnimationState.Preparing;

        // 如果有准备动画，播放它；否则暂停在待机动画的第一帧
        if (idleSprites != null && idleSprites.Length > 0)
        {
            StopSpriteAnimation();
            SetSprite(idleSprites[0]);
        }

        Debug.Log("🎯 AI切换到准备动画");
    }

    public void SetAttackAnimation()
    {
        currentAnimationState = AIAnimationState.Attacking;
        StartSpriteAnimation(attackSprites, loopAttackAnimation);

        Debug.Log("⚡ AI切换到攻击动画");
    }

    public void SetCompletedAnimation()
    {
        currentAnimationState = AIAnimationState.Completed;

        // 停留在攻击动画的最后一帧
        if (attackSprites != null && attackSprites.Length > 0)
        {
            StopSpriteAnimation();
            SetSprite(attackSprites[attackSprites.Length - 1]);
        }

        Debug.Log("✅ AI攻击完成");
    }

    void StartSpriteAnimation(Sprite[] sprites, bool loop)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("⚠️ 无法播放动画：精灵数组为空");
            return;
        }

        StopSpriteAnimation();
        animationCoroutine = StartCoroutine(AnimateSprites(sprites, loop));
    }

    void StopSpriteAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        isAnimating = false;
    }

    IEnumerator AnimateSprites(Sprite[] sprites, bool loop)
    {
        if (sprites == null || sprites.Length == 0) yield break;

        isAnimating = true;
        currentSpriteIndex = 0;

        do
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (!isAnimating) yield break;

                currentSpriteIndex = i;
                SetSprite(sprites[i]);
                yield return new WaitForSeconds(animationSpeed);
            }
        } while (loop && isAnimating);

        isAnimating = false;
    }

    void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    #endregion

    #region 🎯 原有AI逻辑（增强版）

    public void StartReaction()
    {
        hasReacted = false;
        hasStartedReaction = false;
        isMonitoring = true;

        Debug.Log("🤖 AI开始监控苍蝇位置...");
        SetIdleAnimation(); // 开始监控时切换到待机动画

        // 开始监控苍蝇位置
        StartCoroutine(MonitorFlyPosition());
    }

    IEnumerator MonitorFlyPosition()
    {
        while (isMonitoring && gameManager.currentState == GameState.Playing)
        {
            // 查找当前场景中的苍蝇（只找苍蝇，不找蜜蜂）
            FindCurrentFly();

            // ✅ 关键修改：只有苍蝇在TargetZone内，才开始反应计时
            if (currentFly != null && !hasStartedReaction && IsFlyInTargetZone())
            {
                // ✅ 确认这是苍蝇而不是蜜蜂
                Bug bugComponent = currentFly.GetComponent<Bug>();
                if (bugComponent != null && bugComponent.bugType == BugType.Fly)
                {
                    hasStartedReaction = true;
                    Debug.Log($"🎯 苍蝇进入目标区域! AI开始{fixedReactionTime}秒反应计时...");

                    // 🎬 动画：切换到准备状态
                    SetPreparingAnimation();

                    reactionCoroutine = StartCoroutine(ReactAfterDelay(fixedReactionTime));
                    break; // 开始计时后停止监控
                }
                else if (bugComponent != null && bugComponent.bugType == BugType.Bee)
                {
                    Debug.Log("🐝 蜜蜂进入目标区域，但AI不会因此反应");
                    // 蜜蜂进入不触发AI反应，继续监控
                }
            }

            yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次
        }
    }

    void FindCurrentFly()
    {
        // ✅ 修改：查找所有Bug组件，优先找苍蝇
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

        // 如果没有苍蝇，也可以用FlyMovement组件作为备选（向后兼容）
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
            Debug.Log("🚨 AI反应了! 苍蝇在目标区域停留超过时间限制!");

            // 🎬 动画：切换到攻击动画
            SetAttackAnimation();

            // 等待攻击动画播放一段时间
            yield return new WaitForSeconds(0.5f);

            // ✅ 修改：AI只标记反应，不直接结束游戏
            // AI Frog需要执行攻击动作后才结束游戏
            Debug.Log("🤖 AI Controller标记反应完成，等待AI Frog执行攻击...");

            // 通知AI Frog可以攻击了
            AIFrog aiFrog = FindObjectOfType<AIFrog>();
            if (aiFrog != null)
            {
                aiFrog.TriggerAttackAndEndGame();
            }
            else
            {
                // 如果没有AI Frog，直接结束游戏（兼容性）
                gameManager.OnFlyEscape();
            }

            // 🎬 动画：切换到完成状态
            SetCompletedAnimation();
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

        // 停止动画并回到待机状态
        SetIdleAnimation();

        Debug.Log("🛑 AI停止监控");
    }

    #endregion

    #region 🔧 公共接口

    /// <summary>
    /// 手动设置精灵动画帧
    /// </summary>
    public void SetAnimationFrames(Sprite[] idle, Sprite[] attack)
    {
        idleSprites = idle;
        attackSprites = attack;
        Debug.Log($"🎬 手动设置动画帧: 待机={idle?.Length ?? 0}帧, 攻击={attack?.Length ?? 0}帧");
    }

    /// <summary>
    /// 获取当前动画状态
    /// </summary>
    public AIAnimationState GetAnimationState()
    {
        return currentAnimationState;
    }

    /// <summary>
    /// 设置动画速度
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
        Debug.Log($"🎬 动画速度设置为: {speed}秒/帧");
    }

    /// <summary>
    /// 强制播放指定动画
    /// </summary>
    public void ForcePlayAnimation(AIAnimationState state)
    {
        switch (state)
        {
            case AIAnimationState.Idle:
                SetIdleAnimation();
                break;
            case AIAnimationState.Preparing:
                SetPreparingAnimation();
                break;
            case AIAnimationState.Attacking:
                SetAttackAnimation();
                break;
            case AIAnimationState.Completed:
                SetCompletedAnimation();
                break;
        }
    }

    #endregion

    #region 🎨 Editor测试方法

    [ContextMenu("测试待机动画")]
    void TestIdleAnimation()
    {
        SetIdleAnimation();
    }

    [ContextMenu("测试攻击动画")]
    void TestAttackAnimation()
    {
        SetAttackAnimation();
    }

    [ContextMenu("重新加载精灵")]
    void ReloadSprites()
    {
        LoadSprites();
        SetIdleAnimation();
    }

    #endregion

    #region 🔍 调试显示

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 显示当前状态
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"AI状态: {currentAnimationState}\n" +
                $"动画帧: {currentSpriteIndex + 1}/{GetCurrentSpriteArrayLength()}\n" +
                $"监控中: {isMonitoring}\n" +
                $"已反应: {hasReacted}");
        }
    }

    int GetCurrentSpriteArrayLength()
    {
        switch (currentAnimationState)
        {
            case AIAnimationState.Idle:
                return idleSprites?.Length ?? 0;
            case AIAnimationState.Attacking:
                return attackSprites?.Length ?? 0;
            default:
                return 1;
        }
    }

    #endregion
}
