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
        _HazeAlpha ("Do mo kinh (0 trong suot, 1 dac han)", Range(0, 1)) = 0.8
        _BlurPx    ("Do nhoe phia sau (diem anh)", Range(0, 20)) = 7
        _Distort   ("Do meo theo lan khoi", Range(0, 0.05)) = 0.012
        _BgTint    ("Am mau doi len canh phia sau", Range(0, 1)) = 0.5
        _HazeDark  ("Do sang cho khoi mong", Range(0, 1)) = 0.11
        _HazeBright ("Do sang cho khoi day", Range(0, 1)) = 0.36
        _HazePale  ("Pha trang cho nhat mau (0 dam, 1 trang)", Range(0, 1)) = 0.28

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
        // (19/10) Ghi chieu sau tach rieng o luot "BarrierDepth" ben duoi - xem giai thich o do.
        // Vien dong doi (OutlineAll, ZTest Always) van xuyen tuong nhu cu; vien dich (OutlineVisible) bi che.
        // Cull Back, KHONG Cull Off: cube co hai mat lon cach nhau 1m, ve ca hai thi lop man dac
        // gap doi va hai luoi luc giac chong lech thanh van moire. Camera luon o ngoai khoi.
        Cull Back

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Anh canh vat da ve xong (truoc lop trong suot) - de lam KINH MO: lay canh phia sau, lam
            // nhoe, am mau. Can "Opaque Texture" bat trong PC_RPAsset (da bat san).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
                float  _BlurPx;
                float  _Distort;
                float  _BgTint;
                float  _HazeDark;
                float  _HazeBright;
                float  _HazePale;
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

            // Tinh mau + do che cua mot diem tren tuong. withColor = false: chi can do che (luot ghi
            // chieu sau) - bo qua phan lay mau canh phia sau cho do ton.
            half4 Shade(Varyings IN, bool withColor)
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
                // (17/10) Nhat lai theo yeu cau: sang hon (_HazeDark / _HazeBright) va pha trang (_HazePale)
                half3 hazeTint = lerp(cellCol, half3(1, 1, 1), _HazePale);
                half3 haze = hazeTint * (lerp(_HazeDark, _HazeBright, body) + nearGround * 0.10)
                           + _EdgeColor.rgb * streak * 0.18;

                // Mot it o thinh thoang loe sang roi tat - nhu co dien chay qua to ong
                float sparkle = step(1.0 - _Sparkle, rnd2) * pow(saturate(sin(_Time.y * (0.6 + rnd3) + rnd * 6.2831)), 6.0);
                // (19/10) KINH MO kieu Valorant nhung dam hon chut: thay duoc dang nguoi / cay phia sau
                // nhung nhoe va am mau doi. Truoc do man dac ~99% (che hoan toan) - chu project doi y.
                float hazeA = _HazeAlpha * (0.9 + 0.1 * body) * alive;

                // Canh phia sau: lech theo lan khoi cho meo nhe, roi lay trung binh 8 diem tren vong
                // tron + diem giua. Opaque Texture da giam nua do phan giai nen tu no da mem san.
                half3 frosted = 0;
                if (withColor)
                {
                    float2 suv = GetNormalizedScreenSpaceUV(IN.positionCS);
                    suv += (float2(smoke, streak) - 0.5) * _Distort;
                    float2 px = _BlurPx / _ScaledScreenParams.xy;
                    half3 behind = SampleSceneColor(suv);
                    [unroll] for (int k = 0; k < 8; k++)
                    {
                        float ang = k * 0.785398 + 0.39;
                        behind += SampleSceneColor(suv + float2(cos(ang), sin(ang)) * px);
                    }
                    behind /= 9.0;
                    frosted = behind * lerp(half3(1, 1, 1), hazeTint * 1.4, _BgTint);
                }

                float lines = edge * (0.30 + band * 0.9 + ripple * 1.6) * alive;

                half3 emit = cellCol * sparkle * 0.22 * alive * (1.0 - edge)
                           + _Color.rgb * (lines + band * 0.06 * alive + ripple * 0.25 * alive + baseGlow * 0.9 * (1.0 - _Dissolve))
                           + _EdgeColor.rgb * (burn * (edge * 1.6 + 0.05) + ripple * edge * 0.9 * alive);

                float a = saturate(hazeA + edge * 0.15 * alive + baseGlow * 0.3 * (1.0 - _Dissolve));
                // Kinh mo + mot lop khoi mau doi mong phu len + phan phat sang (luoi, vach chan tuong...)
                half3 col = (frosted + haze * 0.55) * hazeA + emit * _Intensity;

                return half4(col * fade, a * fade);
            }
        ENDHLSL

        // LUOT 1 - CHI GHI CHIEU SAU, o phan tuong du dam (do che > 0.5). (19/10)
        //
        // Vi sao can: tuong ve TRUOC moi vat trong suot khac (renderQueue 2995, xem RoundBarrier) va ghi
        // chieu sau o day, nen moi thu trong suot NAM SAU phan tuong dam deu bi che: vien dich
        // (QuickOutline), bang ten tren dau, hao quang, hieu ung. Khong co luot nay thi bang ten dich doc
        // duoc xuyen rao - lo vi tri.
        // Phan tuong mong (dang tan o mep dao / tren cao) KHONG ghi, de may phia sau khong bi cat cut.
        Pass
        {
            Name "BarrierDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            half4 fragDepth(Varyings IN) : SV_Target
            {
                clip(Shade(IN, false).a - 0.5);
                return 0;
            }
            ENDHLSL
        }

        // LUOT 2 - MAU: kinh mo + khoi mau doi + luoi luc giac. Khong ghi chieu sau (luot 1 lo roi).
        Pass
        {
            Name "BarrierCurtain"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = Shade(IN, true);
                clip(c.a - 0.002);
                return c;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
