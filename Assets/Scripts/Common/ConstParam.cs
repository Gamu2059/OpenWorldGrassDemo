using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Common {
    public static class ConstParam {
        public static readonly int ChunkDivisionOneSideCount = 16;
        public static readonly int FrustumPlaneCount = 6;
        
        public static readonly int GrassDataByteSize = Marshal.SizeOf<GrassData>();
        public static readonly int PlaneDataByteSize = Marshal.SizeOf<PlaneData>();
        public static readonly int Vector3ByteSize = Marshal.SizeOf<Vector3>();
        public static readonly int Matrix4x4ByteSize = Marshal.SizeOf<Matrix4x4>();
        public static readonly int Int32ByteSize = Marshal.SizeOf<Int32>();
        
        public static readonly int ConvertGrassMatrixIndex = 0;
        public static readonly int CullFrustumIndex = 1;
        public static readonly int ProcessGrassCullLODIndex = 2;
    }
}