using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện HUD bản mới (20/10) — "kính tối góc vát" giống menu / phòng chờ / Shop, cộng điểm
/// nhấn NEON: viền sáng + quầng sáng quanh khung, số quan trọng có quầng màu phía sau.
///
/// CÁCH LÀM: HUDController vẫn giữ nguyên toàn bộ logic (đọc điện tích, tiền, đạn, đồng hồ...).
/// Script này chỉ DỰNG LẠI PHẦN NHÌN bằng code lúc chạy, rồi trỏ các ô của HUDController sang
/// những object mới. HUD cũ trong scene bị ẩn đi chứ không xoá — muốn quay lại bản cũ chỉ cần
/// bỏ dòng HUDSkin.Apply() trong HUDController.Awake().
///
/// Không dùng ảnh nào ngoài 5 icon sẵn có: khung, thanh, vòng tâm ngắm đều vẽ bằng code.
/// Quầng neon cũng tự vẽ (ảnh mờ đặt sau), KHÔNG dùng hiệu ứng Glow của TextMeshPro — hiệu ứng
/// đó là một biến thể shader, bản build tự lược bỏ nếu không vật liệu nào dùng → mất glow âm thầm.
/// </summary>
public class HUDSkin : MonoBehaviour
{
    // ==================== BẢNG MÀU ====================
    private static readonly Color Cyan = new Color(0.36f, 0.85f, 1f);
    private static readonly Color Red = new Color(1f, 0.36f, 0.32f);
    private static readonly Color Blue = new Color(0.32f, 0.62f, 1f);
    private static readonly Color Gold = new Color(1f, 0.8f, 0.32f);
    private static readonly Color Lime = new Color(0.72f, 1f, 0.3f);
    private static readonly Color Soft = new Color(0.86f, 0.9f, 0.96f);
    private static readonly Color Glass = new Color(0.03f, 0.06f, 0.11f, 0.76f);

    private TMP_FontAsset _black, _bold, _medium;
    private RectTransform _root;
    private RectTransform _canvasRect;
    private HUDController _hud;

    // Những thứ cần chạy hiệu ứng mỗi khung hình
    private Image _chargeEdge, _chargeGlow, _chargeBarGlow;
    private Image _ringGlow;
    private RectTransform _ringGroup;
    private Color _lastRingColor;
    private float _ringPulse;
    private GameObject _knockPill;
    private GameObject _phaseHalo;
    private Image _zoneGlowRed, _zoneGlowBlue, _signDisc;
    private RectTransform _energyRt;
    private readonly Slot[] _slots = new Slot[3];

    private class Slot
    {
        public TMP_Text count, key;
        public Image icon, edge;
    }

    /// <summary>Gọi từ HUDController.Awake(), TRƯỚC mọi thứ khác.</summary>
    public static void Apply(HUDController hud)
    {
        if (hud == null || hud.hudRoot == null) return;
        if (hud.hudRoot.GetComponent<HUDSkin>() != null) return;
        hud.hudRoot.AddComponent<HUDSkin>().Build(hud);
    }

    // ==================== DỰNG ====================

