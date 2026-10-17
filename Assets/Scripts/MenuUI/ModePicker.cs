using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng hỏi "1v1 hay 2v2?" hiện ra khi bấm nút Ghép Trận (16/10).
///
/// Dựng hoàn toàn bằng code lúc chạy, không phải kéo thả gì trong scene: phủ tối toàn màn
/// hình + một bảng nhỏ với hai thẻ lớn 1 VS 1 / 2 VS 2 và nút Cancel. Mượn font và khung ảnh
/// của LobbyView (đã gán sẵn trong MenuScene) cho cùng tông với phòng chờ.
///
/// Nhớ lựa chọn lần trước (PlayerPrefs) để chọn sẵn thẻ đó — bấm Enter là ghép luôn.
/// Esc hoặc Cancel thì đóng, không ghép gì cả.
/// </summary>
public class ModePicker : MonoBehaviour
{
    private const string KeyLastMode = "quick_match_team_size";

    private static readonly Color Cyan = new Color32(0x5C, 0xD8, 0xFF, 0xFF);
    private static readonly Color Soft = new Color(0.86f, 0.9f, 0.96f, 1f);
    private static readonly Color Navy = new Color(0.05f, 0.08f, 0.14f, 0.97f);
    private static readonly Color CardIdle = new Color(0.09f, 0.13f, 0.21f, 1f);

    private static ModePicker _current;

    /// <summary>Bảng đang mở. SettingsUI hỏi để Esc chỉ đóng bảng này, không mở luôn Settings.</summary>
    public static bool IsOpen => _current != null;

    private Action<int> _onPick;
    private CanvasGroup _group;
    private RectTransform _panel;
    private float _openedAt;

    private TMP_FontAsset _fontBlack, _fontBold, _fontMedium;
    private Sprite _frame;

    /// <summary>Mở bảng chọn. <paramref name="onPick"/> nhận 1 (1v1) hoặc 2 (2v2).</summary>
    public static void Show(Transform canvasRoot, Action<int> onPick)
    {
        // Đang mở rồi thì thôi — bấm nút Ghép Trận hai lần không ra hai bảng chồng nhau
        if (IsOpen) return;

        GameObject root = new GameObject("ModePickerPopup", typeof(RectTransform));
        root.transform.SetParent(canvasRoot, false);
        ModePicker picker = root.AddComponent<ModePicker>();
        _current = picker;
        picker._onPick = onPick;
        picker.Build();
    }

    private void Build()
    {
        LobbyView style = FindFirstObjectByType<LobbyView>(FindObjectsInactive.Include);
        if (style != null)
        {
            _fontBlack = style.fontBlack;
            _fontBold = style.fontBold;
            _fontMedium = style.fontMedium;
            _frame = style.cardSprite;
        }

        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        transform.SetAsLastSibling();

        // Canvas riêng thứ tự cao: luôn nằm trên mọi bảng menu, có GraphicRaycaster riêng để bấm được
        Canvas c = gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 250;
        gameObject.AddComponent<GraphicRaycaster>();
        _group = gameObject.AddComponent<CanvasGroup>();

        // Lớp tối chặn bấm xuyên xuống menu. Alpha cao vì project dùng màu LINEAR (xem bảng Quit Game)
        Image dim = gameObject.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.02f, 0.05f, 0.92f);

        // Nền trơn, KHÔNG dùng khung ảnh của LobbyView: ảnh đó vốn hơi trong, nhìn xuyên thấy menu phía sau
        _panel = Rect(rt, "Panel", Vector2.zero, new Vector2(780f, 440f));
        Image bg = _panel.gameObject.AddComponent<Image>();
        bg.color = Navy;
        Img(_panel, "TopLine", new Vector2(0f, 218f), new Vector2(780f, 4f), Cyan);

        Txt(_panel, "Title", "QUICK MATCH", _fontBlack, 46, Color.white, new Vector2(0f, 162f), new Vector2(700f, 70f), 6f);
        Txt(_panel, "Sub", "Choose a mode", _fontMedium, 22, Soft, new Vector2(0f, 112f), new Vector2(700f, 40f), 2f).alpha = 0.7f;

        int last = Mathf.Clamp(PlayerPrefs.GetInt(KeyLastMode, RoomPlayer.MaxTeamSize), 1, RoomPlayer.MaxTeamSize);
        Button duel = ModeCard(_panel, new Vector2(-170f, -20f), "1 VS 1", "Duel  ·  2 players", 1, last == 1);
        Button team = ModeCard(_panel, new Vector2(170f, -20f), "2 VS 2", "Team  ·  4 players", 2, last == 2);

