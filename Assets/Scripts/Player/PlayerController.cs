using Gamu2059.OpenWorldGrassDemo.UI;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Player {
    /// <summary>
    /// プレイヤー制御クラス
    /// </summary>
    public class PlayerController : MonoBehaviour {
        [SerializeField]
        private Transform m_Player;

        [SerializeField]
        private InputController m_InputController;

        [SerializeField]
        private float m_MoveForwardSpeed = 2f;

        [SerializeField]
        private float m_MoveBackSpeed = 1f;

        [SerializeField]
        private float m_TurnAngleSpeed = 45f;

        private void Start() {
            if (m_InputController != null) {
                m_InputController.OnInput += OnReceivedInput;
            }
        }

        private void OnReceivedInput(InputType inputType) {
            var inputForward = (inputType & InputType.Forward) == InputType.Forward;
            var inputBack = (inputType & InputType.Back) == InputType.Back;
            var inputTurnLeft = (inputType & InputType.TurnLeft) == InputType.TurnLeft;
            var inputTurnRight = (inputType & InputType.TurnRight) == InputType.TurnRight;

            if (inputForward && !inputBack) {
                m_Player.Translate(m_MoveForwardSpeed * Time.deltaTime * Vector3.forward, Space.Self);
            }

            if (!inputForward && inputBack) {
                m_Player.Translate(m_MoveBackSpeed * Time.deltaTime * Vector3.back, Space.Self);
            }

            if (inputTurnLeft && !inputTurnRight) {
                m_Player.Rotate(Vector3.up, -m_TurnAngleSpeed * Time.deltaTime, Space.Self);
            }

            if (!inputTurnLeft && inputTurnRight) {
                m_Player.Rotate(Vector3.up, m_TurnAngleSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}