using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DebugButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 5f;
    
    [Header("Events")]
    public UnityEvent OnHoldComplete;
    
    private bool isHolding = false;
    private Coroutine holdCoroutine;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isHolding)
        {
            StartHold();
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHolding)
        {
            CancelHold();
        }
    }
    
    private void StartHold()
    {
        isHolding = true;
        
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
        }
        
        holdCoroutine = StartCoroutine(HoldTimer());
    }
    
    private void CancelHold()
    {
        isHolding = false;
        
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }
    
    private IEnumerator HoldTimer()
    {
        float timer = 0f;
        
        while (isHolding && timer < holdDuration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // If we completed the full hold duration
        if (isHolding && timer >= holdDuration)
        {
            isHolding = false;
            holdCoroutine = null;
            OnHoldComplete?.Invoke();
        }
    }
    private void OnDisable()
    {
        if (isHolding)
        {
            CancelHold();
        }
    }
}