        Button cancel = Btn(_panel, "Cancel", new Vector2(0f, -168f), new Vector2(200f, 50f), new Color(1f, 1f, 1f, 0.06f));
        TMP_Text cl = Txt((RectTransform)cancel.transform, "Label", "Cancel", _fontBold, 22, Soft, Vector2.zero, new Vector2(200f, 50f), 2f);
        cl.alpha = 0.8f;
        cancel.onClick.AddListener(Close);

        // Chọn sẵn thẻ lần trước: bấm Enter / Space là ghép luôn
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null) es.SetSelectedGameObject((last == 1 ? duel : team).gameObject);

        _openedAt = Time.unscaledTime;
        Animate();
    }

    private Button ModeCard(RectTransform parent, Vector2 pos, string big, string small, int teamSize, bool lastPicked)
    {
        Button b = Btn(parent, "Mode" + teamSize, pos, new Vector2(300f, 190f), CardIdle);
        ((Image)b.targetGraphic).sprite = null;   // khung ảnh có viền trong suốt, vạch cyan bị lệch ra ngoài thẻ
        RectTransform r = (RectTransform)b.transform;

        // Vạch cyan trên đầu thẻ — thẻ đã chọn lần trước thì đậm hơn
        Image bar = Img(r, "Bar", new Vector2(0f, 92.5f), new Vector2(300f, 5f), Cyan);
        bar.color = new Color(Cyan.r, Cyan.g, Cyan.b, lastPicked ? 1f : 0.35f);

        Txt(r, "Big", big, _fontBlack, 56, Color.white, new Vector2(0f, 18f), new Vector2(290f, 80f), 4f);
        Txt(r, "Small", small, _fontMedium, 19, Soft, new Vector2(0f, -46f), new Vector2(290f, 34f), 1f).alpha = 0.75f;
        if (lastPicked)
            Txt(r, "Last", "LAST PICKED", _fontBold, 13, Cyan, new Vector2(0f, -76f), new Vector2(290f, 24f), 4f).alpha = 0.8f;

        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.75f, 0.92f, 1f);
        cb.selectedColor = new Color(0.82f, 0.95f, 1f);
        cb.pressedColor = new Color(0.55f, 0.8f, 1f);
        b.colors = cb;

        b.onClick.AddListener(() => Pick(teamSize));
        return b;
    }

    private void Pick(int teamSize)
    {
        PlayerPrefs.SetInt(KeyLastMode, teamSize);
        PlayerPrefs.Save();
        Action<int> cb = _onPick;
        Destroy(gameObject);
        cb?.Invoke(teamSize);
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    // Xoá ở OnDestroy (cuối khung hình) chứ không phải lúc gọi Destroy: SettingsUI chạy Update
    // cùng khung hình đó vẫn phải thấy bảng còn mở, không thì cú Esc vừa đóng bảng lại mở Settings.
    private void OnDestroy()
    {
        if (_current == this) _current = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        Animate();
    }

    // Hiện lên: mờ dần + bảng phóng nhẹ 92% → 100% trong 0,16 giây (giống bảng Quit Game)
    private void Animate()
    {
        float k = Mathf.Clamp01((Time.unscaledTime - _openedAt) / 0.16f);
        float ease = 1f - Mathf.Pow(1f - k, 3f);
        if (_group != null) _group.alpha = ease;
        if (_panel != null) _panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, ease);
    }

    // ---------- tiện ích dựng giao diện ----------

    private static RectTransform Rect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static TMP_Text Txt(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color,
                                Vector2 pos, Vector2 box, float spacing)
    {
        box.y = Mathf.Max(box.y, size * 1.6f);   // khung thấp hơn dòng chữ thì TextMeshPro giấu mất cả dòng
        RectTransform rt = Rect(parent, name, pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.characterSpacing = spacing;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static Image Img(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        Image img = Rect(parent, name, pos, size).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Button Btn(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = Rect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (_frame != null) { img.sprite = _frame; img.type = Image.Type.Sliced; }
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        return b;
    }

#if UNITY_EDITOR
    /// <summary>Chỉ để chụp ảnh xem trước trong Editor: hiện ngay, bỏ qua hiệu ứng mờ dần.</summary>
    public static void PreviewShow(Transform canvasRoot)
    {
        Show(canvasRoot, null);
        ModePicker p = _current;
        if (p != null) { p._openedAt = -10f; p.Animate(); }
    }
#endif
}
