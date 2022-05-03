using System.Runtime.InteropServices;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Common {
    /// <summary>
    /// ComputeShaderに渡す平面のデータ
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PlaneData {
        /// <summary>
        /// 平面の法線
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// ワールド空間上の原点からの最短距離
        /// </summary>
        public float Distance;
    }
}