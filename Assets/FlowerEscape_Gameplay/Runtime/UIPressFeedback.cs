using UnityEngine;
using UnityEngine.EventSystems;

namespace FlowerEscape
{
    /// <summary>
    /// Hiệu ứng nhấn nút: thu nhỏ khi giữ, bật lại khi thả. Dùng chung cho mọi nút trong HUD.
    /// </summary>
    public class UIPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(0.7f, 1f)] private float pressedScale = 0.92f;
        [SerializeField, Min(1f)] private float speed = 18f;

        private Vector3 baseScale = Vector3.one;
        private float current = 1f;
        private float target = 1f;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData) => target = pressedScale;
        public void OnPointerUp(PointerEventData eventData) => target = 1f;
        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        private void OnDisable()
        {
            current = target = 1f;
            transform.localScale = baseScale;
        }

        private void Update()
        {
            if (Mathf.Approximately(current, target)) return;

            // Lerp mượt, không phụ thuộc FPS
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(current - target) < 0.001f) current = target;
            transform.localScale = baseScale * current;
        }
    }
}
