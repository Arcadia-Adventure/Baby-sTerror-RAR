using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PeopleFun.UI
{
    /// <summary>
    /// Fires <see cref="OnHold"/> once the pointer has been held on this element
    /// for <see cref="timeToTrigger"/> seconds. Used by the DevSettings dialog to
    /// copy a row's text to the clipboard on hold.
    /// </summary>
    public class UIHoldEventTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float timeToTrigger = 1f;
        public UnityEvent OnHold;

        private Coroutine holdRoutine;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (holdRoutine != null) StopCoroutine(holdRoutine);
            holdRoutine = StartCoroutine(HoldTimer());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelHold();
        }

        private void OnDisable()
        {
            CancelHold();
        }

        private void CancelHold()
        {
            if (holdRoutine != null)
            {
                StopCoroutine(holdRoutine);
                holdRoutine = null;
            }
        }

        private IEnumerator HoldTimer()
        {
            yield return new WaitForSeconds(timeToTrigger);
            holdRoutine = null;
            OnHold?.Invoke();
        }
    }
}
