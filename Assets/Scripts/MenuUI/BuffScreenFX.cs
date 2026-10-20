using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng trên MÀN HÌNH người chơi khi tự dùng vật phẩm buff (17/10).
///
///   - NƯỚC TĂNG LỰC: viền loé xanh chanh + vệt tốc độ lao ra rìa + góc nhìn phình ra một nhịp
///     (FPSMovement.PunchFov). Suốt thời gian hiệu lực, viền phập phồng nhẹ như nhịp tim; 3 giây
///     cuối nháy nhanh để báo sắp hết — người chơi khỏi phải liếc HUD.
///   - BỘ XẢ ĐIỆN: viền loé xanh lơ + tia điện lách tách chạy quanh mép màn hình + rung nhẹ.
///
/// CHỈ người dùng thấy (InventorySystem gọi khi HasInputAuthority). Tự dựng UI bằng code, không
/// có ảnh nào trong dự án — giống DeathScreen. Canvas thứ tự vẽ -8: dưới HUD (chữ số vẫn đọc được),
/// và bị bảng Settings ẩn đi cùng các giao diện khác khi mở.
/// </summary>
public class BuffScreenFX : MonoBehaviour
{
    private static BuffScreenFX _instance;

    private static readonly Color Lime = new Color(0.72f, 1f, 0.22f);
    private static readonly Color Cyan = new Color(0.45f, 0.9f, 1f);

    private Image _vignette, _streaks;
    private Image[] _bolts;
    private RectTransform _streakRt;

    private float _drinkFlash = -10f;     // thời điểm uống (unscaled)
    private float _bandageFlash = -10f;   // thời điểm xả điện
    private float _deniedFlash = -10f;    // thời điểm bấm bắn vật TRÁI DẤU (bị từ chối)
    private float _nextBoltShuffle;

    private static readonly Color DeniedRed = new Color(1f, 0.16f, 0.12f);

