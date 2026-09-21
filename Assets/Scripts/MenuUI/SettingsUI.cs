using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

    /// <summary>
    /// Settings có đang mở không. ShopUI hỏi cái này để không mở đè lên Settings.
    ///
    /// Để static vì mỗi scene chỉ có đúng một SettingsUI, và các bảng khác không cần
    /// kéo thả tham chiếu tới nó - thêm bảng mới cũng khỏi đụng Inspector.
    /// </summary>
    public static bool IsOpen { get; private set; }

    /// <summary>
    /// Báo cho mọi bảng khác (Shop, Radial Menu) biết Settings vừa mở, để chúng tự đóng.
    ///
    /// Settings đứng trên cùng: Esc là phím "thoát khỏi mọi thứ", nên mở nó ra thì
    /// dọn sạch màn hình. Dùng sự kiện thay vì để Settings tự gọi từng bảng, để
    /// SettingsUI không phải biết trong game có những bảng nào.
    /// </summary>
    public static event System.Action Opened;

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

    [Tooltip("Giới hạn khung hình. ĐỂ TRỐNG CŨNG KHÔNG SAO - giới hạn mặc định (VSync) đã " +
             "được áp từ lúc khởi động game rồi, ô này chỉ để người chơi tự đổi.\n\n" +
             "Đây là ô quan trọng nhất với máy nóng và laptop chạy pin: không giới hạn thì " +
             "GPU vẽ 600-1500 khung/giây mà màn hình chỉ hiện được 60.")]
    public TMP_Dropdown frameCapDropdown;

    [Header("Thoát trận — bảng xác nhận")]
    [Tooltip("Bảng hỏi lại trước khi rời trận. Để trống thì bấm Leave Match là đi luôn, " +
             "không hỏi han gì.")]
    public GameObject leaveConfirmPanel;

    [Tooltip("Nút XÁC NHẬN rời trận.\n\n" +
             "Script chỉ đổi 'interactable', KHÔNG tắt object - nên nút vẫn nằm nguyên chỗ " +
             "và Unity tự đổi sang sprite Disabled. Muốn thấy hiệu ứng thì trên Button đặt " +
             "Transition = Sprite Swap và gán ảnh cho ô Disabled Sprite.")]
    public Button leaveConfirmButton;

    [Tooltip("Dòng chữ trong bảng xác nhận. Hiện lời nhắc kèm số giây đếm ngược.")]
    public TMP_Text leaveConfirmText;

    [Tooltip("Bắt chờ bao nhiêu giây trước khi cho bấm xác nhận.\n\n" +
             "Đây là ma sát CỐ Ý. Rời trận giữa chừng huỷ luôn trận của ba người còn lại, " +
             "nên vài giây buộc phải dừng lại nhìn dòng chữ sẽ ngăn được kha khá cú bấm " +
             "bốc đồng lúc đang cay.")]
    public float leaveConfirmDelay = 3f;

    [Tooltip("Lời nhắc hiện trong lúc đếm ngược.\n\n" +
             "Phải nói đúng HẬU QUẢ THẬT: rời trận không phải là bỏ đồng đội đánh tiếp 2v1, " +
             "mà là HUỶ TRẬN CHO TẤT CẢ - GameManager thấy quân số lệch là kết thúc luôn " +
             "(xem CheckForAbandonedMatch).\n\n" +
             "Câu cũ 'Your teammates may need you' vừa sai vừa nhẹ. Nói thẳng ra rằng ba " +
             "người kia sẽ mất trận thì sức răn đe mạnh hơn nhiều.")]
    public string leaveConfirmMessage = "Leaving will END THE MATCH for everyone";

    // Số giây còn lại. Nhỏ hơn hoặc bằng 0 nghĩa là đã cho bấm xác nhận.
    private float _leaveCountdown;

    [Header("Thoát trận")]
    [Tooltip("Nút RỜI TRẬN, chỉ có nghĩa trong scene gameplay.\n\n" +
             "Gán nút vào đây thì script TỰ ẨN nó khi đang ở menu - cùng một SettingsPanel " +
             "dùng cho cả hai scene, mà ở menu thì 'rời trận' không có nghĩa gì.\n\n" +
             "KHÔNG cần On Click - code tự nối nút này vào LeaveMatch. Để trống ô này " +
             "thì code tự tìm object tên LeaveButton trong Settings Panel.")]
    public Button leaveMatchButton;

    [Header("Nút bấm - code tự nối, KHÔNG cần On Click")]
    [Tooltip("Nút ĐÓNG bảng Settings.\n\n" +
             "⚠️ VÌ SAO CODE TỰ NỐI THAY VÌ KÉO THẢ ON CLICK: SettingsUI nằm trên Canvas của " +
             "SCENE, còn các nút nằm trong PREFAB SettingsPanel - mà prefab thì không được " +
             "tham chiếu tới object của scene. Nên ô On Click trong prefab luôn TRỐNG, mỗi " +
             "scene phải tự nối lại, và đã quên đúng chỗ đó: MenuScene nút Close không làm " +
             "gì, TestScene hai nút Xác nhận / Huỷ không làm gì.\n\n" +
             "Code tự nối thì thêm scene mới cũng chạy, không ai phải nhớ gì. Ô trống thì " +
             "code tự tìm object tên CloseButton (bên ngoài LeaveConfirmPanel).")]
    public Button closeButton;

    [Tooltip("Nút HUỶ trong bảng xác nhận rời trận - chỉ tắt bảng xác nhận, KHÔNG đóng Settings. " +
             "Để trống thì code tự tìm object tên CloseButton nằm BÊN TRONG LeaveConfirmPanel.")]
    public Button leaveCancelButton;

    // Tên object trong prefab SettingsPanel, dùng khi ô tham chiếu để trống.
    // ĐỔI TÊN object trong prefab thì phải đổi ở đây - hoặc gán tay vào ô cho chắc.
    private const string NameLeaveButton = "LeaveButton";
    private const string NameCloseButton = "CloseButton";
    private const string NameConfirmPanel = "LeaveConfirmPanel";
    private const string NameConfirmButton = "Confirmbtn";

    // Danh sách độ phân giải máy hỗ trợ, đã lọc trùng
    private readonly List<Resolution> _resolutions = new List<Resolution>();

    // Cờ chặn: lúc script tự đặt giá trị ban đầu cho slider, Unity vẫn bắn sự kiện
    // onValueChanged. Không chặn thì nó sẽ ghi đè PlayerPrefs bằng chính giá trị vừa đọc ra,
    // và tệ hơn là đổi độ phân giải ngay khi vừa mở bảng.
    private bool _isInitializing;

    // Nhớ độ phân giải người chơi đã chọn. Không thể chỉ dựa vào Screen.width - xem
    // ghi chú ở OnResolutionChanged().
    private const string KeyResWidth = "res_width";
    private const string KeyResHeight = "res_height";

    void Start()
    {
        // Biến static sống qua cả lúc đổi scene. Rời trận khi Settings đang mở thì nó còn
        // kẹt ở true sang scene mới, và Shop sẽ không bao giờ mở được nữa.
        IsOpen = false;

        ResolveMissingReferences();
        BuildResolutionList();
        LoadCurrentValues();
        HookEvents();
        HookButtons();

        // Tắt CẢ bảng xác nhận, không chỉ bảng Settings.
        //
        // ⚠️ Trong prefab, LeaveConfirmPanel đang để Active = BẬT. Nó là con của Settings
        // nên lúc đầu bị ẩn theo - nhưng lần ĐẦU mở Settings thì nó hiện đè lên ngay, vì
        // chưa có gì tắt nó đi. Ở MenuScene còn tệ hơn: ô leaveConfirmPanel trống nên Close()
        // không bao giờ tắt được nó, bảng "rời trận" hiện đè MỖI LẦN mở Settings.
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        TickLeaveCountdown();
    }

    /// <summary>
    /// Đếm ngược trong bảng xác nhận rời trận, rồi mở khoá nút xác nhận.
    /// </summary>
    private void TickLeaveCountdown()
    {
        if (_leaveCountdown <= 0f) return;

        // Bảng bị đóng giữa chừng (bấm Huỷ, hoặc Esc đóng cả Settings) -> bỏ đồng hồ đi.
        // Không có dòng này thì đồng hồ vẫn chạy ngầm, và lần mở sau nó đã đếm xong sẵn -
        // người chơi bấm xác nhận được ngay, mất tác dụng của cả cơ chế chờ.
        if (leaveConfirmPanel != null && !leaveConfirmPanel.activeSelf)
        {
            _leaveCountdown = 0f;
            return;
        }

        // unscaledDeltaTime chứ không phải deltaTime: nếu sau này có lúc nào đó đặt
        // Time.timeScale = 0 (tạm dừng), đồng hồ này vẫn phải chạy - không thì nút
        // xác nhận bị khoá vĩnh viễn và người chơi không thoát được.
        _leaveCountdown -= Time.unscaledDeltaTime;

        RefreshLeaveCountdownText();

        if (_leaveCountdown > 0f) return;

        _leaveCountdown = 0f;

        // Mở khoá. CHỈ đổi interactable, KHÔNG đụng SetActive - nút vẫn nằm nguyên chỗ
        // và Unity tự chuyển từ sprite Disabled sang Normal.
        if (leaveConfirmButton != null) leaveConfirmButton.interactable = true;
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

        // Mở Settings thì luôn bắt đầu ở bảng chính, không bao giờ ở bảng xác nhận.
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(false);
        _leaveCountdown = 0f;

        // Chỉ hiện nút Rời Trận khi đang thật sự ở trong trận.
        //
        // Xét mỗi lần MỞ bảng chứ không xét một lần lúc khởi tạo: cùng một object
        // SettingsUI sống qua cả lúc ở menu lẫn lúc vào trận, nên trạng thái này đổi.
        if (leaveMatchButton != null)
        {
            leaveMatchButton.gameObject.SetActive(FPSMovement.Local != null);
        }

        CursorLock.Request(this);

        IsOpen = true;
        Opened?.Invoke();
    }

    void OnDestroy()
    {
        // Cùng lý do như ở Start: đổi scene thì bảng bị huỷ mà không đi qua Close().
        IsOpen = false;
    }

    /// <summary>
    /// Rời trận giữa chừng, quay về menu. Kéo nút vào ô On Click và chọn hàm này.
    ///
    /// ⚠️ HÀM NÀY KHÔNG CHỈ ẢNH HƯỞNG NGƯỜI BẤM:
    ///   - HOST bấm  -> trận kết thúc cho TẤT CẢ, mọi người nhận
    ///                  "Host left the game" ở menu
    ///   - CLIENT bấm -> quân số lệch, GameManager huỷ trận và những người còn lại
    ///                  thấy "MATCH CANCELLED" trong 8 giây
    ///
    /// Cả hai đường đều đã được xử lý êm rồi, nên nút này an toàn - nhưng vì nó phá trận
    /// của người khác, cân nhắc thêm một bước xác nhận "Bạn chắc chưa?" nếu còn thời gian.
    /// </summary>
    public void LeaveMatch()
    {
        // Không gán bảng xác nhận thì đi thẳng, giữ nguyên hành vi cũ.
        if (leaveConfirmPanel == null)
        {
            DoLeaveMatch();
            return;
        }

        leaveConfirmPanel.SetActive(true);

        // Nạp lại đồng hồ MỖI LẦN MỞ, không phải một lần lúc khởi tạo.
        // Đóng rồi mở lại là phải chờ lại từ đầu - nếu không thì bấm Huỷ rồi bấm lại
        // là bỏ qua được toàn bộ khoảng chờ, và cả cơ chế này thành vô nghĩa.
        _leaveCountdown = leaveConfirmDelay;

        // Khoá nút ngay lập tức, đừng đợi khung hình sau.
        // Chậm một khung là đủ để người bấm nhanh lọt qua.
        if (leaveConfirmButton != null) leaveConfirmButton.interactable = false;

        RefreshLeaveCountdownText();
    }

    /// <summary>Bấm Huỷ trong bảng xác nhận. Kéo nút Huỷ vào ô On Click và chọn hàm này.</summary>
    public void CancelLeaveMatch()
    {
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(false);
    }

    /// <summary>
    /// Bấm XÁC NHẬN. Kéo nút xác nhận vào ô On Click và chọn hàm này.
    ///
    /// Vẫn kiểm lại đồng hồ dù nút đang bị khoá: 'interactable' chỉ chặn CHUỘT, còn hàm
    /// public thì vẫn gọi được từ chỗ khác (phím tắt, script khác, hoặc chính bạn nối nhầm
    /// hai nút vào cùng một hàm). Chốt ở đây mới là chốt thật.
    /// </summary>
    public void ConfirmLeaveMatch()
    {
        if (_leaveCountdown > 0f) return;

        DoLeaveMatch();
    }

    private void DoLeaveMatch()
    {
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(false);

        // Đóng bảng Settings trước, để nó trả đăng ký con trỏ chuột lại cho CursorLock.
        // Không đóng thì đăng ký còn treo lại, và ReleaseAll() bên ReturnToMenu() phải
        // dọn hộ - vẫn chạy đúng, nhưng dựa vào may mắn.
        Close();

        if (NetworkRunnerHandler.Instance != null)
        {
            NetworkRunnerHandler.Instance.ReturnToMenu();
        }
        else
        {
            Debug.LogWarning("[SETTINGS] Không tìm thấy NetworkRunnerHandler để rời trận.");
        }
    }

    private void RefreshLeaveCountdownText()
    {
        if (leaveConfirmText == null) return;

        // CeilToInt để giây cuối vẫn hiện "1" thay vì "0" - đếm tới 0 là lúc hết chờ,
        // không phải một mốc để hiện ra.
        int secondsLeft = Mathf.CeilToInt(_leaveCountdown);

        leaveConfirmText.text = secondsLeft > 0
            ? $"{leaveConfirmMessage}\n{secondsLeft}"
            : leaveConfirmMessage;
    }

    public void Close()
    {
        if (settingsPanel == null) return;

        // Đóng luôn bảng xác nhận. Nó thường là object CON của settingsPanel nên sẽ tự ẩn
        // theo, nhưng nếu ai đó đặt nó ra ngoài thì nó sẽ nằm lơ lửng giữa màn hình sau khi
        // Settings đã đóng. Tắt tay ở đây thì đặt ở đâu cũng đúng.
        if (leaveConfirmPanel != null) leaveConfirmPanel.SetActive(false);
        _leaveCountdown = 0f;

        settingsPanel.SetActive(false);
        IsOpen = false;

        // Trả chuột lại. CursorLock tự lo phần còn lại: chỉ khoá khi KHÔNG còn bảng nào
        // đang mở, và luôn thả tự do khi đang ở menu (chưa có nhân vật).
        //
        // Trước đây chỗ này tự khoá chuột, nên đóng Settings trong lúc Radial Menu
        // vẫn mở sẽ làm người chơi không rê chọn được nữa.
        CursorLock.Release(this);
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

        // Chọn sẵn mục khớp với độ phân giải NGƯỜI CHƠI ĐÃ CHỌN.
        //
        // Ưu tiên giá trị đã lưu, chỉ dùng Screen.width/height khi chưa từng chọn lần nào.
        // Xem OnResolutionChanged() để biết vì sao không tin được Screen.width.
        if (resolutionDropdown != null)
        {
            int wantWidth = PlayerPrefs.GetInt(KeyResWidth, Screen.width);
            int wantHeight = PlayerPrefs.GetInt(KeyResHeight, Screen.height);

            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].width == wantWidth && _resolutions[i].height == wantHeight)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
            resolutionDropdown.RefreshShownValue();
        }

        // Ô giới hạn khung hình. Tự nạp danh sách lựa chọn nên không phải gõ tay
        // trong Inspector - gõ tay thì thứ tự dễ lệch khỏi GameSettings.FrameCapOptions
        // và người chơi chọn "60 FPS" lại ra "không giới hạn".
        if (frameCapDropdown != null)
        {
            frameCapDropdown.ClearOptions();

            List<string> capLabels = new List<string>();
            foreach (int cap in GameSettings.FrameCapOptions)
            {
                capLabels.Add(GameSettings.FrameCapLabel(cap));
            }
            frameCapDropdown.AddOptions(capLabels);

            int current = GameSettings.FrameCap;
            for (int i = 0; i < GameSettings.FrameCapOptions.Length; i++)
            {
                if (GameSettings.FrameCapOptions[i] == current)
                {
                    frameCapDropdown.value = i;
                    break;
                }
            }
            frameCapDropdown.RefreshShownValue();
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

    /// <summary>
    /// Ô tham chiếu nào để trống thì tự tìm theo TÊN trong Settings Panel.
    ///
    /// Ô đã gán tay thì luôn được ưu tiên - tìm theo tên chỉ là lưới đỡ. Làm cả hai thay vì
    /// chỉ tìm theo tên, vì tên object là thứ rất dễ bị ai đó đổi mà không biết có code
    /// phụ thuộc vào nó.
    /// </summary>
    private void ResolveMissingReferences()
    {
        if (settingsPanel == null) return;

        Transform root = settingsPanel.transform;

        if (leaveConfirmPanel == null)
        {
            Transform t = FindDeep(root, NameConfirmPanel, null);
            if (t != null) leaveConfirmPanel = t.gameObject;
        }

        Transform confirmRoot = leaveConfirmPanel != null ? leaveConfirmPanel.transform : null;

        // Prefab có HAI object tên CloseButton: một ở bảng chính, một trong bảng xác nhận.
        // Nên nút Đóng tìm ở NGOÀI bảng xác nhận, nút Huỷ tìm ở TRONG.
        if (leaveMatchButton == null) leaveMatchButton = FindButton(root, NameLeaveButton, confirmRoot);
        if (closeButton == null) closeButton = FindButton(root, NameCloseButton, confirmRoot);

        if (confirmRoot != null)
        {
            if (leaveConfirmButton == null) leaveConfirmButton = FindButton(confirmRoot, NameConfirmButton, null);
            if (leaveCancelButton == null) leaveCancelButton = FindButton(confirmRoot, NameCloseButton, null);
            if (leaveConfirmText == null) leaveConfirmText = FindLooseText(confirmRoot);
        }

        if (closeButton == null)
        {
            Debug.LogWarning("[SETTINGS] Không tìm thấy nút Đóng. Gán tay vào ô Close Button, " +
                             "không thì chỉ đóng được Settings bằng phím " + toggleKey + ".", this);
        }
    }

    /// <summary>
    /// Nối nút vào hàm bằng code, và TẮT mọi On Click cũ đặt trong Inspector.
    ///
    /// Phải tắt On Click cũ, không thì một cú bấm gọi hàm HAI LẦN: một lần từ Inspector,
    /// một lần từ code. Tắt đi cũng vô hiệu luôn lời gọi SAI còn sót trong prefab - nút
    /// Xác nhận đang gọi nhầm LeaveMatch, tức mở lại bảng và đếm lại 3 giây mãi mãi.
    ///
    /// SetPersistentListenerState lúc chạy chỉ đổi bản trong bộ nhớ, không ghi vào prefab
    /// hay scene - hết Play là mọi thứ trong Inspector y nguyên.
    /// </summary>
    private void HookButtons()
    {
        Bind(closeButton, Close);
        Bind(leaveMatchButton, LeaveMatch);
        Bind(leaveConfirmButton, ConfirmLeaveMatch);
        Bind(leaveCancelButton, CancelLeaveMatch);
    }

    private static void Bind(Button button, UnityAction action)
    {
        if (button == null) return;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }

        // Gỡ trước rồi mới gắn, để lỡ Start chạy lại (bật tắt object) cũng không gắn trùng.
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Transform FindDeep(Transform root, string name, Transform exclude)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != name) continue;
            if (exclude != null && t.IsChildOf(exclude)) continue;
            return t;
        }
        return null;
    }

    private static Button FindButton(Transform root, string name, Transform exclude)
    {
        Transform t = FindDeep(root, name, exclude);
        return t != null ? t.GetComponent<Button>() : null;
    }

    /// <summary>Dòng chữ của bảng xác nhận: chữ đầu tiên KHÔNG nằm trong nút nào.</summary>
    private static TMP_Text FindLooseText(Transform root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            // Bỏ chữ trên nút (chữ "Confirm", "Cancel") - thứ cần là dòng lời nhắc.
            if (text.GetComponentInParent<Button>(true) != null) continue;
            return text;
        }
        return null;
    }

    private void HookEvents()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (frameCapDropdown != null) frameCapDropdown.onValueChanged.AddListener(OnFrameCapChanged);
    }

    private void OnFrameCapChanged(int index)
    {
        // Cùng lý do với các ô khác: lúc script tự đặt giá trị ban đầu, Unity vẫn bắn
        // sự kiện này. Không chặn thì vừa mở bảng Settings đã ghi đè lựa chọn của người chơi.
        if (_isInitializing) return;

        if (index < 0 || index >= GameSettings.FrameCapOptions.Length) return;

        GameSettings.SetFrameCap(GameSettings.FrameCapOptions[index]);
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

        // GHI NHỚ LỰA CHỌN, đừng chỉ dựa vào Screen.width để đọc lại sau này.
        //
        // Hai lý do:
        //   1. Screen.SetResolution() KHÔNG có hiệu lực ngay - nó áp vào cuối khung hình,
        //      nên đọc Screen.width ngay sau đó vẫn ra số cũ.
        //   2. Trong Unity Editor nó KHÔNG LÀM GÌ CẢ - cỡ Game view do dropdown của
        //      Game view quyết định. Nên Screen.width vĩnh viễn không đổi, và mở lại
        //      bảng cài đặt sẽ luôn thấy mục cũ.
        PlayerPrefs.SetInt(KeyResWidth, res.width);
        PlayerPrefs.SetInt(KeyResHeight, res.height);
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
