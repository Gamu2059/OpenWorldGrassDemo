using Gamu2059.OpenWorldGrassDemo.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.OpenWorldGrassDemo.Wind {
    /// <summary>
    /// 風のプロパティをシェーダに設定するクラス
    /// </summary>
    public class WindPropertySender : MonoBehaviour {
        /// <summary>
        /// ワールド空間での風の向き
        /// </summary>
        [SerializeField]
        private Vector2 m_WindDir;

        /// <summary>
        /// ワールド空間での風の周波数
        /// </summary>
        [SerializeField]
        private Vector2 m_WindFreq;

        /// <summary>
        /// 風の強さ
        /// </summary>
        [SerializeField]
        private float m_WindStrength;

        /// <summary>
        /// 風の揺らぎの大きさ
        /// </summary>
        [SerializeField]
        private float m_WindWaveScale;

        /// <summary>
        /// 風の揺らぎの周波数
        /// </summary>
        [SerializeField]
        private float m_WindWaveFreq;

        /// <summary>
        /// ノイズテクスチャのUVオフセット
        /// </summary>
        private Vector2 m_NoisePointOffset;

        private float m_NoisePointRadian;

        private void Awake() {
            m_NoisePointOffset = Vector2.zero;
            m_NoisePointRadian = 0;
        }

        private void LateUpdate() {
            // 風の向きに直交する向き
            var windDir = m_WindDir.normalized;
            var acrossDir = new Vector2(windDir.y, windDir.x);
            m_NoisePointRadian += Time.deltaTime;
            m_NoisePointRadian %= Mathf.PI * 2;

            var windDirOffset = windDir * (m_WindWaveFreq * Time.deltaTime);
            var acrossDirOffset = acrossDir * (Mathf.Sin(m_NoisePointRadian) * 0.1f * Time.deltaTime);
            m_NoisePointOffset += windDirOffset + acrossDirOffset;
            m_NoisePointOffset.x %= 1;
            m_NoisePointOffset.y %= 1;

            Shader.SetGlobalVector(ShaderPropertyID.WindDirId, windDir);
            Shader.SetGlobalVector(ShaderPropertyID.WindFreqId, m_WindFreq);
            Shader.SetGlobalVector(ShaderPropertyID.WindNoisePointOffsetId, m_NoisePointOffset);
            Shader.SetGlobalFloat(ShaderPropertyID.WindStrengthId, m_WindStrength);
            Shader.SetGlobalFloat(ShaderPropertyID.WindWaveScaleId, m_WindWaveScale);
        }
    }
}