    private void Build(HUDController hud)
    {
        _hud = hud;
        _black = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/Poppins-Black SDF");
        _bold = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/Poppins-Bold SDF");
        _medium = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/Poppins-Medium SDF");

        Canvas canvas = hud.GetComponentInParent<Canvas>();
        if (canvas != null) _canvasRect = (RectTransform)canvas.rootCanvas.transform;

        // Lấy icon đạn từ HUD cũ TRƯỚC khi trỏ các ô sang object mới
        Sprite iconNormal = OldIcon(hud.normalAmmoText);
        Sprite iconHeavy = OldIcon(hud.heavyAmmoText);
        Sprite iconThird = OldIcon(hud.spikeAmmoText);
        Sprite iconDrink = hud.energyDrinkGroup != null ? FirstSprite(hud.energyDrinkGroup.transform) : null;

        HideOld(hud);

        // Gốc mới phủ đúng kích thước Canvas (gốc HUD cũ chỉ là ô 100x100 giữa màn hình)
        _root = Rt("HUD_v2", hud.hudRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
        _root.SetSiblingIndex(0);   // dưới dòng thông báo kết quả round
        FitRoot();

        BuildTop(hud);
        BuildCrosshair(hud);
        BuildCharge(hud, iconDrink);
        BuildInventory(hud, iconNormal, iconHeavy, iconThird);
        RestyleAnnouncement(hud);
    }

    private void HideOld(HUDController hud)
    {
        Transform r = hud.hudRoot.transform;
        foreach (string n in new[] { "Group_Top", "Group_BottomLeft", "Group_BottomRight" })
        {
            Transform t = r.Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
        if (hud.crosshair != null) hud.crosshair.SetActive(false);
        if (hud.polarityIndicator != null) hud.polarityIndicator.gameObject.SetActive(false);
    }

    // ---------- Cụm trên: tỉ số, đồng hồ, round, pha, khu chiếm ----------

    private void BuildTop(HUDController hud)
    {
        Vector2 top = new Vector2(0.5f, 1f);
        RectTransform score = Panel("Score", _root, top, top, new Vector2(0f, -18f), new Vector2(400f, 88f), Glass, Cyan, 0.6f, 0.42f);

        // Hai "cánh" màu đội mờ ở hai đầu khung
        Wing(score, -1, Red);
        Wing(score, 1, Blue);

        Blob(score, new Vector2(-126f, -2f), new Vector2(150f, 110f), new Color(Red.r, Red.g, Red.b, 0.32f));
        Blob(score, new Vector2(126f, -2f), new Vector2(150f, 110f), new Color(Blue.r, Blue.g, Blue.b, 0.32f));
        score.Find("Edge").SetAsLastSibling();   // viền nằm trên cánh + quầng, dưới chữ

        hud.redScoreText = Txt(score, "RedScore", "0", _black, 52f, Red, new Vector2(-126f, -2f), new Vector2(110f, 70f), 0f, TextAlignmentOptions.Center);
        hud.blueScoreText = Txt(score, "BlueScore", "0", _black, 52f, Blue, new Vector2(126f, -2f), new Vector2(110f, 70f), 0f, TextAlignmentOptions.Center);

        // Vạch ngăn mảnh hai bên đồng hồ
        Bar(score, new Vector2(-66f, 0f), new Vector2(2f, 46f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));
        Bar(score, new Vector2(66f, 0f), new Vector2(2f, 46f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));

        hud.roundNumberText = Txt(score, "Round", "ROUND 1", _bold, 14f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f), new Vector2(0f, 22f), new Vector2(130f, 22f), 5f, TextAlignmentOptions.Center);
        // KHÔNG đặt tên "Timer": HUDController ẩn object tên đó khi Shop mở - ở bản mới đồng hồ nằm
        // trong khung tỉ số, cao hơn hẳn Shop, không cần ẩn
        RectTransform timerSlot = Rt("TimerSlot", score, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(130f, 44f));
        hud.timerText = Txt(timerSlot, "TimerText", "0:00", _bold, 34f, Color.white, Vector2.zero, new Vector2(130f, 44f), 1f, TextAlignmentOptions.Center);

        // Nhãn pha (BUY PHASE / ROUND OVER...) và cụm khu chiếm dùng chung một chỗ - không bao giờ hiện cùng lúc
        _phaseHalo = Blob(_root, new Vector2(0f, -147f), new Vector2(380f, 64f), new Color(0.02f, 0.05f, 0.1f, 0.55f), top).gameObject;
        hud.phaseText = Txt(_root, "Phase", "", _bold, 18f, Cyan, new Vector2(0f, -132f), new Vector2(500f, 30f), 8f, TextAlignmentOptions.Center, top);

        RectTransform zone = Rt("ZoneGroup", _root, top, top, new Vector2(0f, -132f), new Vector2(900f, 40f));
        hud.zoneGroup = zone.gameObject;
        Panel("StatusPill", zone, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 36f), Glass, Cyan, 0.3f, 0.15f);
        hud.zoneStatusText = Txt(zone, "Status", "ZONE NEUTRAL", _bold, 17f, Soft, Vector2.zero, new Vector2(230f, 30f), 4f, TextAlignmentOptions.Center);
        hud.zoneRedFill = ZoneBar(zone, -1, Red);
        hud.zoneBlueFill = ZoneBar(zone, 1, Blue);
    }

