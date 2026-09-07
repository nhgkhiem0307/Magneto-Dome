using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Một dòng trong Room List. NetworkRunnerHandler.OnSessionListUpdated() tạo ra
// (Instantiate) một bản của prefab này cho mỗi phòng đang mở, rồi gọi Setup() để điền
// thông tin phòng vào và gắn nút JOIN.
public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    public void Setup(SessionInfo session, NetworkRunnerHandler handler)
    {
        if (roomNameText != null) roomNameText.text = "Room: " + session.Name;
        if (playerCountText != null) playerCountText.text = $"{session.PlayerCount}/{session.MaxPlayers}";

        if (joinButton != null)
        {
            // Phòng đã đầy hoặc trận đã bắt đầu -> vẫn hiện trong danh sách nhưng không
            // bấm JOIN được, thay vì để người chơi bấm rồi nhận lỗi "Room not found".
            joinButton.interactable = session.IsOpen;

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => handler.JoinRoomFromList(session.Name));
        }
    }
}
