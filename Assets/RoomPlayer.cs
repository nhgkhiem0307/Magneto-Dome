using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    public static RoomPlayer Local { get; private set; }
    public static readonly List<RoomPlayer> AllPlayers = new List<RoomPlayer>();

    [Networked] 
    public PlayerRef PlayerRef { get; set; }

    // Dùng [OnChangedRender] để Fusion 2 tự động gọi hàm OnDataChanged khi dữ liệu Tên/Team đồng bộ về máy
    [Networked, OnChangedRender(nameof(OnDataChanged))] 
    public NetworkString<_16> NickName { get; set; }

    [Networked, OnChangedRender(nameof(OnDataChanged))] 
    public int Team { get; set; } // 0 = Đỏ, 1 = Xanh

    public override void Spawned()
    {
        AllPlayers.Add(this);
        DontDestroyOnLoad(gameObject);

        if (Object.HasInputAuthority)
        {
            Local = this;
            // Gửi tên từ UI cục bộ lên Server khi vừa Spawn
            RPC_SetPlayerInfo(NetworkRunnerHandler.LocalPlayerName);
        }

        NetworkRunnerHandler.Instance?.UpdateLobbyUI();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        AllPlayers.Remove(this);

        // Dọn luôn tham chiếu tĩnh, nếu không thì sau khi về menu nó vẫn trỏ vào
        // một object đã bị huỷ của trận cũ.
        if (Local == this) Local = null;

        NetworkRunnerHandler.Instance?.UpdateLobbyUI();
    }

    // Hàm này sẽ tự động chạy trên CẢ Host lẫn Client mỗi khi NickName hoặc Team thay đổi
    private void OnDataChanged()
    {
        NetworkRunnerHandler.Instance?.UpdateLobbyUI();
    }

    // Client gửi tên lên Server
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerInfo(string name)
    {
        NickName = name; // Khi NickName đổi, OnChangedRender sẽ tự kích hoạt cập nhật UI
    }

    // Client yêu cầu Server đổi Team
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestTeamChange(int newTeam)
    {
        Team = newTeam; // Khi Team đổi, OnChangedRender sẽ tự kích hoạt cập nhật UI
    }
}