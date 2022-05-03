using System;
using Gamu2059.OpenWorldGrassDemo.Common;
using Gamu2059.OpenWorldGrassDemo.Grass;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gamu2059.OpenWorldGrassDemo.Rendering {
    public class GrassDrawPass : ScriptableRenderPass, IDisposable {
        public GrassDrawPass() {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            var manager = GrassChunkManager.Instance;
            if (manager == null) {
                return;
            }

            var chunks = manager.CurrentChunks;
            if (chunks == null) {
                return;
            }

            var cmd = CommandBufferPool.Get(nameof(GrassDrawPass));

            var nearMesh = manager.NearMesh;
            var midMesh = manager.MidMesh;
            var material = manager.GrassMaterial;

            var copyOffset = (uint) (ConstParam.Int32ByteSize * 1);
            foreach (var chunk in chunks) {
                cmd.CopyCounterValue(chunk.NearGrassIndexBuffer, manager.NearGrassArgsBuffer, copyOffset);
                cmd.SetGlobalBuffer(ShaderPropertyID.GrassObjToWorldID, chunk.AllGrassObjToWorldBuffer);
                cmd.SetGlobalBuffer(ShaderPropertyID.GrassIndexesID, chunk.NearGrassIndexBuffer);
                cmd.DrawMeshInstancedIndirect(nearMesh, 0, material, 0, manager.NearGrassArgsBuffer);
            }

            foreach (var chunk in chunks) {
                cmd.CopyCounterValue(chunk.MidGrassIndexBuffer, manager.MidGrassArgsBuffer, copyOffset);
                cmd.SetGlobalBuffer(ShaderPropertyID.GrassObjToWorldID, chunk.AllGrassObjToWorldBuffer);
                cmd.SetGlobalBuffer(ShaderPropertyID.GrassIndexesID, chunk.MidGrassIndexBuffer);
                cmd.DrawMeshInstancedIndirect(midMesh, 0, material, 1, manager.MidGrassArgsBuffer);
            }

            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }

        public void Dispose() {
        }
    }
}