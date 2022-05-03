using System.Collections.Generic;
using Gamu2059.OpenWorldGrassDemo.Common;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Grass {
    /// <summary>
    /// 草の区域の生成データ
    /// </summary>
    public class GrassChunkGenerateData {
        /// <summary>
        /// 区域内の全ての草のデータ
        /// </summary>
        public List<GrassData> GrassDataList;

        /// <summary>
        /// 区域のAABBの最小座標
        /// </summary>
        public Vector3 ChunkBoundsMin;

        /// <summary>
        /// 区域のAABBの最大座標
        /// </summary>
        public Vector3 ChunkBoundsMax;

        /// <summary>
        /// 分割領域のAABBの最小座標の配列
        /// </summary>
        public Vector3[] DivisionBoundsMinArray;

        /// <summary>
        /// 分割領域のAABBの最大座標の配列
        /// </summary>
        public Vector3[] DivisionBoundsMaxArray;
    }
}