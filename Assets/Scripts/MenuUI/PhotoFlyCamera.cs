using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Camera quay chụp đặt sẵn trong TestScene. Bấm Play là dùng được, KHÔNG cần vào trận.
///
/// ⚠️ KHÁC GÌ VỚI CinematicCamera:
///   CinematicCamera  - bật bằng F8 GIỮA TRẬN ĐANG CHƠI, để quay cảnh có người đánh nhau.
///   PhotoFlyCamera   - bấm Play ở TestScene là chạy, KHÔNG có mạng, KHÔNG có nhân vật.
///                      Bản đồ đứng im hoàn toàn. Dùng cho cảnh toàn đảo và ảnh poster.
///
/// Vì sao cần bản không-mạng: vào trận thì nhân vật sinh ra, vòng đấu bắt đầu, vật thể
/// được dựng lại, mây trôi, rào chắn đóng mở - nhiều thứ động đậy trong khung hình. Để
/// quay một cảnh tĩnh sạch sẽ thì cách chắc chắn nhất là KHÔNG khởi động trận nào cả.
///
/// CÁCH DÙNG:
///   1. Mở TestScene, chọn object "PhotoCamera" trong Hierarchy.
///   2. Tick "Auto Orbit" nếu muốn camera tự xoay quanh đảo; bỏ tick nếu muốn tự bay.
///   3. Bấm Play. Quay bằng OBS.
///
/// Tick và bỏ tick ĐƯỢC NGAY TRONG LÚC ĐANG PLAY - ô này được đọc lại mỗi khung hình,
/// nên bạn vừa tự bay tới góc đẹp rồi tick cho nó xoay từ chỗ đó.
///
/// ⚠️ Object này phải để TẮT (bỏ tick trước tên object) khi nộp bài - xem ROLLBACK.
/// </summary>
public class PhotoFlyCamera : MonoBehaviour
{
    [Header("Chế độ")]
    [Tooltip("Tick: camera TỰ xoay quanh tâm, đều tuyệt đối - dùng cho cảnh mở màn trailer.\n\n" +
             "Bỏ tick: bạn tự bay bằng WASD và chuột.\n\n" +
             "Đổi được ngay trong lúc đang Play: bay tới góc đẹp rồi tick, nó sẽ xoay từ " +
             "đúng chỗ đó với bán kính và độ cao hiện tại.")]
    public bool autoOrbit = true;

    [Tooltip("Ẩn toàn bộ giao diện khi Play, để khung hình sạch. Bấm Space để bật/tắt.")]
    public bool hideUI = true;

    [Tooltip("Đóng băng mọi vật có vật lý để bản đồ đứng im tuyệt đối.\n\n" +
             "⚠️ BẮT BUỘC BẬT khi quay cảnh tĩnh. Bình thường vật thể được hệ thống mạng " +
             "ghim tại chỗ ngay khi round bắt đầu; nhưng ở đây KHÔNG CÓ TRẬN NÀO CHẠY nên " +
             "không ai ghim chúng cả. Vật lý tự do sẽ làm mấy cái cây cao và mảnh đổ rạp " +
             "ngay trong khung hình - đúng lỗi 'cây nghiêng ngả'.")]
    public bool freezeMap = true;

    [Tooltip("Tự nhận tag MainCamera khi Play.\n\n" +
             "⚠️ BẮT BUỘC BẬT, nếu không BIỂN MÂY SẼ VỠ. Mỗi cụm mây là một tấm phẳng " +
             "phải xoay mặt về phía camera, và CloudBank tìm camera đó bằng Camera.main - " +
             "tức camera đang bật và mang tag MainCamera. Ngoài trận thì không có camera " +
             "nào như vậy (camera người chơi nằm trên nhân vật, mà nhân vật chưa sinh ra), " +
             "nên mây không biết quay về đâu và hiện ra lộn xộn.")]
    public bool claimMainCamera = true;

