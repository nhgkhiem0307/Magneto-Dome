using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Màn hình khi bị hất khỏi đảo — thay cho bảng chữ "DEAD" + "Respawning in Xs" cũ (14/10).
///
/// Hiện: ai đã hất mình và bằng đòn gì, vòng đếm ngược hồi sinh, hậu quả (địch +8 tiến độ),
/// và một mẹo chơi. Mép màn hình tối lại nhưng GIỮA vẫn trong, vì lúc chết người chơi đang
/// xem trận qua mắt đồng đội (SpectatorController) — không được che mất tầm nhìn đó.
///
/// Tự dựng giao diện bằng code, giống PingDisplay / ControlsOverlay / LoadingScreen —
/// không phải kéo thả gì trong Inspector. Vẽ DƯỚI HUD (sortingOrder âm) để chỉ số của
/// đồng đội đang xem và dòng "SPECTATING" vẫn nằm trên cùng.
/// </summary>
public class DeathScreen : MonoBehaviour
{
    // Màu của game: hai cực đỏ / xanh dùng lặp lại thành mô-típ.
    private static readonly Color Red = new Color32(0xE8, 0x45, 0x3C, 0xFF);
    private static readonly Color Blue = new Color32(0x2E, 0x86, 0xDE, 0xFF);
    private static readonly Color Amber = new Color32(0xFF, 0xB5, 0x47, 0xFF);
    private static readonly Color Soft = new Color(0.86f, 0.89f, 0.94f, 1f);

    // Bản sáng hơn cho CHỮ tên người chơi — màu gốc đậm quá, chìm trên nền tối.
    private static readonly Color RedText = new Color32(0xFF, 0x6B, 0x63, 0xFF);
    private static readonly Color BlueText = new Color32(0x5A, 0xB0, 0xFF, 0xFF);

    private const float FadeIn = 0.35f;
    private const float FadeOut = 0.25f;

    private static readonly string[] Tips =
    {
        "The more charge you carry, the farther every hit sends you. Stay away from the edge.",
        "Same polarity pushes, opposite polarity pulls. Check the color before you click.",
        "Pulling a flying Heavy cuts its hit in half. Pulling a flying Spike doubles it.",
        "If both teams stand in the zone, nobody gains progress. Push them out.",
        "Armor from the shop soaks up charge before your body does.",
        "Your charge resets to zero at the start of every round.",
        "Dash and grapple are never boosted by your charge — only hits you take are.",
    };

    private static DeathScreen _instance;

    private Canvas _canvas;
    private CanvasGroup _group;
    private Image _flash;
    private RectTransform _titleRt;
    private TMP_Text _title;
    private TMP_Text _cause, _count, _countLabel, _bonus, _tip;
    private Image _ringFill;
    private GameObject _bonusPill;

