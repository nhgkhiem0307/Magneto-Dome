// Bau troi chuyen mau cho Sky Arena.
//
// Vi sao khong dung skybox mac dinh cua Unity: skybox mac dinh la bau troi ban ngay
// rat "tai lieu huong dan", nhin ra ngay la project chua ai dung den phan hinh anh.
// Shader nay tao mot dai mau tu dinh troi xuong chan troi roi xuong vuc tham ben duoi,
// nen hon dao lo lung se co nen troi phia tren VA vuc toi phia duoi - dung boi canh game.
//
// Hoan toan thu tuc (procedural), khong can texture, khong can tai asset nao.
Shader "SkyArena/Gradient Sky"
{
    Properties
    {
        [Header(Mau bau troi)]
        _TopColor       ("Mau dinh troi", Color)            = (0.10, 0.28, 0.70, 1)
        _HorizonColor   ("Mau duong chan troi", Color)      = (0.70, 0.85, 0.98, 1)
        _BottomColor    ("Mau phia duoi (vuc tham)", Color) = (0.03, 0.04, 0.09, 1)

        _HorizonSharpness ("Do gat cua chan troi", Range(0.5, 8)) = 2.0
        _BottomSharpness  ("Do gat phia duoi", Range(0.5, 8))     = 2.0
        _Exposure         ("Do sang tong the", Range(0, 3))       = 1.0

        [Header(Mat troi)]
        // LUU Y: phai tu chinh cho khop voi huong Directional Light trong scene.
        // Cach nhanh: chon Directional Light, xem Rotation, roi chinh vector nay
        // cho toi khi dia mat troi nam dung cho anh sang chieu toi.
        _SunDirection ("Huong mat troi", Vector)      = (0.3, 0.45, -0.85, 0)
        _SunColor     ("Mau mat troi", Color)         = (1.0, 0.95, 0.82, 1)
        _SunSize      ("Kich thuoc dia", Range(0, 0.3))   = 0.04
        _SunGlow      ("Do tan cua quang", Range(1, 300)) = 40
        _SunIntensity ("Cuong do", Range(0, 5))           = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Background"
            "Queue"          = "Background"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType"    = "Skybox"
        }

        // Skybox ve sau cung va o vo cuc: khong ghi chieu sau, khong cull mat nao
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirOS      : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _TopColor;
                half4  _HorizonColor;
                half4  _BottomColor;
                float  _HorizonSharpness;
                float  _BottomSharpness;
                float  _Exposure;
                float4 _SunDirection;
                half4  _SunColor;
                float  _SunSize;
                float  _SunGlow;
                float  _SunIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Unity ve skybox bang mot khoi bao quanh camera, nen toa do object
                // CHINH LA huong nhin. Khong can tinh gi them.
                OUT.dirOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dirOS);
                float  h   = dir.y; // -1 = nhin thang xuong, +1 = nhin thang len

                // Nua tren: chan troi -> dinh troi
                float tUp  = pow(saturate(h), 1.0 / _HorizonSharpness);
                half3 upper = lerp(_HorizonColor.rgb, _TopColor.rgb, tUp);

                // Nua duoi: chan troi -> vuc tham
                float tDown = pow(saturate(-h), 1.0 / _BottomSharpness);
                half3 lower = lerp(_HorizonColor.rgb, _BottomColor.rgb, tDown);

                half3 col = (h > 0.0) ? upper : lower;

                // Dia mat troi + quang tan xung quanh
                float3 sunDir = normalize(_SunDirection.xyz);
                float  d      = saturate(dot(dir, sunDir));

                // smoothstep tao ria dia mem, khong bi rang cua
                float disk = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.35, d);
                float glow = pow(d, _SunGlow);

                col += _SunColor.rgb * (disk + glow * 0.6) * _SunIntensity;

                return half4(col * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
