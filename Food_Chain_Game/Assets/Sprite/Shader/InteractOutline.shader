// 상호작용 대상의 빛나는 테두리. 스프라이트의 모양(알파)만 쓰고 색은 SpriteRenderer.color로 칠한다.
// 조명을 받지 않아 어두운 밤에도 같은 밝기로 보인다.
// 원본 자리는 뚫어서(_SourceOffset, _SourceRect) 원본이 시야에 가려져도 면이 아닌 테두리만 보이게 한다
Shader "Food Chain/Interact Outline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _SourceOffset ("Source UV Offset", Vector) = (0,0,0,0)
        [HideInInspector] _SourceRect ("Source UV Rect", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SourceOffset;
                float4 _SourceRect;
            CBUFFER_END

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 sprite = CommonUnlitFragment(input, input.color);

                // 이 픽셀 자리에 있는 원본 스프라이트의 알파만큼 지운다. 원본 그림 영역 밖이면 지우지 않는다
                float2 sourceUV = input.uv + _SourceOffset.xy;
                if (all(sourceUV >= _SourceRect.xy) && all(sourceUV <= _SourceRect.zw))
                    sprite.a *= 1.0 - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sourceUV).a;

                return half4(input.color.rgb, sprite.a);
            }
            ENDHLSL
        }
    }
}
