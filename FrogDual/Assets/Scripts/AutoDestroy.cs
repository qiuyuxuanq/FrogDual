using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Header("Auto Destroy Settings")]
    public float destroyAfterTime = 10f;
    public bool startTimerOnStart = true;
    public bool destroyWhenOffScreen = true;

    [Header("Off Screen Detection")]
    public float screenBoundary = 15f; // 距离屏幕边缘多远算作离开屏幕

    private float timer = 0f;
    private bool timerStarted = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (startTimerOnStart)
        {
            StartTimer();
        }
    }

    void Update()
    {
        // 计时器销毁
        if (timerStarted)
        {
            timer += Time.deltaTime;
            if (timer >= destroyAfterTime)
            {
                DestroyObject("Timer expired");
            }
        }

        // 屏幕外销毁
        if (destroyWhenOffScreen && IsOffScreen())
        {
            DestroyObject("Left screen bounds");
        }
    }

    public void StartTimer()
    {
        timerStarted = true;
        timer = 0f;
    }

    public void StopTimer()
    {
        timerStarted = false;
    }

    bool IsOffScreen()
    {
        if (mainCamera == null)
            return false;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);

        return screenPos.x < -screenBoundary ||
               screenPos.x > Screen.width + screenBoundary ||
               screenPos.y < -screenBoundary ||
               screenPos.y > Screen.height + screenBoundary;
    }

    void DestroyObject(string reason)
    {
        Debug.Log($"🗑️ AutoDestroy: {gameObject.name} destroyed ({reason})");
        Destroy(gameObject);
    }
}