    private void Wing(RectTransform score, int side, Color c)
    {
        RectTransform w = Rt(side < 0 ? "WingRed" : "WingBlue", score, new Vector2(side < 0 ? 0f : 1f, 0.5f),
            new Vector2(0f, 0.5f), Vector2.zero, new Vector2(150f, 88f));
        if (side > 0) w.localScale = new Vector3(-1f, 1f, 1f);   // lật ảnh cho cánh phải
        Image i = w.gameObject.AddComponent<Image>();
        i.sprite = WingSprite; i.raycastTarget = false;
        i.color = new Color(c.r, c.g, c.b, 0.42f);
    }

    /// <summary>Thanh chiếm khu: đỏ bên trái, xanh bên phải, cùng lấp dần VÀO GIỮA - chạm giữa là thắng round.</summary>
    private Image ZoneBar(RectTransform zone, int side, Color c)
    {
        float x = side * 130f;
        Vector2 pivot = new Vector2(side < 0 ? 1f : 0f, 0.5f);
        RectTransform track = Rt(side < 0 ? "RedTrack" : "BlueTrack", zone, new Vector2(0.5f, 0.5f), pivot, new Vector2(x, 0f), new Vector2(260f, 20f));
        Image bg = track.gameObject.AddComponent<Image>();
        bg.sprite = FillSprite; bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 3f; bg.raycastTarget = false;
        bg.color = new Color(0.02f, 0.04f, 0.08f, 0.7f);

        Image glow = Img(track, "Glow", BlobSprite, new Color(c.r, c.g, c.b, 0f), Vector2.zero, new Vector2(40f, 50f));
        if (side < 0) _zoneGlowRed = glow; else _zoneGlowBlue = glow;

        Image fill = Img(track, "Fill", WhiteSprite, c, Vector2.zero, new Vector2(254f, 14f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = side < 0 ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
        fill.fillAmount = 0f;

        // Vạch chia 25 / 50 / 75%
        for (int k = 1; k <= 3; k++)
            Bar(track, new Vector2(-127f + 254f * k / 4f, 0f), new Vector2(2f, 14f), new Color(0f, 0f, 0f, 0.45f));

        Image edge = Img(track, "Edge", EdgeSprite, new Color(c.r, c.g, c.b, 0.6f), Vector2.zero, new Vector2(260f, 20f));
        edge.type = Image.Type.Sliced; edge.pixelsPerUnitMultiplier = 3f;
        return fill;
    }

    // ---------- Tâm ngắm + vòng cực găng ----------

    private void BuildCrosshair(HUDController hud)
    {
        RectTransform ch = Rt("Crosshair", _root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
        hud.crosshair = ch.gameObject;

        // Dấu + mảnh có khe hở giữa, viền tối để nổi trên nền trời sáng
        foreach (Vector2 d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
        {
            Vector2 size = d.x != 0f ? new Vector2(8f, 2f) : new Vector2(2f, 8f);
            Bar(ch, d * 9f, size + new Vector2(2f, 2f), new Color(0f, 0f, 0f, 0.45f));
            Bar(ch, d * 9f, size, Color.white);
        }
        Bar(ch, Vector2.zero, new Vector2(2f, 2f), Color.white);

        _ringGroup = Rt("Ring", ch, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
        _ringGlow = Img(_ringGroup, "RingGlow", RingGlowSprite, new Color(1f, 0.3f, 0.3f, 0.45f), Vector2.zero, new Vector2(84f, 84f));
        Image ring = Img(_ringGroup, "RingLine", RingSprite, Red, Vector2.zero, new Vector2(54f, 54f));
        hud.polarityIndicator = ring;

        // Dấu +/- nằm trong một viên tròn cùng màu, đặt ngay trên vòng (góc dưới phải)
        _signDisc = Img(_ringGroup, "SignDisc", DiscSprite, Red, new Vector2(20f, -20f), new Vector2(26f, 26f));
        hud.polarityText = Txt(_ringGroup, "Sign", "+", _black, 22f, Color.white, new Vector2(20f, -18.5f), new Vector2(26f, 26f), 0f, TextAlignmentOptions.Center);
    }

    // ---------- Góc dưới trái: điện tích, giáp, nước tăng lực ----------

    private void BuildCharge(HUDController hud, Sprite drinkIcon)
    {
        Vector2 bl = Vector2.zero;
        RectTransform panel = Panel("Charge", _root, bl, bl, new Vector2(36f, 34f), new Vector2(450f, 100f), Glass, Cyan, 0.6f, 0.4f);
        _chargeEdge = panel.Find("Edge").GetComponent<Image>();
        _chargeGlow = panel.Find("Glow").GetComponent<Image>();

        Txt(panel, "Label", "CHARGE", _bold, 15f, new Color(Soft.r, Soft.g, Soft.b, 0.75f), new Vector2(24f, 70f), new Vector2(120f, 24f), 6f, TextAlignmentOptions.Left, bl, new Vector2(0f, 0.5f));
        hud.healthText = Txt(panel, "Percent", "0%", _black, 40f, Soft, new Vector2(-22f, 66f), new Vector2(160f, 50f), 0f, TextAlignmentOptions.Right, new Vector2(1f, 0f), new Vector2(1f, 0.5f));

        // Hệ số văng: viên nhỏ cạnh nhãn CHARGE, chỉ hiện khi đã nhiễm điện (HUDController bật/tắt chữ)
        RectTransform pill = Panel("KnockPill", panel, bl, new Vector2(0f, 0.5f), new Vector2(140f, 70f), new Vector2(84f, 30f),
            new Color(0.25f, 0.05f, 0.03f, 0.75f), new Color(1f, 0.45f, 0.3f), 0.7f, 0.25f);
        _knockPill = pill.gameObject;
        hud.knockbackText = Txt(pill, "Knockback", "x1.0", _black, 19f, new Color(1f, 0.62f, 0.45f), Vector2.zero, new Vector2(84f, 30f), 1f, TextAlignmentOptions.Center);

        // Thanh chia vạch
        RectTransform track = Rt("Track", panel, bl, new Vector2(0f, 0.5f), new Vector2(24f, 28f), new Vector2(402f, 22f));
        Image bg = track.gameObject.AddComponent<Image>();
        bg.sprite = SegSprite; bg.raycastTarget = false; bg.color = new Color(1f, 1f, 1f, 0.09f);

        _chargeBarGlow = Img(track, "Glow", BlobSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f), Vector2.zero, new Vector2(440f, 60f));
        Image fill = Img(track, "Fill", SegSprite, Cyan, Vector2.zero, new Vector2(402f, 22f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        hud.healthFill = fill;

        // Giáp: khung mỏng ngay trên, chỉ hiện khi có giáp
        RectTransform armor = Panel("Armor", _root, bl, bl, new Vector2(36f, 144f), new Vector2(300f, 38f), Glass, new Color(0.75f, 0.9f, 1f), 0.45f, 0.18f);
        hud.armorGroup = armor.gameObject;
        Sprite shield = IconFromResources("UI/Gear shop radial kit/hud_icons/ic_shield_full");
        if (shield != null) Img(armor, "Icon", shield, new Color(0.8f, 0.92f, 1f), new Vector2(-126f, 0f), new Vector2(20f, 20f));
        RectTransform at = Rt("Track", armor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(190f, 8f));
        Image atBg = at.gameObject.AddComponent<Image>();
        atBg.sprite = WhiteSprite; atBg.raycastTarget = false; atBg.color = new Color(1f, 1f, 1f, 0.1f);
        Image aFill = Img(at, "Fill", WhiteSprite, new Color(0.75f, 0.92f, 1f), Vector2.zero, new Vector2(190f, 8f));
        aFill.type = Image.Type.Filled; aFill.fillMethod = Image.FillMethod.Horizontal; aFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.armorFill = aFill;
        hud.armorText = Txt(armor, "Value", "0", _bold, 18f, Color.white, new Vector2(122f, 0f), new Vector2(50f, 30f), 0f, TextAlignmentOptions.Center);

        // Nước tăng lực: viên xanh chanh nằm trên giáp (hoặc thế chỗ giáp khi không có giáp)
        RectTransform drink = Panel("EnergyDrink", _root, bl, bl, new Vector2(36f, 190f), new Vector2(118f, 36f), Glass, Lime, 0.6f, 0.25f);
        _energyRt = drink;
        hud.energyDrinkGroup = drink.gameObject;
        if (drinkIcon != null) Img(drink, "Icon", drinkIcon, Lime, new Vector2(-34f, 0f), new Vector2(22f, 22f));
        hud.energyDrinkTimerText = Txt(drink, "Timer", "15s", _bold, 19f, Lime, new Vector2(14f, 0f), new Vector2(70f, 30f), 1f, TextAlignmentOptions.Center);
    }

    // ---------- Góc dưới phải: tiền + 3 ô đạn ----------

    private void BuildInventory(HUDController hud, Sprite a, Sprite b, Sprite c)
    {
        Vector2 br = new Vector2(1f, 0f);
        Sprite[] icons = { a, b, c };
        string[] names = { "Normal", "Heavy", "Third" };
        TMP_Text[] counts = new TMP_Text[3];

        for (int i = 0; i < 3; i++)
        {
            float x = -36f - (2 - i) * 104f;
            RectTransform card = Panel("Slot" + names[i], _root, br, br, new Vector2(x, 34f), new Vector2(94f, 94f), Glass, Cyan, 0.4f, 0.18f);
            Slot s = new Slot();
            s.edge = card.Find("Edge").GetComponent<Image>();
            if (icons[i] != null) s.icon = Img(card, "Icon", icons[i], Color.white, new Vector2(0f, 6f), new Vector2(52f, 52f));
            s.count = Txt(card, "Count", "0", _black, 24f, Color.white, new Vector2(-10f, 8f), new Vector2(60f, 30f), 0f, TextAlignmentOptions.Right, br, new Vector2(1f, 0f));
            s.key = Txt(card, "Key", "", _bold, 13f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f), new Vector2(10f, -8f), new Vector2(30f, 20f), 0f, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f));
            counts[i] = s.count;
            _slots[i] = s;
        }
        hud.normalAmmoText = counts[0];
        hud.heavyAmmoText = counts[1];
        hud.spikeAmmoText = counts[2];

        hud.moneyText = Txt(_root, "Money", "$0", _black, 38f, Gold, new Vector2(-40f, 142f), new Vector2(260f, 50f), 1f, TextAlignmentOptions.Right, br, new Vector2(1f, 0f));
        Blob(_root, new Vector2(-100f, 166f), new Vector2(240f, 80f), new Color(Gold.r, Gold.g, Gold.b, 0.16f), br).transform.SetSiblingIndex(hud.moneyText.transform.GetSiblingIndex());
    }

    // ---------- Dòng thông báo kết quả round: giữ chỗ cũ, đổi áo ----------

    private void RestyleAnnouncement(HUDController hud)
    {
        if (hud.announcementText == null) return;
        if (_black != null) hud.announcementText.font = _black;
        hud.announcementText.characterSpacing = 6f;
        hud.announcementText.fontSize = 42f;

        Transform group = hud.announcementText.transform.parent;
        Image bg = group != null ? group.GetComponent<Image>() : null;
        if (bg == null) return;
        bg.sprite = FillSprite; bg.type = Image.Type.Sliced; bg.color = new Color(0.03f, 0.06f, 0.11f, 0.78f);

        Image edge = Img((RectTransform)group, "Edge", EdgeSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f), Vector2.zero, ((RectTransform)group).sizeDelta);
        edge.type = Image.Type.Sliced;
        edge.transform.SetSiblingIndex(0);   // dưới dòng chữ
    }

    // ==================== MỖI KHUNG HÌNH ====================

    private void LateUpdate()
    {
        FitRoot();
        if (_hud == null) return;

        float t = Time.unscaledTime;

        // --- Điện tích: màu số theo mức, sắp đầy thì khung đỏ lên và đập nhịp ---
        Image fill = _hud.healthFill;
        float ratio = fill != null ? fill.fillAmount : 0f;
        if (_hud.healthText != null)
        {
            Color c = ratio < 0.5f ? Soft : ratio < 0.8f ? Gold : Red;
            _hud.healthText.color = c;
            float beat = ratio >= 0.8f ? Mathf.Abs(Mathf.Sin(t * 5f)) : 0f;
            _hud.healthText.rectTransform.localScale = Vector3.one * (1f + 0.08f * beat);

            if (_chargeEdge != null)
            {
                Color edge = Color.Lerp(Cyan, Red, Mathf.InverseLerp(0.5f, 0.85f, ratio));
                _chargeEdge.color = new Color(edge.r, edge.g, edge.b, 0.5f + 0.4f * beat);
                _chargeGlow.color = new Color(edge.r, edge.g, edge.b, 0.28f + 0.35f * beat);
            }
        }
        if (_chargeBarGlow != null && fill != null)
        {
            Color fc = fill.color;
            _chargeBarGlow.color = new Color(fc.r, fc.g, fc.b, 0.12f + 0.3f * ratio);
            _chargeBarGlow.rectTransform.anchoredPosition = new Vector2((ratio - 1f) * 201f, 0f);
            _chargeBarGlow.rectTransform.sizeDelta = new Vector2(40f + 402f * ratio, 60f);
        }

        // Quầng sau thanh chiếm khu lớn dần theo phần đã lấp (0 tiến độ thì không có quầng)
        ZoneGlow(_zoneGlowRed, _hud.zoneRedFill, -1f);
        ZoneGlow(_zoneGlowBlue, _hud.zoneBlueFill, 1f);

        // Viên hệ số văng bật/tắt theo chữ (HUDController chỉ bật/tắt chữ)
        if (_knockPill != null && _hud.knockbackText != null)
        {
            bool on = _hud.knockbackText.gameObject.activeSelf;
            Transform p = _knockPill.transform;
            for (int i = 0; i < p.childCount; i++)
                if (p.GetChild(i).gameObject != _hud.knockbackText.gameObject) p.GetChild(i).gameObject.SetActive(on);
        }

        // Nền mờ sau nhãn pha bật/tắt theo nhãn
        if (_phaseHalo != null && _hud.phaseText != null && _phaseHalo.activeSelf != _hud.phaseText.gameObject.activeSelf)
            _phaseHalo.SetActive(_hud.phaseText.gameObject.activeSelf);

        // Nước tăng lực: thế chỗ khung giáp khi không có giáp
        if (_energyRt != null && _hud.armorGroup != null)
            _energyRt.anchoredPosition = new Vector2(36f, _hud.armorGroup.activeSelf ? 190f : 144f);

        // --- Vòng cực găng: quầng cùng màu, loé một nhịp khi đổi cực ---
        if (_hud.polarityIndicator != null && _ringGroup != null)
        {
            Color rc = _hud.polarityIndicator.color;
            if (rc != _lastRingColor) { if (_lastRingColor.a > 0f) _ringPulse = 1f; _lastRingColor = rc; }
            _ringPulse = Mathf.MoveTowards(_ringPulse, 0f, Time.unscaledDeltaTime * 3.5f);
            float p = _ringPulse * _ringPulse;
            _ringGroup.localScale = Vector3.one * (1f + 0.4f * p);
            _ringGlow.color = new Color(rc.r, rc.g, rc.b, 0.4f + 0.5f * p);
            if (_signDisc != null) _signDisc.color = rc;
        }

        // --- Ô đạn: ô trống thì mờ đi, có đạn thì viền sáng ---
        PlayerHotbarController hotbar = FPSMovement.Local != null ? FPSMovement.Local.GetComponent<PlayerHotbarController>() : null;
        for (int i = 0; i < 3; i++)
        {
            Slot s = _slots[i];
            if (s == null) continue;
            bool has = s.count.text != "0";
            if (s.icon != null) s.icon.color = new Color(1f, 1f, 1f, has ? 1f : 0.28f);
            s.count.color = new Color(1f, 1f, 1f, has ? 1f : 0.35f);
            s.edge.color = new Color(Cyan.r, Cyan.g, Cyan.b, has ? 0.7f : 0.22f);
            if (hotbar != null && s.key.text.Length == 0)
                s.key.text = KeyName(i == 0 ? hotbar.normalKey : i == 1 ? hotbar.heavyKey : hotbar.spikeKey);
        }
    }

    private static void ZoneGlow(Image glow, Image fill, float side)
    {
        if (glow == null || fill == null) return;
        float r = fill.fillAmount;
        Color c = fill.color;
        glow.color = new Color(c.r, c.g, c.b, r > 0.001f ? 0.18f + 0.3f * r : 0f);
        // Thanh đỏ lấp từ trái, thanh xanh lấp từ phải - tâm quầng đặt giữa phần đã lấp
        glow.rectTransform.anchoredPosition = new Vector2(side * (127f - 127f * r), 0f);
        glow.rectTransform.sizeDelta = new Vector2(30f + 254f * r, 50f);
    }

    private void FitRoot()
    {
        if (_root == null || _canvasRect == null) return;
        Vector2 size = _canvasRect.rect.size;
        if (size.x > 1f && _root.sizeDelta != size) _root.sizeDelta = size;
    }

    private static string KeyName(KeyCode k)
    {
        string s = k.ToString();
        if (s.StartsWith("Alpha")) return s.Substring(5);
        return s.Length > 3 ? s.Substring(0, 3) : s;
    }

    // ==================== KHỐI DỰNG SẴN ====================

    /// <summary>Khung kính: quầng neon (ngoài) + nền tối + viền sáng. Trả về object khung.</summary>
    private RectTransform Panel(string name, RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
                                Color fill, Color edge, float edgeAlpha, float glowAlpha)
    {
        RectTransform p = Rt(name, parent, anchor, pivot, pos, size);
        Image glow = Img(p, "Glow", GlowSprite, new Color(edge.r, edge.g, edge.b, glowAlpha), Vector2.zero, size + new Vector2(36f, 36f));
        glow.type = Image.Type.Sliced;
        Image bg = Img(p, "Fill", FillSprite, fill, Vector2.zero, size);
        bg.type = Image.Type.Sliced;
        Image e = Img(p, "Edge", EdgeSprite, new Color(edge.r, edge.g, edge.b, edgeAlpha), Vector2.zero, size);
        e.type = Image.Type.Sliced;
        return p;
    }

    private static RectTransform Rt(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image Img(RectTransform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rt(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image i = rt.gameObject.AddComponent<Image>();
        i.sprite = sprite;
        i.color = color;
        i.raycastTarget = false;
        return i;
    }

    private static Image Bar(RectTransform parent, Vector2 pos, Vector2 size, Color color) =>
        Img(parent, "Bar", WhiteSprite, color, pos, size);

    private static Image Blob(RectTransform parent, Vector2 pos, Vector2 size, Color color, Vector2? anchor = null)
    {
        Image i = Img(parent, "Halo", BlobSprite, color, pos, size);
        if (anchor.HasValue) i.rectTransform.anchorMin = i.rectTransform.anchorMax = anchor.Value;
        return i;
    }

    private TMP_Text Txt(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color,
                         Vector2 pos, Vector2 box, float spacing, TextAlignmentOptions align,
                         Vector2? anchor = null, Vector2? pivot = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        RectTransform rt = Rt(name, parent, a, pivot ?? (anchor.HasValue ? a : new Vector2(0.5f, 0.5f)), pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.characterSpacing = spacing;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Sprite OldIcon(TMP_Text oldCount)
    {
        if (oldCount == null) return null;
        Transform icon = oldCount.transform.Find("Icon");
        Image img = icon != null ? icon.GetComponent<Image>() : null;
        return img != null ? img.sprite : null;
    }

    private static Sprite FirstSprite(Transform t)
    {
        foreach (Image i in t.GetComponentsInChildren<Image>(true))
            if (i.sprite != null && i.transform != t) return i.sprite;
        return null;
    }

    private static Sprite IconFromResources(string path)
    {
        Texture2D tex = Resources.Load<Texture2D>(path);
        return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
    }

    // ==================== ẢNH VẼ BẰNG CODE ====================
    //
    // Khung góc vát dùng "hàm khoảng cách": mỗi điểm ảnh tính khoảng cách tới mép hình bát giác
    // (chữ nhật vát 4 góc). Âm = ở trong, dương = ở ngoài. Từ một con số đó suy ra được nền
    // (trong thì đặc), viền (gần 0 thì sáng) và quầng (ngoài thì mờ dần) - mép luôn mượt.

    private const int Chamfer = 12;
    private static Sprite _fill, _edge, _glow, _seg, _ring, _ringHalo, _blob, _white, _disc, _wing;

    internal static Sprite FillSprite => _fill != null ? _fill : (_fill = ShapeSprite(64, 0, d => Mathf.Clamp01(0.5f - d), 20));
    internal static Sprite EdgeSprite => _edge != null ? _edge : (_edge = ShapeSprite(64, 0, d => Mathf.Clamp01(1.6f - Mathf.Abs(d + 1.2f)), 20));
    private static Sprite GlowSprite => _glow != null ? _glow : (_glow = ShapeSprite(100, 18,
        d => d >= 0f ? Mathf.Exp(-d / 6f) : Mathf.Clamp01(1f + d / 3f), 38));

    private static Sprite ShapeSprite(int size, int pad, System.Func<float, float> alpha, int border)
    {
        Texture2D tex = NewTex(size, size);
        Color32[] px = new Color32[size * size];
        float h = size * 0.5f - pad;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float ax = Mathf.Abs(x + 0.5f - size * 0.5f), ay = Mathf.Abs(y + 0.5f - size * 0.5f);
                float d = Mathf.Max(Mathf.Max(ax - h, ay - h), (ax + ay - (2f * h - Chamfer)) * 0.70710678f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(d)) * 255f));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                             new Vector4(border, border, border, border));
    }

    /// <summary>Thanh chia 24 vạch xiên - kiểu đồng hồ năng lượng khoa học viễn tưởng.</summary>
    private static Sprite SegSprite
    {
        get
        {
            if (_seg != null) return _seg;
            const int w = 528, h = 24, n = 24;
            float step = (float)w / n;
            Texture2D tex = NewTex(w, h);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sx = x + 0.5f - (y + 0.5f) * 0.25f;          // xiên
                    float local = Mathf.Repeat(sx, step);
                    float a = Mathf.Clamp01(local - 0.5f) * Mathf.Clamp01(step - 5f - local + 0.5f);
                    if (x < 3 || x > w - 3) a = 0f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return _seg = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    private static Sprite RingSprite => _ring != null ? _ring : (_ring = RadialSprite(128, r => Mathf.Clamp01(2.2f - Mathf.Abs(r - 56f))));
    private static Sprite RingGlowSprite => _ringHalo != null ? _ringHalo : (_ringHalo = RadialSprite(128, r => Mathf.Exp(-Mathf.Abs(r - 36f) / 6f)));
    private static Sprite DiscSprite => _disc != null ? _disc : (_disc = RadialSprite(64, r => Mathf.Clamp01(30.5f - r)));

    /// <summary>Cánh màu đội của khung tỉ số: góc vát phía ngoài, mờ dần vào giữa.</summary>
    private static Sprite WingSprite
    {
        get
        {
            if (_wing != null) return _wing;
            const int w = 150, h = 88;
            Texture2D tex = NewTex(w, h);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x + 0.5f, fy = Mathf.Min(y + 0.5f, h - y - 0.5f);
                    float d = Mathf.Min(fx + fy - Chamfer, Mathf.Min(fx, fy)) ;   // >0 = trong hình (góc vát bên trái)
                    float shape = Mathf.Clamp01(d - 1f);
                    float fade = Mathf.Pow(1f - (float)x / w, 1.6f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(shape * fade * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return _wing = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    internal static Sprite BlobSprite => _blob != null ? _blob : (_blob = RadialSprite(64, r => { float k = Mathf.Clamp01(1f - r / 32f); return k * k; }));

    internal static Sprite WhiteSprite
    {
        get
        {
            if (_white != null) return _white;
            Texture2D tex = NewTex(4, 4);
            Color32[] px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            return _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    private static Sprite RadialSprite(int size, System.Func<float, float> alpha)
    {
        Texture2D tex = NewTex(size, size);
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(r)) * 255f));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Texture2D NewTex(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }
}
