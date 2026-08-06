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

    /// <summary>
    /// Áp lại cấu hình hiển thị lúc khởi động game.
    /// RuntimeInitializeOnLoadMethod khiến Unity tự gọi hàm này khi game vừa chạy,
    /// không cần đặt object nào trong scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStartup()
    {
        Screen.fullScreen = Fullscreen;
    }
}