    /// <summary>
    /// Viền đỏ loé ngắn + rung nhẹ: báo "không bắn được vật trái dấu với găng" (19/10).
    /// PlayerMagnetController gọi trên máy người bấm (qua bộ đếm mạng WrongPolarityCount).
    /// Dùng chung canvas viền với hiệu ứng vật phẩm cho khỏi dựng thêm giao diện.
    /// </summary>
    public static void Denied()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("BuffScreenFX");
            _instance = go.AddComponent<BuffScreenFX>();
            _instance.Build();
        }
        _instance._deniedFlash = Time.unscaledTime;
        FPSMovement me = FPSMovement.Local;
        if (me != null) me.ShakeCamera(0.15f);
    }

    /// <summary>Phát hiệu ứng cho món vừa dùng. Món không phải buff bản thân thì bỏ qua.</summary>
    public static void Play(ConsumableType type)
    {
        if (type != ConsumableType.EnergyDrink && type != ConsumableType.Bandage) return;
        if (_instance == null)
        {
            GameObject go = new GameObject("BuffScreenFX");
            _instance = go.AddComponent<BuffScreenFX>();
            _instance.Build();
        }
        _instance.Trigger(type);
    }

    private void Trigger(ConsumableType type)
    {
        FPSMovement me = FPSMovement.Local;
        if (type == ConsumableType.EnergyDrink)
        {
            _drinkFlash = Time.unscaledTime;
            if (me != null) { me.PunchFov(8f, 0.6f); me.ShakeCamera(0.12f); }
        }
        else
        {
            _bandageFlash = Time.unscaledTime;
            _nextBoltShuffle = 0f;
            if (me != null) { me.PunchFov(-3f, 0.5f); me.ShakeCamera(0.22f); }
        }
    }

    // ==================== DỰNG GIAO DIỆN ====================

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -8;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _vignette = FullScreen("Vignette", VignetteTexture());
        _streaks = FullScreen("Streaks", StreakTexture());
        _streakRt = _streaks.rectTransform;

        // 6 tia điện đặt quanh mép, mỗi nhịp đổi chỗ — xem UpdateBolts
        Texture2D bolt = BoltTexture();
        _bolts = new Image[6];
        for (int i = 0; i < _bolts.Length; i++)
        {
            GameObject go = new GameObject("Bolt" + i, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            Image img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(bolt, new Rect(0, 0, bolt.width, bolt.height), new Vector2(0.5f, 0.5f));
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(520f, 130f);
            img.enabled = false;
            _bolts[i] = img;
        }

        SetAlpha(_vignette, 0f);
        SetAlpha(_streaks, 0f);
    }

    private Image FullScreen(string name, Texture2D tex)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        img.raycastTarget = false;
        return img;
    }

    // ==================== MỖI KHUNG HÌNH ====================

    private void Update()
    {
        float now = Time.unscaledTime;
        FPSMovement me = FPSMovement.Local;

        // --- Viền: loé lúc dùng + phập phồng suốt lúc nước tăng lực còn hiệu lực ---
        float drinkT = now - _drinkFlash;
        float bandT = now - _bandageFlash;
        float drinkPop = drinkT < 0.75f ? Pop(drinkT, 0.08f, 0.67f) * 0.38f : 0f;
        float bandPop = bandT < 0.65f ? Pop(bandT, 0.05f, 0.6f) * 0.42f : 0f;

        float linger = 0f;
        if (me != null && me.IsEnergyDrinkActive && drinkT > 0.5f)
        {
            float left = me.EnergyDrinkRemaining;
            linger = left < 3f
                ? 0.16f * (0.5f + 0.5f * Mathf.Sin(now * 25f))         // sắp hết: nháy nhanh
                : 0.06f + 0.05f * Mathf.Pow(Mathf.Abs(Mathf.Sin(now * 3.6f)), 6f);   // nhịp tim
        }

        Color vc;
        float va;
        if (bandPop >= drinkPop + linger) { vc = Cyan; va = bandPop; }
        else { vc = Lime; va = drinkPop + linger; }
        // Tia điện chập chờn làm viền xanh lơ nháy theo
        if (bandPop > 0f) va *= Random.Range(0.7f, 1f);

        // Viền đỏ "bị từ chối": ngắn và gắt, lấn át mọi màu khác trong lúc loé
        float deniedT = now - _deniedFlash;
        float deniedPop = deniedT < 0.4f ? Pop(deniedT, 0.03f, 0.37f) * 0.45f : 0f;
        if (deniedPop > va) { vc = DeniedRed; va = deniedPop; }

        _vignette.color = new Color(vc.r, vc.g, vc.b, va);

        // --- Vệt tốc độ: lao từ trong ra rìa trong 0,6 giây sau khi uống ---
        if (drinkT < 0.6f)
        {
            float p = drinkT / 0.6f;
            _streakRt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, 1f - (1f - p) * (1f - p));
            _streaks.color = new Color(1f, 1f, 0.85f, 0.4f * (1f - p));
        }
        else SetAlpha(_streaks, 0f);

        UpdateBolts(bandT, now);

        // Không còn gì để vẽ thì tắt hẳn canvas cho khỏi tốn
        bool anything = va > 0.001f || drinkT < 0.6f || bandT < 0.5f;
        if (_vignette.enabled != anything) { _vignette.enabled = anything; _streaks.enabled = anything; }
    }

    private void UpdateBolts(float bandT, float now)
    {
        bool on = bandT < 0.5f;
        if (!on)
        {
            foreach (Image b in _bolts) if (b.enabled) b.enabled = false;
            return;
        }

        // Đổi chỗ mỗi 0,06 giây — chính cái giật cục đó đọc thành "điện"
        if (now >= _nextBoltShuffle)
        {
            _nextBoltShuffle = now + 0.06f;
            foreach (Image b in _bolts)
            {
                b.enabled = Random.value < 0.75f;
                int side = Random.Range(0, 4);   // trên, dưới, trái, phải
                float along = Random.Range(-0.42f, 0.42f);
                RectTransform rt = b.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                switch (side)
                {
                    case 0: rt.anchoredPosition = new Vector2(along * 1920f, 470f); rt.localEulerAngles = new Vector3(0, 0, Random.Range(-12f, 12f)); break;
                    case 1: rt.anchoredPosition = new Vector2(along * 1920f, -470f); rt.localEulerAngles = new Vector3(0, 0, Random.Range(-12f, 12f)); break;
                    case 2: rt.anchoredPosition = new Vector2(-880f, along * 1080f); rt.localEulerAngles = new Vector3(0, 0, 90f + Random.Range(-12f, 12f)); break;
                    default: rt.anchoredPosition = new Vector2(880f, along * 1080f); rt.localEulerAngles = new Vector3(0, 0, 90f + Random.Range(-12f, 12f)); break;
                }
                rt.localScale = new Vector3(Random.value < 0.5f ? -1f : 1f, Random.Range(0.7f, 1.2f), 1f);
            }
        }

        float fade = 1f - bandT / 0.5f;
        foreach (Image b in _bolts)
            b.color = new Color(Cyan.r, Cyan.g, Cyan.b, fade * Random.Range(0.6f, 1f));
    }

    // Lên nhanh rồi tắt dần: 0 → 1 trong "rise" giây, về 0 sau "total" giây
    private static float Pop(float t, float rise, float total)
    {
        if (t < rise) return t / rise;
        return Mathf.Clamp01(1f - (t - rise) / (total - rise));
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color; c.a = a; img.color = c;
    }

    // ==================== ẢNH SINH BẰNG CODE ====================

    // Viền: trong suốt ở giữa, đậm dần ra mép (hình elip theo khung màn hình)
    private static Texture2D VignetteTexture()
    {
        const int w = 256, h = 144;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                float a = Mathf.Clamp01((d - 0.74f) / 0.55f);   // giữa màn hình để trống - không che tầm nhìn khi đang đánh
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        t.Apply();
        return t;
    }

    // Vệt tốc độ: các vệt mảnh toả từ tâm, chỉ hiện ở vùng rìa (giữa màn hình để trống cho tầm nhìn)
    private static Texture2D StreakTexture()
    {
        const int s = 256;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var rnd = new System.Random(1710);
        float[] streak = new float[180];
        for (int i = 0; i < streak.Length; i++) streak[i] = rnd.NextDouble() < 0.16 ? (float)(0.4 + rnd.NextDouble() * 0.6) : 0f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f) * streak.Length;
                int bin = Mathf.Clamp((int)ang, 0, streak.Length - 1);
                float frac = ang - Mathf.Floor(ang);
                float thin = 1f - Mathf.Abs(frac - 0.5f) * 2f;       // mảnh ở giữa mỗi ô góc
                float a = streak[bin] * Mathf.Pow(thin, 6f) * Mathf.Clamp01((r - 0.72f) / 0.3f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }

    // Một tia điện ngang ngoằn ngoèo, có quầng mờ quanh lõi sáng
    private static Texture2D BoltTexture()
    {
        const int w = 256, h = 64;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var rnd = new System.Random(4242);
        const int n = 14;
        Vector2[] pts = new Vector2[n];
        for (int i = 0; i < n; i++)
            pts[i] = new Vector2(i / (float)(n - 1) * (w - 8) + 4, h * 0.5f + (float)(rnd.NextDouble() * 2 - 1) * h * 0.32f * Mathf.Sin(i / (float)(n - 1) * Mathf.PI));
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float best = 999f;
                Vector2 p = new Vector2(x, y);
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 a = pts[i], b = pts[i + 1], ab = b - a;
                    float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * k));
                }
                float core = Mathf.Clamp01(1.6f - best);                // lõi ~2px
                float glow = Mathf.Exp(-best * 0.35f) * 0.45f;         // quầng
                float fadeEnds = Mathf.Sin(x / (float)w * Mathf.PI);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow) * fadeEnds));
            }
        t.Apply();
        return t;
    }

#if UNITY_EDITOR
    /// <summary>Chỉ để chụp ảnh: dựng sẵn và đặt về một khoảnh khắc của hiệu ứng.</summary>
    public static BuffScreenFX PreviewAt(ConsumableType type, float secondsAfter)
    {
        Play(type);
        if (type == ConsumableType.EnergyDrink) _instance._drinkFlash = Time.unscaledTime - secondsAfter;
        else _instance._bandageFlash = Time.unscaledTime - secondsAfter;
        _instance.Update();
        return _instance;
    }

    public static void PreviewClear()
    {
        if (_instance != null) Object.DestroyImmediate(_instance.gameObject);
        _instance = null;
    }
#endif
}
