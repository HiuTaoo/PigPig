using UnityEngine;

namespace _Script.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBaseView : MonoBehaviour
    {
        [field: SerializeField] public UIID ViewID { get; private set; }
        
        protected CanvasGroup canvasGroup;
        public bool IsOpen { get; private set; }

        public virtual void OnInit()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void OnOpen(object args = null)
        {
            IsOpen = true;
            gameObject.SetActive(true);
            SetInteractive(true);
        }

        public virtual void OnClose()
        {
            IsOpen = false;
            SetInteractive(false);
            gameObject.SetActive(false);
        }

        protected void SetInteractive(bool state)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = state ? 1f : 0f;
            canvasGroup.interactable = state;
            canvasGroup.blocksRaycasts = state;
        }
    }
}