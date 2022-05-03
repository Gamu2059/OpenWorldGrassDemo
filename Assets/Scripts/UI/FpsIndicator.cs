using UnityEngine;
using UnityEngine.UI;

namespace Gamu2059.OpenWorldGrassDemo.UI {
    /// <summary>
    /// 平均Fpsを表示する
    /// 参考 : https://gist.github.com/sanukin39/63bdfcf6c57466abe6d44ac5a79c5ca9
    /// </summary>
    public class FpsIndicator : MonoBehaviour {
        [SerializeField]
        private Text m_IndicatorText;
        
        [SerializeField]
        private float m_UpdateInterval = 0.5f;

        private int frameCount;
        private float elapsedTime;

        private void Update() {
            frameCount++;
            elapsedTime += Time.deltaTime;
            if (elapsedTime > m_UpdateInterval) {
                var frameRate = Mathf.Round(frameCount / elapsedTime);
                if (m_IndicatorText != null) {
                    m_IndicatorText.text = $"{frameRate} fps";
                }
                
                frameCount = 0;
                elapsedTime = 0;
            }
        }
    }
}