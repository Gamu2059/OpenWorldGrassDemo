using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.OpenWorldGrassDemo.Rendering {
    /// <summary>
    /// URPに自動的に切り替えるクラス
    /// </summary>
    [ExecuteInEditMode]
    public class URPSetup : MonoBehaviour {
        [SerializeField]
        private UniversalRenderPipelineAsset m_RenderPipelineAsset;

        private void OnEnable() {
            GraphicsSettings.renderPipelineAsset = m_RenderPipelineAsset;
        }
    }
}