using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkRunnerHandler Instance { get; private set; }
    public static string LocalPlayerName = "Player";

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

    [Header("Lobby Buttons")]
    public Button startMatchButton;

    [Header("Settings")]
    public string gameSceneName = "GameScene";

    // Đã đổi tên biến từ _runner thành _networkRunner để tránh trùng lặp serialization
    private NetworkRunner _networkRunner;
    private string _currentRoomCode = "";

    private void Awake()
    {
        Instance = this;

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
        ShowPanel(namePanel);
        if (statusErrorText != null) statusErrorText.text = "";
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
        }
        ShowPanel(mainButtonsPanel);
    }

    private void EnsureRunnerExists()
    {
        if (_networkRunner == null)
        {
            GameObject runnerObj = new GameObject("FusionNetworkRunner");
            _networkRunner = runnerObj.AddComponent<NetworkRunner>();
            _networkRunner.ProvideInput = true;
            runnerObj.AddComponent<NetworkSceneManagerDefault>();
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

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}