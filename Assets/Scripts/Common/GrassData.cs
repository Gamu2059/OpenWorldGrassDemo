using System.Runtime.InteropServices;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Common {
    /// <summary>
    /// 保存、およびComputeShaderに渡す草のデータ
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GrassData {
        /// <summary>
        /// 草のワールド空間上の座標
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 草のワールド空間上のY軸角度
        /// </summary>
        public float AngleY;

        /// <summary>
        /// 草のワールド空間上のXY軸スケール (ZはXと共通)
        /// </summary>
        public Vector2 ScaleXY;

        /// <summary>
        /// 草のフェードディスタンスパラメータ
        /// </summary>
        public float FadeDistance;

        /// <summary>
        /// 草が属する分割領域のID
        /// </summary>
        public uint DivisionID;
    }
}