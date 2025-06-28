using UnityEngine;
using System.Collections;

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    public float minReactionTime = 0.3f;
    public float maxReactionTime = 1.5f;
    
    [Header("References")]
    public GameManager gameManager;
    
    private bool hasReacted = false;
    private Coroutine reactionCoroutine;
    
    public void StartReaction()
    {
        hasReacted = false;
        float reactionTime = Random.Range(minReactionTime, maxReactionTime);
        
        Debug.Log($"AI will react in {reactionTime:F2} seconds");
        
        reactionCoroutine = StartCoroutine(ReactAfterDelay(reactionTime));
    }
    
    IEnumerator ReactAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (gameManager.currentState == GameState.Playing)
        {
            hasReacted = true;
            Debug.Log("AI reacted!");
            gameManager.OnAIReact();
        }
    }
    
    public bool HasReacted()
    {
        return hasReacted;
    }
    
    public void StopReaction()
    {
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
        }
    }
}