using UnityEngine;

public class BugSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject flyPrefab;
    public GameObject beePrefab;
    public float spawnRadius = 3f;
    public float flySpawnChance = 0.7f;
    
    [Header("References")]
    public Transform spawnCenter;
    
    private Bug currentBug;
    
    void Start()
    {
        if (spawnCenter == null)
            spawnCenter = transform;
    }
    
    public void SpawnBug()
    {
        if (currentBug != null)
        {
            Destroy(currentBug.gameObject);
        }
        
        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject bugPrefab = Random.value < flySpawnChance ? flyPrefab : beePrefab;
        BugType bugType = bugPrefab == flyPrefab ? BugType.Fly : BugType.Bee;
        
        if (bugPrefab != null)
        {
            GameObject bugObject = Instantiate(bugPrefab, spawnPosition, Quaternion.identity);
            currentBug = bugObject.GetComponent<Bug>();
            
            if (currentBug == null)
            {
                currentBug = bugObject.AddComponent<Bug>();
            }
            
            currentBug.bugType = bugType;
            
            Debug.Log($"Spawned {bugType} at {spawnPosition}");
        }
        else
        {
            CreateSimpleBug(spawnPosition, bugType);
        }
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
        
        Debug.Log($"Created simple {bugType} at {position}");
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