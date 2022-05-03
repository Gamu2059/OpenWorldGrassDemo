using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.UI {
    /// <summary>
    /// セーフエリアにRectTransformのサイズを合わせる
    /// </summary>
    public class SafeArea : MonoBehaviour {
        [SerializeField]
        private RectTransform m_SafeAreaTarget;
        
        private void Awake() {
            // 左下にanchorとpivotを設定
            m_SafeAreaTarget.anchorMin = Vector2.zero;
            m_SafeAreaTarget.anchorMax = Vector2.zero;
            m_SafeAreaTarget.pivot = Vector2.zero;
            
            var safeArea = Screen.safeArea;
            m_SafeAreaTarget.anchoredPosition = safeArea.position;
            m_SafeAreaTarget.sizeDelta = safeArea.size;
        }
    }
}