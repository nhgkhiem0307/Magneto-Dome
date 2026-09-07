using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using Fusion.Addons.Physics;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance { get; private set; }
    public static string LocalPlayerName = "Player";

    // Nhớ tên giữa các lần chơi, giống cách âm lượng và độ nhạy chuột đang làm.
    private const string KeyPlayerName = "player_name";

    [Header("Room Player Prefab")]
    public RoomPlayer roomPlayerPrefab;

    [Header("UI Panels")]
    public GameObject namePanel;
    public GameObject mainButtonsPanel;
    public GameObject roomLobbyPanel;
    public GameObject createRoomPanel;
    public GameObject roomListPanel;
    private GameObject[] _allPanels;

    [Header("Room List UI")]
    [Tooltip("Transform 'Content' bên trong Scroll View của roomListPanel — nơi các dòng RoomItemUI được sinh ra.")]
    public Transform roomListContent;
    [Tooltip("Prefab Assets/Prefab/RoomItemUI.prefab — mỗi phòng trong danh sách là một bản sao của prefab này.")]
    public RoomItemUI roomItemPrefab;

    [Header("UI Inputs & Texts")]
    public TMP_InputField nameInput;
    public TMP_InputField joinCodeInput;
    public TMP_Text yourRoomIDText;
    public TMP_Text roomTitleText;
    public TMP_Text teamRedText;
    public TMP_Text teamBlueText;
    public TMP_Text statusErrorText;

    [Tooltip("(Tuỳ chọn) Hiện tên người chơi đang dùng. " +
             "Cập nhật mỗi khi xác nhận tên và tự nạp lại tên đã lưu lần trước.")]
    public TMP_Text currentNameText;

    [Header("Lobby Buttons")]
    public Button startMatchButton;

    [Header("Settings")]
    [Tooltip("Tên scene gameplay. Scene bắt buộc phải có trong Build Settings.")]
    public string gameSceneName = "TestScene";

    [Tooltip("Tên scene menu, dùng để quay về sau khi kết thúc trận.")]
    public string menuSceneName = "MenuScene";

    [Header("Game Player Prefab")]
    [Tooltip("Prefab nhân vật trong trận (Assets/Prefab/Player.prefab). Bắt buộc phải có component NetworkObject.")]
    public NetworkObject gamePlayerPrefab;

    [Header("Vị trí xuất hiện trong trận")]
    public Vector3 redTeamSpawnPoint = new Vector3(40f, 6f, 60f);
    public Vector3 blueTeamSpawnPoint = new Vector3(40f, 6f, 20f);
    [Tooltip("Khoảng cách giữa 2 người cùng đội, để họ không xuất hiện chồng lên nhau.")]
    public float spawnSpacing = 2f;

    [Header("Hướng nhìn lúc xuất hiện")]
    [Tooltip("Góc xoay quanh trục Y tính bằng độ, để nhân vật quay mặt vào giữa map. 0 = nhìn theo hướng +Z, 180 = nhìn theo hướng -Z.")]
    public float redTeamSpawnYaw = 180f;
    public float blueTeamSpawnYaw = 0f;

    [Header("Game Manager Prefab")]
    [Tooltip("Prefab chứa script GameManager. Bắt buộc phải có component NetworkObject.")]
    public NetworkObject gameManagerPrefab;

    // Điểm xuất hiện của một người chơi. GameManager cũng gọi hàm này khi hồi sinh
    // đầu mỗi round, để chỉ có DUY NHẤT một nơi định nghĩa vị trí spawn.
    public Vector3 GetSpawnPosition(int team, int indexInTeam)
    {
        Vector3 basePoint = team == 0 ? redTeamSpawnPoint : blueTeamSpawnPoint;
        return basePoint + Vector3.right * (indexInTeam * spawnSpacing);
    }

    public float GetSpawnYaw(int team)
    {
        return team == 0 ? redTeamSpawnYaw : blueTeamSpawnYaw;
    }

    // Góc nhìn của người chơi cục bộ, tích luỹ từ chuột mỗi khung hình.
    // Để static vì FPSMovement.Render() cần đọc lại để vẽ camera cho mượt.
    public static float LookYaw { get; private set; }
    public static float LookPitch { get; private set; }

    /// <summary>
    /// Con trỏ chuột có đang được thả ra để bấm giao diện hay không.
    ///
    /// Dùng một điều kiện chung thay vì đi hỏi riêng từng UI (Shop, Radial Menu, Settings...).
    /// UI nào mở khoá chuột là tự động được tính vào đây, không phải sửa lại chỗ này.
    /// </summary>
    public static bool IsCursorFree()
    {
        return CursorLock.IsFree;
    }

    // Đặt lại góc nhìn, dùng lúc nhân vật vừa xuất hiện để khớp với hướng Host đã xoay sẵn.
    // Sau này khi làm hồi sinh đầu mỗi round cũng sẽ gọi lại hàm này.
    public static void SetLookAngles(float yaw, float pitch)
    {
        LookYaw = yaw;
        LookPitch = Mathf.Clamp(pitch, -80f, 80f);
    }

    // Đã đổi tên biến từ _runner thành _networkRunner để tránh trùng lặp serialization
    private NetworkRunner _networkRunner;
    private string _currentRoomCode = "";

    // Đánh dấu Host đã bấm "Bắt Đầu Trận", để phân biệt lần load scene nào mới là vào trận thật
    private bool _matchStarted = false;

    // Đang trong quá trình đóng trận và quay về menu.
    // Tắt Runner mất vài khung hình, cờ này chặn việc gọi chồng lên nhau.
    private bool _isReturningToMenu = false;

    /// <summary>
    /// Máy này có đang ở trong scene gameplay hay không.
    ///
    /// ⚠️ ĐỪNG DÙNG _matchStarted CHO VIỆC NÀY. Cờ đó chỉ được đặt trong OnClickStartMatch(),
    /// mà hàm đó nằm sau "if (IsServer)" nên CHỈ HOST chạy - trên máy Client nó vĩnh viễn
    /// bằng false.
    ///
    /// Đó chính là lý do bản sửa ngày 01/09 không ăn: Host thoát, Client nhận
    /// OnDisconnectedFromServer, nhưng "if (_matchStarted)" trả về false nên nó rơi xuống
    /// nhánh "còn ở phòng chờ" và đi gọi ShowPanel(mainButtonsPanel) - một panel đã bị huỷ
    /// cùng MenuScene. Không ai đưa Client về menu, họ kẹt lại trong TestScene.
    ///
    /// Hỏi scene đang chạy thì đúng trên MỌI máy, bất kể ai là người bấm Bắt Đầu.
    /// </summary>
    private bool IsInGameScene => SceneManager.GetActiveScene().name == gameSceneName;

    /// <summary>
    /// Vừa từ một trận đấu quay về menu, hay vừa mới mở game lên.
    ///
    /// Phải là biến TĨNH: khi về menu, object NetworkRunnerHandler cũ bị huỷ và một bản
    /// mới trong MenuScene nhận vai. Biến thường sẽ mất theo object cũ, chỉ biến tĩnh mới
    /// truyền được thông tin qua ranh giới đó.
    ///
    /// Dùng để chọn màn hình đầu tiên:
    ///   - Mới mở game  -> màn NHẬP TÊN (đây là đường DUY NHẤT để đổi tên, vì không nút
    ///                     nào trong MenuScene quay lại được màn này)
    ///   - Về từ trận   -> thẳng MÀN HÌNH CHÍNH, khỏi bắt gõ lại tên vừa dùng xong
    ///
    /// Biến tĩnh tự mất khi tắt hẳn game, nên lần mở sau lại vào màn nhập tên - đúng ý.
    /// </summary>
    private static bool _returningFromMatch = false;

    /// <summary>
    /// Câu thông báo cần hiện NGAY khi về tới MenuScene. Rỗng = không có gì để báo.
    ///
    /// Vì sao phải để dành lại thay vì hiện tại chỗ: khi Host thoát thì MẠNG ĐÃ CHẾT và
    /// GameManager cũng despawn theo. Không dùng được pha MatchEnd như trường hợp client
    /// thoát (xem GameManager.CheckForAbandonedMatch) - không còn ai đồng bộ gì cho ai nữa.
    ///
    /// Nên câu thông báo được cất vào một biến TĨNH, sống sót qua việc đổi scene, rồi
    /// Start() của MenuScene lấy ra hiện. Không có nó thì client đang đánh nhau bỗng thấy
    /// mình ở menu, không hiểu vì sao.
    /// </summary>
    private static string _pendingMenuMessage = "";

    /// <summary>
    /// Đang có một thao tác mạng chạy dở (tạo phòng, vào phòng, ghép trận, rời phòng).
    ///
    /// ⚠️ MỌI NÚT MẠNG ĐỀU PHẢI HỎI CỜ NÀY TRƯỚC.
    ///
    /// Bốn hàm OnClickMatchmaking / OnConfirmCreateRoom / OnClickJoinByCode /
    /// OnClickLeaveRoom đều là "async void" và đều gọi StartGame() hoặc Shutdown() -
    /// những thao tác mất vài giây. Trong khoảng đó nút vẫn bấm được, mà người chơi thì
    /// LUÔN bấm lại khi thấy không có gì xảy ra.
    ///
    /// Gọi StartGame() lần thứ hai trên một Runner đang khởi động dở là lỗi chắc chắn:
    /// Fusion không cho phép, và trạng thái phòng sẽ hỏng.
    ///
    /// Một cờ dùng CHUNG cho cả bốn nút, không phải mỗi nút một cờ - vì chúng loại trừ
    /// lẫn nhau: đang tạo phòng thì cũng không được bấm vào phòng khác.
    /// </summary>
    private bool _isBusyWithNetwork = false;

    // Runner ĐÃ tắt xong rồi (Fusion vừa gọi OnShutdown).
    //
    // ⚠️ CỜ NÀY CHỐNG TREO MÁY, không phải để cho gọn. Xem ReturnToMenu():
    // gọi await Shutdown() lần nữa trên một Runner đang tắt dở thì lệnh chờ đó
    // KHÔNG BAO GIỜ hoàn thành, và cả game đứng im tại chỗ.
    private bool _runnerIsDown = false;

    // Nhân vật trong trận của từng người chơi. Dùng để dọn dẹp khi họ thoát,
    // và sau này để hồi sinh ở đầu mỗi round.
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        // Object này sống xuyên qua các scene, nên khi quay lại MenuScene sẽ có
        // một bản thứ hai được tạo ra. Phải huỷ bản mới để tránh chạy trùng 2 lần.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Giữ object này sống khi Fusion chuyển từ MenuScene sang scene trong trận.
        // Nếu không, nó sẽ bị huỷ theo MenuScene, callback OnInput mất theo
        // và nhân vật sẽ đứng im không điều khiển được.
        DontDestroyOnLoad(gameObject);

        // Gom tất cả các Panel vào mảng ngay khi game khởi chạy
        _allPanels = new GameObject[] 
        { 
            namePanel, 
            mainButtonsPanel, 
            createRoomPanel, 
            roomListPanel, 
            roomLobbyPanel 
        };
    }

    private void Start()
    {
        // Nạp lại tên đã lưu từ lần chơi trước và điền sẵn vào ô nhập,
        // để người chơi quen chỉ cần bấm xác nhận là xong.
        string savedName = PlayerPrefs.GetString(KeyPlayerName, "");
        if (!string.IsNullOrEmpty(savedName))
        {
            LocalPlayerName = savedName;
            if (nameInput != null) nameInput.text = savedName;
        }

        RefreshCurrentNameText();

        // BẢO HIỂM CHO CON TRỎ CHUỘT.
        //
        // ReturnToMenu() đã gọi CursorLock.ReleaseAll() rồi, nhưng đó là ở scene CŨ.
        // Thả lại một lần nữa khi MenuScene đã chạy thật, để bịt mọi đường khoá chuột khác
        // mà ta chưa lường tới.
        CursorLock.ReleaseAll();

        // VỀ TỪ MỘT TRẬN ĐẤU -> VÀO THẲNG MÀN HÌNH CHÍNH.
        //
        // Đánh xong một trận mà bị ném về màn gõ tên như vừa mở game lần đầu thì vừa thừa
        // vừa mất phương hướng - tên đã lưu trong PlayerPrefs rồi, gõ lại làm gì.
        //
        // Nhưng KHÔNG bỏ hẳn màn nhập tên đi được: đối chiếu 4 nút ShowPanel trong
        // MenuScene thì không nút nào trỏ về namePanel, tức đây là đường DUY NHẤT để đổi
        // tên. Bỏ luôn thì người chơi kẹt với cái tên gõ lần đầu, vĩnh viễn.
        bool skipNameEntry = _returningFromMatch;
        _returningFromMatch = false;

        Debug.Log($"[MENU] Vào menu — bỏ qua màn nhập tên: {skipNameEntry}");

        ShowPanel(skipNameEntry ? mainButtonsPanel : namePanel);

        // Có chuyện gì xảy ra ở trận vừa rồi thì báo ngay tại đây.
        //
        // Đặt SAU ShowPanel: ShowPanel không đụng tới StatusErrorText (nó là con trực tiếp
        // của Canvas, không nằm trong panel nào), nhưng đặt sau cho chắc thứ tự.
        if (!string.IsNullOrEmpty(_pendingMenuMessage))
        {
            SetErrorMessage(_pendingMenuMessage);
            _pendingMenuMessage = "";
        }
        else if (statusErrorText != null)
        {
            statusErrorText.text = "";
        }
    }

    private void Update()
    {
        // Tích luỹ chuyển động chuột MỖI KHUNG HÌNH.
        //
        // Vì sao không đọc chuột ở FixedUpdateNetwork? Vì tick mạng chạy chậm hơn
        // tốc độ khung hình. Nếu chỉ đọc ở tick mạng thì phần chuột rê giữa 2 tick
        // sẽ bị bỏ mất, khiến góc nhìn giật và cảm giác "hụt".
        FPSMovement localPlayer = FPSMovement.Local;
        if (localPlayer == null) return; // chưa vào trận thì chưa có gì để xoay

        // Đang mở giao diện nào đó (Shop, Radial Menu...) thì KHÔNG xoay camera nữa.
        // Lúc đó con trỏ chuột đang được dùng để bấm nút, không phải để ngắm.
        if (IsCursorFree()) return;

        // Lấy độ nhạy từ GameSettings chứ không từ prefab, để người chơi chỉnh được
        // trong Settings và giá trị đó được nhớ giữa các lần chơi.
        float sensitivity = GameSettings.MouseSensitivity;

        LookYaw += Input.GetAxisRaw("Mouse X") * sensitivity;

        LookPitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        LookPitch = Mathf.Clamp(LookPitch, -80f, 80f);
    }

    public void ShowPanel(GameObject panelToShow)
    {
        if (_allPanels == null) return;

        // Duyệt qua từng panel: chỉ kích hoạt (true) panel trùng với panelToShow,
        // tất cả các panel còn lại lập tức bị ẩn (false).
        foreach (var panel in _allPanels)
        {
            if (panel != null)
            {
                panel.SetActive(panel == panelToShow);
            }
        }
    }

    public void OnConfirmName()
    {
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            LocalPlayerName = nameInput.text;

            // Nhớ tên cho lần chơi sau, khỏi phải gõ lại mỗi lần mở game
            PlayerPrefs.SetString(KeyPlayerName, LocalPlayerName);
        }

        RefreshCurrentNameText();
        ShowPanel(mainButtonsPanel);
    }

    /// <summary>
    /// Hiện tên đang dùng lên giao diện.
    ///
    /// Trước đây LocalPlayerName chỉ được gán rồi gửi qua RPC, không hiển thị ở đâu cả -
    /// nên người chơi gõ tên xong không có gì xác nhận là nó đã được ghi nhận.
    /// </summary>
    private void RefreshCurrentNameText()
    {
        if (currentNameText == null) return;

        currentNameText.text = LocalPlayerName;
    }

    private void EnsureRunnerExists()
    {
        if (_networkRunner == null)
        {
            GameObject runnerObj = new GameObject("FusionNetworkRunner");
            _networkRunner = runnerObj.AddComponent<NetworkRunner>();
            _networkRunner.ProvideInput = true;
            runnerObj.AddComponent<NetworkSceneManagerDefault>();

            // Bắt buộc cho Fusion Physics Addon.
            // Component này TẮT hệ thống vật lý tự động của Unity và bắt Fusion tự điều khiển
            // nhịp chạy vật lý theo tick mạng. Nhờ vậy vật thể (đạn) mới đồng bộ và tua lại được.
            // Lưu ý: nó ảnh hưởng tới MỌI Rigidbody trong scene, không riêng vật thể từ tính.
            runnerObj.AddComponent<RunnerSimulatePhysics3D>();

            _networkRunner.AddCallbacks(this);
        }
    }

    // --- 1. GHÉP TRẬN NGẪU NHIÊN ---
    public async void OnClickMatchmaking()
    {
        if (_isBusyWithNetwork) return;
        _isBusyWithNetwork = true;

        try
        {
            if (statusErrorText != null) statusErrorText.text = "";
            EnsureRunnerExists();

            var result = await _networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                PlayerCount = 4,
                // Không đặt SessionName -> Photon tự ghép vào phòng public đang mở còn
                // trống, hoặc tự tạo phòng mới (kèm cái tên tự sinh) nếu chưa có phòng
                // nào. IsVisible mặc định = true nên phòng này SẼ hiện trong Room List
                // và CÓ THỂ bị người khác ghép trận ngẫu nhiên vào - đúng ý "global".
                SceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>()
            });

            // Trước đây không có dòng này nên _currentRoomCode luôn rỗng với phòng
            // Global -> roomTitleText/yourRoomIDText hiện trống, trông như phòng
            // "không có ID". Lấy đúng cái tên Photon vừa gán cho session (dù do ta tạo
            // mới hay vừa được ghép vào phòng có sẵn), dùng luôn làm "ID phòng" hiển thị -
            // vừa đúng sự thật vừa khỏi phải tự sinh thêm một mã khác chồng lên.
            if (result.Ok && _networkRunner.SessionInfo != null)
            {
                _currentRoomCode = _networkRunner.SessionInfo.Name;
            }

            ShowPanel(roomLobbyPanel);
            UpdateLobbyUI();
        }
        finally
        {
            // finally chứ không phải đặt ở dòng cuối: nếu StartGame ném lỗi thì dòng cuối
            // không bao giờ chạy tới, cờ kẹt ở true và mọi nút mạng CHẾT VĨNH VIỄN -
            // người chơi phải tắt game mở lại. finally thì hỏng kiểu gì cũng được gỡ cờ.
            _isBusyWithNetwork = false;
        }
    }

    // --- 1b. MỞ DANH SÁCH PHÒNG ---
    //
    // Trước đây nút này chỉ gọi thẳng ShowPanel(roomListPanel) — hiện cái panel trống ra
    // rồi thôi. Photon không tự gửi danh sách phòng cho máy nào cả, phải CHỦ ĐỘNG xin
    // bằng JoinSessionLobby() thì Fusion mới bắt đầu gọi OnSessionListUpdated() về sau.
    public async void OnClickOpenRoomList()
    {
        if (_isBusyWithNetwork) return;
        _isBusyWithNetwork = true;

        try
        {
            if (statusErrorText != null) statusErrorText.text = "";
            EnsureRunnerExists();

            var result = await _networkRunner.JoinSessionLobby(SessionLobby.ClientServer);

            if (result.Ok)
            {
                ShowPanel(roomListPanel);
            }
            else
            {
                SetErrorMessage("Could not load room list!");
            }
        }
        finally
        {
            _isBusyWithNetwork = false;
        }
    }

    // --- 2. TẠO PHÒNG MỚI ---
    public async void OnConfirmCreateRoom()
    {
        if (_isBusyWithNetwork) return;
        _isBusyWithNetwork = true;

        try
        {
            if (statusErrorText != null) statusErrorText.text = "";
            EnsureRunnerExists();

            _currentRoomCode = UnityEngine.Random.Range(10000, 99999).ToString();
            if (yourRoomIDText != null) yourRoomIDText.text = "Room ID: " + _currentRoomCode;

            var result = await _networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = _currentRoomCode,
                PlayerCount = 4,
                // Phòng CUSTOM là phòng riêng, chỉ vào được bằng đúng mã - không cho lộ
                // ra ngoài. IsVisible=false vừa giấu nó khỏi Room List (JoinSessionLobby
                // sẽ không trả phòng này về nữa), vừa khiến Global Matchmaking
                // (GameMode.AutoHostOrClient không chỉ định SessionName) bỏ qua nó luôn -
                // Photon chỉ tự ghép người vào những phòng Visible, đúng ý "custom không
                // hiện ở room list cũng không bị ghép trận ngẫu nhiên vào".
                IsVisible = false,
                SceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                ShowPanel(roomLobbyPanel);

                // yourRoomIDText nằm trong createRoomPanel, panel vừa bị ẩn đi ở dòng
                // trên - viết chữ vào đó thì không ai thấy được nữa. Còn roomTitleText
                // (nằm trong roomLobbyPanel, panel ĐANG hiện) vốn chỉ được cập nhật gián
                // tiếp qua UpdateLobbyUI() khi RoomPlayer của Host spawn xong - có độ trễ
                // mạng. Gọi thẳng ở đây để Host thấy mã phòng NGAY, không phải chờ.
                UpdateLobbyUI();
            }
            else
            {
                SetErrorMessage("Could not create room!");
            }
        }
        finally
        {
            _isBusyWithNetwork = false;
        }
    }

    // --- 3. VÀO PHÒNG BẰNG MÃ ---
    public async void OnClickJoinByCode()
    {
        if (statusErrorText != null) statusErrorText.text = "";

        // Kiểm ô nhập TRƯỚC khi bật cờ bận: đây chỉ là kiểm tra tại chỗ, chưa đụng tới
        // mạng. Bật cờ rồi mới return thì cờ bị kẹt ở true.
        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            SetErrorMessage("Please enter a Room ID!");
            return;
        }

        await JoinRoomByCode(joinCodeInput.text.Trim());
    }

    // Được RoomItemUI gọi khi người chơi bấm nút JOIN trên một dòng trong Room List.
    public async void JoinRoomFromList(string sessionName)
    {
        await JoinRoomByCode(sessionName);
    }

    // Logic dùng chung cho cả "gõ mã phòng" lẫn "bấm Join trong danh sách phòng" -
    // hai đường đó chỉ khác nhau ở CHỖ lấy ra cái mã phòng, còn lại giống hệt nhau.
    private async System.Threading.Tasks.Task JoinRoomByCode(string code)
    {
        if (_isBusyWithNetwork) return;
        _isBusyWithNetwork = true;

        try
        {
            if (statusErrorText != null) statusErrorText.text = "";
            EnsureRunnerExists();
            _currentRoomCode = code;

            var result = await _networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = _currentRoomCode,
                SceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                ShowPanel(roomLobbyPanel);

                // Cùng lý do như ở OnConfirmCreateRoom(): đừng chờ RoomPlayer spawn xong
                // mới thấy mã phòng.
                UpdateLobbyUI();
            }
            else
            {
                SetErrorMessage("Room not found or already full!");
                ShowPanel(mainButtonsPanel);

                if (_networkRunner != null)
                {
                    Destroy(_networkRunner.gameObject);
                    _networkRunner = null;
                }
            }
        }
        finally
        {
            _isBusyWithNetwork = false;
        }
    }

    // --- KẾT THÚC TRẬN, QUAY VỀ MENU ---

    /// <summary>
    /// Đóng trận đấu và đưa mọi người về MenuScene.
    ///
    /// Host gọi hàm này khi hết trận. Việc tắt Runner sẽ khiến các máy Client
    /// nhận callback OnShutdown, và ở đó chúng cũng tự gọi lại hàm này.
    /// </summary>
    public async void ReturnToMenu()
    {
        // Chặn gọi chồng: tắt Runner mất vài khung hình, trong lúc đó
        // GameManager có thể gọi thêm lần nữa.
        if (_isReturningToMenu) return;
        _isReturningToMenu = true;

        // ĐẶT CỜ NGAY DÒNG ĐẦU, TRƯỚC MỌI LỆNH await.
        //
        // Trước đây cờ này nằm ở cuối hàm, ngay trên LoadScene. Nhưng giữa đầu hàm và
        // cuối hàm có một "await Shutdown()" - mà await nghĩa là hàm TẠM DỪNG rồi mới
        // chạy tiếp. Nếu vì lý do nào đó phần sau await không chạy tới nơi, cờ không bao
        // giờ được đặt, và người chơi bị ném về màn nhập tên.
        //
        // Đặt ở đây thì dù phần sau có hỏng cách nào, cờ vẫn đúng.
        _returningFromMatch = true;

        if (_networkRunner != null)
        {
            // CHỈ gọi Shutdown khi Runner CHƯA tắt.
            //
            // Đây là chỗ gây treo khi Host thoát giữa trận. Luồng chạy như sau:
            // Host thoát -> Fusion tắt Runner ở máy Client -> gọi OnShutdown ->
            // OnShutdown gọi ReturnToMenu -> ReturnToMenu lại "await Shutdown()"
            // trên chính cái Runner đang tắt dở. Lệnh chờ đó không bao giờ hoàn thành,
            // nên hàm dừng lại ngay tại đây: scene không được load, chuột không được trả,
            // người chơi kẹt vĩnh viễn trong một thế giới không còn mạng.
            if (!_runnerIsDown)
            {
                // Bọc try/catch vì đây là async void: một lỗi ném ra trong này sẽ không
                // ai bắt được, và nó giết luôn phần còn lại của hàm - tức là vẫn kẹt.
                try
                {
                    await _networkRunner.Shutdown();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MẠNG] Lỗi khi tắt Runner, vẫn tiếp tục về menu: {e.Message}");
                }
            }

            // Object chứa Runner cũng là DontDestroyOnLoad, không tự mất theo scene
            if (_networkRunner != null) Destroy(_networkRunner.gameObject);
            _networkRunner = null;
        }

        // Dọn sạch các danh sách tĩnh. Chúng sống xuyên scene nên không tự xoá,
        // để sót lại thì trận sau sẽ đếm nhầm số người chơi.
        RoomPlayer.AllPlayers.Clear();
        PlayerHealth.AllPlayers.Clear();

        // Trả chuột lại cho menu.
        //
        // Dùng ReleaseAll() vì mọi bảng giao diện của scene cũ (Shop, Radial Menu,
        // Settings) sắp bị huỷ theo scene mà không kịp gọi Release. Để sót đăng ký
        // của chúng thì trận sau chuột sẽ không bao giờ khoá lại được.
        CursorLock.ReleaseAll();

        // QUAN TRỌNG: phải bỏ Instance TRƯỚC khi load scene.
        //
        // Object này là DontDestroyOnLoad nên nó sống sót qua scene mới. Nhưng mọi
        // tham chiếu UI của nó đã chết theo MenuScene cũ -> menu sẽ hiện ra một đống
        // nút bấm không được. Bỏ Instance ra để bản NetworkRunnerHandler nằm sẵn trong
        // MenuScene mới được nhận vai, rồi huỷ bản cũ này đi.
        Instance = null;
        Destroy(gameObject);

        SceneManager.LoadScene(menuSceneName);
    }

    // --- 4. RỜI PHÒNG ---
    public async void OnClickLeaveRoom()
    {
        if (_isBusyWithNetwork) return;
        _isBusyWithNetwork = true;

        try
        {
            if (_networkRunner != null)
            {
                // Không gọi Shutdown nếu Fusion đã tự tắt Runner - cùng cái bẫy treo máy
                // đã gặp ở ReturnToMenu(): await trên một Runner đang tắt dở không bao giờ
                // hoàn thành, và nút Rời phòng sẽ đứng im mãi mãi.
                if (!_runnerIsDown)
                {
                    try
                    {
                        await _networkRunner.Shutdown();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MẠNG] Lỗi khi rời phòng, vẫn về menu: {e.Message}");
                    }
                }

                if (_networkRunner != null) Destroy(_networkRunner.gameObject);
                _networkRunner = null;
            }

            // Runner cũ đã chết, cờ phải trả về false để lần tạo/vào phòng sau còn chạy được.
            _runnerIsDown = false;

            ShowPanel(mainButtonsPanel);
        }
        finally
        {
            _isBusyWithNetwork = false;
        }
    }

    // --- 5. BẮT ĐẦU TRẬN (LOAD SCENE) ---
    public void OnClickStartMatch()
    {
        // CHẶN BẤM NHIỀU LẦN. Thêm 01/09.
        //
        // Trước đây không có dòng này, nên bấm Bắt Đầu 3 lần là gọi runner.LoadScene()
        // 3 lần. Hậu quả:
        //   - Fusion đang load dở lại nhận lệnh load mới -> lỗi trong Console
        //   - OnSceneLoadDone chạy nhiều lần -> tuy SpawnAllGamePlayers và SpawnGameManager
        //     đều có chốt chống trùng, nhưng đó là chốt CUỐI CÙNG, không nên dựa vào
        //   - Người chơi thấy màn hình load nhấp nháy vài lần
        //
        // Nút bị bấm nhiều lần là chuyện bình thường: mạng lag một nhịp là người ta bấm lại.
        if (_matchStarted)
        {
            Debug.LogWarning("[MẠNG] Trận đã bắt đầu rồi, bỏ qua lần bấm này.");
            return;
        }

        if (_networkRunner != null && _networkRunner.IsServer)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(gameSceneName);
            if (sceneIndex != -1)
            {
                // Bật cờ này TRƯỚC khi load, để khi scene load xong thì biết
                // đây là lần vào trận thật và tiến hành spawn nhân vật.
                _matchStarted = true;

                // Tắt nút đi cho người chơi THẤY là đã bấm được.
                //
                // Dòng return ở đầu hàm đã chặn về mặt logic rồi, nhưng nút vẫn sáng và
                // vẫn bấm được thì người chơi tưởng chưa ăn và cứ bấm tiếp. Tắt nút là
                // phản hồi bằng hình ảnh, khác mục đích với dòng chặn kia.
                if (startMatchButton != null) startMatchButton.interactable = false;

                // KHOÁ PHÒNG LẠI. Thêm 01/09.
                //
                // OnPlayerJoined đã từ chối người vào giữa trận rồi, nhưng đó là lớp chặn
                // SAU KHI họ đã kết nối - họ sẽ thấy mình vào được một nhịp rồi bị đá ra,
                // trông như lỗi. Khoá ở đây thì họ không kết nối được ngay từ đầu, và
                // phòng cũng biến mất khỏi hệ thống ghép trận.
                //
                // Giữ CẢ HAI lớp: khoá cửa là để lịch sự với người chơi, còn dòng từ chối
                // bên OnPlayerJoined mới là thứ bảo đảm đúng đắn - vẫn có khe hở vài mili
                // giây giữa lúc người ta bấm vào và lúc phòng khoá xong.
                if (_networkRunner.SessionInfo != null)
                {
                    _networkRunner.SessionInfo.IsOpen = false;
                    _networkRunner.SessionInfo.IsVisible = false;
                }

                _networkRunner.LoadScene(SceneRef.FromIndex(sceneIndex));
            }
            else
            {
                SetErrorMessage($"Scene '{gameSceneName}' is not in Build Settings!");
            }
        }
    }

    // --- 6. ĐỔI TEAM ---
    public void OnClickSwitchTeam()
    {
        if (RoomPlayer.Local == null) return;

        int targetTeam = RoomPlayer.Local.Team == 0 ? 1 : 0;
        int targetTeamCount = 0;

        foreach (var p in RoomPlayer.AllPlayers)
        {
            if (p.Team == targetTeam) targetTeamCount++;
        }

        if (targetTeamCount < 2)
        {
            RoomPlayer.Local.RPC_RequestTeamChange(targetTeam);
        }
    }

    // --- CẬP NHẬT GIAO DIỆN LOBBY ---
    public void UpdateLobbyUI()
    {
        string redList = "";
        string blueList = "";
        int redCount = 0, blueCount = 0;

        foreach (var p in RoomPlayer.AllPlayers)
        {
            string pName = string.IsNullOrEmpty(p.NickName.ToString()) ? "Loading..." : p.NickName.ToString();
            
            if (p.Team == 0)
            {
                redList += $"- {pName}\n";
                redCount++;
            }
            else
            {
                blueList += $"- {pName}\n";
                blueCount++;
            }
        }

        if (teamRedText != null) teamRedText.text = $"RED TEAM ({redCount}/2):\n" + redList;
        if (teamBlueText != null) teamBlueText.text = $"BLUE TEAM ({blueCount}/2):\n" + blueList;

        if (roomTitleText != null)
        {
            roomTitleText.text = $"ROOM: {_currentRoomCode} ({RoomPlayer.AllPlayers.Count}/4)";
        }

        // Trước đây chỉ OnConfirmCreateRoom() gán dòng này, nên chỉ Host thấy được mã
        // phòng - người join bằng mã thì ô này luôn trống. UpdateLobbyUI() chạy trên MỌI
        // máy (Host lẫn Client) mỗi khi có người vào/ra/đổi đội, nên gán lại ở đây thì
        // cả hai bên đều thấy đúng mã phòng, kể cả trường hợp Host tạo phòng xong mới
        // gán text lần đầu (trước khi RoomPlayer của chính Host kịp Spawned()).
        if (yourRoomIDText != null)
        {
            yourRoomIDText.text = "Room ID: " + _currentRoomCode;
        }

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(_networkRunner != null && _networkRunner.IsServer);
        }
    }

    private void SetErrorMessage(string message)
    {
        if (statusErrorText != null) statusErrorText.text = message;
    }

    // --- FUSION CALLBACKS ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // TRẬN ĐÃ BẮT ĐẦU -> KHÔNG NHẬN THÊM AI. Thêm 01/09.
            //
            // Đây là đấu 2v2 theo round có tính điểm, nên người vào giữa chừng đẻ ra một
            // loạt câu hỏi không có đáp án hay: vào đội nào khi đang 2v1? Có tiền không,
            // hay $0 trong khi người khác đã tích luỹ qua 5 round? Điểm số tính từ đâu?
            //
            // Không game đấu đối kháng nào cho vào giữa trận - CS, Valorant, Rocket League
            // đều chặn, và vì đúng lý do đó. Chặn ở đây xoá bỏ luôn cả một LỚP bug:
            // không có ai vào giữa trận thì không có đội hình lệch, không có kinh tế lệch,
            // không có _spawnedPlayers lệch.
            if (_matchStarted)
            {
                Debug.LogWarning($"[MẠNG] {player} xin vào lúc trận đang chạy -> từ chối.");
                runner.Disconnect(player);
                return;
            }

            var playerObj = runner.Spawn(roomPlayerPrefab, Vector3.zero, Quaternion.identity, player);
            var roomPlayer = playerObj.GetComponent<RoomPlayer>();
            roomPlayer.PlayerRef = player;

            int redCount = 0, blueCount = 0;
            foreach (var p in RoomPlayer.AllPlayers)
            {
                if (p.Team == 0) redCount++; else blueCount++;
            }
            roomPlayer.Team = (redCount <= blueCount) ? 0 : 1;
        }
    }

    /// <summary>
    /// Một người chơi vừa rời phòng hoặc mất kết nối.
    ///
    /// ⚠️ TRƯỚC 01/09 HÀM NÀY KHÔNG DESPAWN NHÂN VẬT, nên người thoát giữa trận để lại
    /// một cái "xác" đứng im giữa map: vẫn chắn đường, vẫn ăn đạn, vẫn bị tính vào quân số.
    ///
    /// May là dọn dẹp phần sau gần như MIỄN PHÍ: PlayerHealth.Despawned() tự gọi
    /// AllPlayers.Remove(this), mà mọi vòng lặp trong GameManager đều duyệt AllPlayers -
    /// thưởng tiền cuối round, hồi sinh, KillZone, tiến độ chiếm khu. Despawn đúng cách
    /// là tất cả tự sạch theo, không phải sửa GameManager một dòng nào.
    /// </summary>
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // CHỈ Host được despawn. Client gọi Despawn sẽ bị Fusion từ chối và ném lỗi,
        // mà hàm này thì chạy trên MỌI máy.
        if (runner.IsServer && _spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
        {
            // Object có thể đã bị huỷ bởi một đường khác (kết thúc trận, đổi scene).
            // Despawn một object đã chết cũng là một lỗi.
            if (playerObject != null) runner.Despawn(playerObject);

            // BẮT BUỘC PHẢI XOÁ KHỎI TỪ ĐIỂN.
            //
            // Không xoá thì PlayerRef đó nằm lại vĩnh viễn, và dòng
            // "if (_spawnedPlayers.ContainsKey(playerRef)) continue;" bên SpawnAllPlayers
            // sẽ CHẶN KHÔNG CHO HỌ SPAWN nếu họ vào lại phòng ở trận sau.
            _spawnedPlayers.Remove(player);

            Debug.Log($"<color=orange>[MẠNG] {player} đã rời trận, nhân vật đã được dọn.</color>");
        }

        // DỌN LUÔN RoomPlayer CỦA NGƯỜI VỪA RỜI. Thêm 07/09.
        //
        // Trước đây hàm này chỉ dọn nhân vật TRONG TRẬN (_spawnedPlayers ở trên), còn
        // RoomPlayer - object đại diện cho họ trong PHÒNG CHỜ - không hề bị despawn.
        // Hai hậu quả:
        //   1. Rời phòng chờ (chưa vào trận) -> RoomPlayer của họ nằm lại vĩnh viễn
        //      trong RoomPlayer.AllPlayers -> UpdateLobbyUI() vẫn đếm và hiện tên họ,
        //      trông như Host "không cập nhật" khi có người thoát.
        //   2. Người đó rời rồi vào lại (rất hay gặp khi test bằng ParrelSync) ->
        //      OnPlayerJoined spawn thêm một RoomPlayer MỚI, còn bản CŨ vẫn còn sống ->
        //      danh sách phòng hiện 2, 3... bản "ma" của cùng một người.
        //
        // CHỈ Host được Despawn - cùng lý do như _spawnedPlayers ở trên.
        if (runner.IsServer)
        {
            RoomPlayer leavingRoomPlayer = RoomPlayer.AllPlayers.Find(p => p.PlayerRef == player);
            if (leavingRoomPlayer != null && leavingRoomPlayer.Object != null)
            {
                runner.Despawn(leavingRoomPlayer.Object);
            }
        }

        UpdateLobbyUI();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // Runner đã tắt xong. PHẢI đặt trước khi gọi ReturnToMenu, nếu không hàm đó
        // sẽ đi gọi Shutdown() lần nữa và treo cứng - xem ghi chú ở ReturnToMenu().
        _runnerIsDown = true;

        // Đang ở trong trận mà Runner tắt -> Host đã kết thúc trận, hoặc mất kết nối.
        // Dù lý do nào thì cũng phải đưa người chơi về menu, không để họ kẹt lại
        // trong một scene không còn mạng.
        //
        // Xét CẢ HAI: _matchStarted đúng cho Host, IsInGameScene đúng cho Client.
        // Chỉ xét _matchStarted thì Client không bao giờ được đưa về menu.
        if (_matchStarted || IsInGameScene)
        {
            // CHỈ báo khi kết thúc BẤT THƯỜNG.
            //
            // ShutdownReason.Ok nghĩa là trận kết thúc đúng luật - lúc đó người chơi đã
            // xem thông báo "RED TEAM WINS!" hay "MATCH CANCELLED" suốt 8 giây rồi.
            // Báo thêm một câu lỗi ở menu nữa là thừa, và làm một kết thúc bình thường
            // trông như có sự cố.
            if (shutdownReason != ShutdownReason.Ok)
            {
                Debug.LogWarning($"[MẠNG] Trận kết thúc bất thường: {shutdownReason}");

                // Không ghi đè câu đã đặt sẵn bên OnDisconnectedFromServer - câu đó cụ thể
                // hơn ("Host left the game"), còn câu này chỉ là phương án dự phòng khi
                // OnShutdown tới trước.
                if (string.IsNullOrEmpty(_pendingMenuMessage))
                {
                    _pendingMenuMessage = "Match ended unexpectedly — connection lost";
                }
            }

            ReturnToMenu();
            return;
        }

        // Còn đang ở phòng chờ thì chỉ cần quay lại màn hình chính
        if (shutdownReason != ShutdownReason.Ok)
        {
            SetErrorMessage("Connection lost!");
            ShowPanel(mainButtonsPanel);
        }
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        SetErrorMessage("Could not connect!");
        ShowPanel(mainButtonsPanel);
    }

    // --- SPAWN NHÂN VẬT KHI VÀO TRẬN ---
    // Fusion gọi hàm này trên MỌI máy sau khi scene đã load xong.
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Chỉ Host mới được quyền spawn (Host Mode - server giữ toàn quyền quyết định).
        // Client chỉ ngồi chờ nhận kết quả từ Host gửi về.
        if (!runner.IsServer) return;

        // Bỏ qua những lần load scene không phải vào trận (ví dụ lúc mới tạo phòng).
        if (!_matchStarted) return;

        // Spawn nhân vật TRƯỚC, GameManager SAU.
        // GameManager có một khoảng chờ khởi động rồi mới bắt đầu round đầu tiên,
        // nên tới lúc nó đếm quân số thì mọi nhân vật đã có mặt đầy đủ.
        SpawnAllGamePlayers(runner);
        SpawnGameManager(runner);
    }

    private void SpawnGameManager(NetworkRunner runner)
    {
        if (gameManagerPrefab == null)
        {
            Debug.LogError("Chưa gán 'Game Manager Prefab' trong Inspector của NetworkRunnerHandler!");
            return;
        }

        // Đã có rồi thì thôi, tránh spawn hai bản cùng điều khiển vòng đấu
        if (GameManager.Instance != null) return;

        runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
    }

    private void SpawnAllGamePlayers(NetworkRunner runner)
    {
        if (gamePlayerPrefab == null)
        {
            Debug.LogError("Chưa gán 'Game Player Prefab' trong Inspector của NetworkRunnerHandler!");
            return;
        }

        // Đếm riêng từng đội để giãn vị trí ra, tránh 2 người cùng đội xuất hiện chồng lên nhau
        int redIndex = 0;
        int blueIndex = 0;

        // RoomPlayer là object của phòng chờ, đã được DontDestroyOnLoad nên vẫn sống
        // sau khi chuyển scene. Nhờ vậy ở đây vẫn đọc được ai thuộc đội nào.
        foreach (RoomPlayer roomPlayer in RoomPlayer.AllPlayers)
        {
            PlayerRef playerRef = roomPlayer.PlayerRef;

            // Người này đã có nhân vật rồi thì bỏ qua, không spawn trùng
            if (_spawnedPlayers.ContainsKey(playerRef)) continue;

            int team = roomPlayer.Team;
            int indexInTeam = team == 0 ? redIndex++ : blueIndex++;

            Vector3 spawnPosition = GetSpawnPosition(team, indexInTeam);

            // Xoay nhân vật quay mặt vào giữa map, tránh trường hợp vừa vào trận đã nhìn ra ngoài rìa
            float spawnYaw = GetSpawnYaw(team);
            Quaternion spawnRotation = Quaternion.Euler(0f, spawnYaw, 0f);

            // Tham số playerRef là mấu chốt: nó trao Input Authority cho đúng người chơi đó,
            // để chỉ mình họ điều khiển được nhân vật này, không ai điều khiển hộ được.
            //
            // Tham số cuối là callback chạy ngay sau khi nhân vật được tạo ra nhưng TRƯỚC khi
            // Spawned() được gọi. Cần nó vì CharacterController giữ một bản toạ độ riêng bên trong,
            // và sẽ kéo nhân vật ngược về vị trí gốc của prefab (0,0,0) dù ta đã truyền vị trí cho Spawn.
            // Cách chữa: tắt CharacterController -> đặt vị trí -> bật lại.
            NetworkObject playerObject = runner.Spawn(
                gamePlayerPrefab,
                spawnPosition,
                spawnRotation,
                playerRef,
                (spawnRunner, spawnedObject) =>
                {
                    CharacterController cc = spawnedObject.GetComponent<CharacterController>();

                    if (cc != null) cc.enabled = false;
                    spawnedObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                    if (cc != null) cc.enabled = true;

                    // Báo cho nhân vật biết hướng nhìn ban đầu, để lúc Spawned() nó tự đặt
                    // góc camera cho khớp. Nếu không làm bước này, ngay tick đầu tiên
                    // FixedUpdateNetwork sẽ bẻ nhân vật về góc 0 độ và mất hết hướng vừa đặt.
                    FPSMovement movement = spawnedObject.GetComponent<FPSMovement>();
                    if (movement != null) movement.SpawnYaw = spawnYaw;

                    // Ghi đội vào chính nhân vật. GameManager cần biết ai thuộc đội nào
                    // để đếm quân số còn sống, và tra ngược qua RoomPlayer mỗi tick thì phí.
                    PlayerHealth health = spawnedObject.GetComponent<PlayerHealth>();
                    if (health != null) health.Team = team;
                }
            );

            _spawnedPlayers[playerRef] = playerObject;
        }
    }

    // --- GỬI INPUT LÊN HOST MỖI TICK MẠNG ---
    // Fusion tự gọi hàm này trên máy của từng người chơi.
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        FPSMovement localPlayer = FPSMovement.Local;
        if (localPlayer == null) return; // đang ở phòng chờ, chưa có nhân vật để điều khiển

        NetworkInputData data = new NetworkInputData();

        data.MoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        data.Yaw = LookYaw;
        data.Pitch = LookPitch;

        // Gửi trạng thái ĐANG GIỮ của phím (GetKey chứ không phải GetKeyDown).
        // Việc phát hiện "vừa bấm xuống" do bên nhận lo, bằng cách so với tick trước.
        data.Buttons.Set((int)InputButton.Dash, Input.GetKey(localPlayer.dashKey));
        data.Buttons.Set((int)InputButton.Jump, Input.GetKey(localPlayer.jumpKey));
        data.Buttons.Set((int)InputButton.PolarityPositive, Input.GetKey(KeyCode.Alpha1));
        data.Buttons.Set((int)InputButton.PolarityNegative, Input.GetKey(KeyCode.Alpha2));

        // Đang mở giao diện thì nuốt luôn hai nút chuột, không cho chúng thành lệnh bắn.
        // Nếu không, bấm nút "Mua" trong Shop cũng đồng thời là một cú hút/đẩy vào thứ
        // đang nằm sau tấm panel.
        bool uiOpen = IsCursorFree();
        data.Buttons.Set((int)InputButton.Fire, !uiOpen && Input.GetMouseButton(0));
        data.Buttons.Set((int)InputButton.Melee, !uiOpen && Input.GetMouseButton(1));
        // Phím tung hứng lấy từ Inspector của PlayerMagnetController, không hardcode
        PlayerMagnetController magnet = localPlayer.GetComponent<PlayerMagnetController>();
        KeyCode tossKey = magnet != null ? magnet.tossKey : KeyCode.V;
        data.Buttons.Set((int)InputButton.Toss, Input.GetKey(tossKey));

        // Phím nhặt đồ, cũng lấy từ Inspector
        PlayerInteract interact = localPlayer.GetComponent<PlayerInteract>();
        KeyCode interactKey = interact != null ? interact.interactKey : KeyCode.F;
        data.Buttons.Set((int)InputButton.Interact, Input.GetKey(interactKey));

        // Ba phím rút đạn từ túi
        PlayerHotbarController hotbar = localPlayer.GetComponent<PlayerHotbarController>();
        if (hotbar != null)
        {
            data.Buttons.Set((int)InputButton.HotbarNormal, Input.GetKey(hotbar.normalKey));
            data.Buttons.Set((int)InputButton.HotbarHeavy, Input.GetKey(hotbar.heavyKey));
            data.Buttons.Set((int)InputButton.HotbarSpike, Input.GetKey(hotbar.spikeKey));
        }

        // CHỈ ĐỂ TEST - XOÁ TRƯỚC KHI NỘP BÀI
        data.Buttons.Set((int)InputButton.DebugSuicide, Input.GetKey(KeyCode.K));

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    /// <summary>
    /// Máy này vừa mất kết nối tới Host. Nguyên nhân phổ biến nhất: HOST THOÁT GIỮA TRẬN.
    ///
    /// ⚠️ TRƯỚC 01/09 HÀM NÀY BỎ TRỐNG, và đó là lý do Host thoát thì mọi người đứng hình.
    ///
    /// Đừng tưởng OnShutdown sẽ lo hộ. Hai callback này báo hai chuyện khác nhau:
    ///   OnDisconnectedFromServer : "đường truyền tới Host đứt"
    ///   OnShutdown               : "Runner ở MÁY NÀY đã dừng hẳn"
    ///
    /// Mất Host không nhất thiết làm Runner của client tự dừng - nó có thể ngồi chờ kết nối
    /// lại. Trong lúc đó scene vẫn chạy nhưng không còn ai mô phỏng: nhân vật đứng im, bấm
    /// gì cũng không phản hồi. Người chơi thấy đúng là "đứng hình rồi crash".
    /// </summary>
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[MẠNG] Mất kết nối tới Host: {reason}");

        // Đang trong trận -> đưa về menu. ReturnToMenu tự chặn gọi chồng, nên gọi ở cả
        // hai callback cũng an toàn: cái nào tới trước thì làm, cái sau tự thoát.
        //
        // ⚠️ PHẢI xét IsInGameScene, không chỉ _matchStarted. Trên máy Client cờ đó luôn
        // là false, và đây chính là chỗ làm bản sửa lần trước không ăn - xem ghi chú
        // đầy đủ ở khai báo IsInGameScene.
        if (_matchStarted || IsInGameScene)
        {
            // Để dành câu thông báo cho MenuScene hiện. Không hiện được tại chỗ vì mạng
            // đã chết, và người chơi sắp bị chuyển scene trong tích tắc.
            _pendingMenuMessage = "Host left the game — match ended";

            ReturnToMenu();
            return;
        }

        // Còn ở phòng chờ thì chỉ cần lùi về màn hình chính
        SetErrorMessage("Host left the room");
        ShowPanel(mainButtonsPanel);
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    /// <summary>
    /// Fusion gọi hàm này mỗi khi danh sách phòng trong Lobby thay đổi (có phòng mới,
    /// phòng đầy, phòng đóng...) - NHƯNG CHỈ SAU KHI đã JoinSessionLobby() thành công.
    /// Trước đây thân hàm để trống nên dù Photon có gửi danh sách về, không ai vẽ nó lên
    /// UI cả - đó là lý do bấm Room List không thấy phòng nào.
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (roomListContent == null || roomItemPrefab == null) return;

        // Xoá sạch danh sách cũ rồi vẽ lại từ đầu - đơn giản và đủ nhanh vì phòng chờ
        // hiếm khi có quá vài chục phòng cùng lúc.
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (SessionInfo session in sessionList)
        {
            // IsVisible=false là phòng đã khoá (trận đã bắt đầu) - xem OnClickStartMatch().
            if (!session.IsVisible) continue;

            RoomItemUI item = Instantiate(roomItemPrefab, roomListContent);
            item.Setup(session, this);
        }
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
