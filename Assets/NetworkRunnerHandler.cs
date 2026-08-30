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

        ShowPanel(namePanel);
        if (statusErrorText != null) statusErrorText.text = "";
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
        if (statusErrorText != null) statusErrorText.text = "";
        EnsureRunnerExists();

        await _networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            PlayerCount = 4,
            SceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>()
        });

        ShowPanel(roomLobbyPanel);
    }

    // --- 2. TẠO PHÒNG MỚI ---
    public async void OnConfirmCreateRoom()
    {
        if (statusErrorText != null) statusErrorText.text = "";
        EnsureRunnerExists();

        _currentRoomCode = UnityEngine.Random.Range(10000, 99999).ToString();
        if (yourRoomIDText != null) yourRoomIDText.text = "Mã Phòng: " + _currentRoomCode;

        var result = await _networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = _currentRoomCode,
            PlayerCount = 4,
            SceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            ShowPanel(roomLobbyPanel);
        }
        else
        {
            SetErrorMessage("Không thể tạo phòng!");
        }
    }

    // --- 3. VÀO PHÒNG BẰNG MÃ ---
    public async void OnClickJoinByCode()
    {
        if (statusErrorText != null) statusErrorText.text = "";

        if (string.IsNullOrEmpty(joinCodeInput.text))
        {
            SetErrorMessage("Vui lòng nhập Mã Phòng!");
            return;
        }

        EnsureRunnerExists();
        _currentRoomCode = joinCodeInput.text.Trim();

        var result = await _networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = _currentRoomCode,
            SceneManager = _networkRunner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            ShowPanel(roomLobbyPanel);
        }
        else
        {
            SetErrorMessage("Phòng không tồn tại hoặc đã đầy!");
            ShowPanel(mainButtonsPanel);
            
            if (_networkRunner != null)
            {
                Destroy(_networkRunner.gameObject);
                _networkRunner = null;
            }
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

        if (_networkRunner != null)
        {
            await _networkRunner.Shutdown();

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
        if (_networkRunner != null)
        {
            await _networkRunner.Shutdown();
            Destroy(_networkRunner.gameObject);
            _networkRunner = null;
        }
        ShowPanel(mainButtonsPanel);
    }

    // --- 5. BẮT ĐẦU TRẬN (LOAD SCENE) ---
    public void OnClickStartMatch()
    {
        if (_networkRunner != null && _networkRunner.IsServer)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(gameSceneName);
            if (sceneIndex != -1)
            {
                // Bật cờ này TRƯỚC khi load, để khi scene load xong thì biết
                // đây là lần vào trận thật và tiến hành spawn nhân vật.
                _matchStarted = true;
                _networkRunner.LoadScene(SceneRef.FromIndex(sceneIndex));
            }
            else
            {
                SetErrorMessage($"Scene '{gameSceneName}' chưa được thêm vào Build Settings!");
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
            string pName = string.IsNullOrEmpty(p.NickName.ToString()) ? "Đang tải..." : p.NickName.ToString();
            
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

        if (teamRedText != null) teamRedText.text = $"ĐỘI ĐỎ ({redCount}/2):\n" + redList;
        if (teamBlueText != null) teamBlueText.text = $"ĐỘI XANH ({blueCount}/2):\n" + blueList;

        if (roomTitleText != null)
        {
            roomTitleText.text = $"PHÒNG: {_currentRoomCode} ({RoomPlayer.AllPlayers.Count}/4)";
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

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        UpdateLobbyUI();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // Đang ở trong trận mà Runner tắt -> Host đã kết thúc trận, hoặc mất kết nối.
        // Dù lý do nào thì cũng phải đưa người chơi về menu, không để họ kẹt lại
        // trong một scene không còn mạng.
        if (_matchStarted)
        {
            if (shutdownReason != ShutdownReason.Ok)
            {
                Debug.LogWarning($"[MẠNG] Trận kết thúc bất thường: {shutdownReason}");
            }

            ReturnToMenu();
            return;
        }

        // Còn đang ở phòng chờ thì chỉ cần quay lại màn hình chính
        if (shutdownReason != ShutdownReason.Ok)
        {
            SetErrorMessage("Kết nối bị ngắt!");
            ShowPanel(mainButtonsPanel);
        }
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        SetErrorMessage("Không thể kết nối!");
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
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
