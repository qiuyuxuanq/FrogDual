using UnityEngine;

public class PlayerFrog : MonoBehaviour
{
    [Header("Frog Visuals")]
    public SpriteRenderer frogRenderer;
    public Color normalColor = Color.green;
    public Color shootingColor = Color.red;
    public float animationDuration = 0.5f;

    [Header("Sprite Animation")]
    public Sprite playerIdleSprite;   // 玩家待机图片
    public Sprite playerAttackSprite; // 玩家攻击图片

    private bool isAttacking = false;

    [Header("🎯 超精确反馈")]
    public bool showPrecisionFeedback = false;  // 显示精确度反馈
    public Color ultraPreciseColor = Color.cyan; // 超精确模式颜色

    private Animator animator;
    private bool isUltraPreciseMode = false;

    void Start()
    {
        if (frogRenderer == null)
            frogRenderer = GetComponent<SpriteRenderer>();

        if (frogRenderer == null)
        {
            // 如果没有SpriteRenderer，创建一个简单的圆形表示青蛙
            CreateSimpleFrog();
        }

        // 设置初始状态为idle图片
        SetIdleSprite();

        animator = GetComponent<Animator>();
    }

    void CreateSimpleFrog()
    {
        frogRenderer = gameObject.AddComponent<SpriteRenderer>();
        frogRenderer.color = normalColor;

        // 创建一个简单的圆形精灵（临时用）
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(primitive.GetComponent<SphereCollider>());
        frogRenderer.sprite = primitive.GetComponent<SpriteRenderer>().sprite;
        Destroy(primitive);

        transform.localScale = Vector3.one * 0.5f;
    }

    void Update()
    {
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            SetAttackSprite();
        }

        // 检测鼠标左键松开
        if (Input.GetMouseButtonUp(0))
        {
            SetIdleSprite();
        }

        // 确保青蛙颜色正确显示
        if (frogRenderer != null && frogRenderer.color == Color.white)
        {
            frogRenderer.color = GetCurrentNormalColor();
        }
    }

    /// <summary>
    /// 获取当前正常颜色（考虑超精确模式）
    /// </summary>
    Color GetCurrentNormalColor()
    {
        if (showPrecisionFeedback && isUltraPreciseMode)
        {
            return ultraPreciseColor;
        }
        return normalColor;
    }

    /// <summary>
    /// 设置超精确模式视觉反馈
    /// </summary>
    public void SetUltraPreciseMode(bool enabled)
    {
        isUltraPreciseMode = enabled;

        if (showPrecisionFeedback && frogRenderer != null)
        {
            frogRenderer.color = GetCurrentNormalColor();
        }

        Debug.Log($"🐸 青蛙超精确模式: {(enabled ? "启用" : "禁用")}");
    }

    // 射击动画
    public void PlayShootAnimation()
    {
        Debug.Log("🐸 玩家青蛙射击!");

        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
        else
        {
            // 使用sprite切换动画
            SetAttackSprite();
            StartCoroutine(ReturnToIdleAfterDelay());
        }
    }

    // 胜利动画（抽雪茄）
    public void PlayVictoryAnimation()
    {
        Debug.Log("🎉 玩家青蛙庆祝胜利!");

        if (animator != null)
        {
            animator.SetTrigger("Victory");
        }
        else
        {
            frogRenderer.color = Color.yellow; // 简单表示胜利
        }
    }

    // 失败动画（翻白眼）
    public void PlayDefeatAnimation()
    {
        Debug.Log("😵 玩家青蛙表示失败!");

        if (animator != null)
        {
            animator.SetTrigger("Defeat");
        }
        else
        {
            // 不改变颜色，只记录失败状态
            Debug.Log("🐸 玩家青蛙失败 - 保持正常外观");
        }
    }

    // 重置状态
    public void ResetToNormal()
    {
        if (frogRenderer != null)
            frogRenderer.color = GetCurrentNormalColor();
    }

    System.Collections.IEnumerator ColorFlash()
    {
        frogRenderer.color = shootingColor;
        yield return new WaitForSeconds(animationDuration);
        frogRenderer.color = GetCurrentNormalColor();
    }

    /// <summary>
    /// 设置为待机图片
    /// </summary>
    public void SetIdleSprite()
    {
        if (frogRenderer != null && playerIdleSprite != null)
        {
            frogRenderer.sprite = playerIdleSprite;
            frogRenderer.color = GetCurrentNormalColor(); // 确保颜色正确
            isAttacking = false;
            Debug.Log("🐸 切换到待机状态");
        }
    }

    /// <summary>
    /// 设置为攻击图片
    /// </summary>
    public void SetAttackSprite()
    {
        if (frogRenderer != null && playerAttackSprite != null)
        {
            frogRenderer.sprite = playerAttackSprite;
            frogRenderer.color = GetCurrentNormalColor(); // 确保颜色正确
            isAttacking = true;
            Debug.Log("🐸 切换到攻击状态");
        }
    }

    /// <summary>
    /// 延迟后返回到待机状态
    /// </summary>
    System.Collections.IEnumerator ReturnToIdleAfterDelay()
    {
        yield return new WaitForSeconds(animationDuration);
        SetIdleSprite();
    }
}
