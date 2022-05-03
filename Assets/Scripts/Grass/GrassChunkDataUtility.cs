using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gamu2059.OpenWorldGrassDemo.Common;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Gamu2059.OpenWorldGrassDemo.Grass {
    /// <summary>
    /// 区域データを保存したり読み込んだりするユーティリティクラス
    /// </summary>
    public static class GrassChunkDataUtility {
        /// <summary>
        /// 区域データのAddressable名を取得する
        /// </summary>
        public static string GetAssetKey(Vector2Int chunkID) {
            return $"gid_{chunkID.x:0000}_{chunkID.y:0000}";
        }

        /// <summary>
        /// 区域データのバイナリファイル名を取得する
        /// </summary>
        public static string GetFileName(Vector2Int chunkID) {
            var fileName = $"{GetAssetKey(chunkID)}.bytes";
            return Path.Combine(Application.dataPath, "Addressables", fileName);
        }

        /// <summary>
        /// 指定したIDで区域データを保存する
        /// </summary>
        public static void SaveGrassChunk(Vector2Int chunkID, in GrassChunkGenerateData chunkGenerateData) {
            // バイナリのデータ構造は次の通り
            // 草データの個数
            // 分割領域の個数
            // 区域のAABBの最小値
            // 区域のAABBの最大値
            // 分割領域のAABBの最小値の配列
            // 分割領域のAABBの最大値の配列
            // 草データの配列

            var intByteSize = ConstParam.Int32ByteSize;
            var vector3ByteSize = ConstParam.Vector3ByteSize;
            var grassDataByteSize = ConstParam.GrassDataByteSize;
            var vector3Ptr = Marshal.AllocHGlobal(vector3ByteSize);
            var grassDataPtr = Marshal.AllocHGlobal(grassDataByteSize);
            var vector3Bytes = new byte[vector3ByteSize];
            var grassDataBytes = new byte[grassDataByteSize];

            var fileName = GetFileName(chunkID);
            using var stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            stream.SetLength(0);

            WriteInt(stream, chunkGenerateData.GrassDataList.Count, intByteSize);
            WriteInt(stream, chunkGenerateData.DivisionBoundsMinArray.Length, intByteSize);
            WriteVector3(stream, chunkGenerateData.ChunkBoundsMin, vector3ByteSize, vector3Ptr, vector3Bytes);
            WriteVector3(stream, chunkGenerateData.ChunkBoundsMax, vector3ByteSize, vector3Ptr, vector3Bytes);

            foreach (var minPos in chunkGenerateData.DivisionBoundsMinArray) {
                WriteVector3(stream, minPos, vector3ByteSize, vector3Ptr, vector3Bytes);
            }

            foreach (var maxPos in chunkGenerateData.DivisionBoundsMaxArray) {
                WriteVector3(stream, maxPos, vector3ByteSize, vector3Ptr, vector3Bytes);
            }

            foreach (var grassData in chunkGenerateData.GrassDataList) {
                WriteGrassData(stream, grassData, grassDataByteSize, grassDataPtr, grassDataBytes);
            }
        }

        /// <summary>
        /// 指定したIDの区域データを読み出す
        /// </summary>
        public static async UniTask<GrassChunkGenerateData> LoadGrassChunkAsync(Vector2Int chunkID, CancellationToken ct) {
            // バイナリのデータ構造は次の通り
            // 草データの個数
            // 分割領域の個数
            // 区域のAABBの最小値
            // 区域のAABBの最大値
            // 分割領域のAABBの最小値の配列
            // 分割領域のAABBの最大値の配列
            // 草データの配列

            if (ct.IsCancellationRequested) {
                return null;
            }

            var vector3BufferPtr = IntPtr.Zero;
            var grassDataBufferPtr = IntPtr.Zero;

            try {
                var dataBytes = await LoadDataBytesAsync(chunkID, ct);
                if (dataBytes == null) {
                    return null;
                }

                var intByteSize = ConstParam.Int32ByteSize;
                var vector3ByteSize = ConstParam.Vector3ByteSize;
                var grassDataByteSize = ConstParam.GrassDataByteSize;

                vector3BufferPtr = Marshal.AllocHGlobal(vector3ByteSize);
                grassDataBufferPtr = Marshal.AllocHGlobal(grassDataByteSize);

                var offset = 0;
                var grassCount = LoadInt(dataBytes, intByteSize, ref offset);
                var divCount = LoadInt(dataBytes, intByteSize, ref offset);

                var minPos = LoadVector3(dataBytes, vector3ByteSize, ref vector3BufferPtr, ref offset);
                var maxPos = LoadVector3(dataBytes, vector3ByteSize, ref vector3BufferPtr, ref offset);

                var divMinPos = new Vector3[divCount];
                var divMaxPos = new Vector3[divCount];
                for (var i = 0; i < divCount; i++) {
                    divMinPos[i] = LoadVector3(dataBytes, vector3ByteSize, ref vector3BufferPtr, ref offset);
                }

                for (var i = 0; i < divCount; i++) {
                    divMaxPos[i] = LoadVector3(dataBytes, vector3ByteSize, ref vector3BufferPtr, ref offset);
                }

                var grassData = new GrassData[grassCount];
                for (var i = 0; i < grassCount; i++) {
                    grassData[i] = LoadGrassData(dataBytes, grassDataByteSize, ref grassDataBufferPtr, ref offset);
                }

                return new GrassChunkGenerateData {
                    GrassDataList = new List<GrassData>(grassData),
                    ChunkBoundsMin = minPos,
                    ChunkBoundsMax = maxPos,
                    DivisionBoundsMinArray = divMinPos,
                    DivisionBoundsMaxArray = divMaxPos,
                };
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                Marshal.FreeHGlobal(vector3BufferPtr);
                Marshal.FreeHGlobal(grassDataBufferPtr);
            }

            return null;
        }

        /// <summary>
        /// 指定したIDの区域のバイナリファイルからバイト配列を読み出す
        /// </summary>
        private static async UniTask<byte[]> LoadDataBytesAsync(Vector2Int chunkID, CancellationToken ct) {
            if (ct.IsCancellationRequested) {
                return null;
            }

#if UNITY_EDITOR
            var fileName = GetFileName(chunkID);
            return File.ReadAllBytes(fileName);
#else
            var key = GetAssetKey(chunkID);
            var file = await Addressables.LoadAssetAsync<TextAsset>(key);
            return file.bytes;
#endif
        }
        
        /// <summary>
        /// Intをバイト配列にして書き込む
        /// </summary>
        private static void WriteInt(in FileStream stream, in int value, in int byteSize) {
            var bytes = BitConverter.GetBytes(value);
            // 第2引数のoffsetと第3引数のcountはbyte配列に対するもので、ファイルに対するものではない
            stream.Write(bytes, 0, byteSize);
        }

        /// <summary>
        /// Vector3をバイト配列にして書き込む
        /// </summary>
        private static void WriteVector3(in FileStream stream, in Vector3 value, in int byteSize, in IntPtr tempPtr,
            in byte[] tempBuffer) {
            Marshal.StructureToPtr(value, tempPtr, false);
            Marshal.Copy(tempPtr, tempBuffer, 0, byteSize);
            stream.Write(tempBuffer, 0, byteSize);
        }

        /// <summary>
        /// GrassDataをバイト配列にして書き込む
        /// </summary>
        private static void WriteGrassData(in FileStream stream, in GrassData value, in int byteSize, in IntPtr tempPtr,
            in byte[] tempBuffer) {
            Marshal.StructureToPtr(value, tempPtr, false);
            Marshal.Copy(tempPtr, tempBuffer, 0, byteSize);
            stream.Write(tempBuffer, 0, byteSize);
        }

        /// <summary>
        /// バイト配列からIntを読み出す
        /// </summary>
        private static int LoadInt(in byte[] bytes, in int byteSize, ref int offset) {
            var value = BitConverter.ToInt32(bytes, offset);
            offset += byteSize;
            return value;
        }

        /// <summary>
        /// バイト配列からVector3を読み出す
        /// </summary>
        private static Vector3 LoadVector3(in byte[] bytes, in int byteSize, ref IntPtr buffer, ref int offset) {
            Marshal.Copy(bytes, offset, buffer, byteSize);
            offset += byteSize;
            return Marshal.PtrToStructure<Vector3>(buffer);
        }

        /// <summary>
        /// バイト配列からGrassDataを読み出す
        /// </summary>
        private static GrassData LoadGrassData(in byte[] bytes, in int byteSize, ref IntPtr buffer, ref int offset) {
            Marshal.Copy(bytes, offset, buffer, byteSize);
            offset += byteSize;
            return Marshal.PtrToStructure<GrassData>(buffer);
        }
    }
}