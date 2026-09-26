using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng hướng dẫn điều khiển hiện trong trận. Bấm F1 để bật/tắt.
///
/// ⚠️ VÌ SAO CẦN: game có 15 phím và một cơ chế chưa game nào có (trúng đòn không mất máu
/// mà bị nhiễm điện rồi văng xa hơn). Người lần đầu mở game - kể cả hội đồng chấm - sẽ
/// bấm bừa vài phím, không hiểu gì, rồi kết luận. Toàn bộ chiều sâu của game không ai thấy.
///
/// ⚠️ TỰ DỰNG BẰNG CODE, KHÔNG CẦN KÉO THẢ GÌ TRONG UNITY.
/// Lý do không phải là lười: bảng phím mà đi gõ tay vào một Text trong scene thì nó sẽ
/// LỆCH với phím thật ngay lần đầu ai đó đổi phím trong Inspector, và không ai nhớ ra để
/// sửa. Ở đây mỗi dòng đọc thẳng KeyCode từ chính component đang xử lý phím đó.
///
/// ⚠️ MỖI DÒNG LÀ MỘT HÀNG THẬT, ĐỪNG QUAY LẠI KIỂU "HAI KHỐI CHỮ".
/// Bản đầu (25/09) đặt toàn bộ cột phím vào MỘT ô chữ và toàn bộ cột mô tả vào một ô
/// khác, khớp nhau theo thứ tự dòng. Chỉ cần MỘT mô tả dài tràn xuống dòng thứ hai là
/// mọi dòng phía dưới lệch hẳn khỏi phím của nó - mà mô tả dài thì chắc chắn có.
/// Giờ mỗi hàng là một object riêng, chiều cao do LayoutGroup tự tính, nên mô tả dài
/// bao nhiêu dòng cũng không kéo hàng khác đi theo.
/// </summary>
public class ControlsOverlay : MonoBehaviour
{
    [Tooltip("Phím bật/tắt bảng hướng dẫn.")]
    public KeyCode toggleKey = KeyCode.F1;

    [Tooltip("Phím thứ hai, làm việc y hệt phím trên. Để None nếu không cần.\n\n" +
             "Có nó vì Unity Editor đôi khi nuốt mất F1 (vốn là phím mở tài liệu của " +
             "Unity), khiến không test được bảng này lúc bấm Play. Bản build thì F1 luôn " +
             "chạy bình thường - phím này chỉ là lối thoát khi test trong Editor.\n\n" +
             "Chỉ F1 được ghi trong bảng, để người chơi không phải nhớ hai phím.")]
    public KeyCode altToggleKey = KeyCode.H;

    [Tooltip("Tự hiện bảng lần đầu vào trận, trong pha chuẩn bị. Tắt đi thì chỉ hiện khi " +
             "người chơi tự bấm phím.\n\n" +
             "Nên bật: người chưa biết game thì cũng KHÔNG BIẾT LÀ CÓ bảng này để mà bấm.")]
    public bool autoShowFirstRound = true;

    // --- Bố cục. Đổi ở đây, không rải số vào giữa code dựng giao diện ---
    private const float PanelWidth = 900f;
    private const float KeyColumnWidth = 250f;   // đủ cho "Tab (hold)" và "Z / X / C"
    private const float ColumnGap = 28f;
    private const float RowSpacing = 9f;
    private const int PadSide = 44;
    private const int PadTop = 34;
    private const int PadBottom = 30;

    private static readonly Color ColAccent = new Color(0.16f, 0.91f, 1f);
    private static readonly Color ColKey = new Color(1f, 0.76f, 0.29f);
    private static readonly Color ColText = new Color(0.91f, 0.93f, 0.95f);
    private static readonly Color ColSection = new Color(0.45f, 0.62f, 0.75f);
    private static readonly Color ColPanel = new Color(0.04f, 0.05f, 0.08f, 0.9f);

    private GameObject _root;
    private RectTransform _body;   // nơi chứa các hàng
    private bool _visible;
    private bool _autoShown;
    private bool _built;

    /// <summary>Tự tạo một bản duy nhất khi game chạy, sống xuyên scene.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        GameObject go = new GameObject("ControlsOverlay");
        go.AddComponent<ControlsOverlay>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        bool inMatch = FPSMovement.Local != null;

        if (!inMatch)
        {
            if (_visible) Show(false);
            _autoShown = false;   // vào trận sau lại được tự hiện một lần nữa
            return;
        }

        bool pressed = Input.GetKeyDown(toggleKey)
                       || (altToggleKey != KeyCode.None && Input.GetKeyDown(altToggleKey));

        if (pressed) Show(!_visible);

        GameManager gm = GameManager.Instance;
        bool gmAlive = gm != null && gm.Object != null && gm.Object.IsValid;
        if (!gmAlive) return;

