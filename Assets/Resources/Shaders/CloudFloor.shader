// Day bien may - lop may mem trai toi chan troi, nam DUOI cac cum may cua CloudBank (17/10).
//
// Vi sao can: bien may chi la mot vanh cum may. Nhin ra / nhin xuong tu mep dao la thay cac
// khe giua cum may lo ra mau troi phang li - lo ngay "bien may het o day". Lop day nay lap
// khoang trong do bang may mem, roi TAN dan vao mau chan troi o rat xa -> cam giac bien may
// keo dai vo tan, khong co mep.
//
// Mot tam phang duy nhat, mot lan ve, nhieu tinh bang toan hoc (khong texture). Ve TRUOC cac
// cum may (Queue Transparent-10) nen cum may luon nam tren.
// Toan bo thong so do CloudBank.cs gan luc chay - khong can keo tha gi.
Shader "SkyArena/Cloud Floor"
{
    Properties
    {
        _ColorLow   ("Mau cho may mong", Color)  = (0.56, 0.85, 0.96, 1)
        _ColorHigh  ("Mau cho may day", Color)   = (0.90, 0.94, 0.99, 1)
        _Horizon    ("Mau chan troi", Color)     = (0.72, 0.88, 0.98, 1)
        _NoiseScale ("Co cum may (1/met)", Float) = 0.012
        _Opacity    ("Do dac gan", Range(0, 1))  = 0.96
        _FadeStart  ("Bat dau tan (met tu camera)", Float) = 250
        _FadeEnd    ("Tan het (met tu camera)", Float)     = 1600
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent-10"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CloudFloor"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorLow;
                half4 _ColorHigh;
                half4 _Horizon;
                float _NoiseScale;
                float _Opacity;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0, amp = 0.55;
                for (int k = 0; k < 4; k++) { v += ValueNoise(p) * amp; p = p * 2.07 + 13.7; amp *= 0.5; }
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 P = IN.positionWS;
                float dist = length(P.xz - _WorldSpaceCameraPos.xz);

                // Cum may lon troi rat cham. Hai tang: cum to (bo cuc) + chi tiet (vien bong)
                float2 q = P.xz * _NoiseScale + _Time.y * float2(0.004, 0.0025);
                float big = Fbm(q * 0.35);
                float n   = Fbm(q + big * 1.5);
                float puff = smoothstep(0.36, 0.72, n * 0.65 + big * 0.45);

                half3 col = lerp(_ColorLow.rgb, _ColorHigh.rgb, puff);

                // Cang xa cang hoa vao mau chan troi (giong khong khi), roi tan han. Mau phai doi
                // XONG truoc khi tan het, khong thi rim xa con trang -> lo duong ke ngang voi troi.
                float far = smoothstep(_FadeStart * 0.25, _FadeEnd * 0.7, dist);
                col = lerp(col, _Horizon.rgb, far);
                float a = _Opacity * (1.0 - smoothstep(_FadeStart, _FadeEnd, dist));
                return half4(col, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
