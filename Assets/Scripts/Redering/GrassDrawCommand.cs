using System;
using Gamu2059.OpenWorldGrassDemo.Common;
using Gamu2059.OpenWorldGrassDemo.Grass;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.OpenWorldGrassDemo.Rendering {
    /// <summary>
    /// ビルトインレンダーパイプライン用の草描画コマンドを発行する
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ImageEffectAllowedInSceneView]
    public class GrassDrawCommand : MonoBehaviour {
        private Camera m_TargetCamera;
        private CommandBuffer m_Cmd;

        private void Awake() {
            m_TargetCamera = GetComponent<Camera>();
        }

        private void OnPreRender() {
            if (m_TargetCamera == null) {
                return;
            }

            if (m_Cmd == null) {
                m_Cmd = CommandBufferPool.Get(nameof(GrassDrawCommand));
            }

            m_TargetCamera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_Cmd);
            SetupCommandBuffer();
            m_TargetCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_Cmd);
        }

        private void SetupCommandBuffer() {
            m_Cmd.Clear();

            var manager = GrassChunkManager.Instance;
            if (manager == null) {
                return;
            }

            var chunks = manager.CurrentChunks;
            if (chunks == null) {
                return;
            }

            var nearMesh = manager.NearMesh;
            var midMesh = manager.MidMesh;
            var material = manager.GrassMaterial;

            var copyOffset = (uint) (ConstParam.Int32ByteSize * 1);
            foreach (var chunk in chunks) {
                m_Cmd.CopyCounterValue(chunk.NearGrassIndexBuffer, manager.NearGrassArgsBuffer, copyOffset);
                m_Cmd.SetGlobalBuffer(ShaderPropertyID.GrassObjToWorldID, chunk.AllGrassObjToWorldBuffer);
                m_Cmd.SetGlobalBuffer(ShaderPropertyID.GrassIndexesID, chunk.NearGrassIndexBuffer);
                m_Cmd.DrawMeshInstancedIndirect(nearMesh, 0, material, 0, manager.NearGrassArgsBuffer);
            }

            foreach (var chunk in chunks) {
                m_Cmd.CopyCounterValue(chunk.MidGrassIndexBuffer, manager.MidGrassArgsBuffer, copyOffset);
                m_Cmd.SetGlobalBuffer(ShaderPropertyID.GrassObjToWorldID, chunk.AllGrassObjToWorldBuffer);
                m_Cmd.SetGlobalBuffer(ShaderPropertyID.GrassIndexesID, chunk.MidGrassIndexBuffer);
                m_Cmd.DrawMeshInstancedIndirect(midMesh, 0, material, 1, manager.MidGrassArgsBuffer);
            }
        }
    }
}