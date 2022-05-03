using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Gamu2059.OpenWorldGrassDemo.UI {
    /// <summary>
    /// 入力タイプ
    /// </summary>
    [Flags]
    public enum InputType {
        None = 0,
        Forward = 1 << 0,
        Back = 1 << 1,
        TurnLeft = 1 << 2,
        TurnRight = 1 << 3,
    }

    /// <summary>
    /// 入力を受け付けてイベントを発行するクラス
    /// </summary>
    public class InputController : MonoBehaviour {
        [SerializeField]
        private GameObject m_ForwardObj;

        [SerializeField]
        private GameObject m_BackObj;

        [SerializeField]
        private GameObject m_TurnLeftObj;

        [SerializeField]
        private GameObject m_TurnRightObj;

        private bool m_PushForward;
        private bool m_PushBack;
        private bool m_PushTurnLeft;
        private bool m_PushTurnRight;

        public event Action<InputType> OnInput;

        private void Start() {
            SetupButton(m_ForwardObj, _ => m_PushForward = true, _ => m_PushForward = false);
            SetupButton(m_BackObj, _ => m_PushBack = true, _ => m_PushBack = false);
            SetupButton(m_TurnLeftObj, _ => m_PushTurnLeft = true, _ => m_PushTurnLeft = false);
            SetupButton(m_TurnRightObj, _ => m_PushTurnRight = true, _ => m_PushTurnRight = false);
        }

        private void Update() {
            var input = InputType.None;
            var vertical = Input.GetAxis("Vertical");
            var horizontal = Input.GetAxis("Horizontal");

            if (vertical > 0 || m_PushForward) {
                input |= InputType.Forward;
            }

            if (vertical < 0 || m_PushBack) {
                input |= InputType.Back;
            }

            if (horizontal < 0 || m_PushTurnLeft) {
                input |= InputType.TurnLeft;
            }

            if (horizontal > 0 || m_PushTurnRight) {
                input |= InputType.TurnRight;
            }

            OnInput?.Invoke(input);
        }

        private void SetupButton(GameObject target, UnityAction<BaseEventData> onPushDown,
            UnityAction<BaseEventData> onPushUp) {
            if (target == null) return;

            if (!target.TryGetComponent<EventTrigger>(out var trigger)) {
                trigger = target.AddComponent<EventTrigger>();
            }

            var pushDownEvent = new EventTrigger.TriggerEvent();
            pushDownEvent.AddListener(onPushDown);

            var pushDownEntry = new EventTrigger.Entry();
            pushDownEntry.eventID = EventTriggerType.PointerDown;
            pushDownEntry.callback = pushDownEvent;

            var pushUpEvent = new EventTrigger.TriggerEvent();
            pushUpEvent.AddListener(onPushUp);

            var pushUpEntry = new EventTrigger.Entry();
            pushUpEntry.eventID = EventTriggerType.PointerUp;
            pushUpEntry.callback = pushUpEvent;

            trigger.triggers.Add(pushDownEntry);
            trigger.triggers.Add(pushUpEntry);
        }
    }
}