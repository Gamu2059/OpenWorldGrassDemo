Shader "Custom/Grass-forBiRP"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)

        [Space()]

        _NoiseTex ("Noise", 2D) = "white" {}

        [Space()]

        _BezierP1_P0("Bezier P1 P0", Vector) = (0,0,0,0)
        _BezierP1_P1("Bezier P1 P1", Vector) = (0,0,0,0)
        _BezierP1_P2("Bezier P1 P2", Vector) = (0,0,0,0)

        [Space()]

        _BezierP2_P0("Bezier P2 P0", Vector) = (0,0,0,0)
        _BezierP2_P1("Bezier P2 P1", Vector) = (0,0,0,0)
        _BezierP2_P2("Bezier P2 P2", Vector) = (0,0,0,0)

        [Space()]

        // x:smoothness y:reflectance z:specularAddRate
        _MetallicParam ("Metallic Param", Vector) = (0,0,0,0)
        _LightBlendRate ("Light Blend Rate", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityStandardCore.cginc"
    #include "CustomGrassCG.cginc"

    float CalcSpecular(float3 positionWS, float3 normalWS)
    {
        UnityLight light = MainLight();
        float3 lightDir = normalize(light.dir);
        float3 viewDir = normalize(UnityWorldSpaceViewDir(positionWS));
        return CalcSpecular(normalWS, lightDir, viewDir);
    }

    GrassVarying VertWithInteractive(GrassAttribute i, uint instanceID : SV_InstanceID)
    {
        uint grassID = _GrassIndexes[instanceID];
        float4x4 objToWorld = _GrassObjToWorld[grassID];

        // 草の根元の座標
        float3 rootPositionWS = CustomTransformObjectToWorld(0, objToWorld);
        // 草の向きと風の向きが同じ場合は法線を逆向きにしないといけないので、最初に法線を取っておく
        float3 originNormalWS = CustomTransformObjectToWorldNormal(i.normal, objToWorld);
        // 草の根元から先にかけて[0,1]の範囲を取る値
        float t = i.color.a;

        WindParam windParam = GetWindParam(rootPositionWS, t, grassID);

        // 頂点補正
        i.vertex.y = windParam.windBezier.y;
        // 法線補正
        i.normal = CalcNormalOSWithWind(windParam, originNormalWS, t);

        float3 positionWS = CustomTransformObjectToWorld(i.vertex.xyz, objToWorld);
        positionWS.xz += windParam.windBezier.x * windParam.windWS;
        float3 normalWS = CustomTransformObjectToWorldNormal(i.normal, objToWorld);

        positionWS.xz += CalcPlayerPosBending(rootPositionWS, i.color.a);

        float specular = CalcSpecular(positionWS, normalWS);

        GrassVarying o;
        o.positionCS = UnityWorldToClipPos(positionWS);
        o.normalWS = normalWS;
        o.vAlpha = i.color.a;
        o.specular = specular * i.color.a * i.color.a * i.color.a;
        o.shadowCoord = 0;
        return o;
    }

    GrassVarying Vert(GrassAttribute i, uint instanceID : SV_InstanceID)
    {
        uint grassID = _GrassIndexes[instanceID];
        float4x4 objToWorld = _GrassObjToWorld[grassID];

        // 草の根元の座標
        float3 rootPositionWS = CustomTransformObjectToWorld(0, objToWorld);
        // 草の向きと風の向きが同じ場合は法線を逆向きにしないといけないので、最初に法線を取っておく
        float3 originNormalWS = CustomTransformObjectToWorldNormal(i.normal, objToWorld);
        // 草の根元から先にかけて[0,1]の範囲を取る値
        float t = i.color.a;

        WindParam windParam = GetWindParam(rootPositionWS, t, grassID);

        // 頂点補正
        i.vertex.y = windParam.windBezier.y;
        // 法線補正
        i.normal = CalcNormalOSWithWind(windParam, originNormalWS, t);

        float3 positionWS = CustomTransformObjectToWorld(i.vertex.xyz, objToWorld);
        positionWS.xz += windParam.windBezier.x * windParam.windWS;
        float3 normalWS = CustomTransformObjectToWorldNormal(i.normal, objToWorld);

        float specular = CalcSpecular(positionWS, normalWS);

        GrassVarying o;
        o.positionCS = UnityWorldToClipPos(positionWS);
        o.normalWS = normalWS;
        o.vAlpha = i.color.a;
        o.specular = specular * i.color.a * i.color.a * i.color.a;
        o.shadowCoord = 0;
        return o;
    }

    half4 Frag(GrassVarying i, half facing : VFACE) : SV_Target
    {
        UnityLight light = MainLight();

        // 表と裏で法線の向きを補正
        float3 n = normalize(i.normalWS);
        n = facing > 0 ? n : -n;
        float ndl = dot(n, light.dir);

        // half-lambert
        float halfNdl = abs(ndl) * 0.5f + 0.5f;
        half3 baseColor = _Tint * halfNdl;

        half3 lightColor = light.color;
        float lightRate = i.vAlpha * i.vAlpha * i.vAlpha * saturate(-ndl) * _LightBlendRate;

        half3 specularColor = 2 * lightColor * i.specular;
        baseColor = lerp(baseColor, lightColor, lightRate) + specularColor;
        return half4(baseColor, 1);
    }
    ENDCG

    SubShader
    {
        LOD 100
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
        }

        Blend One Zero
        Cull Off
        ZTest LEqual
        ZWrite On

        Pass
        {
            Name "DrawGrass with Interactive"

            CGPROGRAM
            #pragma vertex VertWithInteractive
            #pragma fragment Frag
            ENDCG
        }

        Pass
        {
            Name "DrawGrass"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDCG
        }
    }
}