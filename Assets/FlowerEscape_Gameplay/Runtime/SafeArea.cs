using UnityEngine;

namespace FlowerEscape
{
    /// <summary>
    /// Co RectTransform vào vùng an toàn của màn hình (tránh tai thỏ, camera đục lỗ, thanh home).
    /// Gắn vào một RectTransform kéo giãn full Canvas; các nhóm HUD đặt bên trong.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            // Xoay màn hình hoặc đổi độ phân giải thì tính lại
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
