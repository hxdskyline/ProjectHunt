using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectHunt.Build
{
    public sealed class BuildDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public BuildSelectionController selectionController;

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _parentCanvas = GetComponentInParent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || selectionController == null)
            {
                return;
            }

            selectionController.CacheDragStart(_rectTransform);

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _parentCanvas == null)
            {
                return;
            }

            _rectTransform.anchoredPosition += eventData.delta / _parentCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
            }

            if (_rectTransform != null && selectionController != null)
            {
                selectionController.RestoreDragStart(_rectTransform);
            }
        }
    }
}
