using UnityEngine;

public class PlayerFrog : MonoBehaviour
{
    [Header("Frog Visuals")]
    public SpriteRenderer frogRenderer;
    public Color normalColor = Color.green;
    public Color shootingColor = Color.red;
    public float animationDuration = 0.5f;
    
    private Animator animator;
    
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
            frogRenderer.color = normalColor;
        }
    }
    
    // 射击动画
    public void PlayShootAnimation()
    {
        Debug.Log("Player frog is shooting!");
        
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
        Debug.Log("Player frog celebrates victory!");
        
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
        Debug.Log("Player frog shows defeat!");
        
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
            frogRenderer.color = normalColor;
    }
    
    System.Collections.IEnumerator ColorFlash()
    {
        frogRenderer.color = shootingColor;
        yield return new WaitForSeconds(animationDuration);
        frogRenderer.color = normalColor;
    }
}