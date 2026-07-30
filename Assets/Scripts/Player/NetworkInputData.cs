using Fusion;
using UnityEngine;

/// <summary>
/// Gói dữ liệu điều khiển được gửi từ máy người chơi lên Host mỗi tick mạng.
///
/// Cách hoạt động (Host Mode - server authoritative):
/// máy người chơi CHỈ gửi ý định ("tôi đang bấm W", "tôi đang nhìn hướng này"),
/// còn Host mới là bên tính toán nhân vật đi tới đâu. Nhờ vậy người chơi không thể
/// gian lận bằng cách sửa vị trí nhân vật ở máy mình.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    // Hướng di chuyển WASD. x = trái/phải (A/D), y = tiến/lùi (W/S).
    public Vector2 MoveDirection;

    // Góc xoay ngang của thân người, tính bằng độ.
    // Được tích luỹ từ chuột ở máy người chơi rồi gửi sang dạng góc tuyệt đối.
    public float Yaw;

    // Góc ngẩng/cúi của camera, tính bằng độ, đã giới hạn trong khoảng -80 đến 80.
    public float Pitch;

    // Toàn bộ nút bấm được nén chung vào 1 biến (mỗi nút 1 bit) để tiết kiệm băng thông.
    public NetworkButtons Buttons;
}

/// <summary>
/// Danh sách các nút bấm được gửi qua mạng.
///
/// Mỗi nút chiếm đúng 1 bit trong NetworkButtons, nên bắt buộc phải đánh số
/// từ 0 trở lên và KHÔNG được trùng nhau. Khi bổ sung thao tác mới
/// (Bắn, Cận chiến, Tung hứng, Blink, Phòng thủ...) thì thêm vào cuối danh sách này.
/// </summary>
public enum InputButton
{
    Dash = 0,               // phím Q
    PolarityPositive = 1,   // phím 1 - đổi găng sang cực Dương (Đỏ)
    PolarityNegative = 2,   // phím 2 - đổi găng sang cực Âm (Xanh)
    Fire = 3,               // chuột trái - nạp điện / hút / đẩy
    Melee = 4,              // chuột phải - cận chiến, hoặc bắn vật đang cầm
    Toss = 5,               // phím V - tung hứng vật đang cầm
}
