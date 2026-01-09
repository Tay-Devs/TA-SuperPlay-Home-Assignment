Shader "UI/DiscoBall_Sphere_Spin_URP"
{
    Properties
{
    [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)

    [Vector2] _StripCount ("Strip Count (X,Y)", Vector) = (8,4,0,0)
    [Vector2] _TextureOffset ("Texture Offset", Vector) = (0,0,0,0)

    _SpinSpeed ("Spin Speed", Float) = 0.2

    _SphereStrength ("Sphere Distortion", Range(0,1)) = 1
    _EdgeSoftness ("Edge Softness", Range(0,0.05)) = 0.01
}

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float2 _StripCount;
            float2 _TextureOffset;
            float  _SpinSpeed;
            float  _SphereStrength;
            float  _EdgeSoftness;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color * _Color;
                OUT.uv = IN.uv; // 🚨 never tile here
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // ----- Sphere space -----
                float2 sphereUV = IN.uv * 2.0 - 1.0;
                float radius = length(sphereUV);

                float alphaMask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, radius);
                if (alphaMask <= 0.001)
                    discard;

                float z = sqrt(saturate(1.0 - radius * radius));
                float2 projectedUV = sphereUV / (z + 1.0);
                projectedUV = lerp(sphereUV, projectedUV, _SphereStrength);

                // Back to 0–1
                projectedUV = projectedUV * 0.5 + 0.5;

                // ----- Spin (longitude roll) -----
                projectedUV.x += _Time.y * _SpinSpeed;

                // ----- Strip tiling -----
                projectedUV = projectedUV * _StripCount + _TextureOffset;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, projectedUV);
                col *= IN.color;
                col.a *= alphaMask;

                return col;
            }
            ENDHLSL
        }
    }
}
