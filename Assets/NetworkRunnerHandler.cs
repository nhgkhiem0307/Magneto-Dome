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
    [Header("UI Panels")]
    public GameObject namePanel;
    public GameObject mainButtonsPanel;
    public GameObject createRoomPanel;
    public GameObject roomListPanel;
    public GameObject roomLobbyPanel;

    [Header("UI Inputs & Texts (TextMeshPro)")]
    public TMP_InputField nameInput;
    public TMP_InputField joinCodeInput;
    public TMP_Text yourRoomIDText;
    public TMP_Text roomTitleText;
    public TMP_Text teamRedText;
    public TMP_Text teamBlueText;
    public TMP_Text statusErrorText;

    [Header("Lobby Buttons")]
    public Button startMatchButton;
    public Button switchTeamButton;

    [Header("Room List UI")]
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    [Header("Settings")]
    public string gameSceneName = "GameScene";

    private NetworkRunner _runner;
    private string _playerNickname = "Player";
    private string _currentRoomCode = "";

    // Lưu danh sách người chơi cục bộ để hiển thị UI
    private Dictionary<PlayerRef, (string Name, int Team)> _lobbyPlayers = new Dictionary<PlayerRef, (string, int)>();

    private void Start()
    {
        ShowPanel(namePanel);
        if (statusErrorText != null) statusErrorText.text = "";
    }

    public void ShowPanel(GameObject panelToShow)
    {
        if (namePanel != null) namePanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);
        if (roomLobbyPanel != null) roomLobbyPanel.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
    }

    public void OnConfirmName()
    {
        if (!string.IsNullOrEmpty(nameInput.text))
        {
            _playerNickname = nameInput.text;
        }
        ShowPanel(mainButtonsPanel);
    }

    private void EnsureRunnerExists()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);
        }
    }

    // --- 1. GHÉP TRẬN NGẪU NHIÊN ---
    public async void OnClickMatchmaking()
    {
        if (statusErrorText != null) statusErrorText.text = "";
        EnsureRunnerExists();

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            PlayerCount = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
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

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = _currentRoomCode,
            PlayerCount = 4,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
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

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = _currentRoomCode,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            ShowPanel(roomLobbyPanel);
        }
        else
        {
            SetErrorMessage("Phòng không tồn tại hoặc đã đầy!");
            ShowPanel(mainButtonsPanel);
        }
    }

    // --- 4. RỜI PHÒNG ---
    public async void OnClickLeaveRoom()
    {
        if (_runner != null)
        {
            await _runner.Shutdown();
        }
        _lobbyPlayers.Clear();
        ShowPanel(mainButtonsPanel);
    }

    // --- 5. BẮT ĐẦU TRẬN ---
    public void OnClickStartMatch()
    {
        if (_runner != null && _runner.IsServer)
        {
            _runner.LoadScene(SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(gameSceneName)));
        }
    }

    // --- 6. ĐỔI TEAM (SWITCH TEAM) ---
    public void OnClickSwitchTeam()
    {
        if (_runner == null) return;

        if (_lobbyPlayers.TryGetValue(_runner.LocalPlayer, out var myInfo))
        {
            int targetTeam = myInfo.Team == 0 ? 1 : 0;

            int targetTeamCount = 0;
            foreach (var p in _lobbyPlayers.Values)
            {
                if (p.Team == targetTeam) targetTeamCount++;
            }

            if (targetTeamCount < 2)
            {
                if (_runner.IsServer)
                {
                    _lobbyPlayers[_runner.LocalPlayer] = (myInfo.Name, targetTeam);
                    BroadcastLobbyState();
                }
                else
                {
                    RPC_RequestChangeTeam(_runner.LocalPlayer, targetTeam);
                }
            }
        }
    }

    // --- HỆ THỐNG RPC ĐỒNG BỘ TÊN & TEAM ---

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SendPlayerInfo(PlayerRef player, string name)
    {
        if (!_lobbyPlayers.ContainsKey(player))
        {
            int redCount = 0, blueCount = 0;
            foreach (var p in _lobbyPlayers.Values)
            {
                if (p.Team == 0) redCount++;
                else blueCount++;
            }
            int assignedTeam = (redCount <= blueCount) ? 0 : 1;
            _lobbyPlayers[player] = (name, assignedTeam);
        }
        BroadcastLobbyState();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestChangeTeam(PlayerRef player, int newTeam)
    {
        if (_lobbyPlayers.ContainsKey(player))
        {
            string pName = _lobbyPlayers[player].Name;
            _lobbyPlayers[player] = (pName, newTeam);
            BroadcastLobbyState();
        }
    }

    private void BroadcastLobbyState()
    {
        if (!_runner.IsServer) return;

        List<PlayerRef> players = new List<PlayerRef>();
        List<string> names = new List<string>();
        List<int> teams = new List<int>();

        foreach (var kvp in _lobbyPlayers)
        {
            players.Add(kvp.Key);
            names.Add(kvp.Value.Name);
            teams.Add(kvp.Value.Team);
        }

        RPC_UpdateLobbyUI(players.ToArray(), names.ToArray(), teams.ToArray());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateLobbyUI(PlayerRef[] players, string[] names, int[] teams)
    {
        _lobbyPlayers.Clear();
        for (int i = 0; i < players.Length; i++)
        {
            _lobbyPlayers[players[i]] = (names[i], teams[i]);
        }
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        string redList = "";
        string blueList = "";
        int redCount = 0;
        int blueCount = 0;

        foreach (var p in _lobbyPlayers.Values)
        {
            if (p.Team == 0)
            {
                redList += $"- {p.Name}\n";
                redCount++;
            }
            else
            {
                blueList += $"- {p.Name}\n";
                blueCount++;
            }
        }

        if (teamRedText != null) teamRedText.text = $"ĐỘI ĐỎ ({redCount}/2):\n" + redList;
        if (teamBlueText != null) teamBlueText.text = $"ĐỘI XANH ({blueCount}/2):\n" + blueList;

        if (roomTitleText != null)
        {
            roomTitleText.text = $"PHÒNG: {_currentRoomCode} ({_lobbyPlayers.Count}/4)";
        }

        if (startMatchButton != null)
        {
            startMatchButton.gameObject.SetActive(_runner != null && _runner.IsServer);
        }
    }

    private void SetErrorMessage(string message)
    {
        if (statusErrorText != null)
        {
            statusErrorText.text = message;
        }
    }

    // --- FUSION CALLBACKS ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            RPC_SendPlayerInfo(player, _playerNickname);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            _lobbyPlayers.Remove(player);
            BroadcastLobbyState();
        }
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
        SetErrorMessage("Không thể kết nối tới phòng!");
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