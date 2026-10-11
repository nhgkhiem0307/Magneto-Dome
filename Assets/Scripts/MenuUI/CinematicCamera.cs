using UnityEngine;

/// <summary>
/// Camera quay phim: bay tự do hoặc xoay quanh một điểm, để quay trailer và chụp poster.
///
/// ⚠️ CHỈ CÓ TRONG EDITOR VÀ BẢN BUILD CÓ TICK "Development Build".
/// Bản nộp không hề chứa đoạn code này, người chấm không thể bấm trúng.
///
/// ⚠️ VÌ SAO CẦN: game không có camera bay tự do, nên cảnh toàn đảo chỉ còn cách quay
/// cửa sổ Scene view - mà rê camera bằng tay thì LUÔN GIẬT. Cảnh mở màn trailer là cảnh
/// đáng giá nhất, giật một nhịp là hỏng cả ấn tượng đầu tiên.
///
/// Chế độ xoay quanh tâm cho ra chuyển động đều tuyệt đối, thứ mà tay người không làm
/// được: bán kính và tốc độ là con số, không phải cử động cổ tay.
///
/// PHÍM:
///   F8          bật/tắt chế độ quay phim
///   WASD        bay ngang      ·  E / Q   lên / xuống
///   Chuột       xoay hướng nhìn
///   Shift/Ctrl  bay nhanh / chậm
///   R           bật/tắt xoay quanh tâm đảo
///   [ và ]      xoay chậm hơn / nhanh hơn
///   Space       ẩn/hiện toàn bộ giao diện (để quay cảnh sạch)
/// </summary>
public class CinematicCamera : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    /// <summary>Đang ở chế độ quay phim hay không. NetworkRunnerHandler đọc để ngắt điều khiển nhân vật.</summary>
    public static bool Active { get; private set; }

    private const KeyCode ToggleKey = KeyCode.F8;

    private Camera _cam;
    private Camera _playerCam;
    private AudioListener _ownListener;

    private float _yaw, _pitch;
    private float _speed = 12f;

    private bool _orbit;
    private float _orbitSpeed = 6f;      // độ mỗi giây
    private Vector3 _orbitCenter;
    private float _orbitRadius, _orbitAngle, _orbitHeight;

    private Canvas[] _hiddenCanvases;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        GameObject go = new GameObject("CinematicCamera");
        go.AddComponent<CinematicCamera>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) Toggle();
        if (!Active) return;

        if (Input.GetKeyDown(KeyCode.Space)) ToggleUI();
        if (Input.GetKeyDown(KeyCode.R)) StartOrbit(!_orbit);

        if (Input.GetKeyDown(KeyCode.LeftBracket)) _orbitSpeed = Mathf.Max(0.5f, _orbitSpeed - 2f);
        if (Input.GetKeyDown(KeyCode.RightBracket)) _orbitSpeed += 2f;

        if (_orbit) UpdateOrbit();
        else UpdateFreeFly();
    }

    // ==================== BẬT / TẮT ====================

    private void Toggle()
    {
        if (Active) Stop();
        else StartCine();
    }

    private void StartCine()
    {
        _playerCam = Camera.main;
        if (_playerCam == null)
        {
            Debug.LogWarning("[QUAY PHIM] Chưa vào trận nên không có camera để mượn thông số.");
            return;
        }

        GameObject go = new GameObject("CineCam");
        _cam = go.AddComponent<Camera>();

        // Chép thông số từ camera người chơi để hình ảnh giống hệt lúc chơi: cùng góc
        // mở, cùng bầu trời, cùng khoảng cắt xa.
        _cam.CopyFrom(_playerCam);

        // BỎ LỚP VIEWMODEL. Hai bàn tay được vẽ bằng một camera phủ riêng, đặt sát mặt -
        // camera quay phim mà thấy chúng thì sẽ có hai bàn tay khổng lồ lơ lửng giữa trời.
        int vm = LayerMask.NameToLayer("Viewmodel");
        if (vm >= 0) _cam.cullingMask &= ~(1 << vm);

        _cam.depth = _playerCam.depth + 10f;   // vẽ đè lên camera người chơi

        go.transform.SetPositionAndRotation(_playerCam.transform.position,
                                            _playerCam.transform.rotation);
        _yaw = go.transform.eulerAngles.y;
        _pitch = go.transform.eulerAngles.x;
        if (_pitch > 180f) _pitch -= 360f;

        _playerCam.enabled = false;
        Active = true;

        // Thả chuột ra qua CursorLock để không giành quyền với Settings hay Shop.
        CursorLock.Request(this);
        Cursor.lockState = CursorLockMode.Locked;   // vẫn khoá để xoay camera mượt
        Cursor.visible = false;

        Debug.Log("[QUAY PHIM] BẬT - WASD bay, E/Q lên xuống, R xoay quanh tâm, " +
                  "Space ẩn giao diện, F8 thoát.");
    }

    private void Stop()
    {
        Active = false;
        _orbit = false;

        if (_hiddenCanvases != null) ToggleUI();     // trả giao diện về trước khi thoát
        if (_cam != null) Destroy(_cam.gameObject);
        if (_playerCam != null) _playerCam.enabled = true;

        CursorLock.Release(this);
        Debug.Log("[QUAY PHIM] TẮT - trả điều khiển về cho nhân vật.");
    }

    // ==================== BAY TỰ DO ====================

    private void UpdateFreeFly()
    {
        Transform t = _cam.transform;

        _yaw += Input.GetAxisRaw("Mouse X") * 2.2f;
        _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * 2.2f, -89f, 89f);
        t.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        float mul = Input.GetKey(KeyCode.LeftShift) ? 4f
                  : Input.GetKey(KeyCode.LeftControl) ? 0.25f : 1f;

        Vector3 move = t.forward * Input.GetAxisRaw("Vertical")
                     + t.right * Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        t.position += move.normalized * (_speed * mul * Time.unscaledDeltaTime);
    }

    // ==================== XOAY QUANH TÂM ====================

    /// <summary>
    /// Lấy chỗ đang đứng làm bán kính, tâm là giữa đảo, rồi xoay đều quanh nó.
    ///
    /// Đây là thứ không rê tay được: tốc độ là một con số nên chuyển động đều tuyệt đối
    /// từ khung hình đầu tới khung hình cuối. Cảnh mở màn trailer cần đúng loại này.
    /// </summary>
    private void StartOrbit(bool on)
    {
        _orbit = on;
        if (!on) return;

        // Tâm đảo lấy theo khu chiếm đóng nếu có - nó nằm giữa bản đồ. Không có thì lấy
        // gốc toạ độ, vì bản đồ được dựng quanh đó.
        _orbitCenter = ControlZone.Instance != null
            ? ControlZone.Instance.transform.position
            : Vector3.zero;

        Vector3 p = _cam.transform.position;
        Vector3 flat = new Vector3(p.x - _orbitCenter.x, 0f, p.z - _orbitCenter.z);

        _orbitRadius = Mathf.Max(5f, flat.magnitude);
        _orbitHeight = p.y - _orbitCenter.y;
        _orbitAngle = Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg;

        Debug.Log($"[QUAY PHIM] Xoay quanh tâm: bán kính {_orbitRadius:F0}m, " +
                  $"cao {_orbitHeight:F0}m, {_orbitSpeed:F0} độ/giây. " +
                  "Dùng [ và ] để đổi tốc độ.");
    }

    private void UpdateOrbit()
    {
        _orbitAngle += _orbitSpeed * Time.unscaledDeltaTime;

        // Vẫn cho chỉnh bán kính và độ cao trong lúc xoay, để vừa xoay vừa tiến vào -
        // chuyển động kép này làm cảnh có chiều sâu hơn hẳn xoay đơn thuần.
        if (Input.GetKey(KeyCode.W)) _orbitRadius -= 8f * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.S)) _orbitRadius += 8f * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.E)) _orbitHeight += 6f * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.Q)) _orbitHeight -= 6f * Time.unscaledDeltaTime;
        _orbitRadius = Mathf.Max(3f, _orbitRadius);

        float rad = _orbitAngle * Mathf.Deg2Rad;
        Vector3 pos = _orbitCenter + new Vector3(Mathf.Cos(rad) * _orbitRadius,
                                                 _orbitHeight,
                                                 Mathf.Sin(rad) * _orbitRadius);

        _cam.transform.position = pos;
        _cam.transform.LookAt(_orbitCenter);
    }

    // ==================== ẨN GIAO DIỆN ====================

    /// <summary>
    /// Ẩn mọi Canvas để quay cảnh sạch, bấm lần nữa thì hiện lại.
    ///
    /// Tự bật lại khi thoát chế độ quay phim - đây là chỗ đã sinh lỗi thật: tắt Canvas
    /// bằng tay để chụp ảnh rồi quên bật, và cả HUD biến mất mà game vẫn chạy bình thường
    /// nên rất khó phát hiện.
    /// </summary>
    private void ToggleUI()
    {
        if (_hiddenCanvases != null)
        {
            foreach (Canvas c in _hiddenCanvases)
                if (c != null) c.enabled = true;

            _hiddenCanvases = null;
            return;
        }

        System.Collections.Generic.List<Canvas> list = new System.Collections.Generic.List<Canvas>();

        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            list.Add(c);
        }

        _hiddenCanvases = list.ToArray();
    }

#endif
}
