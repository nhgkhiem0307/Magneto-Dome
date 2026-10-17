// Man chan pha mua do (RoundBarrier) - lam lai 17/10.
//
// Thay cho "SkyArena/Force Field" (luoi o vuong trang, nhin ro hai dau tuong cut giua troi).
//
// HAI YEU CAU CUA CHU PROJECT:
//   1. CHE KIN phia ben kia - truoc khi vao tran, hai doi KHONG duoc thay vi tri cua nhau.
//      => than tuong la mot lop khoi nang luong gan nhu dac (_HazeAlpha ~0.9) theo mau doi,
//         luoi luc giac mong noi len tren. Khong con "lo" de nhin xuyen.
//   2. KHONG BAO GIO THAY MEP / GOC cua khoi tuong 200m.
//      => tuong chi tan dan BEN NGOAI MEP DAO (tinh tu tam dao, _FadeStart -> _FadeEnd) va
//         theo do cao. Moi duong nhin tu can cu nay sang can cu kia deu cat tuong o cho NAM
//         TREN DAO (dao gan tron, doan noi hai diem tren dao luon nam trong dao) - tuc la cho
//         dac. Con hai dau tuong o tit ngoai khoang trong, da tan vao troi.
//      (Ban dau tan theo khoang cach toi CAMERA - nhung nhu vay nhin xien doc tuong thi phan
//       xa trong suot, nhin xuyen duoc sang can cu dich. Da bo.)
//
// Moi toa do tinh bang MET trong the gioi (khong dung UV) - cube keo 200 x 200 x 1 ma o van deu.
//
// Script RoundBarrier dieu khien: _Color (mau doi), _BaseY (mat dat), _FadeCenter (tam dao),
// _Dissolve (tan ra khi vao tran), _Hit0.._Hit3 (gon song: xyz = vi tri, w = thoi diem).
Shader "SkyArena/Barrier Curtain"
{
    Properties
    {
        [Header(Mau)]
        [HDR] _Color     ("Mau doi", Color)       = (1.0, 0.30, 0.22, 1)
        [HDR] _EdgeColor ("Mau loe sang", Color)  = (1.0, 0.85, 0.75, 1)
        _Intensity       ("Cuong do tong", Range(0, 4)) = 1.0

        [Header(Lop khoi che tam nhin)]
        _HazeAlpha ("Do dac cua man (0.99 khong nhin xuyen)", Range(0, 1)) = 0.99
        _HazeDark  ("Do sang cho khoi mong", Range(0, 1)) = 0.045

        [Header(Luoi luc giac)]
        _HexSize   ("Be ngang moi o (met)", Range(0.2, 3)) = 0.55
        _LineWidth ("Do day duong vien o", Range(0.005, 0.2)) = 0.03
        _CellBright ("Moi o lech sang toi (0 = dong mau)", Range(0, 1)) = 0.4
        _CellHue    ("Moi o lech sac (radian)", Range(0, 1)) = 0.2
        _Sparkle    ("Ti le o loe sang", Range(0, 0.3)) = 0.07

        [Header(Tan dan de khong bao gio thay mep)]
        _FadeCenter      ("Tam dao (x, z)", Vector)                    = (40, 40, 0, 0)
        _FadeStart       ("Bat dau tan, tinh tu tam dao (met)", Float) = 40
        _FadeEnd         ("Tan het, tinh tu tam dao (met)", Float)     = 85
        _BaseY           ("Do cao mat dat (met)", Float)               = 4.4
        _HeightFadeStart ("Bat dau tan theo do cao (met)", Float)      = 9
        _HeightFadeEnd   ("Tan het theo do cao (met)", Float)          = 22
        _DepthFadeStart  ("Bat dau chim vao may, duoi mat dao (met)", Float) = 6
        _DepthFadeEnd    ("Chim het vao may, duoi mat dao (met)", Float)     = 45
        _IslandRadius    ("Ban kinh dao cho vach sang chan tuong (met)", Float) = 36

        [Header(Script dieu khien)]
        _Dissolve ("Tan ra (0 nguyen, 1 bien mat)", Range(0, 1)) = 0
        _Hit0 ("Gon song 0", Vector) = (0, -999, 0, -99)
        _Hit1 ("Gon song 1", Vector) = (0, -999, 0, -99)
        _Hit2 ("Gon song 2", Vector) = (0, -999, 0, -99)
        _Hit3 ("Gon song 3", Vector) = (0, -999, 0, -99)
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

        // Premultiplied alpha: alpha CHE canh phia sau, rgb la mau man + phan phat sang.
        Blend One OneMinusSrcAlpha
        ZWrite Off
        // Cull Back, KHONG Cull Off: cube co hai mat lon cach nhau 1m, ve ca hai thi lop man dac
        // gap doi va hai luoi luc giac chong lech thanh van moire. Camera luon o ngoai khoi.
        Cull Back

        Pass
        {
            Name "BarrierCurtain"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _EdgeColor;
                float  _Intensity;
                float  _HazeAlpha;
                float  _HazeDark;
                float  _HexSize;
                float  _LineWidth;
                float  _CellBright;
                float  _CellHue;
                float  _Sparkle;
                float4 _FadeCenter;
                float  _FadeStart;
                float  _FadeEnd;
                float  _BaseY;
                float  _HeightFadeStart;
                float  _HeightFadeEnd;
                float  _DepthFadeStart;
                float  _DepthFadeEnd;
                float  _IslandRadius;
                float  _Dissolve;
                float4 _Hit0;
                float4 _Hit1;
                float4 _Hit2;
                float4 _Hit3;
            CBUFFER_END

            static const float2 HEX_S = float2(1.0, 1.7320508);

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Nhieu gia tri muot (value noise) + 3 tang cong don (fbm) cho lop khoi
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0, amp = 0.55;
                for (int k = 0; k < 3; k++) { v += ValueNoise(p) * amp; p = p * 2.03 + 17.1; amp *= 0.5; }
                return v;
            }

            // Xoay sac mau quanh truc xam (1,1,1) mot goc nho - doi sac ma giu nguyen do sang.
            float3 HueShift(float3 c, float angle)
            {
                const float3 k = float3(0.57735, 0.57735, 0.57735);
                float ca = cos(angle);
                return c * ca + cross(k, c) * sin(angle) + k * dot(k, c) * (1.0 - ca);
            }

            // Khoang cach tu tam o toi canh, chuan hoa: tam = 0, canh = 0.5.
            float HexDist(float2 p)
            {
                p = abs(p);
                return max(dot(p, HEX_S * 0.5), p.x);
            }

            // xy = toa do trong o (tu tam o), zw = toa do tam o.
            float4 HexCell(float2 p)
            {
                float4 hc = floor(float4(p, p - float2(0.5, 1.0)) / HEX_S.xyxy) + 0.5;
                float4 h  = float4(p - hc.xy * HEX_S, p - (hc.zw + 0.5) * HEX_S);
                return dot(h.xy, h.xy) < dot(h.zw, h.zw)
                     ? float4(h.xy, p - h.xy)
                     : float4(h.zw, p - h.zw);
            }

            // Mot vong gon song lan ra tu cho bi cham, tat dan trong 1.1 giay
            float Ripple(float3 posWS, float4 hit)
            {
                float age = _Time.y - hit.w;
                if (age < 0.0 || age > 1.1) return 0.0;
                float r = distance(posWS, hit.xyz);
                float ring = exp(-pow((r - age * 6.5) * 1.6, 2.0));
                float core = exp(-r * 1.4) * saturate(1.0 - age * 3.0);   // loe sang ngay cho cham
                return (ring + core) * (1.0 - age / 1.1);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 P = IN.positionWS;

                // Toa do tren mat tuong (met): truc ngang = truc X cua object, truc doc = Y the gioi.
                float3 tangent = TransformObjectToWorldDir(float3(1, 0, 0));   // da chuan hoa san
                float2 wallUV = float2(dot(P, tangent), P.y);
                float h = P.y - _BaseY;   // do cao so voi mat dat

                // --- TAN DAN: ngoai mep dao, len cao, va duoi mat dat ---
                float fromCenter = length(P.xz - _FadeCenter.xy);
                float sideFade   = 1.0 - smoothstep(_FadeStart, _FadeEnd, fromCenter);
                float heightFade = 1.0 - smoothstep(_HeightFadeStart, _HeightFadeEnd, h);
                // (17/10) Tuong chay tiep XUONG duoi vach dao roi chim dan vao bien may - dung mep dao
                // nhin xuong van thay man chan cam xuong may. Phan nam trong long dao thi dat tu che.
                float belowFade  = smoothstep(-_DepthFadeEnd, -_DepthFadeStart, h);
                float fade = sideFade * heightFade * belowFade;
                if (fade <= 0.001) discard;

                // --- LUOI LUC GIAC ---
                float4 hc   = HexCell(wallUV / _HexSize);
                float  d    = HexDist(hc.xy);
                float  aa   = fwidth(d) * 1.2 + 1e-4;
                float  edge = smoothstep(0.5 - _LineWidth - aa, 0.5 - _LineWidth + aa, d);
                float  rnd  = Hash21(hc.zw);
                float  rnd2 = Hash21(hc.zw + 17.0);
                float  rnd3 = Hash21(hc.zw + 41.0);

                // --- LOP KHOI NANG LUONG: troi cham tu duoi len, gon theo chieu doc ---
                float2 q = float2(wallUV.x * 0.16, h * 0.11 - _Time.y * 0.12);
                float smoke = Fbm(q + Fbm(q * 0.6 + _Time.y * 0.03) * 0.8);
                float streak = pow(saturate(Fbm(float2(wallUV.x * 0.55, h * 0.04 - _Time.y * 0.25))), 3.0);

                // Vet sang quet tu duoi len, cach nhau ~8m
                float s    = frac(h * 0.125 - _Time.y * 0.30);
                float band = exp(-pow((s - 0.5) * 10.0, 2.0));

                // Vach sang sat chan tuong: CHI dung ngang mat dat (ca tren lan duoi) va CHI trong pham
                // vi dao. Khong gioi han thi phan tuong duoi vuc sang ruc ca mang, hoac thanh mot vach
                // sang lo lung giua khoang trong ngoai dao.
                float onIsland = 1.0 - smoothstep(_IslandRadius - 4.0, _IslandRadius, fromCenter);
                float baseGlow = exp(-abs(h) * 5.0) * onIsland;

                // --- GON SONG KHI BI CHAM ---
                float ripple = Ripple(P, _Hit0) + Ripple(P, _Hit1) + Ripple(P, _Hit2) + Ripple(P, _Hit3);

                // --- TAN RA KHI VAO TRAN: tung o bien mat tu DUOI len, vien o sap tan loe sang ---
                float key   = saturate(h / 14.0) * 0.72 + rnd * 0.28;
                float alive = step(_Dissolve * 1.02, key);
                float burn  = alive * (1.0 - smoothstep(0.0, 0.09, key - _Dissolve * 1.02)) * step(0.001, _Dissolve);
                if (alive < 0.5 && baseGlow * (1.0 - _Dissolve) < 0.002) discard;

                // Mau than man: toi o cho khoi mong, sang o cho khoi day, cang gan dat cang sang
                // Mau DAM va SAU: cho khoi mong gan nhu toi, cho khoi day sang len - tuong phan ro
                // de nhin ra la khoi dang chay, khong phai mot tam nhua phang mau.
                float nearGround = exp(-max(h, 0.0) * 0.3);
                float body = smoothstep(0.25, 0.85, smoke);
                // Con so nho vi hau ky cua scene (Bloom + chinh mau) day sang len rat nhieu -
                // 0.45 nhin ra hong cam nhat, 0.2 moi ra do / xanh sam.
                // (17/10) MOI O LECH MAU MOT CHUT: sang toi +-20%, sac chi +-6 do quanh mau doi. Da thu
                // +-13 do: do bao hoa rat nhay, xoay vai do la co o ra mau o liu / hong tim nhu vai vun.
                // Lech SANG TOI moi la thu tao chieu sau ma khong doi mau doi. Duong vien o van
                // giu MOT mau (khung xuong cua tuong), chi long o la lech.
                half3 cellCol = HueShift(_Color.rgb, (rnd3 - 0.5) * _CellHue) * (1.0 + (rnd2 - 0.5) * _CellBright);
                half3 haze = cellCol * (lerp(_HazeDark, 0.20, body) + nearGround * 0.10)
                           + _EdgeColor.rgb * streak * 0.18;

                // Mot it o thinh thoang loe sang roi tat - nhu co dien chay qua to ong
                float sparkle = step(1.0 - _Sparkle, rnd2) * pow(saturate(sin(_Time.y * (0.6 + rnd3) + rnd * 6.2831)), 6.0);
                // Do dac gan nhu khong doi (0.94 - 1.0 x _HazeAlpha): cho mong nhat cung khong lo
                // bong nguoi phia sau - day la yeu cau che vi tri cua chu project.
                float hazeA = _HazeAlpha * (0.985 + 0.015 * body) * alive;

                float lines = edge * (0.30 + band * 0.9 + ripple * 1.6) * alive;

                half3 emit = cellCol * sparkle * 0.22 * alive * (1.0 - edge)
                           + _Color.rgb * (lines + band * 0.06 * alive + ripple * 0.25 * alive + baseGlow * 0.9 * (1.0 - _Dissolve))
                           + _EdgeColor.rgb * (burn * (edge * 1.6 + 0.05) + ripple * edge * 0.9 * alive);

                float a = saturate(hazeA + edge * 0.15 * alive + baseGlow * 0.3 * (1.0 - _Dissolve));
                half3 col = haze * hazeA + emit;
                return half4(col * _Intensity * fade, a * fade);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
