// Sprite cộng sáng, dùng cho sóng âm Đông Sơn và mọi hiệu ứng của người chơi.
//
// Ràng buộc đọc hiểu thị giác ở CLAUDE.md mục 2.1 yêu cầu hiệu ứng của người chơi vẽ ở
// alpha thấp với chế độ additive và nằm dưới sorting layer của đòn địch. Additive làm các
// lớp sóng chồng nhau sáng dần thay vì đục dần, nên dù cuối ván màn hình phủ kín sóng thì
// đạn địch vẽ đặc vẫn nổi lên trên.
//
// Không dùng shader additive dựng sẵn của pipeline cũ: chúng không tương thích URP và sẽ
// hiện màu hồng.
Shader "LAC/SpriteAdditive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Khong khai bao _MainTex_ST: 2D SRP Batcher tat batching cho moi vat lieu co
            // thuoc tinh _ST hoac _TexelSize. Sprite khong can tiling nen bo di la duoc.
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
