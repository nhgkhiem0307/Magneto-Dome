using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng cài đặt: âm lượng, độ phân giải, toàn màn hình, độ nhạy chuột.
/// Đáp ứng yêu cầu GDD chương 6.
///
/// Đặt trên Canvas. Dùng được ở cả MenuScene lẫn trong trận — chỉ cần gắn thêm
/// một bản nữa vào Canvas của TestScene nếu muốn mở Settings giữa trận.
///
/// Mọi ô tham chiếu đều null-check, gán thiếu cũng không lỗi.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingsPanel;

    [Tooltip("Phím mở/đóng nhanh. Để None nếu chỉ muốn mở bằng nút bấm.")]
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("Âm lượng")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Text masterValueText;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;

    [Header("Điều khiển")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityValueText;

    [Header("Hiển thị")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    // Danh sách độ phân giải máy hỗ trợ, đã lọc trùng
    private readonly List<Resolution> _resolutions = new List<Resolution>();

    // Cờ chặn: lúc script tự đặt giá trị ban đầu cho slider, Unity vẫn bắn sự kiện
    // onValueChanged. Không chặn thì nó sẽ ghi đè PlayerPrefs bằng chính giá trị vừa đọc ra,
    // và tệ hơn là đổi độ phân giải ngay khi vừa mở bảng.
    private bool _isInitializing;

    void Start()
    {
        BuildResolutionList();
        LoadCurrentValues();
        HookEvents();

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    // --- ĐÓNG MỞ ---

    public void Toggle()
    {
        if (settingsPanel == null) return;

        if (settingsPanel.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (settingsPanel == null) return;

        LoadCurrentValues();
        settingsPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(false);

        // Đang trong trận thì khoá chuột lại để chơi tiếp.
        // Ở menu thì giữ chuột tự do để còn bấm nút.
        bool inMatch = FPSMovement.Local != null;

        Cursor.lockState = inMatch ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !inMatch;
    }

    // --- KHỞI TẠO ---

    private void BuildResolutionList()
    {
        if (resolutionDropdown == null) return;

        _resolutions.Clear();

        // Unity trả về cả những mục trùng độ phân giải nhưng khác tần số quét.
        // Lọc bớt cho danh sách gọn, người chơi chỉ quan tâm số pixel.
        HashSet<string> seen = new HashSet<string>();

        foreach (Resolution res in Screen.resolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (seen.Contains(key)) continue;

            seen.Add(key);
            _resolutions.Add(res);
        }

        List<string> options = new List<string>();
        foreach (Resolution res in _resolutions)
        {
            options.Add($"{res.width} x {res.height}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    private void LoadCurrentValues()
    {
        _isInitializing = true;

        AudioManager audio = AudioManager.Instance;
        if (audio != null)
        {
            SetSliderSafe(masterSlider, audio.MasterVolume);
            SetSliderSafe(musicSlider, audio.MusicVolume);
            SetSliderSafe(sfxSlider, audio.SfxVolume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = GameSettings.MinSensitivity;
            sensitivitySlider.maxValue = GameSettings.MaxSensitivity;
            sensitivitySlider.value = GameSettings.MouseSensitivity;
        }

        if (fullscreenToggle != null) fullscreenToggle.isOn = Screen.fullScreen;

        // Chọn sẵn mục khớp với độ phân giải đang dùng
        if (resolutionDropdown != null)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].width == Screen.width && _resolutions[i].height == Screen.height)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
            resolutionDropdown.RefreshShownValue();
        }

        RefreshAllLabels();

        _isInitializing = false;
    }

    private void SetSliderSafe(Slider slider, float value)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
    }

    private void HookEvents()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    // --- CÁC SỰ KIỆN ---

    private void OnMasterChanged(float value)
    {
        if (_isInitializing) return;
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
        RefreshAllLabels();
    }

    private void OnMusicChanged(float value)
    {
        if (_isInitializing) return;
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
        RefreshAllLabels();
    }

    private void OnSfxChanged(float value)
    {
        if (_isInitializing) return;
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
        RefreshAllLabels();

        // Kêu một tiếng để người chơi nghe thử mức vừa chỉnh
        AudioManager.ButtonClick();
    }

    private void OnSensitivityChanged(float value)
    {
        if (_isInitializing) return;
        GameSettings.SetMouseSensitivity(value);
        RefreshAllLabels();
    }

    private void OnFullscreenChanged(bool value)
    {
        if (_isInitializing) return;
        GameSettings.SetFullscreen(value);
    }

    private void OnResolutionChanged(int index)
    {
        if (_isInitializing) return;
        if (index < 0 || index >= _resolutions.Count) return;

        Resolution res = _resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    // --- NHÃN SỐ ---

    private void RefreshAllLabels()
    {
        // Âm lượng hiện dạng phần trăm cho dễ hiểu hơn số thập phân
        if (masterValueText != null && masterSlider != null)
            masterValueText.text = $"{Mathf.RoundToInt(masterSlider.value * 100f)}%";

        if (musicValueText != null && musicSlider != null)
            musicValueText.text = $"{Mathf.RoundToInt(musicSlider.value * 100f)}%";

        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";

        if (sensitivityValueText != null && sensitivitySlider != null)
            sensitivityValueText.text = sensitivitySlider.value.ToString("0.00");
    }
}
