using UnityEngine;

public class GuaranteedFlyMovement : MonoBehaviour
{
    public Vector3 targetPosition;
    public float guaranteedStayTime = 3f;
    public float speed = 2f;

    private bool hasReachedTarget = false;
    private float stayTimer = 0f;

    public void Initialize(Vector3 target, float stayTime)
    {
        targetPosition = target;
        guaranteedStayTime = stayTime;
    }

    void Update()
    {
        if (!hasReachedTarget)
        {
            // 飞向目标区域
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // 检查是否到达目标
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance < 1f)
            {
                hasReachedTarget = true;
                Debug.Log($"🎯 保证苍蝇到达目标区域，将停留{guaranteedStayTime}秒");
            }
        }
        else
        {
            // 在目标区域内缓慢移动
            stayTimer += Time.deltaTime;

            // 小幅度随机移动
            Vector3 randomOffset = Random.insideUnitSphere * 0.5f * Time.deltaTime;
            randomOffset.z = 0f;
            transform.position += randomOffset;

            // 确保不离开目标区域太远
            float distanceFromTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceFromTarget > 2f)
            {
                Vector3 backToTarget = (targetPosition - transform.position).normalized * Time.deltaTime;
                transform.position += backToTarget;
            }

            // 停留时间结束后离开
            if (stayTimer >= guaranteedStayTime)
            {
                // 飞离屏幕
                Vector3 exitDirection = Random.insideUnitCircle.normalized;
                transform.position += exitDirection * speed * 2f * Time.deltaTime;

                // 如果离屏幕太远就销毁
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
                    if (screenPoint.x < -0.5f || screenPoint.x > 1.5f ||
                        screenPoint.y < -0.5f || screenPoint.y > 1.5f)
                    {
                        Debug.Log("🗑️ 保证苍蝇完成任务，自动销毁");
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
