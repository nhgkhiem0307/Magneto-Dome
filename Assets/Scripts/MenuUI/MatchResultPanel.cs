using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng kết quả hiện khi trận đấu kết thúc: THẮNG, THUA, hoặc TRẬN BỊ HUỶ.
///
/// ⚠️ VÌ SAO CẦN BẢNG RIÊNG, KHÔNG DÙNG DÒNG THÔNG BÁO CŨ:
/// trước đây kết thúc trận chỉ hiện một dòng chữ nhỏ dùng chung với thông báo hết round,
/// nội dung là "RED TEAM WINS!". Hai vấn đề:
///   1. Nó nói ĐỘI NÀO thắng, không nói NGƯỜI ĐANG XEM thắng hay thua. Người chơi phải
///      tự nhớ mình đội nào rồi đối chiếu - đúng lúc họ chỉ muốn biết ngay kết quả.
///   2. Kết thúc TRẬN và kết thúc ROUND trông giống nhau, nên khoảnh khắc đáng lẽ là cao
///      trào của cả trận đấu lại trôi qua như một thông báo thường.
///
/// ⚠️ VÌ SAO CÓ HIỆU ỨNG XUẤT HIỆN THEO LỚP, KHÔNG HIỆN CÙNG MỘT LÚC:
/// mọi thứ bật ra cùng lúc thì mắt không biết nhìn đâu trước và toàn bộ thành một mảng
/// chữ. Cho từng lớp vào theo thứ tự - màn tối, thanh điện ảnh, tiêu đề, rồi tỉ số - thì
/// mắt được dẫn theo đúng thứ tự quan trọng, và khoảnh khắc này có nhịp riêng của nó.
///
/// ⚠️ TỰ DỰNG BẰNG CODE, KHÔNG CẦN KÉO THẢ GÌ TRONG UNITY.
/// </summary>
public class MatchResultPanel : MonoBehaviour
{
    // --- Bảng màu theo kết quả ---
    private static readonly Color ColWin = new Color(1f, 0.78f, 0.25f);     // hổ phách
    private static readonly Color ColLose = new Color(1f, 0.33f, 0.28f);    // đỏ
    private static readonly Color ColCancel = new Color(0.62f, 0.72f, 0.82f); // xám lạnh
    private static readonly Color ColText = new Color(0.92f, 0.94f, 0.96f);
    private static readonly Color ColMuted = new Color(0.58f, 0.64f, 0.71f);

    // --- Nhịp xuất hiện (giây) ---
    private const float DimIn = 0.35f;
    private const float BarsIn = 0.45f;
    private const float TitleAt = 0.18f, TitleIn = 0.40f;
    private const float LinesAt = 0.30f, LinesIn = 0.45f;
    private const float BodyAt = 0.55f, BodyIn = 0.35f;
    private const float FootAt = 0.80f, FootIn = 0.30f;

    private const float BarHeight = 96f;    // thanh điện ảnh trên/dưới
    private const float LineWidth = 560f;   // gạch ngang hai bên tiêu đề

    private GameObject _root;
    private Image _dim, _barTop, _barBottom, _lineTop, _lineBottom, _timerFill, _timerTrack;
    private TMP_Text _title, _subtitle, _scoreMine, _scoreDash, _scoreTheirs, _scoreLabel, _countdown;
    private CanvasGroup _gTitle, _gBody, _gFoot;
    private RectTransform _titleRt;

    private bool _built, _visible;
    private float _shownAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        GameObject go = new GameObject("MatchResultPanel");
        go.AddComponent<MatchResultPanel>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        // Mạng đã chết thì object vẫn còn nằm đó vài khung hình - đụng vào đồng hồ mạng
        // lúc đó là ném lỗi mỗi khung hình. Cùng cái bẫy đã sửa ở PlayerHealth và HUD.
        bool alive = gm != null && gm.Object != null && gm.Object.IsValid && gm.Runner != null;

        if (!alive || gm.Phase != GameManager.GamePhase.MatchEnd)
        {
            if (_visible) Show(false);
            return;
        }

        if (!_visible)
        {
            Show(true);
            Fill(gm);
        }

