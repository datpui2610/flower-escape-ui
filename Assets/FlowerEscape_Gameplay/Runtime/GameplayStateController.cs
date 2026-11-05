using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FlowerEscape
{
    public enum GameplayState
    {
        Combat,     // đánh quái: bảng gear nhỏ ở dưới, vùng chiến đấu ở trên
        GearSetup   // xếp gear: bảng gear phóng to + thanh mua gear, nút START
    }

    /// <summary>
    /// Chuyển qua lại 2 trạng thái của màn gameplay chính bằng CanvasGroup (mờ dần) + phóng to/thu nhỏ bảng gear.
    /// Phím tắt khi Play: Space/Tab = đổi trạng thái, 1 = Combat, 2 = Xếp gear.
    /// </summary>
    public class GameplayStateController : MonoBehaviour
    {
        [Header("Nhóm UI của từng trạng thái")]
        [SerializeField] private CanvasGroup combatGroup;
        [SerializeField] private CanvasGroup gearSetupGroup;

        [Header("Bảng gear (dùng cho hiệu ứng phóng to / thu nhỏ)")]
        [SerializeField] private RectTransform combatBoard;
        [SerializeField] private RectTransform gearSetupBoard;

        [Header("Cài đặt")]
        [SerializeField] private GameplayState startState = GameplayState.Combat;
        [SerializeField, Min(0f)] private float transitionDuration = 0.35f;

        [Tooltip("Các ô gear được 'nảy' khi bấm Refresh")]
        [SerializeField] private RectTransform[] refreshTargets;

        public GameplayState CurrentState { get; private set; }

        private Coroutine transition;

        private void Start()
        {
            Apply(startState, instant: true);
        }

        // Gọi từ Button.onClick (đã nối sẵn trong scene)
        public void ShowCombat() => SwitchTo(GameplayState.Combat);
        public void ShowGearSetup() => SwitchTo(GameplayState.GearSetup);

        public void Toggle()
        {
            SwitchTo(CurrentState == GameplayState.Combat ? GameplayState.GearSetup : GameplayState.Combat);
        }

        public void SwitchTo(GameplayState state)
        {
            if (state == CurrentState && transition == null) return;
            Apply(state, instant: false);
        }

        public void PlayRefresh()
        {
            if (refreshTargets == null) return;
            for (int i = 0; i < refreshTargets.Length; i++)
            {
                if (refreshTargets[i] != null) StartCoroutine(Punch(refreshTargets[i], i * 0.06f));
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) Toggle();
            else if (keyboard.digit1Key.wasPressedThisFrame) ShowCombat();
            else if (keyboard.digit2Key.wasPressedThisFrame) ShowGearSetup();
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Tab)) Toggle();
            else if (Input.GetKeyDown(KeyCode.Alpha1)) ShowCombat();
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ShowGearSetup();
#endif
        }

        private void Apply(GameplayState state, bool instant)
        {
            CurrentState = state;

            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }

            if (instant || transitionDuration <= 0f || !isActiveAndEnabled)
            {
                SetGroupVisible(combatGroup, state == GameplayState.Combat);
                SetGroupVisible(gearSetupGroup, state == GameplayState.GearSetup);
                SetScale(combatBoard, 1f);
                SetScale(gearSetupBoard, 1f);
                return;
            }

            transition = StartCoroutine(Transition(state));
        }

        private IEnumerator Transition(GameplayState target)
        {
            bool toGear = target == GameplayState.GearSetup;
            CanvasGroup from = toGear ? combatGroup : gearSetupGroup;
            CanvasGroup to = toGear ? gearSetupGroup : combatGroup;
            RectTransform fromBoard = toGear ? combatBoard : gearSetupBoard;
            RectTransform toBoard = toGear ? gearSetupBoard : combatBoard;

            // Khoá bấm trong lúc chuyển để tránh bấm 2 lần
            SetInteractable(from, false);
            SetInteractable(to, false);

            float fromAlpha = from != null ? from.alpha : 0f;
            float toAlpha = to != null ? to.alpha : 0f;

            // Vào xếp gear: bảng to dần ra; về combat: bảng thu nhỏ lại
            float outScale = toGear ? 1.12f : 0.88f;
            float inScale = toGear ? 0.85f : 1.15f;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / transitionDuration;
                float e = EaseOutCubic(Mathf.Clamp01(t));

                if (from != null) from.alpha = Mathf.Lerp(fromAlpha, 0f, e);
                if (to != null) to.alpha = Mathf.Lerp(toAlpha, 1f, e);
                SetScale(fromBoard, Mathf.Lerp(1f, outScale, e));
                SetScale(toBoard, Mathf.Lerp(inScale, 1f, e));
                yield return null;
            }

            SetGroupVisible(from, false);
            SetGroupVisible(to, true);
            SetScale(fromBoard, 1f);
            SetScale(toBoard, 1f);
            transition = null;
        }

        private static IEnumerator Punch(RectTransform target, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            Vector3 baseScale = Vector3.one;
            const float duration = 0.22f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                // Nảy lên 1.15 rồi về 1 (đường cong sin)
                float k = 1f + 0.15f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                target.localScale = baseScale * k;
                yield return null;
            }
            target.localScale = baseScale;
        }

        private static void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            SetInteractable(group, visible);
        }

        private static void SetInteractable(CanvasGroup group, bool value)
        {
            if (group == null) return;
            group.interactable = value;
            group.blocksRaycasts = value;
        }

        private static void SetScale(RectTransform rect, float scale)
        {
            if (rect != null) rect.localScale = new Vector3(scale, scale, 1f);
        }

        private static float EaseOutCubic(float x)
        {
            float a = 1f - x;
            return 1f - a * a * a;
        }

        // Chuột phải vào component trong Inspector để xem trước từng trạng thái khi chưa Play
        [ContextMenu("Xem trước: Combat")]
        private void PreviewCombat()
        {
            SetGroupVisible(combatGroup, true);
            SetGroupVisible(gearSetupGroup, false);
        }

        [ContextMenu("Xem trước: Xếp gear")]
        private void PreviewGearSetup()
        {
            SetGroupVisible(combatGroup, false);
            SetGroupVisible(gearSetupGroup, true);
        }
    }
}
