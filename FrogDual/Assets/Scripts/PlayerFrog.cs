using UnityEngine;

public class PlayerFrog : MonoBehaviour
{
    [Header("Frog Visuals")]
    public SpriteRenderer frogRenderer;
    public Color normalColor = Color.green;
    public Color shootingColor = Color.red;
    public float animationDuration = 0.5f;

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
            // 简单的颜色变化动画
            StartCoroutine(ColorFlash());
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
            frogRenderer.color = Color.gray; // 简单表示失败
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
}
