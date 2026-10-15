// Tuong luc giac cua Loi Tuong Dien Tu (EM Barrier Core).
//
// Mot shader, ba che do (_Mode) - de ca bo hinh chi phu thuoc DUY NHAT file nay,
// khong can model hay texture nao. Nam trong Resources/ nen chac chan vao ban build.
//   0 = Tam khien: luoi o luc giac, cong sang (additive).
//   1 = Loi kim loai duoi dat: dac, toi mau, co vong sang tren mat.
//   2 = Vet sang tren mat dat quanh loi: hoa van luc giac mo dan ra ngoai.
//
// Tat ca toa do o che do 0 tinh bang MET tren mat tuong (UV0 = met, goc o mep duoi trai),
// nen doi kich thuoc tuong khong lam meo o luc giac.
//
// Vien ngoai lom chom theo hinh o: o nao co TAM nam ngoai khung thi bo ca o. Nho vay
// mep tuong la mot chuoi o luc giac nguyen ven chu khong bi cat thang nhu mieng kinh.
Shader "SkyArena/Hex Shield"
{
    Properties
    {
        _Mode       ("Che do (0 khien, 1 loi, 2 vet dat)", Float) = 0

        [Header(Mau)]
        [HDR] _Color     ("Mau than", Color)     = (0.45, 0.85, 1.0, 1)
        [HDR] _EdgeColor ("Mau duong vien", Color) = (0.6, 0.93, 1.0, 1)
        _TintColor ("Mau nhuom long o", Color)  = (0.30, 0.70, 0.95, 1)
        _TintAlpha  ("Do nhuom long o", Range(0, 1)) = 0.16
        _Intensity  ("Cuong do tong", Range(0, 4)) = 1.6

        [Header(Luoi luc giac)]
        _Size       ("Kich thuoc tam khien (met)", Vector) = (4, 3, 0, 0)
        _HexSize    ("Be ngang moi o (met)", Range(0.1, 1.5)) = 0.5
        _LineWidth  ("Do day duong vien o", Range(0.005, 0.2)) = 0.028
        _Fill       ("Do sang long o", Range(0, 1)) = 0.04

        [Header(Trang thai do script dieu khien)]
        _Reveal     ("Lan rong tu loi (0-1)", Range(0, 1)) = 1
        _Flicker    ("Nhap nhay (0-1)", Range(0, 1)) = 1
        _CorePos    ("Vi tri loi tren mat khien (met)", Vector) = (2, -0.3, 0, 0)
        _HitPos     ("Cho trung don (met)", Vector) = (0, 0, 0, 0)
        _HitAge     ("Giay tu luc trung", Float) = 99

        [HideInInspector] _SrcBlend ("", Float) = 5
        [HideInInspector] _DstBlend ("", Float) = 1
        [HideInInspector] _ZWrite   ("", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // Khien va vet dat dung "premultiplied alpha" (One, OneMinusSrcAlpha): mot lan ve
        // vua NHUOM mau canh phia sau (phan alpha) vua PHAT SANG (phan rgb vuot alpha).
        // Chi cong sang thuan thi tren nen troi ban ngay ca tam bi bech thanh trang.
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull Off

        Pass
        {
            Name "HexShield"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float3 normalOS   : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _Mode;
                half4  _Color;
                half4  _EdgeColor;
                half4  _TintColor;
                float  _TintAlpha;
                float  _Intensity;
                float4 _Size;
                float  _HexSize;
                float  _LineWidth;
                float  _Fill;
                float  _Reveal;
                float  _Flicker;
                float4 _CorePos;
                float4 _HitPos;
                float  _HitAge;
                float  _SrcBlend;
                float  _DstBlend;
                float  _ZWrite;
            CBUFFER_END

            // Ty le cua luoi luc giac dinh nhon (canh dung): ngang 1, doc can3.
            static const float2 HEX_S = float2(1.0, 1.7320508);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Khoang cach tu tam o toi canh, chuan hoa: tam = 0, canh = 0.5.
            float HexDist(float2 p)
            {
                p = abs(p);
                return max(dot(p, HEX_S * 0.5), p.x);
            }

            // Tra ve: xy = toa do trong o (tinh tu tam o), zw = toa do tam o (don vi o).
            float4 HexCell(float2 p)
            {
                float4 hc = floor(float4(p, p - float2(0.5, 1.0)) / HEX_S.xyxy) + 0.5;
                float4 h  = float4(p - hc.xy * HEX_S, p - (hc.zw + 0.5) * HEX_S);
                return dot(h.xy, h.xy) < dot(h.zw, h.zw)
                     ? float4(h.xy, p - h.xy)
                     : float4(h.zw, p - h.zw);
            }

            // Tam o (met) co nam trong khung tam khien khong.
            float InsidePanel(float2 centerM)
            {
                float2 d = abs(centerM - _Size.xy * 0.5) - _Size.xy * 0.5;
                return step(max(d.x, d.y), 0.0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.uv         = IN.uv;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalOS   = IN.normalOS;
                return OUT;
            }

            // ==================== 0. TAM KHIEN ====================
            half4 FragShield(Varyings IN)
            {
                float2 pM = IN.uv;                 // met tren mat tuong
                float2 p  = pM / _HexSize;         // don vi o

                float4 hc      = HexCell(p);
                float2 local   = hc.xy;
                float2 centerM = hc.zw * _HexSize;

                // O nao co tam nam ngoai khung thi bo nguyen o -> mep lom chom theo o.
                float inside = InsidePanel(centerM);
                if (inside < 0.5) return 0;

                float e  = HexDist(local);                       // 0 o tam, 0.5 o canh
                float aa = fwidth(e) + 1e-5;
                float lineMask  = smoothstep(0.5 - _LineWidth - aa, 0.5 - aa, e);
                float inner = pow(saturate(e * 2.0), 6.0);       // long o sang dan ra canh, nhu kinh

                // --- Vien ngoai: canh nao giap o da bi bo thi la duong bao, cho sang han ---
                // Tim o hang xom ben kia canh gan nhat: be goc cua 'local' ve boi so 60 do.
                float  ang   = atan2(local.y, local.x);
                float  snap  = round(ang / (PI / 3.0)) * (PI / 3.0);
                float2 nbM   = (hc.zw + float2(cos(snap), sin(snap))) * _HexSize;
                float  rim   = 1.0 - InsidePanel(nbM);
                float  outline = lineMask * rim;

                float rnd = Hash21(hc.zw);

                // --- Lan rong tu loi luc vua tha (va thu ve luc bien mat) ---
                float maxD  = length(max(abs(_CorePos.xy), abs(_Size.xy - _CorePos.xy))) + 0.6;
                float dCore = (distance(centerM, _CorePos.xy) + rnd * 0.35) / maxD;
                float shown = step(dCore, _Reveal);
                if (shown < 0.5) return 0;
                float front = smoothstep(0.12, 0.0, _Reveal - dCore) * step(_Reveal, 0.999);

                // --- Song dong ---
                float t = _Time.y;
                float sparkle = pow(saturate(sin(t * 0.9 + rnd * 61.0)), 24.0);       // o le chop nhe
                float bandY   = frac(t * 0.22) * (_Size.y + 2.0) - 1.0;                // dai sang chay len
                float band    = exp(-pow((pM.y - bandY) / 0.35, 2.0));
                float nearCore = exp(-distance(centerM, _CorePos.xy) * 1.4);           // gan loi sang hon

                // --- Gon song khi trung don ---
                float ringR  = _HitAge * 5.5;
                float hitD   = distance(centerM, _HitPos.xy);
                float ripple = exp(-pow((hitD - ringR) / 0.26, 2.0)) * saturate(1.0 - _HitAge / 0.9);
                float hitFlash = exp(-hitD * 2.5) * saturate(1.0 - _HitAge / 0.25);

                // --- Sang hon khi nhin xien, de tuong khong "bien mat" khi dung cheo ---
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float fres = pow(1.0 - abs(dot(N, V)), 3.0);

                float body = _Fill + inner * 0.13 + sparkle * 0.45 + band * 0.18
                           + nearCore * 0.25 + ripple * 0.55 + hitFlash * 0.7 + front * 1.3;
                float edge = lineMask * (0.36 + band * 0.5 + ripple * 1.1) + outline * 1.7;

                half3 glow = (_Color.rgb * body + _EdgeColor.rgb * edge)
                           * _Intensity * (1.0 + fres * 1.2) * _Flicker;

                // Long o nhuom xanh nhe canh phia sau, dam hon o mep o va khi nhin xien.
                float tint = saturate(_TintAlpha * (1.0 + inner * 0.8 + fres * 1.5 + ripple * 0.6)) * _Flicker;
                return half4(_TintColor.rgb * tint + glow, tint);
            }

            // ==================== 1. LOI KIM LOAI ====================
            half4 FragCore(Varyings IN)
            {
                float3 N = normalize(IN.normalWS);
                Light  L = GetMainLight();
                float  ndl = saturate(dot(N, L.direction)) * 0.75 + 0.25;
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  fres = pow(1.0 - saturate(dot(N, V)), 4.0);

                half3 metal = half3(0.055, 0.07, 0.10) * ndl * L.color + fres * 0.18;

                float3 p = IN.positionOS;
                float  r = length(p.xz) / max(_Size.z, 1e-3);   // _Size.z = ban kinh loi
                float  pulse = 0.75 + 0.25 * sin(_Time.y * 3.0);

                float glow = 0;
                if (IN.normalOS.y > 0.5)
                {
                    // Mat tren: mot vong sang + cham sang o tam
                    glow += smoothstep(0.06, 0.0, abs(r - 0.62)) * 1.4;
                    glow += smoothstep(0.22, 0.0, r) * 1.8 * pulse;
                }
                else if (IN.normalOS.y > -0.5)
                {
                    // Mat ben: mot khe sang chay ngang giua than
                    float h = p.y / max(_Size.w, 1e-3);          // _Size.w = chieu cao loi
                    glow += smoothstep(0.08, 0.0, abs(h - 0.55)) * 0.9 * pulse;
                }

                half3 col = metal + _Color.rgb * glow * _Intensity * _Flicker;
                return half4(col, 1.0);
            }

            // ==================== 2. VET SANG DUOI DAT ====================
            half4 FragGround(Varyings IN)
            {
                float2 uv = IN.uv * 2.0 - 1.0;                 // -1..1
                float  r  = length(uv);
                float  fall = saturate(1.0 - r);
                fall *= fall;

                float4 hc = HexCell(uv * 4.0);
                float  e  = HexDist(hc.xy);
                float  aa = fwidth(e) + 1e-5;
                float  lineMask = smoothstep(0.5 - 0.06 - aa, 0.5 - aa, e);

                float glow = fall * (0.12 + lineMask * 0.55) + exp(-r * 6.0) * 0.6;
                half3 col = _Color.rgb * glow * _Intensity * _Flicker * saturate(_Reveal * 1.5);
                return half4(col, 0.0);   // chi phat sang, khong nhuom dat
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_Mode < 0.5) return FragShield(IN);
                if (_Mode < 1.5) return FragCore(IN);
                return FragGround(IN);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