        Animate(gm);
    }

    private void Show(bool on)
    {
        if (on && !_built) Build();
        if (_root == null) return;

        _visible = on;
        _root.SetActive(on);

        if (on) _shownAt = Time.unscaledTime;
    }

    // ==================== NỘI DUNG ====================

    private void Fill(GameManager gm)
    {
        PlayerHealth me = FPSMovement.Local != null
            ? FPSMovement.Local.GetComponent<PlayerHealth>()
            : null;

        Color accent;

        if (gm.MatchAbandoned || gm.MatchWinner < 0)
        {
            // TRẬN BỊ HUỶ: không ai thắng. Phải nói rõ LÝ DO, nếu không đội đang dẫn điểm
            // sẽ tưởng mình bị xử thua.
            _title.text = "MATCH CANCELLED";
            _subtitle.text = "A PLAYER LEFT THE MATCH";
            accent = ColCancel;
        }
        else if (me != null)
        {
            bool won = gm.MatchWinner == me.Team;
            _title.text = won ? "VICTORY" : "DEFEAT";
            _subtitle.text = won ? "YOUR TEAM WINS THE MATCH" : "YOUR TEAM LOSES THE MATCH";
            accent = won ? ColWin : ColLose;
        }
        else
        {
            // Không xác định được đội của mình (hiếm) - báo theo tên đội cho khỏi sai.
            _title.text = gm.MatchWinner == 0 ? "RED TEAM WINS" : "BLUE TEAM WINS";
            _subtitle.text = "";
            accent = ColWin;
        }

        _title.color = accent;
        _lineTop.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        _lineBottom.color = _lineTop.color;
        _timerFill.color = new Color(accent.r, accent.g, accent.b, 0.75f);

        // TỈ SỐ THEO GÓC NHÌN NGƯỜI XEM: điểm mình trước và tô màu nhấn, điểm địch mờ đi.
        // Cùng một tỉ số nhưng đọc được ngay ai là ai, không phải nhớ mình đội nào.
        int mine, theirs;
        if (me != null)
        {
            mine = me.Team == 0 ? gm.RedScore : gm.BlueScore;
            theirs = me.Team == 0 ? gm.BlueScore : gm.RedScore;
            _scoreLabel.text = "YOU                    OPPONENT";
        }
        else
        {
            mine = gm.RedScore;
            theirs = gm.BlueScore;
            _scoreLabel.text = "RED                         BLUE";
        }

        _scoreMine.text = mine.ToString();
        _scoreTheirs.text = theirs.ToString();
        _scoreMine.color = accent;
        _scoreTheirs.color = ColMuted;
    }

    // ==================== HIỆU ỨNG ====================

    private void Animate(GameManager gm)
    {
        float t = Time.unscaledTime - _shownAt;

        // Màn tối vào trước, dọn chỗ cho mọi thứ phía sau.
        SetAlpha(_dim, Mathf.Lerp(0f, 0.86f, Ease(t / DimIn)));

        // Thanh điện ảnh trên dưới trượt vào. Đây là quy ước phim ảnh: khung hình hẹp lại
        // báo cho người xem biết "đoạn này không phải lúc chơi nữa, ngồi xem".
        float bars = Ease(t / BarsIn) * BarHeight;
        _barTop.rectTransform.sizeDelta = new Vector2(0f, bars);
        _barBottom.rectTransform.sizeDelta = new Vector2(0f, bars);

        // Tiêu đề: nảy từ to về đúng cỡ. Vượt cỡ rồi lún lại làm chữ có cảm giác CÓ SỨC
        // NẶNG, thay vì chỉ hiện ra.
        float tt = Mathf.Clamp01((t - TitleAt) / TitleIn);
        _gTitle.alpha = tt;
        float pop = Mathf.Lerp(1.35f, 1f, Ease(tt));
        if (tt >= 1f) pop = 1f + 0.012f * Mathf.Sin((t - TitleAt - TitleIn) * 2.2f); // thở nhẹ
        _titleRt.localScale = Vector3.one * pop;

        // Hai gạch ngang mở ra từ giữa, ôm lấy tiêu đề.
        float lw = Mathf.Lerp(0f, LineWidth, Ease(Mathf.Clamp01((t - LinesAt) / LinesIn)));
        _lineTop.rectTransform.sizeDelta = new Vector2(lw, 2f);
        _lineBottom.rectTransform.sizeDelta = new Vector2(lw, 2f);

        _gBody.alpha = Mathf.Clamp01((t - BodyAt) / BodyIn);
        _gFoot.alpha = Mathf.Clamp01((t - FootAt) / FootIn);

        // Đồng hồ về menu: vừa đếm bằng chữ vừa rút bằng thanh. Thanh cho biết "sắp xong"
        // mà không cần đọc số - liếc một cái là đủ.
        float? left = gm.PhaseTimer.RemainingTime(gm.Runner);
        float total = Mathf.Max(0.01f, gm.matchEndDuration);

        _countdown.text = left.HasValue
            ? $"RETURNING TO MENU IN {Mathf.CeilToInt(left.Value)}"
            : "RETURNING TO MENU";

        float ratio = left.HasValue ? Mathf.Clamp01(left.Value / total) : 1f;
        _timerFill.rectTransform.sizeDelta = new Vector2(360f * ratio, 4f);
    }

    /// <summary>Đường cong mềm hai đầu: vào nhanh, dừng êm. Chuyển động tuyến tính nhìn máy móc.</summary>
    private static float Ease(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    // ==================== DỰNG GIAO DIỆN ====================

    private void Build()
    {
        _built = true;

        GameObject canvasGo = new GameObject("MatchResultCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Vẽ trên HUD nhưng DƯỚI bảng hướng dẫn (500) và màn chờ (900).
        canvas.sortingOrder = 400;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root = new GameObject("Content", typeof(RectTransform));
        _root.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)_root.transform);

        _dim = Img("Dim", _root.transform, new Color(0.02f, 0.03f, 0.05f, 0f));
        Stretch(_dim.rectTransform);

        // Thanh điện ảnh: neo vào mép trên và mép dưới, cao dần khi hiện ra.
        _barTop = Img("BarTop", _root.transform, new Color(0.01f, 0.012f, 0.02f, 0.96f));
        Edge(_barTop.rectTransform, true);
        _barBottom = Img("BarBottom", _root.transform, new Color(0.01f, 0.012f, 0.02f, 0.96f));
        Edge(_barBottom.rectTransform, false);

        _lineTop = Img("LineTop", _root.transform, ColWin);
        Center(_lineTop.rectTransform, new Vector2(0f, 150f), new Vector2(0f, 2f));
        _lineBottom = Img("LineBottom", _root.transform, ColWin);
        Center(_lineBottom.rectTransform, new Vector2(0f, -20f), new Vector2(0f, 2f));

        // --- Tiêu đề, có nhóm riêng để nảy và mờ dần độc lập ---
        GameObject titleGo = new GameObject("TitleGroup", typeof(RectTransform));
        titleGo.transform.SetParent(_root.transform, false);
        _titleRt = (RectTransform)titleGo.transform;
        Center(_titleRt, new Vector2(0f, 68f), new Vector2(1600f, 150f));
        _gTitle = titleGo.AddComponent<CanvasGroup>();

        _title = Txt("Title", titleGo.transform, 110f, ColWin, FontStyles.Bold, 0f);
        _title.characterSpacing = 10f;

        // --- Phần thân: phụ đề và tỉ số ---
        GameObject bodyGo = new GameObject("BodyGroup", typeof(RectTransform));
        bodyGo.transform.SetParent(_root.transform, false);
        Stretch((RectTransform)bodyGo.transform);
        _gBody = bodyGo.AddComponent<CanvasGroup>();

        _subtitle = Txt("Subtitle", bodyGo.transform, 26f, ColText, FontStyles.Normal, -58f);
        _subtitle.characterSpacing = 8f;

        _scoreLabel = Txt("ScoreLabel", bodyGo.transform, 18f, ColMuted, FontStyles.Normal, -118f);
        _scoreLabel.characterSpacing = 6f;

        _scoreMine = Txt("ScoreMine", bodyGo.transform, 76f, ColWin, FontStyles.Bold, -190f);
        Center(_scoreMine.rectTransform, new Vector2(-150f, -190f), new Vector2(240f, 110f));

        _scoreDash = Txt("ScoreDash", bodyGo.transform, 46f, ColMuted, FontStyles.Normal, -190f);
        _scoreDash.text = "–";

        _scoreTheirs = Txt("ScoreTheirs", bodyGo.transform, 76f, ColMuted, FontStyles.Bold, -190f);
        Center(_scoreTheirs.rectTransform, new Vector2(150f, -190f), new Vector2(240f, 110f));

        // --- Chân: đồng hồ về menu ---
        GameObject footGo = new GameObject("FootGroup", typeof(RectTransform));
        footGo.transform.SetParent(_root.transform, false);
        Stretch((RectTransform)footGo.transform);
        _gFoot = footGo.AddComponent<CanvasGroup>();

        _countdown = Txt("Countdown", footGo.transform, 20f, ColMuted, FontStyles.Normal, -300f);
        _countdown.characterSpacing = 8f;

        _timerTrack = Img("TimerTrack", footGo.transform, new Color(1f, 1f, 1f, 0.12f));
        Center(_timerTrack.rectTransform, new Vector2(0f, -336f), new Vector2(360f, 4f));

        _timerFill = Img("TimerFill", footGo.transform, ColWin);
        Center(_timerFill.rectTransform, new Vector2(0f, -336f), new Vector2(360f, 4f));

        _root.SetActive(false);
    }

    // ---- tiện ích dựng ----

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Edge(RectTransform rt, bool top)
    {
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
    }

    private static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Image Img(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = color;

        // KHÔNG chặn chuột: người chơi vẫn xoay camera ngắm cảnh trong lúc chờ, và quan
        // trọng hơn là không cướp chuột của bảng Settings nếu họ đang mở.
        img.raycastTarget = false;

        return img;
    }

    private TMP_Text Txt(string name, Transform parent, float size, Color color,
                         FontStyles style, float offsetY)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Center((RectTransform)go.transform, new Vector2(0f, offsetY), new Vector2(1500f, size * 1.7f));

        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return t;
    }
}