    [Header("Tự xoay quanh tâm")]
    [Tooltip("Tâm để xoay quanh. Để trống thì lấy gốc toạ độ - bản đồ được dựng quanh đó.")]
    public Transform orbitTarget;

    [Tooltip("Tốc độ xoay, độ mỗi giây. 6 độ/giây = một vòng hết 60 giây, rất chậm và " +
             "điện ảnh. Nhanh hơn 15 là bắt đầu thấy chóng mặt.")]
    public float orbitSpeed = 6f;

    [Tooltip("Khoảng cách từ tâm, mét. Đảo bán kính khoảng 36m. 70 là thấy trọn đảo mà đảo " +
             "vẫn choiển khung hình; trên 100 thì đảo teo lại thành một cục nhỏ giữa trền mây.")]
    public float orbitRadius = 70f;

    [Tooltip("Độ cao so với tâm, mét. Số dương là nhìn từ trên xuống. 22-30 vừa thấy mặt đảo " +
             "vừa thấy đáy đảo lơ lửng - cái đáy ấy mới là thứ chứng minh đảo đang bay.")]
    public float orbitHeight = 26f;

    [Tooltip("Vừa xoay vừa tiến dần vào tâm, mét mỗi giây. Chuyển động kép này tạo chiều " +
             "sâu hơn hẳn xoay đơn thuần - đúng kiểu mở màn trailer game thật. Để 0 nếu " +
             "chỉ muốn xoay.")]
    public float orbitZoomSpeed = 1.5f;

    [Header("Tự bay")]
    public float moveSpeed = 25f;
    public float lookSensitivity = 2.2f;

    private Camera _cam;
    private float _yaw, _pitch;
    private float _angle;
    private float _radius;
    private bool _wasOrbiting;
    private Canvas[] _hidden;

    private void Start()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = gameObject.AddComponent<Camera>();

        // Vẽ đè lên mọi camera khác, kể cả camera nhân vật nếu có.
        _cam.depth = 100f;

        // BỎ LỚP VIEWMODEL: hai bàn tay được vẽ bằng camera phủ riêng đặt sát mặt. Camera
        // này thấy chúng thì sẽ có hai bàn tay khổng lồ lơ lửng giữa trời.
        int vm = LayerMask.NameToLayer("Viewmodel");
        if (vm >= 0) _cam.cullingMask &= ~(1 << vm);

        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
        if (_pitch > 180f) _pitch -= 360f;

        _radius = orbitRadius;

        // Nhận tag MainCamera TRƯỚC khi mọi thứ khác chạy, để biển mây tìm thấy camera
        // ngay từ khung hình đầu. An toàn: object này mặc định tắt, nên lúc chơi thật
        // không bao giờ có hai camera cùng mang tag.
        if (claimMainCamera) gameObject.tag = "MainCamera";

        // URP gắn một component dữ liệu kèm theo mọi camera. Camera tạo bằng code lúc đang
        // chạy thì chưa có, nên gắn tay luôn và bật sẵn ảnh độ sâu: thiếu ảnh độ sâu thì
        // mây bị cắt thành cạnh thẳng ngay chỗ giao với đảo, trông như dán giấy.
        UniversalAdditionalCameraData urp = _cam.GetUniversalAdditionalCameraData();
        if (urp != null) urp.requiresDepthOption = CameraOverrideOption.On;

        if (freezeMap) FreezeAllPhysics();

        if (hideUI) ToggleUI();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[CAMERA CHỤP] Sẵn sàng. " +
                  (autoOrbit ? "Đang tự xoay." : "Tự bay: WASD, E/Q lên xuống, chuột xoay.") +
                  " Space ẩn/hiện giao diện. Bỏ tick Auto Orbit để tự bay.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) ToggleUI();

        // Vừa bật tự xoay -> lấy chỗ đang đứng làm điểm khởi đầu, không giật về chỗ khác.
        if (autoOrbit && !_wasOrbiting) SyncOrbitFromCurrentPosition();
        _wasOrbiting = autoOrbit;

