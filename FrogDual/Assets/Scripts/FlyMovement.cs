using UnityEngine;

public class FlyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    public float randomOffset = 0.5f;
    public float pathUpdateInterval = 0.2f;

    [Header("References")]
    public TargetZone targetZone;

    private Vector3 startPosition;
    private bool isFlying = false;
    private bool hasPassedTargetZone = false;
    private float pathUpdateTimer = 0f;
    private Vector3 currentDirection;

    public void Initialize(Vector3 target, float flySpeed)
    {
        speed = flySpeed;
        startPosition = transform.position;
        isFlying = true;

        if (targetZone == null)
            targetZone = FindObjectOfType<TargetZone>();

        UpdateFlightDirection();

        Debug.Log($"🐛 苍蝇开始智能追踪飞行：从 {startPosition}");
    }

    void UpdateFlightDirection()
    {
        if (targetZone == null) return;

        Vector3 currentTargetPos = targetZone.transform.position;
        Vector3 directionToTarget = (currentTargetPos - transform.position).normalized;

        if (!hasPassedTargetZone)
        {
            currentDirection = directionToTarget;
        }
        else
        {
            currentDirection = CalculateExitDirection(currentTargetPos);
        }

        Vector2 randomOffset = Random.insideUnitCircle * this.randomOffset * 0.1f;
        currentDirection += new Vector3(randomOffset.x, randomOffset.y, 0f);
        currentDirection = currentDirection.normalized;
    }

    Vector3 CalculateExitDirection(Vector3 targetPos)
    {
        Vector3 exitDirection = currentDirection;

        if (Mathf.Abs(exitDirection.x) > Mathf.Abs(exitDirection.y))
        {
            exitDirection = exitDirection.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            exitDirection = exitDirection.y > 0 ? Vector3.up : Vector3.down;
        }

        return exitDirection;
    }

    void Update()
    {
        if (!isFlying) return;

        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval && !hasPassedTargetZone)
        {
            UpdateFlightDirection();
            pathUpdateTimer = 0f;
        }

        transform.position += currentDirection * speed * Time.deltaTime;

        CheckTargetZonePassage();

        float bobAmount = Mathf.Sin(Time.time * 5f) * 0.1f;
        transform.position += Vector3.up * bobAmount * Time.deltaTime;

        CheckScreenBounds();
    }

    void CheckTargetZonePassage()
    {
        if (hasPassedTargetZone || targetZone == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetZone.transform.position);

        if (distanceToTarget < 15f)
        {
            hasPassedTargetZone = true;
            OnPassTargetZone();
            UpdateFlightDirection();
        }
    }

    void OnPassTargetZone()
    {
        Debug.Log("🎯 苍蝇成功穿过目标区域！");
    }

    void CheckScreenBounds()
    {
        Vector3 pos = transform.position;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;

        float rightBound = cameraPos.x + cameraWidth - 1f;
        float leftBound = cameraPos.x - cameraWidth + 1f;
        float topBound = cameraPos.y + cameraHeight - 1f;
        float bottomBound = cameraPos.y - cameraHeight + 1f;

        if (pos.x > rightBound || pos.x < leftBound || pos.y > topBound || pos.y < bottomBound)
        {
            OnReachScreenEdge();
        }
    }

    void OnReachScreenEdge()
    {
        Debug.Log("🔄 虫子碰到摄像机边缘，反弹回屏幕内");

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;

        float rightBound = cameraPos.x + cameraWidth - 2f;
        float leftBound = cameraPos.x - cameraWidth + 2f;
        float topBound = cameraPos.y + cameraHeight - 2f;
        float bottomBound = cameraPos.y - cameraHeight + 2f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, leftBound, rightBound);
        pos.y = Mathf.Clamp(pos.y, bottomBound, topBound);
        transform.position = pos;

        Vector3 screenCenter = cameraPos;
        Vector3 directionToCenter = (screenCenter - transform.position).normalized;

        Vector2 randomOffsetDir = Random.insideUnitCircle * 0.3f;
        directionToCenter += new Vector3(randomOffsetDir.x, randomOffsetDir.y, 0f);

        currentDirection = directionToCenter.normalized;
        hasPassedTargetZone = false;

        // 关键：保持 isFlying = true，永远不设置为 false
    }

    public bool IsInTargetZone(TargetZone targetZone)
    {
        if (targetZone == null) return false;
        return targetZone.IsPositionInZone(transform.position);
    }
}
