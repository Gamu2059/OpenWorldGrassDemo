using UnityEngine;

namespace Gamu2059.OpenWorldGrassDemo.Common {
    public static class ShaderPropertyID {
        public static readonly int GrassDataID = Shader.PropertyToID("_GrassData");
        public static readonly int GrassObjToWorldID = Shader.PropertyToID("_GrassObjToWorld");

        public static readonly int FrustumPlanesID = Shader.PropertyToID("_FrustumPlanes");
        public static readonly int CullBoundsMinID = Shader.PropertyToID("_CullBoundsMin");
        public static readonly int CullBoundsMaxID = Shader.PropertyToID("_CullBoundsMax");
        public static readonly int CullBoundsResultID = Shader.PropertyToID("_CullBoundsResult");

        public static readonly int NearGrassIndexesID = Shader.PropertyToID("_NearGrassIndexes");
        public static readonly int MidGrassIndexesID = Shader.PropertyToID("_MidGrassIndexes");
        public static readonly int GrassIndexesID = Shader.PropertyToID("_GrassIndexes");

        public static readonly int CameraPosID = Shader.PropertyToID("_CameraPos");
        public static readonly int DistanceCullMinID = Shader.PropertyToID("_DistanceCullMin");
        public static readonly int DistanceCullMaxID = Shader.PropertyToID("_DistanceCullMax");
        public static readonly int LODThresholdID = Shader.PropertyToID("_LODThreshold");
        
        public static readonly int PlayerPosID = Shader.PropertyToID("_PlayerPos");
        public static readonly int PlayerRadiusParamID = Shader.PropertyToID("_PlayerRadiusParam");
        
        public static readonly int WindDirId = Shader.PropertyToID("_WindDir");
        public static readonly int WindFreqId = Shader.PropertyToID("_WindFreq");
        public static readonly int WindNoisePointOffsetId = Shader.PropertyToID("_WindNoisePointOffset");
        public static readonly int WindStrengthId = Shader.PropertyToID("_WindStrength");
        public static readonly int WindWaveScaleId = Shader.PropertyToID("_WindWaveScale");
    }
}