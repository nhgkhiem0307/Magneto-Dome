// Tuong nang luong - dung cho rao chan Buy Phase (RoundBarrier) va EM Barrier Core.
//
// Vi sao khong di tim asset: RoundBarrier.cs chi bat/tat Collider.enabled va
// Renderer.enabled, nen "rao chan" chi can la MOT KHOI CUBE co material dep.
// Toan bo ve ngoai nam o shader nay, khong phu thuoc model hay texture nao.
//
// Ba lop hieu ung chong len nhau:
//   1. Vien sang (Fresnel) - sang ruc o cac canh nhin cheo, mo o giua. Day la thu
//      lam no trong giong truong luc chu khong giong tam kinh mau.
//   2. Luoi o vuong troi - cho thay no la vat the nhan tao, dang hoat dong.
//   3. Nhip dap - do sang len xuong theo thoi gian.
//
// Blend SrcAlpha One = cong sang (additive). Bat Bloom trong post-processing
// thi vien se toa sang thuc su. Khong co Bloom thi nhin kha nhat.
Shader "SkyArena/Force Field"
{
    Properties
    {
        [Header(Mau sac)]
        _Color    ("Mau than tuong", Color) = (0.15, 0.55, 1.0, 1)
        _RimColor ("Mau vien sang", Color)  = (0.55, 0.90, 1.0, 1)
        _RimPower ("Do gat cua vien", Range(0.5, 8)) = 2.5
        _RimBoost ("Cuong do vien", Range(0, 5))     = 1.6

        [Header(Luoi o vuong)]
        // Cang lon cang nhieu o, luoi cang day.
        // MEO: dat X va Y theo dung ti le kich thuoc buc tuong thi o moi vuong.
        // Vi du tuong scale (20, 6, 0.5) thi dat tiling (20, 6) la vua.
        _GridTiling ("Mat do luoi (X, Y)", Vector)          = (16, 16, 0, 0)
        _GridWidth  ("Do day duong luoi", Range(0.002, 0.3)) = 0.07
        _GridBoost  ("Cuong do luoi", Range(0, 5))           = 1.0
        _ScrollSpeed("Toc do troi luoi (X, Y)", Vector)      = (0, 0.12, 0, 0)

        [Header(Nhip dap)]
        _PulseSpeed  ("Toc do nhip", Range(0, 10))   = 1.5
        _PulseAmount ("Bien do nhip", Range(0, 1))   = 0.2

        [Header(Tong the)]
        _Alpha    ("Do dam tong the", Range(0, 3)) = 1.0
        _BaseFill ("Do mo cua phan giua", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        // Cong sang, khong ghi chieu sau, ve ca hai mat (dung trong ra ngoai deu thay)
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForceFieldUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _RimColor;
                float  _RimPower;
                float  _RimBoost;
                float4 _GridTiling;
                float  _GridWidth;
                float  _GridBoost;
                float4 _ScrollSpeed;
                float  _PulseSpeed;
                float  _PulseAmount;
                float  _Alpha;
                float  _BaseFill;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.uv         = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Mat sau (Cull Off) co phap tuyen quay nguoc - lat lai cho vien van dung
                if (dot(N, V) < 0.0) N = -N;

                // --- 1. VIEN SANG (Fresnel) ---
                // Nhin thang vao mat phang -> dot gan 1 -> vien mo.
                // Nhin cheo -> dot gan 0 -> vien sang ruc. Dung dac trung truong luc.
                float fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);

                // --- 2. LUOI O VUONG TROI ---
                float2 uv = IN.uv * _GridTiling.xy + _ScrollSpeed.xy * _Time.y;

                // frac() cho toa do trong tung o; lay khoang cach toi canh gan nhat
                float2 cell = abs(frac(uv) - 0.5);
                float  edge = max(cell.x, cell.y);

                // fwidth chong rang cua khi nhin tu xa hoac goc cheo
                float aa   = fwidth(edge) + 1e-5;
                float grid = smoothstep(0.5 - _GridWidth - aa, 0.5 - _GridWidth + aa, edge);

                // --- 3. NHIP DAP ---
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed * 6.2831) * _PulseAmount;

                // --- GOP LAI ---
                half3 col = _Color.rgb * (grid * _GridBoost + _BaseFill)
                          + _RimColor.rgb * fresnel * _RimBoost;

                float a = saturate((grid * _GridBoost + _BaseFill + fresnel * _RimBoost))
                        * _Alpha * pulse;

                return half4(col * pulse, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
