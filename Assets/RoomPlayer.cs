using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RoomPlayer : NetworkBehaviour
{
    public static RoomPlayer Local { get; private set; }
    public static readonly List<RoomPlayer> AllPlayers = new List<RoomPlayer>();

    /// <summary>Số người mỗi đội ở chế độ lớn nhất (2v2). Giao diện cấp sẵn chừng này ô cho mỗi đội.</summary>
    public const int MaxTeamSize = 2;

    /// <summary>
    /// Số người mỗi đội của phòng HIỆN TẠI: 1 = đấu 1v1, 2 = đấu 2v2 (16/10).
    ///
    /// Chế độ nằm trên RoomPlayer của CHỦ PHÒNG (ô RoomTeamSize) — đó là object mạng duy nhất
    /// mà mọi máy đều thấy và chỉ Host được ghi. Chưa thấy chủ phòng thì coi như 2v2.
    /// </summary>
    public static int TeamSize
    {
        get
        {
            RoomPlayer host = HostPlayer;
            if (host == null) return MaxTeamSize;
            int size = host.RoomTeamSize;
            return size >= 1 && size <= MaxTeamSize ? size : MaxTeamSize;
        }
    }

    /// <summary>RoomPlayer của chủ phòng, hoặc null nếu chưa spawn xong.</summary>
    public static RoomPlayer HostPlayer
    {
        get
        {
            foreach (RoomPlayer p in AllPlayers)
                if (p != null && p.Object != null && p.Object.IsValid && p.IsHost) return p;
            return null;
        }
    }

    /// <summary>Số người đang có trong phòng (chỉ đếm RoomPlayer đã spawn xong).</summary>
    public static int Count
    {
        get
        {
            int n = 0;
            foreach (RoomPlayer p in AllPlayers)
                if (p != null && p.Object != null && p.Object.IsValid) n++;
            return n;
        }
    }

    [Networked]
    public PlayerRef PlayerRef { get; set; }

    // Dùng [OnChangedRender] để Fusion 2 tự động gọi hàm OnDataChanged khi dữ liệu Tên/Team đồng bộ về máy
    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkString<_16> NickName { get; set; }

    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public int Team { get; set; } // 0 = Đỏ, 1 = Xanh

    /// <summary>
    /// Đây có phải chủ phòng không (16/10). Phòng chờ dùng để hiện vương miện và để biết ai được
    /// đổi đội. Phải là [Networked]: máy Client không tự biết được ai là Host.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public NetworkBool IsHost { get; set; }

    /// <summary>
    /// Chế độ của phòng: số người mỗi đội (1 hoặc 2). CHỈ có nghĩa trên RoomPlayer của chủ phòng —
    /// đọc qua RoomPlayer.TeamSize, đừng đọc thẳng ô này của người khác.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnDataChanged))]
    public int RoomTeamSize { get; set; }

    public override void Spawned()
    {
        AllPlayers.Add(this);
        DontDestroyOnLoad(gameObject);

        // Host sinh ra RoomPlayer cho mọi người. RoomPlayer nào có quyền điều khiển trùng với
        // chính máy Host thì đó là của chủ phòng.
        if (HasStateAuthority && Object.InputAuthority == Runner.LocalPlayer)
        {
            IsHost = true;

            // Chế độ chủ phòng đã chọn lúc ghép trận (tạo phòng thường thì mặc định 2v2)
            NetworkRunnerHandler h = NetworkRunnerHandler.Instance;
            RoomTeamSize = h != null ? h.PendingTeamSize : MaxTeamSize;
        }

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

    // Yêu cầu Server đổi Team cho chính mình.
    //
    // TỪ 16/10: CHỈ CHỦ PHÒNG được đổi đội. Client gửi yêu cầu này lên thì Host bỏ qua.
    // Chặn ở đây (phía Host) chứ không chỉ ẩn nút, vì ẩn nút chỉ là giao diện — chốt chặn thật
    // phải nằm ở nơi giữ quyền ghi dữ liệu.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestTeamChange(int newTeam)
    {
        if (!IsHost) return;
        TryMoveToTeam(newTeam);
    }

    /// <summary>
    /// Chủ phòng chuyển người này sang đội khác (bấm vào thẻ người chơi trong phòng chờ).
    /// Chỉ chạy được trên máy Host — Host giữ quyền ghi của mọi RoomPlayer nên đổi thẳng được,
    /// không cần gửi RPC. Trả về false nếu đội đích đã đủ người.
    /// </summary>
    public bool TryMoveToTeam(int newTeam)
    {
        if (!HasStateAuthority) return false;
        if (newTeam != 0 && newTeam != 1) return false;
        if (newTeam == Team) return false;
        if (CountInTeam(newTeam) >= TeamSize) return false;

        Team = newTeam; // Khi Team đổi, OnChangedRender sẽ tự kích hoạt cập nhật UI
        return true;
    }

    /// <summary>
    /// Chủ phòng đổi chế độ 1v1 ⇄ 2v2. Chỉ chạy trên máy Host.
    ///
    /// Phòng đang đông hơn sức chứa của chế độ mới (ví dụ 3 người mà đòi 1v1) thì TỪ CHỐI —
    /// chủ phòng phải tự kick bớt. Không tự đuổi ai: đuổi nhầm người là mất bạn chơi.
    /// Đổi được thì tự dàn lại đội: đang 2 người cùng bên Đỏ mà chuyển 1v1 thì một người
    /// (không phải chủ phòng) sang Xanh.
    /// </summary>
    /// <param name="why">Lý do từ chối, để hiện lên cho chủ phòng đọc.</param>
    public static bool TrySetTeamSize(int size, out string why)
    {
        why = "";
        RoomPlayer host = HostPlayer;
        if (host == null || !host.HasStateAuthority) return false;

        size = Mathf.Clamp(size, 1, MaxTeamSize);
        if (size == TeamSize) return true;

        int count = Count;
        if (count > size * 2)
        {
            int extra = count - size * 2;
            why = $"{size}v{size} fits {size * 2} players — kick {extra} player{(extra > 1 ? "s" : "")} first";
            return false;
        }

        host.RoomTeamSize = size;

        // Dàn lại đội cho vừa chế độ mới. Đội kia chắc chắn còn chỗ vì tổng số đã ≤ size * 2.
        for (int t = 0; t < 2; t++)
        {
            while (CountInTeam(t) > size)
            {
                RoomPlayer mover = null;
                foreach (RoomPlayer p in AllPlayers)
                {
                    if (p == null || p.Object == null || !p.Object.IsValid || p.Team != t) continue;
                    if (mover == null || mover.IsHost) mover = p;   // ưu tiên chuyển người KHÔNG phải chủ phòng
                }
                if (mover == null) break;
                mover.Team = 1 - t;
            }
        }

        NetworkRunnerHandler.Instance?.OnRoomModeChanged(size);
        return true;
    }

    /// <summary>
    /// Chủ phòng đuổi người này khỏi phòng. Chỉ chạy trên máy Host, không đuổi được chính mình.
    ///
    /// Báo trước cho người bị đuổi bằng RPC để họ tự rời phòng êm và đọc được câu
    /// "You were kicked" — nếu cắt kết nối luôn thì máy họ chỉ thấy "Host left the room",
    /// tưởng chủ phòng thoát. Vẫn cắt kết nối sau 1,5 giây phòng khi máy họ không chịu rời.
    /// </summary>
    public void Kick()
    {
        if (!HasStateAuthority || IsHost) return;
        RPC_YouWereKicked();
        NetworkRunnerHandler.Instance?.DisconnectLater(PlayerRef, 1.5f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_YouWereKicked()
    {
        NetworkRunnerHandler.Instance?.OnKickedByHost();
    }

    public static int CountInTeam(int team)
    {
        int n = 0;
        foreach (RoomPlayer p in AllPlayers)
            if (p != null && p.Object != null && p.Object.IsValid && p.Team == team) n++;
        return n;
    }
}