        if (autoOrbit) UpdateOrbit();
        else UpdateFreeFly();
    }

    /// <summary>
    /// Ghim mọi vật có vật lý tại đúng chỗ đã đặt trong Editor.
    ///
    /// Trong một trận thật, hệ thống tự ghim vật thể ngay khi round bắt đầu - chính là
    /// để tránh chuyện cây đổ. Ở chế độ chụp ảnh thì không có trận nào, nên phải tự làm.
    ///
    /// Chỉ đổi isKinematic chứ không tắt component: tắt đi thì lúc thoát Play, Unity có
    /// thể không khôi phục đúng trạng thái ban đầu của scene.
    /// </summary>
    private void FreezeAllPhysics()
    {
        int n = 0;

        foreach (Rigidbody rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            if (rb == null || rb.isKinematic) continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            n++;
        }

        if (n > 0) Debug.Log($"[CAMERA CHỤP] Đã ghim {n} vật thể, bản đồ đứng im.");
    }

    // ==================== TỰ XOAY ====================

    private Vector3 Center => orbitTarget != null ? orbitTarget.position : Vector3.zero;

    /// <summary>
    /// Lấy bán kính, độ cao và góc hiện tại làm điểm bắt đầu xoay.
    ///
    /// Thiếu bước này thì lúc tick vào ô Auto Orbit, camera nhảy phắt về toạ độ ghi trong
    /// Inspector - mất đúng góc bạn vừa mất công bay tới.
    /// </summary>
    private void SyncOrbitFromCurrentPosition()
    {
        Vector3 flat = transform.position - Center;
        flat.y = 0f;

        if (flat.sqrMagnitude > 1f)
        {
            _radius = flat.magnitude;
            _angle = Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg;
            orbitHeight = transform.position.y - Center.y;
        }
        else
        {
            _radius = orbitRadius;
            _angle = 0f;
        }
    }

    private void UpdateOrbit()
    {
        _angle += orbitSpeed * Time.unscaledDeltaTime;
        _radius = Mathf.Max(5f, _radius - orbitZoomSpeed * Time.unscaledDeltaTime);

        float rad = _angle * Mathf.Deg2Rad;
        Vector3 c = Center;

        transform.position = c + new Vector3(Mathf.Cos(rad) * _radius,
                                             orbitHeight,
                                             Mathf.Sin(rad) * _radius);
        transform.LookAt(c);
    }

    // ==================== TỰ BAY ====================

    private void UpdateFreeFly()
    {
        _yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * lookSensitivity, -89f, 89f);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        float mul = Input.GetKey(KeyCode.LeftShift) ? 4f
                  : Input.GetKey(KeyCode.LeftControl) ? 0.25f : 1f;

        Vector3 move = transform.forward * Input.GetAxisRaw("Vertical")
                     + transform.right * Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        transform.position += move.normalized * (moveSpeed * mul * Time.unscaledDeltaTime);
    }

    // ==================== ẨN GIAO DIỆN ====================

    private void ToggleUI()
    {
        if (_hidden != null)
        {
            foreach (Canvas c in _hidden)
                if (c != null) c.enabled = true;

            _hidden = null;
            return;
        }

        System.Collections.Generic.List<Canvas> list = new System.Collections.Generic.List<Canvas>();

        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            list.Add(c);
        }

        _hidden = list.ToArray();
    }

    // Vẽ vòng tròn quỹ đạo trong Scene view để canh bán kính mà không cần bấm Play.
    private void OnDrawGizmosSelected()
    {
        Vector3 c = orbitTarget != null ? orbitTarget.position : Vector3.zero;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);

        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= 64; i++)
        {
            float a = i / 64f * Mathf.PI * 2f;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * orbitRadius, orbitHeight,
                                        Mathf.Sin(a) * orbitRadius);
            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }

        Gizmos.DrawLine(transform.position, c);
    }
}
