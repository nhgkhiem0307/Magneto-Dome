using UnityEngine;

/// <summary>
/// Rung camera. Gắn lên chính GameObject của Camera trong Player.prefab.
///
/// ĐÂY LÀ MONOBEHAVIOUR THUẦN, CỐ Ý KHÔNG NETWORKED.
/// Rung camera là thứ chỉ MÌNH BẠN nhìn thấy trên màn hình của mình — người chơi khác
/// không cần và không nên biết camera bạn đang rung. Đồng bộ nó qua mạng là lãng phí
/// băng thông cho một thứ không ảnh hưởng tới gameplay. Đúng theo nguyên tắc
/// "chỉ đồng bộ những gì BẮT BUỘC" đã dùng cho rào chắn Buy Phase và âm thanh.
///
/// Script này KHÔNG tự đụng vào transform của camera. Nó chỉ TÍNH RA độ lệch,
/// rồi FPSMovement.Render() lấy con số đó cộng vào góc nhìn.
///
/// Vì sao phải vòng vo vậy thay vì cho nó tự xoay camera: FPSMovement.Render() gán đè
/// localRotation của camera mỗi khung hình để áp góc ngẩng/cúi theo chuột. Nếu script này
/// cũng tự ý xoay camera thì hai bên sẽ tranh nhau, và ai chạy sau sẽ xoá công của người kia.
/// Ai thắng thì phụ thuộc thứ tự Unity gọi hàm — thứ rất khó đoán và hay đổi.
/// Gộp về một chỗ ghi duy nhất (Render) thì không bao giờ có chuyện đó.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Rung theo cú va đập (Dash, bắn vật...)")]
    [Tooltip("Góc lệch tối đa của camera khi rung mạnh nhất, theo độ. " +
             "X = ngẩng/cúi, Y = liếc trái/phải, Z = nghiêng đầu.")]
    public Vector3 maxShakeAngles = new Vector3(2.5f, 2.5f, 3.5f);

    [Tooltip("Camera bị xê dịch tối đa bao nhiêu mét khi rung mạnh nhất.")]
    public float maxShakeOffset = 0.06f;

    [Tooltip("Tốc độ dao động. Cao = rung nhanh và gắt, thấp = lắc lư chậm. " +
             "Khoảng 20-30 cho cảm giác 'va đập', dưới 10 sẽ thành say sóng.")]
    public float shakeFrequency = 25f;

    [Tooltip("Một cú rung đầy (trauma = 1) tắt hẳn sau bao nhiêu giây.")]
    public float shakeDuration = 0.5f;

    [Header("Nhấp nhô đầu khi đi bộ")]
    [Tooltip("Bỏ tick nếu thấy chóng mặt. Hoặc để nguyên và hạ hai ô biên độ bên dưới về 0.")]
    public bool enableHeadBob = true;

    [Tooltip("Số bước chân mỗi giây khi chạy hết tốc lực.")]
    public float bobFrequency = 8f;

    [Tooltip("Đầu nhún lên xuống bao nhiêu mét. 0.04-0.06 là vừa, trên 0.1 là quá đà.")]
    public float bobVerticalAmount = 0.045f;

    [Tooltip("Đầu lắc nghiêng trái phải bao nhiêu độ theo mỗi bước chân.")]
    public float bobRollAmount = 0.6f;

    [Tooltip("Thời gian nhấp nhô tăng/giảm dần khi bắt đầu chạy hoặc dừng lại. " +
             "Không có nó thì camera đứng khựng giữa chừng một bước, nhìn rất gợn.")]
    public float bobSmoothing = 8f;

    // --- KẾT QUẢ TÍNH RA, FPSMovement ĐỌC HAI Ô NÀY ---

    /// <summary>Độ lệch góc camera ở khung hình này, tính bằng độ.</summary>
    public Vector3 RotationOffset { get; private set; }

    /// <summary>Độ xê dịch vị trí camera ở khung hình này, tính bằng mét (toạ độ cục bộ).</summary>
    public Vector3 PositionOffset { get; private set; }

    // "Trauma" = mức chấn động hiện tại, chạy từ 0 (yên) tới 1 (rung tối đa).
    // Mỗi cú va đập cộng thêm vào đây, và nó tự trôi về 0 theo thời gian.
    //
    // Vì sao dùng một con số cộng dồn thay vì chạy riêng từng hiệu ứng rung:
    // dash rồi bắn vật ngay sau đó sẽ cộng lại thành một cú rung mạnh hơn — đúng như
    // cảm giác thật. Nếu chạy riêng thì hai hiệu ứng sẽ đè nhau, cú sau xoá cú trước.
    private float _trauma;

    // Mốc riêng để đọc Perlin noise. Mỗi trục một mốc khác nhau, nếu dùng chung
    // thì cả ba trục sẽ lệch giống hệt nhau và camera chỉ chạy theo đúng một đường chéo.
    private float _seedX, _seedY, _seedZ;

    // Pha của nhịp bước chân, cộng dồn theo thời gian. Phải cộng dồn chứ không được
    // tính thẳng từ Time.time, vì khi đứng lại rồi chạy tiếp, tính từ Time.time sẽ
    // nhảy vào giữa một bước chân bất kỳ và camera giật một cái.
    private float _bobPhase;

    // Tốc độ đi bộ đã được làm mượt, thang 0..1.
    private float _smoothedSpeed01;

    private void Awake()
    {
        // Random để hai người chơi cạnh nhau không rung y hệt nhau
        _seedX = Random.value * 100f;
        _seedY = Random.value * 100f;
        _seedZ = Random.value * 100f;
    }

    /// <summary>
    /// Thêm một cú chấn động. Gọi từ FPSMovement khi dash, bắn vật...
    /// </summary>
    /// <param name="amount">
    /// Độ mạnh, thang 0..1. 0.2 = rung nhẹ, 0.5 = rõ ràng, 1 = tối đa.
    /// Cộng dồn với chấn động đang có sẵn, nhưng tổng không bao giờ vượt quá 1.
    /// </param>
    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    /// <summary>
    /// Tính lại độ lệch cho khung hình này. FPSMovement.Render() gọi vào đây
    /// TRƯỚC khi nó áp góc nhìn lên camera.
    /// </summary>
    /// <param name="walkSpeed01">
    /// Đang đi nhanh cỡ nào, thang 0 (đứng yên) tới 1 (chạy hết tốc trên mặt đất).
    /// Đang bay trên không thì truyền 0 — không có mặt đất thì không có bước chân.
    /// </param>
    public void Tick(float walkSpeed01)
    {
        float dt = Time.deltaTime;

        Vector3 rot = Vector3.zero;
        Vector3 pos = Vector3.zero;

        // --- 1. RUNG THEO CHẤN ĐỘNG ---
        if (_trauma > 0f)
        {
            // BÌNH PHƯƠNG trauma trước khi dùng, không lấy thẳng.
            //
            // Lý do: mắt người cảm nhận độ rung không tuyến tính. Nếu lấy thẳng thì
            // đoạn cuối lúc trauma còn 0.2 vẫn thấy camera lắc lay rõ mồn một, cảm giác
            // như camera bị lỏng. Bình phương làm cái đuôi tắt nhanh hẳn (0.2 -> 0.04),
            // nên cú rung có điểm dừng dứt khoát, mà lúc mạnh nhất vẫn nguyên vẹn.
            float strength = _trauma * _trauma;

            float t = Time.time * shakeFrequency;

            // Dùng Perlin noise chứ KHÔNG dùng Random.Range.
            //
            // Random cho ra giá trị nhảy loạn không liên quan gì tới khung hình trước,
            // camera sẽ giật xành xạch như hỏng chứ không phải rung. Perlin cho ra một
            // đường cong liền mạch, hai khung hình liền nhau có giá trị gần nhau,
            // nên camera trôi mượt qua lại — đó mới là cảm giác chấn động thật.
            //
            // PerlinNoise trả về 0..1, phải đổi sang -1..1 mới lệch được cả hai phía.
            rot.x = maxShakeAngles.x * strength * (Mathf.PerlinNoise(_seedX, t) * 2f - 1f);
            rot.y = maxShakeAngles.y * strength * (Mathf.PerlinNoise(_seedY, t) * 2f - 1f);
            rot.z = maxShakeAngles.z * strength * (Mathf.PerlinNoise(_seedZ, t) * 2f - 1f);

            pos.x = maxShakeOffset * strength * (Mathf.PerlinNoise(_seedZ, t * 0.8f) * 2f - 1f);
            pos.y = maxShakeOffset * strength * (Mathf.PerlinNoise(_seedX, t * 0.8f) * 2f - 1f);

            // Trôi về 0. Chia cho shakeDuration để ô Inspector đọc thẳng ra giây:
            // đặt 0.5 thì một cú rung đầy tắt hẳn sau đúng 0.5 giây.
            if (shakeDuration > 0f)
            {
                _trauma = Mathf.Max(0f, _trauma - dt / shakeDuration);
            }
            else
            {
                _trauma = 0f;
            }
        }

        // --- 2. NHẤP NHÔ ĐẦU KHI ĐI BỘ ---
        if (enableHeadBob)
        {
            // Làm mượt tốc độ trước khi dùng, để lúc bắt đầu chạy và lúc dừng lại
            // nhịp nhấp nhô lớn dần / nhỏ dần chứ không bật tắt đột ngột.
            _smoothedSpeed01 = Mathf.Lerp(_smoothedSpeed01, Mathf.Clamp01(walkSpeed01), bobSmoothing * dt);

            if (_smoothedSpeed01 > 0.01f)
            {
                // Nhân tần số với tốc độ: đi chậm thì bước chân thưa, chạy nhanh thì bước dồn.
                _bobPhase += dt * bobFrequency * _smoothedSpeed01 * Mathf.PI * 2f;

                // Đầu nhún XUỐNG hai lần mỗi chu kỳ (mỗi chân một lần), nên trục dọc
                // chạy nhanh gấp đôi trục lắc ngang. Đó là lý do có số 2 ở đây
                // mà không có ở hai dòng dưới — bỏ đi sẽ thành đi cà nhắc một bên.
                pos.y += Mathf.Sin(_bobPhase * 2f) * bobVerticalAmount * _smoothedSpeed01;
                pos.x += Mathf.Cos(_bobPhase) * bobVerticalAmount * 0.5f * _smoothedSpeed01;
                rot.z += Mathf.Sin(_bobPhase) * bobRollAmount * _smoothedSpeed01;
            }
            else
            {
                // Đứng yên: kéo pha về mốc 0 để lần chạy tới bắt đầu từ tư thế đứng thẳng,
                // không phải từ giữa một bước chân dở dang.
                _bobPhase = Mathf.Lerp(_bobPhase, 0f, bobSmoothing * dt);
            }
        }

        RotationOffset = rot;
        PositionOffset = pos;
    }

    /// <summary>
    /// Tắt rung ngay lập tức. Dùng khi hồi sinh hoặc sang round mới, để camera
    /// không còn dư chấn động của round trước.
    /// </summary>
    public void ResetShake()
    {
        _trauma = 0f;
        _bobPhase = 0f;
        _smoothedSpeed01 = 0f;
        RotationOffset = Vector3.zero;
        PositionOffset = Vector3.zero;
    }
}
