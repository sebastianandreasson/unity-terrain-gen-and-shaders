//
// Created by @Forkercat on 03/04/2021.
//
// A URP grass shader using compute shader rather than geometry shader.
// This file contains vertex and fragment functions. It also defines the
// structures which should be the same with the ones used in GrassCompute.compute.
//
// References & Credits:
// 1. GrassBlades.hlsl (https://gist.github.com/NedMakesGames/3e67fabe49e2e3363a657ef8a6a09838)
// 2. GrassGeometry.shader (https://pastebin.com/VQHj0Uuc)
//

// Make sure this file is not included twice
#ifndef GRASS_COMPUTE_INCLUDED
#define GRASS_COMPUTE_INCLUDED

// Include some helper functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// This describes a vertex on the generated mesh
struct DrawVertex
{
    float3 positionWS; // The position in world space
    float2 uv;
    float3 diffuseColor;
};

// A triangle on the generated mesh
struct DrawTriangle
{
    float3 normalOS;
    DrawVertex vertices[3]; // The three points on the triangle
};

// A buffer containing the generated mesh
StructuredBuffer<DrawTriangle> _DrawTriangles;

struct v2f
{
    float4 positionCS : SV_POSITION; // Position in clip space
    float2 uv : TEXCOORD0;          // The height of this vertex on the grass blade
    float3 positionWS : TEXCOORD1; // Position in world space
    float3 normalWS : TEXCOORD2;   // Normal vector in world space
    float3 diffuseColor : COLOR;
};

// Properties
float4 _TopColor;
float4 _BaseColor;
float _AmbientStrength;

float _FogStartDistance;
float _FogEndDistance;

// ----------------------------------------

// Vertex function

// -- retrieve data generated from compute shader
v2f vert(uint vertexID : SV_VertexID)
{
    // Initialize the output struct
    v2f output = (v2f)0;

    // Get the vertex from the buffer
    // Since the buffer is structured in triangles, we need to divide the vertexID by three
    // to get the triangle, and then modulo by 3 to get the vertex on the triangle
    DrawTriangle tri = _DrawTriangles[vertexID / 3];
    DrawVertex input = tri.vertices[vertexID % 3];

    output.positionCS = TransformWorldToHClip(input.positionWS);
    output.positionWS = input.positionWS;
    
    float3 faceNormal = GetMainLight().direction * tri.normalOS;
    output.normalWS = TransformObjectToWorldNormal(faceNormal, true);
    
    output.uv = input.uv;

    output.diffuseColor = input.diffuseColor;

    return output;
}

// ----------------------------------------

// Fragment function

half4 frag(v2f input) : SV_Target
{
    // For Shadow Caster Pass
#ifdef SHADERPASS_SHADOWCASTER
    return 0;
#else
    // For Color Pass
  
    float shadow = 0;
#if SHADOWS_SCREEN
    // Defines the color variable
    half4 shadowCoord = ComputeScreenPos(input.positionCS);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif  // SHADOWS_SCREEN

    // Get Light
    Light mainLight = GetMainLight(shadowCoord);
#ifdef _MAIN_LIGHT_SHADOWS
    shadow = mainLight.shadowAttenuation;
#endif

  
    float4 baseColor = lerp(_BaseColor, _TopColor, saturate(input.uv.y))
                          * float4(input.diffuseColor, 1);

    // Multiply with lighting color
    float4 litColor = baseColor * float4(mainLight.color, 1);
    
    // Multiply with vertex color, and shadows
    float4 final = litColor * shadow;

    // Add in base color when lights are turned down
    final += saturate((1 - shadow) * baseColor * 0.2);

    // Fog
    float distanceFromCamera = distance(_WorldSpaceCameraPos, input.positionWS);
    float fogFactor = (distanceFromCamera - _FogStartDistance) / (_FogEndDistance - _FogStartDistance);
    final.rgb = MixFog(final.rgb, 1 - saturate(fogFactor));

    // Add in ambient color
    final += UNITY_LIGHTMODEL_AMBIENT * _AmbientStrength;
    // final += half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) * _AmbientStrength;  // light color ambient
    // final += unity_AmbientSky * _AmbientStrength;  // skybox ambient

    return final;

#endif  // SHADERPASS_SHADOWCASTER
}

#endif  // GRASS_COMPUTE_INCLUDED
