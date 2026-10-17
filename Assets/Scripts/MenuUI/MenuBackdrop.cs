using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Camera bay vòng chậm quanh hòn đảo làm nền cho màn hình chính (16/10).
///
/// Hòn đảo trong MenuScene là BẢN SAO CHỈ CÓ HÌNH của đảo trong TestScene — đã bỏ hết
/// NetworkObject, Rigidbody, va chạm và script gameplay. Nó chỉ để nhìn: menu có Fusion Runner
/// chạy (phòng chờ), nên mọi object mạng thừa trong scene này đều có thể bị Fusion đăng ký nhầm.
///
/// Kèm hai thứ cho ánh sáng (16/10):
///  - MẶT TRỜI XOAY THEO CAMERA: mặt trời đứng yên thì khi camera bay ra sau đảo sẽ nhìn thấy mặt
///    ngược sáng, tối om. Cho nắng luôn chiếu chéo từ sau vai camera → đảo sáng rõ ở mọi góc.
///  - CHỈNH MÀU TÔNG XANH chỉ cho menu: tạo một Volume hậu kỳ lúc chạy (không cần file profile),
///    nằm trong MenuScene nên sang TestScene là tự mất — không đụng gì tới màu trong trận.
///
/// Đặt trên Main Camera của MenuScene. Không cần kéo thả gì thêm.
/// </summary>
public class MenuBackdrop : MonoBehaviour
{
    [Header("Bay vòng")]
    [Tooltip("Tâm hòn đảo — camera nhìn vào điểm này và bay vòng quanh nó.")]
    public Vector3 center = new Vector3(40f, 8f, 40f);

    [Tooltip("Bán kính vòng bay (mét).")]
    public float radius = 88f;

    [Tooltip("Độ cao camera so với tâm (mét).")]
    public float height = 30f;

    [Tooltip("Tốc độ bay vòng (độ mỗi giây). Chậm thôi — đây là nền, không phải cảnh hành động.")]
    public float degreesPerSecond = 2.5f;

    [Tooltip("Lệch điểm nhìn sang trái (mét) để hòn đảo nằm lệch phải màn hình, chừa chỗ cho menu bên trái.")]
    public float lookOffsetLeft = 22f;

    [Tooltip("Góc bắt đầu (độ).")]
    public float startAngle = 210f;

    [Header("Mặt trời theo camera")]
    [Tooltip("Bật: hướng nắng xoay theo camera để đảo không bao giờ bị ngược sáng.")]
    public bool sunFollowsCamera = true;

    [Tooltip("Nắng lệch bao nhiêu độ so với hướng camera nhìn. Âm = chiếu từ phía sau-bên trái camera.")]
    public float sunYawOffset = -40f;

    [Tooltip("Độ cao mặt trời (độ). Thấp quá thì bóng dài, cao quá thì mặt đảo bị phẳng.")]
    public float sunPitch = 40f;

    [Tooltip("Màu nắng — trắng hơi ngả xanh cho hợp tông game.")]
    public Color sunColor = new Color(0.93f, 0.97f, 1f);

    public float sunIntensity = 1.15f;

    [Header("Chỉnh màu tông xanh (chỉ menu)")]
    [Tooltip("Âm = lạnh hơn, ngả xanh. 0 = giữ nguyên.")]
    [Range(-60f, 30f)] public float temperature = -18f;

    [Range(-50f, 50f)] public float saturation = 10f;

    [Tooltip("Phủ một chút màu lên toàn cảnh. Trắng = không phủ.")]
    public Color colorFilter = new Color(0.93f, 0.98f, 1f);

    [Range(-1f, 1f)] public float postExposure = 0.08f;

    private float _angle;
    private Light _sun;
    private Volume _volume;
    private VolumeProfile _profile;

    private void OnEnable()
    {
        _angle = startAngle;
        SetupLook();
        Apply();
    }

    private void OnDestroy()
    {
        if (_volume != null) Destroy(_volume.gameObject);
        if (_profile != null) Destroy(_profile);
    }

    private void Update()
    {
        // unscaledDeltaTime: menu không bao giờ bị dừng thời gian, nhưng cứ an toàn.
        _angle += degreesPerSecond * Time.unscaledDeltaTime;
        Apply();
    }

    /// <summary>Đặt camera ở góc chỉ định — dùng chung cho chạy thật và chụp ảnh xem trước.</summary>
    public void SetAngle(float degrees)
    {
        _angle = degrees;
        Apply();
    }

    /// <summary>
    /// Màu nắng + Volume chỉnh màu. Gọi được nhiều lần (chụp ảnh xem trước trong Editor cũng gọi).
    /// </summary>
    public void SetupLook()
    {
        _sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
        if (_sun != null)
        {
            _sun.color = sunColor;
            _sun.intensity = sunIntensity;
        }

        if (_volume == null)
        {
            GameObject go = new GameObject("MenuColorGrade (runtime)");
            go.hideFlags = HideFlags.DontSave;   // không bao giờ bị lưu vào scene
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 10f;              // đè lên profile mặc định của dự án

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.hideFlags = HideFlags.DontSave;
            _volume.sharedProfile = _profile;
        }

        WhiteBalance wb = _profile.TryGet(out WhiteBalance w) ? w : _profile.Add<WhiteBalance>(true);
        wb.temperature.Override(temperature);

        ColorAdjustments ca = _profile.TryGet(out ColorAdjustments c) ? c : _profile.Add<ColorAdjustments>(true);
        ca.saturation.Override(saturation);
        ca.colorFilter.Override(colorFilter);
        ca.postExposure.Override(postExposure);
    }

    private void Apply()
    {
        float rad = _angle * Mathf.Deg2Rad;
        Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, height, Mathf.Sin(rad) * radius);
        transform.position = pos;

        // Nhìn vào tâm, rồi dời điểm nhìn sang TRÁI của camera — đảo sẽ trôi sang phải màn hình.
        Vector3 toCenter = center - pos;
        Vector3 right = Vector3.Cross(Vector3.up, toCenter).normalized;
        transform.rotation = Quaternion.LookRotation(center - right * lookOffsetLeft - pos, Vector3.up);

        // Nắng đi CÙNG chiều camera nhìn (lệch một góc) = chiếu vào mặt đảo đang hướng về camera.
        if (sunFollowsCamera && _sun != null)
        {
            float camYaw = Quaternion.LookRotation(toCenter).eulerAngles.y;
            _sun.transform.rotation = Quaternion.Euler(sunPitch, camYaw + sunYawOffset, 0f);
        }
    }
}