    private bool _showing;
    private float _openedAt, _closedAt = -99f;
    private float _countTotal = 5f;
    private int _tipIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("DeathScreen");
        _instance = go.AddComponent<DeathScreen>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        Build();
        _group.alpha = 0f;
        _canvas.enabled = false;
        _tipIndex = Random.Range(0, Tips.Length);
    }

    private void Update()
    {
        PlayerHealth me = FPSMovement.Local != null ? FPSMovement.Local.GetComponent<PlayerHealth>() : null;
        bool dead = me != null && me.NetworkReady && !me.IsAlive;

        if (dead && !_showing) Open(me);
        else if (!dead && _showing) { _showing = false; _closedAt = Time.unscaledTime; }

        if (_showing) Animate(me);
        else if (_canvas.enabled) FadeAway();
    }

    // ==================== HIỆN / ẨN ====================

    private void Open(PlayerHealth me)
    {
        _showing = true;
        _openedAt = Time.unscaledTime;
        _canvas.enabled = true;

        // Đếm ngược: lấy tổng thời gian chờ ngay lúc mở, để vòng tròn chạy đúng từ đầy về rỗng.
        _countTotal = Mathf.Max(0.5f, me.RespawnSecondsLeft);

        _cause.text = CauseLine(me);

        // Tự rơi thì không phải "bị hạ" — đổi tiêu đề cho đúng chuyện vừa xảy ra.
        _title.text = me.KilledBy == PlayerRef.None ? "FELL OFF" : "KNOCKED OUT";

        GameManager gm = GameManager.Instance;
        bool showBonus = gm != null && gm.Object != null && gm.Object.IsValid
                      && gm.Phase == GameManager.GamePhase.Combat && gm.deathZoneBonus > 0f;
        _bonusPill.SetActive(showBonus);
        if (showBonus) _bonus.text = $"ENEMY TEAM  +{gm.deathZoneBonus:0}  ZONE PROGRESS";

        _tipIndex = (_tipIndex + 1) % Tips.Length;
        _tip.text = "<color=#FFB547><b>TIP</b></color>   " + Tips[_tipIndex];
    }

    private void Animate(PlayerHealth me)
    {
        float t = Time.unscaledTime - _openedAt;
        float k = EaseOut(Mathf.Clamp01(t / FadeIn));

        _group.alpha = k;
        _flash.color = new Color(Red.r, Red.g, Red.b, 0.4f * (1f - Mathf.Clamp01(t / 0.45f)));
        _titleRt.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k);

        if (me.IsWaitingToRespawn)
        {
            float left = me.RespawnSecondsLeft;
            _ringFill.fillAmount = Mathf.Clamp01(left / _countTotal);
            _count.text = Mathf.CeilToInt(left).ToString();
            _countLabel.text = "RESPAWNING";
        }
        else
        {
            // Chết mà không có đồng hồ (vd. round vừa kết thúc): đợi sang round mới.
            _ringFill.fillAmount = 0f;
            _count.text = "–";
            _countLabel.text = "NEXT ROUND";
        }
    }

    private void FadeAway()
    {
        float k = Mathf.Clamp01((Time.unscaledTime - _closedAt) / FadeOut);
        _group.alpha = 1f - k;
        if (k >= 1f) _canvas.enabled = false;
    }

    // ==================== NỘI DUNG ====================

    private static string CauseLine(PlayerHealth me)
    {
        PlayerRef killer = me.KilledBy;
        if (killer == PlayerRef.None) return "You fell off the island";

        string hex = ColorUtility.ToHtmlStringRGB(TeamColor(killer));
        string name = $"<color=#{hex}><b>{NameOf(killer)}</b></color>";

        switch ((PlayerHealth.KnockCause)me.KilledByCause)
        {
            case PlayerHealth.KnockCause.Melee: return $"Punched off the island by {name}";
            case PlayerHealth.KnockCause.Projectile: return $"Knocked off the island by {name}'s shot";
            case PlayerHealth.KnockCause.Explosion: return $"Blown off the island by {name}'s TNT";
            default: return $"Knocked off the island by {name}";
        }
    }

    // Tên lấy từ RoomPlayer của phòng chờ, cùng cách SpectatorController đang làm.
    private static string NameOf(PlayerRef who)
    {
        foreach (RoomPlayer rp in RoomPlayer.AllPlayers)
        {
            if (rp == null || rp.Object == null || !rp.Object.IsValid) continue;
            if (rp.PlayerRef != who) continue;
            string nick = rp.NickName.ToString();
            if (!string.IsNullOrEmpty(nick)) return nick;
        }
        return "an enemy";
    }

    private static Color TeamColor(PlayerRef who)
    {
        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null || !p.NetworkReady) continue;
            if (p.Object.InputAuthority != who) continue;
            return p.Team == 0 ? RedText : BlueText;
        }
        return RedText;
    }

    private static float EaseOut(float x) => 1f - Mathf.Pow(1f - x, 3f);

    // ==================== DỰNG GIAO DIỆN ====================

    private void Build()
    {
        GameObject canvasGo = new GameObject("DeathCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -5;   // dưới HUD (0): chỉ số đồng đội + "SPECTATING" nằm trên

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _group = canvasGo.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        RectTransform root = (RectTransform)canvasGo.transform;

        // Mép tối đỏ thẫm, giữa trong — vẫn xem được đồng đội.
        RawImage vignette = NewRaw(root, "Vignette", VignetteTexture());
        Stretch(vignette.rectTransform);

        _flash = NewImage(root, "Flash", null, Color.clear);
        Stretch(_flash.rectTransform);

        TMP_FontAsset black = LoadFont("Poppins-Black SDF");
        TMP_FontAsset bold = LoadFont("Poppins-Bold SDF");
        TMP_FontAsset medium = LoadFont("Poppins-Medium SDF");

        // --- Tiêu đề ---
        TMP_Text kicker = NewText(root, "Kicker", bold, 22, Red, new Vector2(0f, 150f), new Vector2(800f, 40f));
        kicker.text = "ELIMINATED";
        kicker.characterSpacing = 22f;

        TMP_Text title = NewText(root, "Title", black, 96, Color.white, new Vector2(0f, 85f), new Vector2(1400f, 120f));
        title.text = "KNOCKED OUT";
        title.characterSpacing = 3f;
        Underlay(title);
        _titleRt = title.rectTransform;
        _title = title;

        // Gạch ngang hai nửa đỏ — xanh: hai cực của găng tay, mô-típ của cả game.
        NewImage(root, "LineRed", null, Red, new Vector2(-62f, 20f), new Vector2(120f, 4f));
        NewImage(root, "LineBlue", null, Blue, new Vector2(62f, 20f), new Vector2(120f, 4f));

        _cause = NewText(root, "Cause", medium, 32, Soft, new Vector2(0f, -26f), new Vector2(1400f, 50f));
        Underlay(_cause);

        // --- Vòng đếm ngược ---
        Sprite ring = RingSprite();
        NewImage(root, "RingBack", ring, new Color(1f, 1f, 1f, 0.14f), new Vector2(0f, -150f), new Vector2(150f, 150f));
        _ringFill = NewImage(root, "RingFill", ring, new Color(0.62f, 0.93f, 1f, 1f), new Vector2(0f, -150f), new Vector2(150f, 150f));
        _ringFill.type = Image.Type.Filled;
        _ringFill.fillMethod = Image.FillMethod.Radial360;
        _ringFill.fillOrigin = (int)Image.Origin360.Top;
        _ringFill.fillClockwise = false;

        _count = NewText(root, "Count", black, 64, Color.white, new Vector2(0f, -147f), new Vector2(150f, 90f));
        Underlay(_count);
        _countLabel = NewText(root, "CountLabel", bold, 18, Soft, new Vector2(0f, -248f), new Vector2(400f, 30f));
        _countLabel.characterSpacing = 14f;

        // --- Hậu quả ---
        Image pill = NewImage(root, "BonusPill", PillSprite(), new Color(0.06f, 0.07f, 0.1f, 0.72f),
                              new Vector2(0f, -305f), new Vector2(520f, 46f));
        pill.type = Image.Type.Sliced;
        _bonusPill = pill.gameObject;
        _bonus = NewText(pill.rectTransform, "Bonus", bold, 19, Amber, Vector2.zero, new Vector2(520f, 46f));
        _bonus.characterSpacing = 4f;

        // --- Mẹo ---
        _tip = NewText(root, "Tip", medium, 22, new Color(1f, 1f, 1f, 0.82f), new Vector2(0f, -430f), new Vector2(1300f, 40f));
        Underlay(_tip);
    }

    private static TMP_FontAsset LoadFont(string file)
    {
        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/" + file);
        return f != null ? f : TMP_Settings.defaultFontAsset;
    }

    private static TMP_Text NewText(RectTransform parent, string name, TMP_FontAsset font, float size,
                                    Color color, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        Place(t.rectTransform, pos, box);
        return t;
    }

    // Bóng mờ dưới chữ — trời ban ngày rất sáng, thiếu cái này chữ trắng chìm vào mây.
    private static void Underlay(TMP_Text t)
    {
        Material m = t.fontMaterial;   // tạo bản riêng, không đụng material dùng chung
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.65f));
        m.SetFloat("_UnderlayOffsetY", -0.5f);
        m.SetFloat("_UnderlaySoftness", 0.35f);
        m.SetFloat("_UnderlayDilate", 0f);
    }

    private static Image NewImage(RectTransform parent, string name, Sprite sprite, Color color,
                                  Vector2 pos = default, Vector2 box = default)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (box != default) Place(img.rectTransform, pos, box);
        return img;
    }

    private static RawImage NewRaw(RectTransform parent, string name, Texture tex)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RawImage img = go.AddComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
        return img;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 box)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ---------- Ảnh tự sinh — không cần file ảnh nào ----------

    private static Texture2D VignetteTexture()
    {
        const int n = 256;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color edge = new Color(0.09f, 0.015f, 0.04f, 1f);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
            float a = 0.28f + 0.68f * Mathf.Pow(Mathf.SmoothStep(0.15f, 1f, r), 1.2f);
            px[y * n + x] = new Color(edge.r, edge.g, edge.b, Mathf.Clamp01(a));
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    private static Sprite RingSprite()
    {
        const int n = 256;
        const float outer = 0.5f, thick = 0.055f;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        float aa = 1.5f / n;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01((outer - aa - d) / aa) * Mathf.Clamp01((d - (outer - thick)) / aa);
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite PillSprite()
    {
        const int n = 64;
        const float r = 31f;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, r, n - r), cy = Mathf.Clamp(y + 0.5f, r, n - r);
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
            px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                             SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

#if UNITY_EDITOR
    /// <summary>Chỉ để chụp ảnh xem trước trong Editor — không chạy trong game.</summary>
    public static DeathScreen CreateForPreview(string causeLine, int seconds, float ringFill, bool bonus, int tip)
    {
        DeathScreen ds = new GameObject("DeathScreenPreview").AddComponent<DeathScreen>();
        ds.Build();
        ds._canvas.enabled = true;
        ds._group.alpha = 1f;
        ds._flash.color = Color.clear;
        ds._cause.text = causeLine;
        if (causeLine.StartsWith("You fell")) ds._title.text = "FELL OFF";
        ds._count.text = seconds.ToString();
        ds._countLabel.text = "RESPAWNING";
        ds._ringFill.fillAmount = ringFill;
        ds._bonusPill.SetActive(bonus);
        ds._bonus.text = "ENEMY TEAM  +8  ZONE PROGRESS";
        ds._tip.text = "<color=#FFB547><b>TIP</b></color>   " + Tips[tip % Tips.Length];
        ds.enabled = false;
        return ds;
    }

    public Canvas PreviewCanvas => _canvas;
#endif
}
