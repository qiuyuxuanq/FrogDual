using UnityEngine;

public enum BugType
{
    Fly,
    Bee
}

public class Bug : MonoBehaviour
{
    [Header("Bug Properties")]
    public BugType bugType = BugType.Fly;
    public float moveSpeed = 2f;
    
    private Vector3 direction;
    
    void Start()
    {
        direction = Random.insideUnitCircle.normalized;
    }
    
    void Update()
    {
        // 检查是否有FlyMovement组件在控制移动
        FlyMovement flyMovement = GetComponent<FlyMovement>();
        
        if (flyMovement == null)
        {
            // 没有FlyMovement组件时，只使用简单的移动逻辑（蜜蜂等静态虫子）
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        // 如果有FlyMovement组件，完全由FlyMovement控制移动和生命周期
        // 不再有任何自动销毁逻辑！
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ScreenBounds"))
        {
            direction = Vector3.Reflect(direction, Vector3.up);
        }
    }
}