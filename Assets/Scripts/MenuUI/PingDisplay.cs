using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiện độ trễ mạng (ping) ở góc trên bên phải khi đang trong trận.
///
/// VÌ SAO CẦN: game nhiều người chơi nào cũng hiện ping, vì nó là thứ duy nhất cho người
/// chơi biết "mình giật là do mạng hay do game". Thiếu nó thì mọi lỗi mạng đều bị đổ cho
/// game. Với đồ án thì còn một lý do nữa: chỉ tiêu "ping dưới 100ms" phải đo được, mà
/// trước đây không có chỗ nào trong game hiện con số đó ra cả.
///
/// Tự dựng lấy giao diện của mình bằng code, giống LoadingScreen và ControlsOverlay —
/// không phải kéo thả gì trong Inspector, nên không thể quên gán như ô knockbackText.
///
/// PHÍM: F4 ẩn/hiện.
/// </summary>
public class PingDisplay : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.F4;

    /// <summary>Đọc lại ping bao lâu một lần. 4 lần/giây là đủ, đọc mỗi khung hình chỉ tổ nhảy số.</summary>
    private const float RefreshInterval = 0.25f;

    private static PingDisplay _instance;

    private TMP_Text _label;
    private NetworkRunner _runner;
    private float _nextRefresh;
    private bool _hidden;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("PingDisplay");
        _instance = go.AddComponent<PingDisplay>();
        DontDestroyOnLoad(go);
    }

    private void Start()
    {
        Build();
    }

    private void Build()
    {
        GameObject canvasGo = new GameObject("PingCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Thấp hơn màn chờ (900) và bảng hướng dẫn (500): ping là thông tin phụ,
        // không được che thứ gì quan trọng.
        canvas.sortingOrder = 400;

        UnityEngine.UI.CanvasScaler scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textGo = new GameObject("PingText", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);

        // Neo vào GÓC TRÊN BÊN PHẢI chứ không phải giữa màn hình: góc này đang trống,
        // và đó cũng là chỗ người chơi game mạng quen tìm ping.
        RectTransform rt = (RectTransform)textGo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(260f, 40f);
        rt.anchoredPosition = new Vector2(-24f, -18f);

        _label = textGo.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 22f;
        _label.alignment = TextAlignmentOptions.Right;
        _label.raycastTarget = false;
        _label.text = string.Empty;
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
        {
            _hidden = !_hidden;
            if (_label != null) _label.enabled = !_hidden;
        }

        if (_hidden || _label == null) return;
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + RefreshInterval;

        Refresh();
    }

    private void Refresh()
    {
        // Runner bị huỷ mỗi khi về menu rồi tạo lại khi vào trận, nên phải tìm lại
        // chứ không giữ mãi một tham chiếu.
        if (_runner == null || !_runner.IsRunning)
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
        }

        if (_runner == null || !_runner.IsRunning)
        {
            _label.text = string.Empty;   // đang ở menu thì không hiện gì
            return;
        }

        // MÁY HOST KHÔNG CÓ PING. Nó CHÍNH LÀ máy chủ, nên độ trễ tới chính mình bằng 0.
        // Hiện "0 ms" ở đây sẽ gây hiểu nhầm là mạng hoàn hảo, nên nói thẳng là HOST.
        if (_runner.IsServer)
        {
            _label.text = "<color=#7F8C9B>HOST</color>";
            return;
        }

        // GetPlayerRtt(PlayerRef.None) trả về thời gian đi-về tới máy chủ, tính bằng GIÂY.
        int ms = Mathf.RoundToInt((float)_runner.GetPlayerRtt(PlayerRef.None) * 1000f);

        // Màu theo mức: dưới 100ms là đạt chỉ tiêu của đồ án, trên 200ms là đã khó chơi.
        string color = ms < 100 ? "#4CD97B" : ms < 200 ? "#E8C34C" : "#E8453C";

        _label.text = $"<color={color}>{ms} ms</color>";
    }
}