        // TỰ HIỆN TRONG PHA CHUẨN BỊ ĐẦU TIÊN - lúc duy nhất người chơi rảnh tay:
        // rào còn đóng, chưa ai bắn ai. Hiện lúc đang đánh nhau thì vừa che vừa không ai đọc.
        if (autoShowFirstRound && !_autoShown && gm.Phase == GameManager.GamePhase.BuyPhase)
        {
            _autoShown = true;
            Show(true);
        }

        // Vào chiến đấu là tự tắt, khỏi phải nhớ bấm lại.
        if (_visible && gm.Phase == GameManager.GamePhase.Combat) Show(false);
    }

    private void Show(bool on)
    {
        if (on && !_built) Build();
        if (_root == null) return;

        _visible = on;
        _root.SetActive(on);

        if (on) Refresh();
    }

    // ==================== NỘI DUNG ====================

    /// <summary>
    /// Dựng lại toàn bộ các hàng theo phím THẬT đang gán trên nhân vật của mình.
    /// Đọc lại mỗi lần mở vì phím nằm trên component của nhân vật, mà nhân vật chỉ tồn
    /// tại sau khi vào trận.
    /// </summary>
    private void Refresh()
    {
        FPSMovement move = FPSMovement.Local;
        if (move == null || _body == null) return;

        for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

        PlayerHotbarController hotbar = move.GetComponent<PlayerHotbarController>();
        PlayerMagnetController magnet = move.GetComponent<PlayerMagnetController>();
        PlayerInteract interact = move.GetComponent<PlayerInteract>();

        // Mấy thứ này nằm trên Canvas chứ không trên nhân vật.
        RadialMenuController radial = FindFirstObjectByType<RadialMenuController>();
        ShopUI shop = FindFirstObjectByType<ShopUI>();
        SettingsUI settings = FindFirstObjectByType<SettingsUI>();

        Section("MOVEMENT");
        Row("W A S D", "Move");
        Row("Mouse", "Look around");
        Row(Name(move.jumpKey), "Jump — you can still steer in mid-air");
        Row(Name(move.dashKey), "Dash");

        Section("MAGNETIC GLOVE");
        Row("1  /  2", "Switch charge:  <color=#FF5555><b>POSITIVE</b></color>  /  <color=#5599FF><b>NEGATIVE</b></color>");
        Row("Left Click", "Neutral object → charge it.  Already charged → " +
                          "<b>same</b> charge pushes it away, <b>opposite</b> pulls it to your hand");
        Row("Right Click", "Empty hands → melee punch.  Holding an object → throw it");
        Row(Name(magnet != null ? magnet.tossKey : KeyCode.V), "Toss the held object straight up");

        Section("ITEMS");
        Row(Name(interact != null ? interact.interactKey : KeyCode.F), "Store the held object in your bag");
        Row(KeyTriple(hotbar), "Draw ammo from the bag");
        Row(Name(radial != null ? radial.menuKey : KeyCode.Tab) + "  (hold)",
            "Item wheel — aim with the mouse, release to use");
        Row(Name(shop != null ? shop.shopKey : KeyCode.B), "Shop — buy phase only");

        Section("SYSTEM");
        Row(Name(settings != null ? settings.toggleKey : KeyCode.Escape), "Settings");
        Row(Name(toggleKey), "Show / hide this panel");
    }

    private static string KeyTriple(PlayerHotbarController hotbar)
    {
        if (hotbar == null) return "Z / X / C";
        return $"{Name(hotbar.normalKey)} / {Name(hotbar.heavyKey)} / {Name(hotbar.spikeKey)}";
    }

    /// <summary>Tên phím cho người đọc: "Alpha1" -> "1", "LeftShift" -> "Shift".</summary>
    private static string Name(KeyCode key)
    {
        string raw = key.ToString();

        if (raw.StartsWith("Alpha")) return raw.Substring(5);
        if (raw.StartsWith("Left")) return raw.Substring(4);
        if (raw.StartsWith("Right")) return raw.Substring(5);

        return raw;
    }

    // ==================== DỰNG TỪNG HÀNG ====================

    /// <summary>
    /// Một hàng = một object có HorizontalLayoutGroup, gồm ô phím và ô mô tả.
    ///
    /// Ô phím rộng CỐ ĐỊNH nên mọi mô tả thẳng hàng nhau; ô mô tả lấy hết phần còn lại
    /// (flexibleWidth) nên chữ dài tự xuống dòng TRONG ô của nó, không đẩy hàng khác.
    /// </summary>
    private void Row(string key, string action)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(_body, false);

        HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = ColumnGap;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        h.childAlignment = TextAnchor.UpperLeft;

        TMP_Text k = MakeText("Key", row.transform, key, 24f, ColKey, TextAlignmentOptions.TopRight);
        k.fontStyle = FontStyles.Bold;
        k.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement kl = k.gameObject.AddComponent<LayoutElement>();
        kl.preferredWidth = KeyColumnWidth;
        kl.flexibleWidth = 0f;

        TMP_Text a = MakeText("Action", row.transform, action, 22f, ColText, TextAlignmentOptions.TopLeft);
        LayoutElement al = a.gameObject.AddComponent<LayoutElement>();
        al.flexibleWidth = 1f;
    }

    /// <summary>Tiêu đề nhóm. Thay cho dòng trống - nhóm phím lại thì mắt quét nhanh hơn.</summary>
    private void Section(string title)
    {
        GameObject go = new GameObject("Section", typeof(RectTransform));
        go.transform.SetParent(_body, false);

        TMP_Text t = MakeText("Label", go.transform, title, 17f, ColSection, TextAlignmentOptions.BottomLeft);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 8f;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        LayoutElement le = go.AddComponent<LayoutElement>();

        // Chừa khoảng thở phía trên mỗi nhóm. Nhóm đầu tiên cũng chừa, không sao -
        // nó nằm ngay dưới gạch ngang của tiêu đề.
        le.preferredHeight = 44f;
        le.minHeight = 44f;
    }

    // ==================== KHUNG BẢNG ====================

    private void Build()
    {
        _built = true;

        GameObject canvasGo = new GameObject("ControlsCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;   // vẽ trên HUD, Shop, Radial Menu

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // --- Tấm nền ---
        _root = new GameObject("Panel", typeof(RectTransform));
        _root.transform.SetParent(canvasGo.transform, false);

        RectTransform panel = (RectTransform)_root.transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(PanelWidth, 0f);   // chiều cao do nội dung quyết định

        Image bg = _root.AddComponent<Image>();

        // Nền tối nhưng KHÔNG đục hẳn: vẫn thấy sân đấu phía sau. Cũng vì vậy mà nó không
        // chặn chuột - mở bảng ra vẫn xoay camera và chơi bình thường.
        bg.color = ColPanel;
        bg.raycastTarget = false;

        // BẢNG TỰ CO THEO NỘI DUNG.
        //
        // ⚠️ Đây là chỗ bản đầu làm sai: panel bị đặt cứng 880x700 nên thừa một mảng trống
        // to ở dưới, còn cột mô tả thì lại chật. Giờ chiều cao = đúng nội dung, không thừa
        // một pixel, và thêm bớt phím sau này cũng không phải chỉnh lại con số nào.
        VerticalLayoutGroup v = _root.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(PadSide, PadSide, PadTop, PadBottom);
        v.spacing = 14f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        ContentSizeFitter fit = _root.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Tiêu đề ---
        TMP_Text title = MakeText("Title", _root.transform, "CONTROLS", 40f, ColAccent,
                                  TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 6f;
        AddHeight(title.gameObject, 52f);

        AddRule();

        // --- Vùng chứa các hàng phím ---
        GameObject bodyGo = new GameObject("Rows", typeof(RectTransform));
        bodyGo.transform.SetParent(_root.transform, false);
        _body = (RectTransform)bodyGo.transform;

        VerticalLayoutGroup bv = bodyGo.AddComponent<VerticalLayoutGroup>();
        bv.spacing = RowSpacing;
        bv.childControlWidth = true;
        bv.childControlHeight = true;
        bv.childForceExpandWidth = true;
        bv.childForceExpandHeight = false;

        AddRule();

        // --- Giải thích cơ chế ---
        //
        // Đây mới là phần quan trọng nhất. Biết phím nào làm gì mà không hiểu "trúng đòn
        // không mất máu" thì người chơi vẫn chơi sai: họ né đòn để giữ máu, trong khi thứ
        // thật sự giết họ là đứng gần rìa vực.
        TMP_Text rule = MakeText("Rule", _root.transform,
            "<b><color=#FFC24B>OVERLOAD</color></b>   There is no health bar. " +
            "Getting hit <b>charges</b> you, and the more charge you carry the further " +
            "every hit throws you.\n" +
            "<b>You only die by falling off the island.</b>  Watch the edge, not your bar.",
            21f, new Color(0.75f, 0.8f, 0.86f), TextAlignmentOptions.TopLeft);
        rule.lineSpacing = 18f;

        _root.SetActive(false);
    }

    /// <summary>Gạch ngang mảnh ngăn các phần.</summary>
    private void AddRule()
    {
        GameObject go = new GameObject("Rule", typeof(RectTransform));
        go.transform.SetParent(_root.transform, false);

        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f);
        img.raycastTarget = false;

        AddHeight(go, 2f);
    }

    private static void AddHeight(GameObject go, float height)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    private static TMP_Text MakeText(string name, Transform parent, string content,
                                     float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.richText = true;

        return text;
    }
}
