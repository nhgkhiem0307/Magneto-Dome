using UnityEngine;

/// <summary>
/// Luôn quay mặt về phía camera của người chơi. Dùng cho mọi thứ nổi trong thế giới:
/// marker đồng đội, thanh điện tích trên đầu, chữ báo sát thương...
///
/// Gắn lên chính object cần quay (Canvas World Space, Quad, Sprite... đều được).
///
/// Chạy ở LateUpdate chứ không phải Update: camera được xoay trong Render() của
/// FPSMovement, mà thứ tự Update giữa các script thì không đoán được. Quay ở LateUpdate
/// bảo đảm camera đã ở vị trí cuối cùng của khung hình rồi mới tính hướng - nếu không
/// marker sẽ trễ một khung hình và rung nhẹ khi bạn xoay chuột nhanh.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Header("Kiểu quay")]
    [Tooltip("BẬT: chỉ xoay quanh trục đứng, marker luôn thẳng đứng. Hợp với thanh máu/điện.\n" +
             "TẮT: quay hẳn mặt về camera kể cả khi nhìn từ trên xuống. Hợp với icon tròn.")]
    public bool uprightOnly = true;

    [Tooltip("Lật ngược 180 độ. Bật nếu chữ/hình bị hiện ngược - " +
             "tuỳ hướng mặt trước của mesh hay Canvas mà cần hoặc không.")]
    public bool flip = false;

    [Header("Giữ cỡ khi nhìn từ xa")]
    [Tooltip("BẬT: vật to dần theo khoảng cách để lúc nào cũng đọc được. " +
             "Cần cho marker và thanh điện - đứng bên kia bản đồ vẫn phải thấy.")]
    public bool constantScreenSize = false;

    [Tooltip("Cỡ ở khoảng cách chuẩn 10m. Xa hơn thì to lên theo tỉ lệ.")]
    public float sizeAt10Meters = 1f;

    [Tooltip("Chặn trên, để đứng cách 100m marker không phình ra che cả màn hình.")]
    public float maxScale = 4f;

    private Camera _cam;
    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void LateUpdate()
    {
        // Camera có thể chưa tồn tại lúc mới spawn, hoặc đổi khi quan sát đồng đội.
        // Tìm lại mỗi khi mất chứ không cache một lần rồi thôi.
        if (_cam == null || !_cam.isActiveAndEnabled)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        Vector3 toCam = _cam.transform.position - transform.position;

        if (uprightOnly)
        {
            // Bỏ thành phần dọc: marker chỉ xoay trái phải, không bao giờ ngả ngửa.
            // Nhìn từ trên cao xuống thì nó vẫn đứng thẳng thay vì nằm bẹp.
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) return;
        }

        if (flip) toCam = -toCam;

        transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);

        if (!constantScreenSize) return;

        // To dần theo khoảng cách để cỡ trên màn hình không đổi.
        // Dùng khoảng cách THẬT chứ không phải khoảng cách ngang, vì đứng dưới nhìn lên
        // cũng phải đọc được.
        float distance = Vector3.Distance(_cam.transform.position, transform.position);
        float factor = Mathf.Min(distance / 10f, maxScale);

        transform.localScale = _baseScale * sizeAt10Meters * Mathf.Max(factor, 0.1f);
    }
}
