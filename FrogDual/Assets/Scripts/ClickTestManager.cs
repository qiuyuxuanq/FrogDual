using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClickTestManager : MonoBehaviour
{
    [Header("测试设置")]
    public bool enableTestMode = true;
    public TargetZone targetZone;
    public Camera gameCamera;
    
    [Header("UI显示")]
    public Canvas uiCanvas;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI instructionText;
    
    void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        SetupUI();
        
        if (enableTestMode)
        {
            Debug.Log("🧪 点击测试模式已启用！点击红色圆圈进行测试");
        }
    }
    
    void SetupUI()
    {
        if (uiCanvas == null)
        {
            // 创建UI Canvas
            GameObject canvasObj = new GameObject("TestUI");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 100;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // 创建反馈文字
        if (feedbackText == null)
        {
            CreateFeedbackText();
        }
        
        // 创建说明文字
        if (instructionText == null)
        {
            CreateInstructionText();
        }
    }
    
    void CreateFeedbackText()
    {
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.SetParent(uiCanvas.transform, false);
        
        feedbackText = textObj.AddComponent<TextMeshProUGUI>();
        feedbackText.text = "点击红色圆圈进行测试";
        feedbackText.fontSize = 36;
        feedbackText.color = Color.white;
        feedbackText.alignment = TextAlignmentOptions.Center;
        
        RectTransform rect = feedbackText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 0.8f);
        rect.sizeDelta = new Vector2(400, 100);
        rect.anchoredPosition = Vector2.zero;
    }
    
    void CreateInstructionText()
    {
        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(uiCanvas.transform, false);
        
        instructionText = textObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "🎯 测试模式：点击测试TargetZone检测";
        instructionText.fontSize = 24;
        instructionText.color = Color.yellow;
        instructionText.alignment = TextAlignmentOptions.Center;
        
        RectTransform rect = instructionText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.9f);
        rect.anchorMax = new Vector2(0.5f, 0.9f);
        rect.sizeDelta = new Vector2(500, 50);
        rect.anchoredPosition = Vector2.zero;
    }
    
    void Update()
    {
        if (enableTestMode && Input.GetMouseButtonDown(0))
        {
            TestClick();
        }
    }
    
    void TestClick()
    {
        Vector3 mouseWorldPos = gameCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        
        bool inTargetZone = targetZone.IsPositionInZone(mouseWorldPos);
        
        if (inTargetZone)
        {
            ShowFeedback("🎯 YES! 点击在目标区域内!", Color.green);
        }
        else
        {
            ShowFeedback("❌ FAIL! 点击在目标区域外!", Color.red);
        }
        
        Debug.Log($"🧪 测试点击: {mouseWorldPos} - 结果: {(inTargetZone ? "成功" : "失败")}");
    }
    
    void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            
            // 3秒后恢复默认文字
            Invoke(nameof(ResetFeedback), 3f);
        }
    }
    
    void ResetFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "点击红色圆圈进行测试";
            feedbackText.color = Color.white;
        }
    }
}