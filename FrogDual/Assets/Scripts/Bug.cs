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
    public float lifetime = 10f;
    
    private Vector3 direction;
    private float timer;
    
    void Start()
    {
        direction = Random.insideUnitCircle.normalized;
        timer = lifetime;
        
        Debug.Log($"{bugType} spawned at {transform.position}");
    }
    
    void Update()
    {
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Debug.Log($"{bugType} disappeared after {lifetime} seconds");
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ScreenBounds"))
        {
            direction = Vector3.Reflect(direction, Vector3.up);
        }
    }
}