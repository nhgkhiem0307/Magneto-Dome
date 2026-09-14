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
            // Phải xét CẢ số người, không chỉ IsOpen. Phòng đầy 4/4 vẫn có IsOpen = true
            // (nó chỉ báo "chưa khoá vì trận bắt đầu"), nên chỉ xét IsOpen là nút JOIN của
            // phòng đầy vẫn bấm được, rồi văng ra "Room not found or already full!" -
            // đúng cái mà dòng ghi chú bên trên nói là đã tránh được.
            joinButton.interactable = session.IsOpen && session.PlayerCount < session.MaxPlayers;

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => handler.JoinRoomFromList(session.Name));
        }
    }
}
