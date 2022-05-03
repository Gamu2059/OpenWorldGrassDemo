using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gamu2059.OpenWorldGrassDemo.Common;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Grass {
    /// <summary>
    /// 区域データを保持するクラス
    /// </summary>
    public class GrassChunk {
        /// <summary>
        /// 区域ID
        /// </summary>
        public Vector2Int ID { get; private set; }
        
        /// <summary>
        /// 区域の読み込み状態
        /// </summary>
        public GrassChunkState State { get; private set; }
        
        /// <summary>
        /// 区域のAABB
        /// </summary>
        public Bounds ChunkBounds { get; private set; }

        /// <summary>
        /// 区域に草が存在しない
        /// </summary>
        public bool IsEmptyChunk => m_GrassCount < 1;

        /// <summary>
        /// 草データのモデル行列リスト
        /// </summary>
        public ComputeBuffer AllGrassObjToWorldBuffer { get; private set; }
        
        /// <summary>
        /// 近距離描画のインデックスリスト
        /// </summary>
        public ComputeBuffer NearGrassIndexBuffer { get; private set; }
        
        /// <summary>
        /// 中距離描画のインデックスリスト
        /// </summary>
        public ComputeBuffer MidGrassIndexBuffer { get; private set; }
        
        /// <summary>
        /// 使用するComputeShader
        /// </summary>
        private ComputeShader m_ComputeShader;
        
        /// <summary>
        /// 区域内の草の総量
        /// </summary>
        private int m_GrassCount;
        
        /// <summary>
        /// 区域内の分割領域の総量
        /// </summary>
        private int m_ChunkDivisionCount;

        /// <summary>
        /// 草データリスト
        /// </summary>
        private ComputeBuffer m_AllGrassDataBuffer;

        /// <summary>
        /// 分割領域のAABBの最小座標リスト
        /// </summary>
        private ComputeBuffer m_ChunkDivisionBoundsMinBuffer;

        /// <summary>
        /// 分割領域のAABBの最大座標リスト
        /// </summary>
        private ComputeBuffer m_ChunkDivisionBoundsMaxBuffer;

        /// <summary>
        /// 分割領域のカリング結果
        /// </summary>
        private ComputeBuffer m_ChunkDivisionCullResultBuffer;

        /// <summary>
        /// IDをセットする
        /// </summary>
        public void SetID(Vector2Int id) {
            ID = id;
        }

        /// <summary>
        /// 区域を用意する
        /// </summary>
        public async UniTask SetupAsync(ComputeShader computeShader, CancellationToken ct) {
            if (ct.IsCancellationRequested) {
                return;
            }

            if (!(State == GrassChunkState.None || State == GrassChunkState.Disposed)) {
                return;
            }

            State = GrassChunkState.Loading;

            m_ComputeShader = computeShader;
            var data  = await GrassChunkDataUtility.LoadGrassChunkAsync(ID, ct);
            if (ct.IsCancellationRequested || data == null) {
                // リクエストを再度受け付けられるようにしておく
                State = GrassChunkState.Disposed;
                return;
            }

            ChunkBounds = new Bounds {
                min = data.ChunkBoundsMin,
                max = data.ChunkBoundsMax,
            };

            m_GrassCount = data.GrassDataList.Count;
            m_ChunkDivisionCount = data.DivisionBoundsMinArray.Length;

            if (m_GrassCount < 1) {
                State = GrassChunkState.Loaded;
                return;
            }

            DisposeComputeBuffer();
            SetupComputeBuffer(ref data.GrassDataList, ref data.DivisionBoundsMinArray, ref data.DivisionBoundsMaxArray);
            ComputeConvertGrassData();

            State = GrassChunkState.Loaded;
        }

        /// <summary>
        /// 区域を破棄する
        /// </summary>
        /// <param name="forceDispose">true:読み込み状態に関係なく破棄する</param>
        public void Dispose(bool forceDispose = false) {
            if (!forceDispose && State != GrassChunkState.Loaded) {
                return;
            }

            State = GrassChunkState.Disposing;
            DisposeComputeBuffer();
            State = GrassChunkState.Disposed;
        }

        /// <summary>
        /// ComputeBufferを用意する
        /// </summary>
        private void SetupComputeBuffer(ref List<GrassData> grassDataList, ref Vector3[] chunkDivisionBoundsMin, ref Vector3[] chunkDivisionBoundsMax) {
            var grassDataSize = ConstParam.GrassDataByteSize;
            var vector3Size = ConstParam.Vector3ByteSize;
            var matrix4x4Size = ConstParam.Matrix4x4ByteSize;
            var int32Size = ConstParam.Int32ByteSize;

            m_AllGrassDataBuffer = new ComputeBuffer(m_GrassCount, grassDataSize);
            m_AllGrassDataBuffer.SetData(grassDataList);
            m_ChunkDivisionBoundsMinBuffer = new ComputeBuffer(m_ChunkDivisionCount, vector3Size);
            m_ChunkDivisionBoundsMinBuffer.SetData(chunkDivisionBoundsMin);
            m_ChunkDivisionBoundsMaxBuffer = new ComputeBuffer(m_ChunkDivisionCount, vector3Size);
            m_ChunkDivisionBoundsMaxBuffer.SetData(chunkDivisionBoundsMax);

            m_ChunkDivisionCullResultBuffer = new ComputeBuffer(m_ChunkDivisionCount, int32Size);
            AllGrassObjToWorldBuffer = new ComputeBuffer(m_GrassCount, matrix4x4Size);
            
            // インデックスリストは追加型の構造にする
            NearGrassIndexBuffer = new ComputeBuffer(m_GrassCount, int32Size, ComputeBufferType.Append);
            MidGrassIndexBuffer = new ComputeBuffer(m_GrassCount, int32Size, ComputeBufferType.Append);
        }

        /// <summary>
        /// ComputeBufferを破棄する
        /// </summary>
        private void DisposeComputeBuffer() {
            m_AllGrassDataBuffer?.Release();
            m_ChunkDivisionBoundsMinBuffer?.Release();
            m_ChunkDivisionBoundsMaxBuffer?.Release();
            m_ChunkDivisionCullResultBuffer?.Release();
            AllGrassObjToWorldBuffer?.Release();
            NearGrassIndexBuffer?.Release();
            MidGrassIndexBuffer?.Release();
        }

        /// <summary>
        /// 草データからモデル行列への変換処理
        /// </summary>
        private void ComputeConvertGrassData() {
            var kernelIndex = ConstParam.ConvertGrassMatrixIndex;
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.GrassDataID, m_AllGrassDataBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.GrassObjToWorldID, AllGrassObjToWorldBuffer);
            m_ComputeShader.Dispatch(kernelIndex, Mathf.CeilToInt(m_GrassCount / 64f), 1, 1);
        }

        /// <summary>
        /// 分割領域のカリング処理
        /// </summary>
        public void ComputeFrustumCullChunkDivision(ComputeBuffer frustumPlaneBuffer) {
            var kernelIndex = ConstParam.CullFrustumIndex;
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.FrustumPlanesID, frustumPlaneBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.CullBoundsMinID, m_ChunkDivisionBoundsMinBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.CullBoundsMaxID, m_ChunkDivisionBoundsMaxBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.CullBoundsResultID, m_ChunkDivisionCullResultBuffer);
            m_ComputeShader.Dispatch(kernelIndex, Mathf.CeilToInt(m_ChunkDivisionCount / 64f), 1, 1);
        }

        /// <summary>
        /// 草の距離カリングとLOD処理
        /// </summary>
        public void ComputeProcessGrassCullLOD() {
            NearGrassIndexBuffer.SetCounterValue(0);
            MidGrassIndexBuffer.SetCounterValue(0);

            var kernelIndex = ConstParam.ProcessGrassCullLODIndex;
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.GrassDataID, m_AllGrassDataBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.CullBoundsResultID, m_ChunkDivisionCullResultBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.NearGrassIndexesID, NearGrassIndexBuffer);
            m_ComputeShader.SetBuffer(kernelIndex, ShaderPropertyID.MidGrassIndexesID, MidGrassIndexBuffer);
            m_ComputeShader.Dispatch(kernelIndex, Mathf.CeilToInt(m_GrassCount / 64f), 1, 1);
        }
    }
}