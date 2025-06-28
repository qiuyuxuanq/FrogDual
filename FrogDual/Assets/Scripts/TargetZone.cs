using UnityEngine;

public class TargetZone : MonoBehaviour
{
    [Header("Target Zone Settings")]
    public float radius = 2f;
    public Color zoneColor = Color.red;
    public bool showGizmo = true;
    
    void Start()
    {
        SetupCollider();
    }
    
    void SetupCollider()
    {
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        collider.radius = radius;
        collider.isTrigger = true;
        
        Debug.Log($"Target zone setup with radius: {radius}");
    }
    
    public bool IsPositionInZone(Vector3 worldPosition)
    {
        float distance = Vector3.Distance(transform.position, worldPosition);
        bool inZone = distance <= radius;
        
        Debug.Log($"Position {worldPosition} is {(inZone ? "IN" : "OUT")} target zone (distance: {distance:F2})");
        
        return inZone;
    }
    
    public bool IsBugInZone(Bug bug)
    {
        if (bug == null) return false;
        return IsPositionInZone(bug.transform.position);
    }
    
    void OnDrawGizmos()
    {
        if (showGizmo)
        {
            Gizmos.color = zoneColor;
            DrawWireCircle(transform.position, radius);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.3f);
        DrawWireCircle(transform.position, radius);
        Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, radius);
    }
    
    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}