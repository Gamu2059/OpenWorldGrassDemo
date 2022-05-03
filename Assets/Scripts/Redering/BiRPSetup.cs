using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.OpenWorldGrassDemo.Rendering {
    /// <summary>
    /// ビルトインレンダーパイプラインに自動的に切り替えるクラス
    /// </summary>
    [ExecuteInEditMode]
    public class BiRPSetup : MonoBehaviour {
        private void OnEnable() {
            GraphicsSettings.renderPipelineAsset = null;
        }
    }
}