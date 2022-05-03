#ifndef CUSTOM_GRASS_CG_INCLUDED
#define CUSTOM_GRASS_CG_INCLUDED

#include "CustomLightingCG.cginc"

struct GrassAttribute
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
};

struct GrassVarying
{
    float4 positionCS : SV_POSITION;
    float3 normalWS: TEXCOORD1;
    float4 shadowCoord : TEXCOORD2;
    half vAlpha : VECTOR0;
    half specular : VECTOR1;
};

struct WindParam
{
    float4 noise;
    half2 p0;
    half2 p1;
    half2 p2;
    half2 windBezier;
    float2 windWS;
};

half4 _Tint;

sampler2D _NoiseTex;
float4 _NoiseTex_ST;

half2 _BezierP1_P0;
half2 _BezierP1_P1;
half2 _BezierP1_P2;
half2 _BezierP2_P0;
half2 _BezierP2_P1;
half2 _BezierP2_P2;

// x:smoothness y:reflectance z:specularAddRate
float4 _MetallicParam;

float _LightBlendRate;

float2 _WindDir;
float2 _WindFreq;
float2 _WindNoisePointOffset;

float _WindStrength;
float _WindWaveScale;

float3 _PlayerPos;
float4 _PlayerRadiusParam;

StructuredBuffer<float4x4> _GrassObjToWorld;
StructuredBuffer<uint> _GrassIndexes;

float3 CustomTransformObjectToWorld(float3 positionOS, float4x4 objToWorld)
{
    return mul(objToWorld, float4(positionOS, 1.0)).xyz;
}

float3 CustomTransformObjectToWorldNormal(float3 normalOS, float4x4 objToWorld)
{
    return normalize(mul((float3x3)objToWorld, normalOS));
}

float CalcRand(float2 co)
{
    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float2 Calc2dBezier(float2 p0, float2 p1, float2 p2, float t)
{
    float invT = 1 - t;
    return invT * invT * p0 + 2 * t * invT * p1 + t * t * p2;
}

float2 CalcPlayerPosBending(float3 rootPositionWS, float t)
{
    // 根本のワールド座標を使ってXZ空間の距離と向きを求める
    float2 diffXZ = rootPositionWS.xz - _PlayerPos.xz;
    float distXZ = max(length(diffXZ), FLT_EPS);
    float2 dirXZ = diffXZ / distXZ;

    // プレイヤーの移動方向と内積を取って移動量に補正をかける
    float scaleRadius = saturate(dot(dirXZ, _PlayerRadiusParam.xy));
    float scaleRadius2 = scaleRadius * scaleRadius;
    float radius = _PlayerRadiusParam.z + _PlayerRadiusParam.w * scaleRadius2;

    // XZ空間の距離とPlayerPosRadiusの差分から、最大移動量を計算する
    float invY = 1 - t;
    float bendPower = (max(distXZ, radius) - distXZ) * (1 - invY * invY);
    return dirXZ * bendPower;
}

WindParam GetWindParam(float3 rootPositionWS, float t, float noiseOffset)
{
    WindParam o;

    float2 noiseUV = rootPositionWS.xz * -_WindFreq + _WindNoisePointOffset + noiseOffset;
    o.noise = tex2Dlod(_NoiseTex, float4(noiseUV, 0, 0));
    float windT = _WindWaveScale * (o.noise.r * 2 - 1) + _WindStrength;

    // 草のカーブを決定するベジェの制御点を計算
    o.p0 = 0;
    o.p1 = Calc2dBezier(_BezierP1_P0, _BezierP1_P1, _BezierP1_P2, windT);
    o.p2 = Calc2dBezier(_BezierP2_P0, _BezierP2_P1, _BezierP2_P2, windT);

    o.windBezier = Calc2dBezier(o.p0, o.p1, o.p2, t);

    // ノイズを[0,1]に補正して、風の向きにゆらぎを持たせる
    float2 windDirNoise = o.noise.gb + 0.5f;
    o.windWS = _WindDir * windDirNoise;

    return o;
}

float3 CalcNormalOSWithWind(WindParam windParam, float3 originNormalWS, float t)
{
    float2 bezier0 = Calc2dBezier(windParam.p0, windParam.p1, windParam.p2, t - 0.001);
    float2 bezier1 = Calc2dBezier(windParam.p0, windParam.p1, windParam.p2, t + 0.001);
    float2 diffBezier = normalize(bezier1 - bezier0);
    diffBezier.x *= -1;

    float3 normalOS = 0;
    normalOS.y = diffBezier.x * dot(originNormalWS.xz, _WindDir.xy) >= 0 ? 1 : -1;
    normalOS.z = diffBezier.y;

    return normalOS;
}

float CalcSpecular(float3 normalWS, float3 lightDir, float3 viewDir)
{
    float3 halfDir = normalize(lightDir + viewDir);
    float ndl = dot(normalWS, lightDir);
    float ndv = dot(normalWS, viewDir);
    float d = D_GGX(halfDir, normalWS, _MetallicParam.x);
    float f = F_Flesnel(viewDir, halfDir, _MetallicParam.y);
    float g = G_CookTorrance(lightDir, viewDir, halfDir, normalWS);
    return (d * f * g) / (4 * ndv * ndl + FLT_EPS) * _MetallicParam.z;
}

#endif
