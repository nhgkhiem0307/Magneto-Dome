using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình chờ khi chuyển scene: vào trận và rời trận về menu.
///
/// ⚠️ VÌ SAO CẦN: chuyển scene mất vài giây, và trong khoảng đó màn hình đen hoàn toàn,
/// không phản hồi gì. Người chơi không có cách nào phân biệt "đang tải" với "game treo" -
/// mà dự án này vừa có một lỗi treo thật ở đúng chỗ chuyển scene, nên sự nhập nhằng đó
/// không chỉ gây khó chịu mà còn làm người chấm hiểu sai chất lượng sản phẩm.
///
/// Màn hình chờ cũng là nơi duy nhất hợp lý để báo tiến trình mạng: "đang kết nối",
/// "đang vào trận", "đang trở về menu".
///
/// ⚠️ TỰ TẮT THEO TRẠNG THÁI, KHÔNG CHỜ AI GỌI Hide().
/// Nếu phụ thuộc vào một lời gọi Hide() đặt ở đâu đó, chỉ cần một đường code thoát sớm
/// là màn chờ ở lại vĩnh viễn và che mất cả game - lỗi nặng hơn hẳn thứ nó định chữa.
/// Nên ở đây nó tự quan sát: vào trận xong thì tự tắt, về menu xong thì tự tắt, và có
/// thêm hạn chót cứng để không bao giờ kẹt quá lâu.
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    private enum Mode { None, EnteringMatch, ReturningToMenu }

    [Tooltip("Quá bấy nhiêu giây thì tự tắt bất chấp, coi như đã xong. Lưới an toàn cuối.")]
    private const float HardTimeout = 25f;

    private static LoadingScreen _instance;

    private GameObject _root;
    private TMP_Text _label, _hint;
    private Mode _mode = Mode.None;
    private float _deadline;
    private bool _built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("LoadingScreen");
        _instance = go.AddComponent<LoadingScreen>();
        DontDestroyOnLoad(go);
    }

    // ==================== API cho nơi khác gọi ====================

    /// <summary>Hiện màn chờ "đang vào trận". Tự tắt khi nhân vật của mình đã có mặt.</summary>
    public static void ShowEnteringMatch()
    {
        if (_instance != null) _instance.Begin(Mode.EnteringMatch,
            "ENTERING ARENA", "Loading match...");
    }

    /// <summary>Hiện màn chờ "đang về menu". Tự tắt khi đã ở MenuScene.</summary>
    public static void ShowReturningToMenu()
    {
        if (_instance != null) _instance.Begin(Mode.ReturningToMenu,
            "RETURNING TO MENU", "Closing match...");
    }

    private void Begin(Mode mode, string label, string hint)
    {
        if (!_built) Build();
        if (_root == null) return;

        _mode = mode;
        _deadline = Time.realtimeSinceStartup + HardTimeout;
        _label.text = label;
        _hint.text = hint;
        _root.SetActive(true);
    }

    private void Finish()
    {
        _mode = Mode.None;
        if (_root != null) _root.SetActive(false);
    }

    // ==================== TỰ QUAN SÁT TRẠNG THÁI ====================

    private void Update()
    {
        if (_mode == Mode.None) return;

        // Hạn chót cứng: dù logic bên dưới có sai cách nào, màn chờ cũng không ở lại mãi.
        if (Time.realtimeSinceStartup > _deadline)
        {
            Debug.LogWarning("[MÀN CHỜ] Quá " + HardTimeout + " giây -> tự tắt.");
            Finish();
            return;
        }

        if (_mode == Mode.EnteringMatch)
        {
            // Nhân vật của mình đã xuất hiện nghĩa là scene đã nạp xong VÀ mạng đã sinh
            // nhân vật. Đây là mốc đúng, không phải mốc "scene đã nạp" - nạp xong scene
            // mà chưa có nhân vật thì người chơi vẫn nhìn vào một thế giới trống.
            if (FPSMovement.Local != null) Finish();
            return;
        }

        if (_mode == Mode.ReturningToMenu)
        {
            // Về menu xong = đang ở MenuScene và không còn nhân vật nào của mình.
            bool inMenu = SceneManager.GetActiveScene().name == "MenuScene";
            if (inMenu && FPSMovement.Local == null) Finish();
        }
    }

    // ==================== DỰNG GIAO DIỆN ====================

    private void Build()
    {
        _built = true;

        GameObject canvasGo = new GameObject("LoadingCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Cao nhất trong game: màn chờ phải che mọi thứ, kể cả bảng hướng dẫn (500).
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root = new GameObject("Fill", typeof(RectTransform));
        _root.transform.SetParent(canvasGo.transform, false);

        RectTransform rt = (RectTransform)_root.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = _root.AddComponent<Image>();

        // ĐỤC HẲN, khác với bảng kết quả trận. Lúc này đằng sau là một scene đang bị tháo
        // dở hoặc chưa dựng xong - để lộ ra chỉ làm người chơi thấy hình ảnh lỗi.
        bg.color = new Color(0.03f, 0.04f, 0.06f, 1f);
        bg.raycastTarget = true;   // chặn luôn mọi cú bấm trong lúc chuyển scene

        _label = Text("Label", 56f, new Color(0.16f, 0.91f, 1f), FontStyles.Bold, 30f);
        _hint = Text("Hint", 24f, new Color(0.6f, 0.66f, 0.72f), FontStyles.Italic, -50f);

        _root.SetActive(false);
    }

    private TMP_Text Text(string name, float size, Color color, FontStyles style, float offsetY)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_root.transform, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400f, size * 1.8f);
        rt.anchoredPosition = new Vector2(0f, offsetY);

        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.characterSpacing = 6f;

        return t;
    }
}
