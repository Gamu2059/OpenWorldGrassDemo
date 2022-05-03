using UnityEngine.Rendering.Universal;

namespace Gamu2059.OpenWorldGrassDemo.Rendering {
    public class GrassDrawFeature : ScriptableRendererFeature {
        private GrassDrawPass pass;

        public override void Create() {
            DisposePass();
            pass = new GrassDrawPass();
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            DisposePass();
        }

        private void OnDisable() {
            DisposePass();
        }

        private void OnDestroy() {
            DisposePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (pass != null) {
                renderer.EnqueuePass(pass);
            }
        }

        private void DisposePass() {
            pass?.Dispose();
            pass = null;
        }
    }
}