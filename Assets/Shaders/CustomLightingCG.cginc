// Reference https://qiita.com/edo_m18/items/636ee3e54e8cc72b1241#ggxtrowbridge-reitz

#ifndef CUSTOM_LIGHTING_CG_INCLUDED
#define CUSTOM_LIGHTING_CG_INCLUDED

#ifndef FLT_EPS
#define FLT_EPS 5.960464478e-8
#endif

#ifndef PI
#define PI 3.14159265358979323846
#endif

float D_GGX(float3 halfDir, float3 normalDir, float smoothness)
{
    float ndh = dot(halfDir, normalDir);
    float roughness = 1 - saturate(smoothness);
    float alpha = roughness * roughness;
    float alpha2 = alpha * alpha;
    float t = ((ndh * ndh) * (alpha2 - 1) + 1);
    return alpha2 / (PI * t * t);
}

float F_Flesnel(float3 viewDir, float3 halfDir, float reflectance)
{
    float vdh = saturate(dot(viewDir, halfDir));
    float f0 = saturate(reflectance);
    float f = pow(1 - vdh, 5);
    f *= (1 - f0);
    f += f0;
    return f;
}

float G_CookTorrance(float3 lightDir, float3 viewDir, float3 halfDir, float3 normalDir)
{
    float ndh = saturate(dot(normalDir, halfDir));
    float ndl = saturate(dot(normalDir, lightDir));
    float ndv = saturate(dot(normalDir, viewDir));
    float vdh = saturate(dot(viewDir, halfDir));

    float nh2 = 2 * ndh;
    float g1 = (nh2 * ndv) / vdh;
    float g2 = (nh2 * ndl) / vdh;
    return min(1, min(g1, g2));
}

#endif
