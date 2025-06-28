Shader "URP/TriangleShader_GPUOnly"
{
    Properties { _ColorMultiplier("Color", Color) = (1,1,1,1) }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float3> _Vertices0;
            StructuredBuffer<float3> _Vertices1;
            StructuredBuffer<float3> _Vertices2;
            StructuredBuffer<float4> _Colors;
            StructuredBuffer<uint> _VisibleIndices;

            float4 _ColorMultiplier;

            struct Attributes
            {
                float3 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                uint triIdx = _VisibleIndices[input.instanceID];
                float3 p0 = _Vertices0[triIdx];
                float3 p1 = _Vertices1[triIdx];
                float3 p2 = _Vertices2[triIdx];

                float3 worldPos = p0;
                if (all(input.positionOS == float3(1, 0, 0))) worldPos = p1;
                else if (all(input.positionOS == float3(0, 1, 0))) worldPos = p2;

                Varyings o;
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.color = _Colors[triIdx] * _ColorMultiplier;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}
