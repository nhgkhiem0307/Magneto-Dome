using UnityEngine;

/// <summary>
/// Cấu hình người chơi tự chỉnh được, lưu vào PlayerPrefs để nhớ giữa các lần chơi.
///
/// Là class tĩnh, không phải MonoBehaviour - vì nó chỉ chứa mấy con số, không cần
/// tồn tại dưới dạng object trong scene. Nhờ vậy script nào cũng đọc được ngay
/// mà không phải đi tìm tham chiếu.
///
/// Âm lượng KHÔNG nằm ở đây mà nằm trong AudioManager, vì nó cần tác động
/// trực tiếp lên AudioSource.
/// </summary>
public static class GameSettings
{
    private const string KeySensitivity = "mouse_sensitivity";
    private const string KeyFullscreen = "fullscreen";
    private const string KeyFrameCap = "frame_cap";

    /// <summary>Giới hạn để người chơi không kéo thành vô dụng hoặc không điều khiển nổi.</summary>
    public const float MinSensitivity = 0.1f;
    public const float MaxSensitivity = 5f;

    private static float _mouseSensitivity = -1f;

    /// <summary>
    /// Độ nhạy chuột. NetworkRunnerHandler đọc giá trị này mỗi khung hình
    /// khi tích luỹ chuyển động chuột.
    /// </summary>
    public static float MouseSensitivity
    {
        get
        {
            // Lần đầu truy cập thì đọc từ PlayerPrefs. Dùng -1 làm dấu "chưa nạp"
            // vì độ nhạy hợp lệ luôn là số dương.
            if (_mouseSensitivity < 0f)
            {
                _mouseSensitivity = PlayerPrefs.GetFloat(KeySensitivity, 1f);
            }
            return _mouseSensitivity;
        }
    }

    public static void SetMouseSensitivity(float value)
    {
        _mouseSensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
        PlayerPrefs.SetFloat(KeySensitivity, _mouseSensitivity);
    }

    /// <summary>
    /// Đặt giá trị mặc định lấy từ Inspector của prefab nhân vật, NHƯNG chỉ khi
    /// người chơi chưa từng tự chỉnh. Nhờ vậy con số bạn đã cân bằng tay vẫn được
    /// dùng làm mốc khởi đầu, mà người chơi vẫn ghi đè được.
    /// </summary>
    public static void SeedDefaultSensitivity(float defaultValue)
    {
        if (PlayerPrefs.HasKey(KeySensitivity)) return;

        SetMouseSensitivity(defaultValue);
    }

