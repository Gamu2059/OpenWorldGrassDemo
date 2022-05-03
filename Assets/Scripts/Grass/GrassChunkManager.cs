using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Gamu2059.OpenWorldGrassDemo.Common;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gamu2059.OpenWorldGrassDemo.Grass {
    /// <summary>
    /// 草の区域のマネージャ
    /// </summary>
    public class GrassChunkManager : MonoBehaviour {
        #region Serializable Field

        /// <summary>
        /// 草の生やす位置をレイキャストで決める時のレイキャストの高さ
        /// </summary>
        [SerializeField]
        private float m_GenerateRaycastPositionY;

        /// <summary>
        /// 草を生やす位置をレイキャストで決める時のレイキャストの長さ
        /// </summary>
        [SerializeField]
        private float m_GenerateRaycastDistance;

        /// <summary>
        /// 区域の数
        /// </summary>
        [SerializeField]
        private Vector2Int m_ChunkCount;

        /// <summary>
        /// 区域の座標オフセット
        /// </summary>
        [SerializeField]
        private Vector2 m_ChunkPositionOffset;

        /// <summary>
        /// 区域のサイズ
        /// </summary>
        [SerializeField]
        private Vector2 m_ChunkSize;

        /// <summary>
        /// 区域内の草の密度
        /// </summary>
        [SerializeField]
        private float m_GrassDensity;

        /// <summary>
        /// 草の座標をランダムにずらす範囲
        /// </summary>
        [SerializeField]
        private Vector2 m_GrassPositionRandomRange;

        /// <summary>
        /// 草のスケールのテーブル
        /// </summary>
        [SerializeField]
        private Vector2[] m_GrassScaleXYTable;

        /// <summary>
        /// 読み込む区域のマンハッタン距離
        /// </summary>
        [SerializeField]
        private int m_LoadChunkDistance;

        /// <summary>
        /// 距離カリングの最小距離
        /// </summary>
        [SerializeField]
        private float m_DistanceCullMin;

        /// <summary>
        /// 距離カリングの最大座標
        /// </summary>
        [SerializeField]
        private float m_DistanceCullMax;

        /// <summary>
        /// 草のメッシュLODの距離閾値
        /// </summary>
        [SerializeField]
        private float m_LODThreshold;

        [SerializeField]
        private ComputeShader m_ComputeShader;

        [SerializeField]
        private Mesh m_NearMesh;

        [SerializeField]
        private Mesh m_MidMesh;

        [SerializeField]
        private Material m_GrassMaterial;

        #endregion

        public static GrassChunkManager Instance { get; private set; }

        /// <summary>
        /// 直前のフレームで読み込むべき範囲のIDセット
        /// </summary>
        private HashSet<Vector2Int> m_PreFlameIdSet;

        /// <summary>
        /// 全ての区域の状態を保持するディクショナリ
        /// </summary>
        private Dictionary<Vector2Int, GrassChunkState> m_ChunkStateDict;

        /// <summary>
        /// 読み込み中の区域を保持するセット
        /// </summary>
        private HashSet<GrassChunk> m_ChunkLoadingSet;

        /// <summary>
        /// 読み込み完了後の区域を保持するセット
        /// </summary>
        private HashSet<GrassChunk> m_ChunkLoadedSet;

        /// <summary>
        /// 破棄中の区域を保持するセット
        /// </summary>
        private HashSet<GrassChunk> m_ChunkDisposingSet;

        /// <summary>
        /// 破棄完了後の区域を保持するプール
        /// </summary>
        private ObjectPool<GrassChunk> m_ChunkPool;

        /// <summary>
        /// 読み込みリクエストのキュー
        /// </summary>
        private Queue<GrassChunk> m_LoadRequestQueue;

        private Plane[] m_FrustumPlanes;
        private PlaneData[] m_ComputeFrustumPlanes;
        private ComputeBuffer m_FrustumPlaneBuffer;

        public Mesh MidMesh => m_MidMesh;
        public Mesh NearMesh => m_NearMesh;
        public Material GrassMaterial => m_GrassMaterial;
        public ComputeBuffer NearGrassArgsBuffer { get; private set; }
        public ComputeBuffer MidGrassArgsBuffer { get; private set; }
        public GrassChunk[] CurrentChunks { get; private set; }

        private void Awake() {
            if (Instance != null && this != Instance) {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            m_PreFlameIdSet = new HashSet<Vector2Int>();
            m_ChunkStateDict = new Dictionary<Vector2Int, GrassChunkState>();
            m_ChunkLoadingSet = new HashSet<GrassChunk>();
            m_ChunkLoadedSet = new HashSet<GrassChunk>();
            m_ChunkDisposingSet = new HashSet<GrassChunk>();
            m_ChunkPool = new ObjectPool<GrassChunk>(null, null);
            m_LoadRequestQueue = new Queue<GrassChunk>();

            m_FrustumPlanes = new Plane[ConstParam.FrustumPlaneCount];
            m_ComputeFrustumPlanes = new PlaneData[ConstParam.FrustumPlaneCount];

            var planeDataSize = ConstParam.PlaneDataByteSize;
            m_FrustumPlaneBuffer?.Release();
            m_FrustumPlaneBuffer = new ComputeBuffer(ConstParam.FrustumPlaneCount, planeDataSize);

            DisposeArgsBuffer();
            SetupArgsBuffer();
        }

        private void OnDestroy() {
            if (Instance != null && this == Instance) {
                Instance = null;
            }

            foreach (var g in m_ChunkLoadingSet) {
                g.Dispose(true);
            }

            foreach (var g in m_ChunkLoadedSet) {
                g.Dispose(true);
            }

            m_FrustumPlaneBuffer?.Release();
            DisposeArgsBuffer();
        }

        private void LateUpdate() {
            var camera = Camera.main;
            if (camera == null) {
                return;
            }

            var cameraPosition = camera.transform.position;

            // カメラの座標を区域IDへ変換する
            var centerID = CalcCurrentCenterID(cameraPosition);

            // 中心IDから読み込んでおくべき範囲のIDを調べる
            var loadIdSet = CalcLoadAreaIDs(centerID, m_LoadChunkDistance);

            // 読み込み状況を更新する
            UpdateChunk(loadIdSet);

            if (m_ChunkLoadedSet.Count < 1) {
                CurrentChunks = null;
                return;
            }

            // ComputeBuffer.GetDataがとても重いため、区域の視錐台カリングはComputeShaderで行わない
            var mat = camera.projectionMatrix * camera.worldToCameraMatrix;
            GeometryUtility.CalculateFrustumPlanes(mat, m_FrustumPlanes);
            CurrentChunks = m_ChunkLoadedSet
                .Where(c => !c.IsEmptyChunk)
                .Where(c => GeometryUtility.TestPlanesAABB(m_FrustumPlanes, c.ChunkBounds))
                .OrderBy(c => {
                    var d = c.ID - centerID;
                    return Mathf.Abs(d.x) + Mathf.Abs(d.y);
                }).ToArray();

            m_ComputeShader.SetVector(ShaderPropertyID.CameraPosID, cameraPosition);
            m_ComputeShader.SetFloat(ShaderPropertyID.DistanceCullMinID, m_DistanceCullMin);
            m_ComputeShader.SetFloat(ShaderPropertyID.DistanceCullMaxID, m_DistanceCullMax);
            m_ComputeShader.SetFloat(ShaderPropertyID.LODThresholdID, m_LODThreshold);

            // 視錐台平面をComputeBufferに詰める
            for (var i = 0; i < m_FrustumPlanes.Length; i++) {
                m_ComputeFrustumPlanes[i].Normal = m_FrustumPlanes[i].normal;
                m_ComputeFrustumPlanes[i].Distance = m_FrustumPlanes[i].distance;
            }
            m_FrustumPlaneBuffer.SetData(m_ComputeFrustumPlanes);
            
            foreach (var chunk in CurrentChunks) {
                chunk.ComputeFrustumCullChunkDivision(m_FrustumPlaneBuffer);
                chunk.ComputeProcessGrassCullLOD();
            }
        }

        private void SetupArgsBuffer() {
            var size = ConstParam.Int32ByteSize * 5;
            var nearMeshArgsArray = SetupArgsArray(m_NearMesh);
            var midMeshArgsArray = SetupArgsArray(m_MidMesh);
            NearGrassArgsBuffer = new ComputeBuffer(1, size, ComputeBufferType.IndirectArguments);
            NearGrassArgsBuffer.SetData(nearMeshArgsArray);
            MidGrassArgsBuffer = new ComputeBuffer(1, size, ComputeBufferType.IndirectArguments);
            MidGrassArgsBuffer.SetData(midMeshArgsArray);
        }

        private uint[] SetupArgsArray(in Mesh mesh) {
            var args = new uint[] {0, 0, 0, 0, 0};
            args[0] = mesh.GetIndexCount(0);
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            return args;
        }

        private void DisposeArgsBuffer() {
            NearGrassArgsBuffer?.Release();
            MidGrassArgsBuffer?.Release();
        }

        private Vector2Int CalcCurrentCenterID(Vector3 centerPosition) {
            var centerID = Vector2.zero;
            centerID.x = Mathf.CeilToInt((centerPosition.x - m_ChunkPositionOffset.x) / m_ChunkSize.x - 1);
            centerID.y = Mathf.CeilToInt((centerPosition.z - m_ChunkPositionOffset.y) / m_ChunkSize.y - 1);
            centerID.x += m_ChunkCount.x * 0.5f;
            centerID.y += m_ChunkCount.y * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(centerID.x), Mathf.RoundToInt(centerID.y));
        }

        private HashSet<Vector2Int> CalcLoadAreaIDs(Vector2Int id, int loadIdDistance) {
            var loadIdSet = new HashSet<Vector2Int>();
            for (var y = -loadIdDistance; y <= loadIdDistance; y++) {
                var areaY = id.y + y;
                if (areaY < 0 || areaY >= m_ChunkCount.y) {
                    continue;
                }

                var xDist = loadIdDistance - Mathf.Abs(y);
                for (var x = -xDist; x <= xDist; x++) {
                    var areaX = id.x + x;
                    if (areaX < 0 || areaX >= m_ChunkCount.x) {
                        continue;
                    }

                    loadIdSet.Add(new Vector2Int(areaX, areaY));
                }
            }

            return loadIdSet;
        }

        /// <summary>
        /// 区域の読み込み状態を更新する
        /// </summary>
        private void UpdateChunk(HashSet<Vector2Int> loadIdSet) {
            // 読み込んでおくべき範囲は常に読み込みリクエストを行う
            foreach (var loadId in loadIdSet) {
                EnqueueLoadChunk(loadId);
            }

            // 読み込み中リストを更新
            var removeChunkList = new List<GrassChunk>();
            foreach (var chunk in m_ChunkLoadingSet) {
                if (chunk.State != GrassChunkState.Loaded) {
                    continue;
                }

                // 読み込んでおくべき範囲にあるなら読み込み完了リストへ
                if (loadIdSet.Contains(chunk.ID)) {
                    m_ChunkLoadedSet.Add(chunk);
                    SetState(chunk.ID, GrassChunkState.Loaded);
                    removeChunkList.Add(chunk);
                    continue;
                }

                // それ以外は破棄リクエストを行う
                DisposeChunk(chunk);
                removeChunkList.Add(chunk);
            }

            removeChunkList.ForEach(g => m_ChunkLoadingSet.Remove(g));

            // 読み込み完了リストを更新
            removeChunkList.Clear();
            foreach (var chunk in m_ChunkLoadedSet) {
                // 読み込んでおくべき範囲から外れていたら破棄リクエストを行う
                if (!loadIdSet.Contains(chunk.ID)) {
                    DisposeChunk(chunk);
                    removeChunkList.Add(chunk);
                }
            }

            removeChunkList.ForEach(g => m_ChunkLoadedSet.Remove(g));

            // 破棄中リストを更新
            removeChunkList.Clear();
            foreach (var chunk in m_ChunkDisposingSet) {
                // 破棄完了ならプールへ戻す
                if (chunk.State == GrassChunkState.Disposed) {
                    m_ChunkPool.Release(chunk);
                    SetState(chunk.ID, GrassChunkState.Disposed);
                    removeChunkList.Add(chunk);
                }
            }

            removeChunkList.ForEach(g => m_ChunkDisposingSet.Remove(g));

            m_PreFlameIdSet.Clear();
            m_PreFlameIdSet = loadIdSet;

            // キューから読み込みリクエストを1つ取り出して処理する
            LoadChunk();
        }

        private GrassChunkState GetState(Vector2Int id) {
            if (m_ChunkStateDict.TryGetValue(id, out var state)) {
                return state;
            }

            m_ChunkStateDict.Add(id, GrassChunkState.None);
            return GrassChunkState.None;
        }

        private void SetState(Vector2Int id, GrassChunkState state) {
            if (m_ChunkStateDict.ContainsKey(id)) {
                m_ChunkStateDict[id] = state;
            } else {
                m_ChunkStateDict.Add(id, state);
            }
        }

        /// <summary>
        /// 読み込む区域をキューに詰める
        /// </summary>
        private void EnqueueLoadChunk(Vector2Int chunkID) {
            var state = GetState(chunkID);
            if (!(state == GrassChunkState.None || state == GrassChunkState.Disposed)) {
                return;
            }

            var grassGroup = m_ChunkPool.Get();
            grassGroup.SetID(chunkID);
            EnqueueLoadChunk(grassGroup);
        }

        /// <summary>
        /// 読み込む区域をキューに詰める
        /// </summary>
        private void EnqueueLoadChunk(GrassChunk grassChunk) {
            if (grassChunk == null) {
                return;
            }

            var state = GetState(grassChunk.ID);
            if (!(state == GrassChunkState.None || state == GrassChunkState.Disposed)) {
                return;
            }

            SetState(grassChunk.ID, GrassChunkState.Loading);
            m_LoadRequestQueue.Enqueue(grassChunk);
        }

        /// <summary>
        /// キューから区域を取り出して読み込む
        /// </summary>
        private void LoadChunk() {
            if (m_LoadRequestQueue.Count < 1) {
                return;
            }

            var grassGroup = m_LoadRequestQueue.Dequeue();
            var ct = gameObject.GetCancellationTokenOnDestroy();
            grassGroup.SetupAsync(m_ComputeShader, ct).Forget();
            m_ChunkLoadingSet.Add(grassGroup);
        }

        /// <summary>
        /// 区域を破棄する
        /// </summary>
        private void DisposeChunk(GrassChunk grassChunk) {
            if (grassChunk == null) {
                return;
            }

            var state = GetState(grassChunk.ID);
            if (state != GrassChunkState.Loaded) {
                return;
            }

            SetState(grassChunk.ID, GrassChunkState.Disposing);
            grassChunk.Dispose();
            m_ChunkDisposingSet.Add(grassChunk);
        }

#if UNITY_EDITOR
        [ContextMenu("草区域の自動生成")]
        private void GenerateGrassChunk() {
            Debug.Log("[GenerateGrassChunk] Start");

            var grassCount = Vector2Int.zero;
            grassCount.x = Mathf.FloorToInt(m_ChunkSize.x * m_GrassDensity);
            grassCount.y = Mathf.FloorToInt(m_ChunkSize.y * m_GrassDensity);
            var distance = Vector2.one / Mathf.Max(m_GrassDensity, Mathf.Epsilon);

            var multiplyOffset = Vector2.one * -0.5f;
            multiplyOffset.x *= m_ChunkCount.x;
            multiplyOffset.y *= m_ChunkCount.y;
            multiplyOffset += Vector2.one * 0.5f;

            for (var y = 0; y < m_ChunkCount.y; y++) {
                for (var x = 0; x < m_ChunkCount.x; x++) {
                    var xPos = (x + multiplyOffset.x) * m_ChunkSize.x + m_ChunkPositionOffset.x;
                    var zPos = (y + multiplyOffset.y) * m_ChunkSize.y + m_ChunkPositionOffset.y;
                    var id = new Vector2Int(x, y);
                    var pos = new Vector3(xPos, 0, zPos);
                    var generateData = GenerateGrassChunkData(pos, grassCount, distance);
                    GrassChunkDataUtility.SaveGrassChunk(id, generateData);
                }
            }

            Debug.Log("[GenerateGrassChunk] Complete");
        }

        /// <summary>
        /// 区域データを生成する
        /// </summary>
        private GrassChunkGenerateData GenerateGrassChunkData(Vector3 chunkPosition, Vector2Int grassCount,
            Vector2 grassDistance) {
            var grassDataList = new List<GrassData>();
            var minTemp = Vector3.one * float.MaxValue;
            var maxTemp = Vector3.one * float.MinValue;
            var minOffset = -Vector3.one;
            var maxOffset = Vector3.one;
            var chunkBoundsMin = minTemp;
            var chunkBoundsMax = maxTemp;

            // 草データを生成する
            for (var x = 0; x < grassCount.x; x++) {
                for (var y = 0; y < grassCount.y; y++) {
                    var pos = new Vector3(x - grassCount.x / 2f + 0.5f, 0, y - grassCount.y / 2f + 0.5f);
                    pos.x *= grassDistance.x;
                    pos.z *= grassDistance.y;
                    pos.x += Random.Range(-0.5f, 0.5f) * m_GrassPositionRandomRange.x;
                    pos.z += Random.Range(-0.5f, 0.5f) * m_GrassPositionRandomRange.y;
                    pos += chunkPosition;

                    var rayPosition = pos;
                    rayPosition.y = m_GenerateRaycastPositionY;
                    var ray = new Ray(rayPosition, Vector3.down);
                    if (!Physics.Raycast(ray, out var hitInfo, m_GenerateRaycastDistance)) {
                        continue;
                    }

                    // 7番レイヤーは草を生やさないためのものなので除外
                    if (hitInfo.collider.gameObject.layer == 7) {
                        continue;
                    }

                    pos.y = hitInfo.point.y;
                    var angleY = Random.Range(0, 360f);
                    var scaleXY = m_GrassScaleXYTable[Random.Range(0, m_GrassScaleXYTable.Length)];
                    var fadeDistance = Random.Range(0.01f, 0.99f);
                    grassDataList.Add(new GrassData {
                        Position = pos,
                        AngleY = angleY,
                        ScaleXY = scaleXY,
                        FadeDistance = fadeDistance,
                    });

                    // 草の生成のついでに区域のAABBの最小最大も求める
                    UpdateMinMax(ref chunkBoundsMin, ref chunkBoundsMax, pos, minOffset, maxOffset);
                }
            }

            // 分割領域の最小最大も求める
            var divisionOneSideCount = ConstParam.ChunkDivisionOneSideCount;
            var divisionCount = divisionOneSideCount * divisionOneSideCount;
            var divisionBoundsMin = Enumerable.Repeat(minTemp, divisionCount).ToArray();
            var divisionBoundsMax = Enumerable.Repeat(maxTemp, divisionCount).ToArray();
            for (var i = 0; i < grassDataList.Count; i++) {
                var grassData = grassDataList[i];
                var pos = grassData.Position;

                // 草が属する分割領域のIDを求める
                var divX = Mathf.InverseLerp(chunkBoundsMin.x, chunkBoundsMax.x, pos.x) * divisionOneSideCount;
                var divZ = Mathf.InverseLerp(chunkBoundsMin.z, chunkBoundsMax.z, pos.z) * divisionOneSideCount;
                var xID = Mathf.Min(Mathf.FloorToInt(divX), divisionOneSideCount - 1);
                var zID = Mathf.Min(Mathf.FloorToInt(divZ), divisionOneSideCount - 1);
                var id = zID * divisionOneSideCount + xID;
                grassData.DivisionID = (uint) id;
                grassDataList[i] = grassData;

                UpdateMinMax(ref divisionBoundsMin[id], ref divisionBoundsMax[id], pos, minOffset, maxOffset);
            }

            return new GrassChunkGenerateData {
                GrassDataList = grassDataList,
                ChunkBoundsMin = chunkBoundsMin,
                ChunkBoundsMax = chunkBoundsMax,
                DivisionBoundsMinArray = divisionBoundsMin,
                DivisionBoundsMaxArray = divisionBoundsMax,
            };
        }

        private void UpdateMinMax(ref Vector3 minPos, ref Vector3 maxPos, Vector3 pos, Vector3 minOffset,
            Vector3 maxOffset) {
            minPos.x = Mathf.Min(minPos.x, pos.x + minOffset.x);
            minPos.y = Mathf.Min(minPos.y, pos.y + minOffset.y);
            minPos.z = Mathf.Min(minPos.z, pos.z + minOffset.z);
            maxPos.x = Mathf.Max(maxPos.x, pos.x + maxOffset.x);
            maxPos.y = Mathf.Max(maxPos.y, pos.y + maxOffset.y);
            maxPos.z = Mathf.Max(maxPos.z, pos.z + maxOffset.z);
        }
#endif
    }
}