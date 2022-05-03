using Gamu2059.OpenWorldGrassDemo.Common;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Player {
    /// <summary>
    /// プレイヤーの位置と半径をシェーダに設定するクラス
    /// </summary>
    public class PlayerPropertySender : MonoBehaviour {
        [SerializeField]
        private Transform m_Player;

        /// <summary>
        /// 考慮するプレイヤーの移動速度の範囲
        /// </summary>
        [SerializeField]
        private Vector2 m_SpeedRange;

        /// <summary>
        /// 基本半径
        /// </summary>
        [SerializeField]
        private float m_BaseRadius;

        /// <summary>
        /// プレイヤーの移動速度に紐づく加算半径 (x:min, y:max)
        /// </summary>
        [SerializeField]
        private Vector2 m_ScaleRadiusRange;

        /// <summary>
        /// 前のフレームの半径との補間値
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float m_RadiusLerp;

        private Vector3 m_PreFramePosition;
        private float m_PreFrameRadius;
        private Vector2 m_PreFrameMoveDir;

        private void Start() {
            if (m_Player == null) {
                return;
            }

            m_PreFramePosition = m_Player.position;
            m_PreFrameRadius = 0f;
            m_PreFrameMoveDir = Vector2.zero;
        }

        private void LateUpdate() {
            if (m_Player == null) {
                return;
            }

            var currentPosition = m_Player.position;
            var dir = currentPosition - m_PreFramePosition;
            var dist = Mathf.Max(dir.magnitude, Mathf.Epsilon);
            var magnitude = dist / Time.deltaTime;
            var t = Mathf.InverseLerp(m_SpeedRange.x, m_SpeedRange.y, magnitude);
            var radius = Mathf.Lerp(m_ScaleRadiusRange.x, m_ScaleRadiusRange.y, t);
            radius = Mathf.Lerp(m_PreFrameRadius, radius, m_RadiusLerp);
            var normalDir = new Vector2(dir.x, dir.z).normalized;
            if (dist <= 0.01f) {
                normalDir = m_PreFrameMoveDir;
            }

            m_PreFramePosition = currentPosition;
            m_PreFrameRadius = radius;
            m_PreFrameMoveDir = normalDir;

            Shader.SetGlobalVector(ShaderPropertyID.PlayerPosID, currentPosition);
            // xy : moveDir, z : baseRadius, w : scaleRadius
            var param = new Vector4(normalDir.x, normalDir.y, m_BaseRadius, radius);
            Shader.SetGlobalVector(ShaderPropertyID.PlayerRadiusParamID, param);
        }
    }
}