    // --- TOÀN MÀN HÌNH ---

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
    }

    public static void SetFullscreen(bool value)
    {
        PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
        Screen.fullScreen = value;
    }

    // --- GIỚI HẠN KHUNG HÌNH ---
    //
    // ⚠️ ĐÂY LÀ THỨ CHỮA BỆNH NÓNG MÁY. Đọc kỹ trước khi đổi mặc định.
    //
    // Trước 31/08 game KHÔNG hề giới hạn khung hình: QualitySettings để vSyncCount = 0,
    // và không chỗ nào đặt Application.targetFrameRate. Unity mặc định như vậy nghĩa là
    // "vẽ nhanh hết mức card đồ hoạ làm được".
    //
    // Hậu quả: đứng trong menu hay quay mặt vào vách đá, cảnh vật nhẹ tênh, máy vẫn đẩy
    // 600-1500 khung hình mỗi giây. GPU chạy 100% tải liên tục để sinh ra hàng nghìn khung
    // hình mà MÀN HÌNH KHÔNG BAO GIỜ HIỆN RA - màn 60Hz chỉ lấy được 60 cái, phần còn lại
    // vẽ xong rồi vứt đi. Quạt rú, máy nóng, pin tụt, mà người chơi không nhận được gì.
    //
    // Đây gần như luôn là lý do một game "nhẹ" lại làm laptop nóng hơn game nặng - game
    // nặng thì tự nó đã không chạy nổi quá 60fps nên GPU không bao giờ rảnh để chạy hoang.

    /// <summary>
    /// Giới hạn mặc định: 120 khung/giây, KHÔNG dùng VSync.
    ///
    /// ⚠️ ĐỪNG ĐỔI VỀ VSYNC (0). Đã thử ngày 31/08 và phải bỏ ngay: chuột bị trễ thấy rõ.
    ///
    /// Vì sao VSync trễ mà giới hạn bằng số thì không - hai cách hoạt động khác hẳn nhau:
    ///
    ///   VSync            : vẽ xong thì GIỮ KHUNG HÌNH LẠI chờ màn hình quét tới lượt.
    ///                      Chuỗi chờ đó xếp hàng 1-2 khung hình -> ở 60Hz là 16-33ms
    ///                      từ lúc rê chuột tới lúc thấy hình đổi. Game ngắm bắn thì
    ///                      cảm nhận được ngay.
    ///   targetFrameRate  : vẽ xong thì CHO CPU NGỦ tới lượt sau. Không giữ lại gì cả,
    ///                      nên không thêm mili giây trễ nào.
    ///
    /// Cả hai đều chặn được GPU chạy hoang 1000 khung/giây - tức là chữa nóng máy như nhau.
    /// Nhưng chỉ một cách giữ được cảm giác chuột. Đánh đổi của cách này là có thể xé hình
    /// nhẹ (screen tearing); ai khó chịu với xé hình hơn với độ trễ thì tự chọn VSync
    /// trong Settings.
    ///
    /// Vì sao 120 chứ không phải 60: cao hơn tần số quét màn hình một mức thì thời gian
    /// một khung hình ngắn lại (8.3ms thay vì 16.7ms), nên chuột vẫn nhạy gần như lúc
    /// chưa giới hạn, mà GPU chỉ còn làm khoảng 1/8 khối lượng so với lúc thả tự do.
    /// </summary>
    public static int FrameCap
    {
        get => PlayerPrefs.GetInt(KeyFrameCap, 120);
    }

    /// <summary>Các mức cho người chơi chọn trong Settings.</summary>
    public static readonly int[] FrameCapOptions = { 120, 60, 90, 144, 240, -1, 0 };

    public static string FrameCapLabel(int cap)
    {
        if (cap == 0) return "VSync - no tearing, more input lag";
        if (cap < 0) return "Unlimited - runs hot";
        return cap + " FPS";
    }

    public static void SetFrameCap(int cap)
    {
        PlayerPrefs.SetInt(KeyFrameCap, cap);
        ApplyFrameCap();
    }

    /// <summary>
    /// Áp giới hạn khung hình đang lưu. Gọi lúc khởi động và mỗi khi người chơi đổi.
    /// </summary>
    public static void ApplyFrameCap()
    {
        int cap = FrameCap;

        if (cap == 0)
        {
            // VSync - CHỈ dùng khi người chơi TỰ CHỌN, không còn là mặc định nữa.
            //
            // Ưu điểm: hết xé hình hoàn toàn, và GPU nghỉ ngơi triệt để nhất.
            // Nhược điểm: trễ chuột 16-33ms vì khung hình bị giữ lại xếp hàng.
            // Xem ghi chú dài ở property FrameCap để biết vì sao đây không phải mặc định.
            //
            // targetFrameRate phải trả về -1: để cả hai cùng giới hạn thì Unity lấy
            // cái NGHIÊM NGẶT HƠN, dễ thành khoá nhầm ở một con số thấp hơn ý muốn.
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            return;
        }

        // Có chọn số cụ thể (hoặc chọn không giới hạn) thì phải TẮT VSync,
        // nếu không VSync vẫn chặn trước và con số vừa chọn thành vô nghĩa.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = cap; // -1 nghĩa là thả tự do
    }

    /// <summary>
    /// Áp lại cấu hình hiển thị lúc khởi động game.
    /// RuntimeInitializeOnLoadMethod khiến Unity tự gọi hàm này khi game vừa chạy,
    /// không cần đặt object nào trong scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStartup()
    {
        Screen.fullScreen = Fullscreen;

        // Chạy ở đây nên có hiệu lực NGAY, không cần gắn thêm object nào vào scene
        // và cũng không cần người chơi vào Settings bấm gì cả.
        ApplyFrameCap();
    }
}
