using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Chế độ quan sát khi bị loại: xem trận đấu qua mắt đồng đội còn sống, kiểu Valorant.
///
/// ĐẶT TRÊN CANVAS, giống HUDController và ShopUI. Không đặt trên prefab nhân vật,
/// vì như vậy nó sẽ chạy trên cả nhân vật của người khác.
///
/// Toàn bộ việc này THUẦN CỤC BỘ, không cần đồng bộ gì qua mạng. Lý do:
/// camera của nhân vật người khác trên máy mình vốn đã được xoay đúng sẵn
/// (FPSMovement.Render áp NetPitch cho họ, NetworkTransform lo hướng thân).
/// Nên "quan sát" chỉ là đổi xem camera nào đang bật.
/// </summary>
public class SpectatorController : MonoBehaviour
{
    [Header("Giao diện")]
    [Tooltip("Khung hiện khi đang quan sát người khác.")]
    public GameObject spectatorPanel;

    [Tooltip("Dòng chữ báo đang xem qua mắt ai.")]
    public TMP_Text spectatingText;

    [Header("Điều khiển")]
    [Tooltip("Phím chuyển sang đồng đội khác. Ở 2v2 thường chỉ có một đồng đội nên ít dùng tới.")]
    public KeyCode cycleKey = KeyCode.Space;

    /// <summary>
    /// Người đang được quan sát, null nếu đang nhìn bằng mắt mình.
    /// HUDController đọc giá trị này để hiện máu/đạn của người đang xem
    /// thay vì hiện máu 0 của cái xác mình.
    /// </summary>
    public static FPSMovement Watching { get; private set; }

    // Người đang được quan sát. Null nghĩa là đang dùng camera của chính mình.
    private FPSMovement _watching;

    void Update()
    {
        FPSMovement local = FPSMovement.Local;

        // Rời trận hoặc chưa vào trận
        if (local == null)
        {
            StopWatching();
            HidePanel();
            return;
        }

        PlayerHealth myHealth = local.GetComponent<PlayerHealth>();
        if (myHealth == null) return;

        // CÒN SỐNG -> luôn nhìn bằng mắt mình
        if (myHealth.IsAlive)
        {
            if (_watching != null) StopWatching();
            HidePanel();
            return;
        }

        // ĐÃ BỊ LOẠI -> tìm đồng đội để xem nhờ

        // Người đang xem cũng chết hoặc biến mất thì phải đổi
        if (!IsWatchable(_watching))
        {
            SwitchToNextTeammate(local, myHealth.Team);
        }
        else if (Input.GetKeyDown(cycleKey))
        {
            SwitchToNextTeammate(local, myHealth.Team);
        }

        UpdatePanel();
    }

    // --- CHỌN MỤC TIÊU ---

    private bool IsWatchable(FPSMovement candidate)
    {
        if (candidate == null) return false;

        PlayerHealth health = candidate.GetComponent<PlayerHealth>();
        return health != null && health.IsAlive;
    }

    private void SwitchToNextTeammate(FPSMovement local, int myTeam)
    {
        List<FPSMovement> teammates = CollectAliveTeammates(local, myTeam);

        if (teammates.Count == 0)
        {
            // Cả đội chết hết rồi, quay về nhìn bằng mắt mình (xác đang nằm đâu đó)
            StopWatching();
            return;
        }

        // Tìm vị trí người đang xem trong danh sách để nhảy sang người kế tiếp
        int nextIndex = 0;
        if (_watching != null)
        {
            int currentIndex = teammates.IndexOf(_watching);
            if (currentIndex >= 0)
            {
                nextIndex = (currentIndex + 1) % teammates.Count;
            }
        }

        StartWatching(local, teammates[nextIndex]);
    }

    private List<FPSMovement> CollectAliveTeammates(FPSMovement local, int myTeam)
    {
        List<FPSMovement> result = new List<FPSMovement>();

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null || !p.IsAlive) continue;
            if (p.Team != myTeam) continue; // không cho xem trộm đội địch

            FPSMovement movement = p.GetComponent<FPSMovement>();
            if (movement == null || movement == local) continue;

            result.Add(movement);
        }

        return result;
    }

    // --- ĐỔI CAMERA ---

    private void StartWatching(FPSMovement local, FPSMovement target)
    {
        if (target == _watching) return;

        // Tắt camera người đang xem trước đó
        if (_watching != null) SetCameraEnabled(_watching, false);

        _watching = target;
        Watching = target;

        SetCameraEnabled(local, false);
        SetCameraEnabled(target, true);
    }

    private void StopWatching()
    {
        if (_watching != null)
        {
            SetCameraEnabled(_watching, false);
            _watching = null;
        }

        Watching = null;

        // Trả camera về cho chính mình
        if (FPSMovement.Local != null) SetCameraEnabled(FPSMovement.Local, true);
    }

    // Bật/tắt cả Camera lẫn AudioListener.
    //
    // Phải tắt AudioListener của người kia, nếu không Unity sẽ cảnh báo
    // "có nhiều hơn một AudioListener trong scene" và âm thanh 3D tính sai khoảng cách.
    private void SetCameraEnabled(FPSMovement player, bool enabled)
    {
        if (player == null || player.cameraTransform == null) return;

        Camera cam = player.cameraTransform.GetComponent<Camera>();
        if (cam != null) cam.enabled = enabled;

        AudioListener listener = player.cameraTransform.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = enabled;
    }

    // --- GIAO DIỆN ---

    private void UpdatePanel()
    {
        if (spectatorPanel != null) spectatorPanel.SetActive(true);

        if (spectatingText == null) return;

        if (_watching == null)
        {
            spectatingText.text = "No teammates left to spectate";
            return;
        }

        string playerName = GetPlayerName(_watching);
        spectatingText.text = $"Spectating: {playerName}";
    }

    private void HidePanel()
    {
        if (spectatorPanel != null) spectatorPanel.SetActive(false);
    }

    // Lấy tên người chơi từ RoomPlayer của phòng chờ, đối chiếu theo PlayerRef
    private string GetPlayerName(FPSMovement player)
    {
        if (player.Object == null) return "Teammate";

        foreach (RoomPlayer rp in RoomPlayer.AllPlayers)
        {
            if (rp == null) continue;
            if (rp.PlayerRef != player.Object.InputAuthority) continue;

            string nickname = rp.NickName.ToString();
            return string.IsNullOrEmpty(nickname) ? "Đồng đội" : nickname;
        }

        return "Teammate";
    }
}
