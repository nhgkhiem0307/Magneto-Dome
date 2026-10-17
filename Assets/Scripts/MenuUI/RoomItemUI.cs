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
        // (16/10) Số chỗ là sức chứa THẬT theo chế độ: phòng nào cũng mở 4 chỗ trên Photon,
        // phòng 1v1 thì chủ phòng tự chặn ở người thứ 3 — ghi "/4" là sai.
        int teamSize = NetworkRunnerHandler.SessionTeamSize(session);
        int capacity = NetworkRunnerHandler.SessionCapacity(session);
        if (playerCountText != null) playerCountText.text = $"{session.PlayerCount}/{capacity}";
        ShowModeTag($"{teamSize}v{teamSize}");

        if (joinButton != null)
        {
            // Phòng đã đầy hoặc trận đã bắt đầu -> vẫn hiện trong danh sách nhưng không
            // bấm JOIN được, thay vì để người chơi bấm rồi nhận lỗi "Room not found".
            // Phải xét CẢ số người, không chỉ IsOpen. Phòng đầy 4/4 vẫn có IsOpen = true
            // (nó chỉ báo "chưa khoá vì trận bắt đầu"), nên chỉ xét IsOpen là nút JOIN của
            // phòng đầy vẫn bấm được, rồi văng ra "Room not found or already full!" -
            // đúng cái mà dòng ghi chú bên trên nói là đã tránh được.
            joinButton.interactable = session.IsOpen && session.PlayerCount < capacity;

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => handler.JoinRoomFromList(session.Name));
        }
    }

    private TMP_Text _modeTag;

    // Nhãn chế độ "1v1" / "2v2" dựng bằng code, đặt ở khoảng trống giữa số người và nút JOIN.
    // Không ghép chung vào ô số người: ô đó căn giữa, chữ dài ra là đè lên icon người bên trái nó.
    private void ShowModeTag(string text)
    {
        if (_modeTag == null)
        {
            GameObject go = new GameObject("ModeTag", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(135f, 0f);
            rt.sizeDelta = new Vector2(110f, 50f);
            _modeTag = go.AddComponent<TextMeshProUGUI>();
            if (roomNameText != null) _modeTag.font = roomNameText.font;
            _modeTag.fontSize = 26;
            _modeTag.fontStyle = FontStyles.Bold;
            _modeTag.color = new Color32(0x5C, 0xD8, 0xFF, 0xFF);
            _modeTag.alignment = TextAlignmentOptions.Center;
            _modeTag.textWrappingMode = TextWrappingModes.NoWrap;
            _modeTag.raycastTarget = false;
        }
        _modeTag.text = text;
    }
}
