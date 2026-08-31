using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
///
/// ⚠️ NGOẠI LỆ CỦA QUY TẮC TRÊN: script này CÓ tự ghi vào hai thứ — góc nhìn (fieldOfView)
/// của Camera và độ mờ hậu kỳ. Được phép, vì hai thứ đó KHÔNG PHẢI transform nên không ai
/// tranh chấp: trong toàn bộ project không có script nào khác đụng tới chúng.
/// Chỉ transform mới phải đi vòng qua Render().
///
/// BA HỆ THỐNG RIÊNG BIỆT, cố ý KHÔNG gộp làm một:
///
///   1. Trauma      — rung nền do va đập (Dash). Perlin tần số cao, đuôi tắt nhanh.
///   2. Kick        — cú GIẬT khi mình ĐÁNH RA (bắn vật, đấm). Lệch tức thì rồi lò xo
///                    kéo về. Sắc, gọn, dứt khoát.
///   3. Disorient   — VÁNG ĐẦU khi mình ĂN ĐÒN. Lắc chậm, nghiêng đầu, FOV phập phồng,
///                    kèm mờ màn hình. Đuôi dài, lảo đảo.
///
/// Vì sao phải tách 2 và 3 thay vì chỉ chỉnh to nhỏ một hệ thống: chúng NGƯỢC NHAU về
/// bản chất. Đánh ra cần cảm giác "chắc tay" = tần số cao + tắt nhanh. Ăn đòn cần cảm giác
/// "mất kiểm soát" = tần số thấp + tắt chậm. Cùng một bộ tham số không thể ra cả hai:
/// vặn to trauma lên thì đánh ra thành lảo đảo, mà ăn đòn thì vẫn chỉ là rung mạnh hơn.
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

    // ==================== 2. CÚ GIẬT KHI ĐÁNH RA ====================

    [Header("Cú giật khi ĐÁNH RA (bắn vật, đấm)")]
    [Tooltip("Camera lệch NGAY LẬP TỨC bấy nhiêu độ ở cường độ 1, rồi lò xo kéo về.\n\n" +
             "X ÂM = hất camera NGẨNG LÊN (giống độ nảy của súng). Y = liếc ngang, " +
             "Z = nghiêng đầu — hai trục này được đảo dấu ngẫu nhiên mỗi cú nên bắn liên " +
             "tiếp không bao giờ giật y hệt nhau.")]
    public Vector3 kickAngles = new Vector3(-4f, 1.2f, 1.8f);

    [Tooltip("Camera thụt về sau bao nhiêu mét ở cường độ 1. Chính cái thụt này tạo cảm " +
             "giác 'đằng sau cú đánh có khối lượng', chứ không chỉ là xoay góc.")]
    public float kickBackDistance = 0.09f;

    [Tooltip("Độ cứng lò xo kéo camera về chỗ cũ. CAO = bật về nhanh và dứt khoát.\n\n" +
             "Đây là ô quyết định chữ 'dứt khoát'. Dưới 100 sẽ thành đung đưa nhũn nhặn.")]
    public float kickStiffness = 280f;

    [Tooltip("Ma sát của lò xo. Thấp = nảy qua nảy lại vài nhịp, cao = về thẳng không nảy.\n\n" +
             "Khoảng 22-30 cho ra đúng một nhịp nảy ngược nhẹ — chính nhịp đó làm cú đánh " +
             "có 'điểm kết', thay vì trôi tuột về vị trí cũ không ai nhận ra.")]
    public float kickDamping = 26f;

    // ==================== 3. VÁNG ĐẦU KHI ĂN ĐÒN ====================

    [Header("Váng đầu khi ĂN ĐÒN")]
    [Tooltip("Biên độ lảo đảo tối đa, theo độ. To hơn hẳn rung thường vì đây là lúc " +
             "người chơi ĐANG MẤT KIỂM SOÁT, không phải chỉ bị xóc.")]
    public float disorientMaxAngle = 7f;

    [Tooltip("Trục nghiêng đầu (Z) được nhân thêm bấy nhiêu lần.\n\n" +
             "ĐÂY MỚI LÀ THỨ GÂY BUỒN NÔN. Não người quen với việc đường chân trời luôn " +
             "nằm ngang; nghiêng nó đi là phá vỡ tiền đình. Lắc trái phải (Y) chỉ gây khó " +
             "ngắm chứ không gây choáng. Muốn 'buồn nôn' hơn thì tăng ô này, đừng tăng ô trên.")]
    public float disorientRollBias = 2.2f;

    [Tooltip("Tốc độ lảo đảo. CỐ Ý ĐỂ THẤP hơn nhiều so với Shake Frequency (25).\n\n" +
             "Rung nhanh = 'bị va đập'. Lắc chậm = 'say sóng'. Đẩy ô này lên 15-20 thì " +
             "hiệu ứng váng đầu biến mất, chỉ còn lại rung mạnh.")]
    public float disorientFrequency = 2.6f;

    [Tooltip("Một cú váng đầu đầy tan hết sau bao nhiêu giây. Dài hơn Shake Duration " +
             "nhiều lần — dư âm kéo dài chính là phần 'váng'.")]
    public float disorientDuration = 1.5f;

    [Tooltip("Góc nhìn (FOV) phập phồng thêm/bớt bao nhiêu độ lúc choáng nhất. " +
             "Đặt 0 để tắt.\n\n" +
             "Cảnh vật lúc phình ra lúc co lại là dấu hiệu say sóng kinh điển. Đừng để quá " +
             "6 - trên mức đó người chơi thấy khó chịu thật chứ không còn là hiệu ứng nữa.")]
    public float disorientFovPunch = 4f;

    [Header("Mờ màn hình khi ăn đòn")]
    [Tooltip("Bỏ tick nếu máy yếu hoặc thấy chóng mặt quá. Tắt thì phần lảo đảo vẫn chạy.")]
    public bool enableHitBlur = true;

    [Tooltip("Độ mờ tối đa. 1-1.5 là mờ nhoè rõ nhưng vẫn nhìn được đường; " +
             "trên 2 là gần như mù hẳn.")]
    public float blurMaxRadius = 1.3f;

    [Tooltip("Màn hình mờ trong bao nhiêu giây. CỐ Ý NGẮN hơn Disorient Duration — " +
             "mờ mắt phải tan trước, để người chơi kịp nhìn đường chạy trong khi " +
             "camera vẫn còn lảo đảo. Mờ lâu bằng váng đầu là ức chế, không phải đã mắt.")]
    public float blurDuration = 0.45f;

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

    // --- HỆ THỐNG 2: CÚ GIẬT ---
    // Độ lệch hiện tại và vận tốc của nó. Đây là một lò xo thật sự (khối lượng - lò xo -
    // giảm chấn), không phải Lerp về 0. Lò xo cho phép VỌT QUÁ đích rồi quay lại,
    // còn Lerp thì chỉ trườn về một chiều và không bao giờ có nhịp nảy ngược.
    private Vector3 _kickRot;
    private Vector3 _kickRotVel;
    private float _kickPos;      // âm = camera thụt về sau
    private float _kickPosVel;

    // --- HỆ THỐNG 3: VÁNG ĐẦU ---
    private float _disorient;    // 0..1, tự trôi về 0
    private float _blur;         // 0..1, trôi về 0 nhanh hơn _disorient
    private float _seedD1, _seedD2, _seedD3;

    // Camera nằm ngay trên GameObject này, lấy để phập phồng FOV.
    private Camera _cam;
    private float _baseFov;

    // Bộ mờ hậu kỳ. Tạo BẰNG CODE lúc cần chứ không phải kéo thả trong Editor —
    // xem giải thích ở EnsureBlurSetup().
    private Volume _blurVolume;
    private VolumeProfile _blurProfile;
    private DepthOfField _dof;

    private void Awake()
    {
        // Random để hai người chơi cạnh nhau không rung y hệt nhau
        _seedX = Random.value * 100f;
        _seedY = Random.value * 100f;
        _seedZ = Random.value * 100f;

        // Mốc riêng cho phần váng đầu. Nếu xài chung _seedX/Y/Z với phần rung thường
        // thì hai hiệu ứng sẽ lệch theo cùng một đường cong, chồng lên nhau thành
        // một cú lắc to đúng một hướng thay vì hai chuyển động độc lập.
        _seedD1 = Random.value * 100f;
        _seedD2 = Random.value * 100f;
        _seedD3 = Random.value * 100f;

        _cam = GetComponent<Camera>();
        if (_cam != null) _baseFov = _cam.fieldOfView;
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
    /// Cú GIẬT khi mình đánh ra: bắn vật đang cầm, đấm cận chiến.
    ///
    /// Khác AddTrauma ở chỗ nó có HƯỚNG rõ ràng (hất camera ngẩng lên và thụt về sau)
    /// thay vì rung loạn quanh chỗ cũ. Có hướng thì não đọc ra được "vừa có một lực
    /// đi ra khỏi tay mình"; rung loạn thì chỉ đọc ra "vừa có gì đó va vào".
    /// </summary>
    /// <param name="strength">
    /// Cường độ. 1 = đúng bằng con số đặt ở Kick Angles. Cho phép vượt 1 (đấm mạnh hơn bắn),
    /// nhưng chặn ở 3 để một chuỗi đòn dồn dập không hất camera lên trời.
    /// </param>
    public void AddKick(float strength)
    {
        if (strength <= 0f) return;
        strength = Mathf.Min(strength, 3f);

        // Đảo dấu ngẫu nhiên trục ngang: cú này giật sang trái thì cú sau có thể giật
        // sang phải. Không có dòng này thì bắn 5 phát liền là 5 lần giật giống hệt nhau,
        // mắt nhận ra ngay tính lặp lại và hiệu ứng mất hết sức nặng.
        float side = Random.value < 0.5f ? -1f : 1f;
        float jitter = Random.Range(0.6f, 1f);

        // CỘNG THẲNG vào độ lệch, không cộng vào vận tốc.
        //
        // Cộng vào vận tốc thì camera phải mất vài khung hình mới lệch tới đỉnh -> cú đánh
        // có cảm giác "nhão", đúng thứ cần tránh. Đặt lệch ngay lập tức là cách duy nhất
        // để khung hình đầu tiên sau cú đánh đã thấy khác hẳn khung hình trước đó.
        // Ô Inspector nhờ vậy cũng đọc thẳng ra kết quả: điền 4 độ là lệch đúng 4 độ.
        _kickRot += new Vector3(
            kickAngles.x * strength,
            kickAngles.y * strength * side * jitter,
            kickAngles.z * strength * -side * jitter);

        _kickPos -= kickBackDistance * strength;
    }

    /// <summary>
    /// VÁNG ĐẦU khi mình ăn đòn: trúng đạn, bị đấm, dính nổ.
    ///
    /// Kích hoạt cùng lúc ba thứ: lảo đảo chậm, FOV phập phồng, và mờ màn hình.
    /// Gọi kèm AddTrauma để có thêm cú xóc sắc nét ở khoảnh khắc đầu — váng đầu một mình
    /// thì khởi đầu quá êm, không ra được cảm giác "vừa bị nện".
    /// </summary>
    /// <param name="strength">Độ nặng của đòn, thang 0..1. Cộng dồn, chặn trần ở 1.</param>
    public void AddDisorient(float strength)
    {
        if (strength <= 0f) return;

        _disorient = Mathf.Clamp01(_disorient + strength);
        _blur = Mathf.Clamp01(_blur + strength);

        if (enableHitBlur) EnsureBlurSetup();
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

        // --- 2. CÚ GIẬT KHI ĐÁNH RA (lò xo kéo về) ---
        if (_kickRot.sqrMagnitude > 0.0000001f || _kickRotVel.sqrMagnitude > 0.0000001f ||
            Mathf.Abs(_kickPos) > 0.00001f || Mathf.Abs(_kickPosVel) > 0.00001f)
        {
            // Chia nhỏ bước thời gian trước khi mô phỏng lò xo.
            //
            // BẮT BUỘC PHẢI CÓ. Lò xo cứng (kickStiffness 280) tích phân bằng phương pháp
            // Euler sẽ NỔ TUNG nếu dt quá lớn: gia tốc tính ra vọt quá đích, khung sau
            // vọt ngược lại còn xa hơn, vài vòng là camera văng ra vô cực. Máy đang chạy
            // 60fps mà khựng một cái (nạp texture, Fusion tua lại) là đủ để dt lên 0.1s.
            // Cắt thành nhiều bước <= 1/120 giây thì cứng cỡ nào cũng ổn định.
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / (1f / 120f)), 1, 8);
            float sdt = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                // Định luật lò xo có giảm chấn: gia tốc = -độ_cứng * độ_lệch - ma_sát * vận_tốc.
                // Số hạng đầu kéo về 0, số hạng sau hãm lại để không dao động mãi.
                _kickRotVel += (-_kickRot * kickStiffness - _kickRotVel * kickDamping) * sdt;
                _kickRot += _kickRotVel * sdt;

                _kickPosVel += (-_kickPos * kickStiffness - _kickPosVel * kickDamping) * sdt;
                _kickPos += _kickPosVel * sdt;
            }

            rot += _kickRot;

            // Thụt về sau. Lưu ý localPosition được tính trong hệ toạ độ của THÂN nhân vật
            // (chỉ có góc xoay ngang), không phải hệ của camera — nên trục Z ở đây là
            // hướng trước/sau của thân người, KHÔNG bị ảnh hưởng bởi góc ngẩng/cúi.
            // Đó là điều mong muốn: đang ngước nhìn trời mà bắn thì camera vẫn thụt ra sau
            // chứ không thụt xuống đất.
            pos.z += _kickPos;
        }

        // --- 3. VÁNG ĐẦU KHI ĂN ĐÒN ---
        if (_disorient > 0f)
        {
            // KHÔNG bình phương như phần trauma ở trên, mà lấy CĂN BẬC HAI — ngược hẳn lại.
            //
            // Trauma bình phương để cái đuôi tắt nhanh, cú va đập có điểm dừng dứt khoát.
            // Váng đầu cần đúng điều ngược lại: dư âm phải dai dẳng thì mới ra cảm giác
            // "chưa hoàn hồn". Căn bậc hai kéo giá trị lên (0.25 -> 0.5), nên nửa sau của
            // hiệu ứng vẫn còn lảo đảo thấy rõ thay vì tắt ngóm.
            float d = Mathf.Sqrt(_disorient);

            float t = Time.time * disorientFrequency;

            rot.x += disorientMaxAngle * d * (Mathf.PerlinNoise(_seedD1, t) * 2f - 1f);
            rot.y += disorientMaxAngle * d * (Mathf.PerlinNoise(_seedD2, t) * 2f - 1f);

            // Trục nghiêng đầu chạy CHẬM HƠN nữa (t * 0.6) và biên độ lớn hơn.
            // Nghiêng nhanh chỉ là rung; nghiêng chậm mới làm đường chân trời đảo qua đảo lại
            // đủ lâu để mắt kịp nhận ra là nó đang nghiêng — đó mới là cái gây choáng.
            rot.z += disorientMaxAngle * disorientRollBias * d
                     * (Mathf.PerlinNoise(_seedD3, t * 0.6f) * 2f - 1f);

            // FOV phập phồng: cảnh vật lúc phình ra lúc co lại.
            // Dùng Sin chứ không dùng Perlin vì ở đây cần một nhịp ĐỀU ĐẶN như hơi thở;
            // Perlin cho nhịp bất quy tắc, trông giống lỗi hiển thị hơn là giống choáng.
            if (_cam != null && disorientFovPunch > 0f)
            {
                _cam.fieldOfView = _baseFov + disorientFovPunch * d
                                   * Mathf.Sin(Time.time * disorientFrequency * 1.7f);
            }

            if (disorientDuration > 0f)
            {
                _disorient = Mathf.Max(0f, _disorient - dt / disorientDuration);
            }
            else
            {
                _disorient = 0f;
            }

            // Trả FOV về đúng gốc ở khung hình cuối cùng.
            // Không có dòng này thì FOV dừng lại ở đâu đó lệch vài phần trăm độ và
            // ở lại đó vĩnh viễn — mỗi lần ăn đòn lại lệch thêm một ít, sau vài round
            // là góc nhìn sai hẳn mà không ai biết tại sao.
            if (_disorient <= 0f && _cam != null) _cam.fieldOfView = _baseFov;
        }

        // --- 3b. MỜ MÀN HÌNH ---
        // Tách khỏi _disorient vì cố ý tan nhanh hơn: mắt sáng lại trước, camera còn lảo đảo.
        if (_blur > 0f)
        {
            _blur = blurDuration > 0f ? Mathf.Max(0f, _blur - dt / blurDuration) : 0f;
            ApplyBlur(_blur);
        }

        // --- 4. NHẤP NHÔ ĐẦU KHI ĐI BỘ ---
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

        _kickRot = Vector3.zero;
        _kickRotVel = Vector3.zero;
        _kickPos = 0f;
        _kickPosVel = 0f;

        _disorient = 0f;
        _blur = 0f;
        ApplyBlur(0f);

        if (_cam != null) _cam.fieldOfView = _baseFov;

        RotationOffset = Vector3.zero;
        PositionOffset = Vector3.zero;
    }

    // ==================== MỜ MÀN HÌNH ====================

    /// <summary>
    /// Dựng bộ mờ hậu kỳ BẰNG CODE, tạo một lần duy nhất ở cú ăn đòn đầu tiên.
    ///
    /// Vì sao tạo bằng code thay vì kéo thả trong Editor: cách thông thường đòi hỏi tạo
    /// một file VolumeProfile mới trong Project, thêm override Depth Of Field vào đó, tạo
    /// GameObject Volume trong Player.prefab rồi gán profile — bốn bước thủ công, quên một
    /// bước là hiệu ứng im lặng không chạy mà Console không báo gì. Dựng bằng code thì
    /// gắn script vào là xong, không có gì để quên.
    ///
    /// Vì sao tạo MUỘN chứ không tạo trong Awake: Awake chạy trên MỌI nhân vật, kể cả
    /// nhân vật của người chơi khác trên máy này. Hàm này chỉ được gọi từ AddDisorient,
    /// mà AddDisorient chỉ tới được nhân vật của chính mình (FPSMovement lọc bằng
    /// HasInputAuthority). Nên trong phòng 4 người, máy này chỉ tạo đúng 1 bộ mờ.
    /// </summary>
    private void EnsureBlurSetup()
    {
        if (_blurVolume != null) return;

        _blurProfile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Tham số true = bật sẵn overrideState cho mọi ô. Thiếu nó thì mọi giá trị đặt
        // bên dưới đều bị URP bỏ qua, vì Volume mặc định coi ô chưa tick là "không can thiệp".
        _dof = _blurProfile.Add<DepthOfField>(true);

        // Gaussian chứ không phải Bokeh. Bokeh mô phỏng ống kính thật nên đắt và cho ra
        // các vòng sáng lung linh - đẹp cho ảnh tĩnh, sai hoàn toàn cho cảm giác choáng.
        // Gaussian chỉ là nhoè đều, rẻ hơn nhiều và đúng thứ cần.
        _dof.mode.value = DepthOfFieldMode.Gaussian;

        // Khoảng rõ nét bắt đầu ngay trước mũi và kết thúc rất gần, nghĩa là TOÀN BỘ
        // cảnh vật đều nằm ngoài vùng nét -> mờ cả màn hình chứ không phải chỉ mờ hậu cảnh.
        _dof.gaussianStart.value = 0.1f;
        _dof.gaussianEnd.value = 1.5f;
        _dof.gaussianMaxRadius.value = 0f;

        _blurVolume = gameObject.AddComponent<Volume>();
        _blurVolume.isGlobal = true;

        // Ưu tiên cao để đè lên DefaultVolumeProfile toàn cục (tonemapping, bloom...).
        // Đè ở đây chỉ có nghĩa là "ô Depth Of Field nghe theo tôi", các ô khác của
        // profile toàn cục vẫn giữ nguyên vì profile này không đụng tới chúng.
        _blurVolume.priority = 100f;
        _blurVolume.profile = _blurProfile;
        _blurVolume.weight = 0f;
    }

    private void ApplyBlur(float amount01)
    {
        if (_blurVolume == null || _dof == null) return;

        // Điều khiển bằng weight của Volume chứ không tắt/bật component.
        // Weight 0 thì URP bỏ qua hoàn toàn, không tốn một mili-giây nào - nên không cần
        // và không nên tắt component, việc tắt/bật liên tục mới là thứ gây khựng hình.
        _blurVolume.weight = amount01;
        _dof.gaussianMaxRadius.value = Mathf.Max(0.01f, blurMaxRadius * amount01);
    }

    private void OnDestroy()
    {
        // VolumeProfile tạo bằng CreateInstance là một ScriptableObject sống ngoài scene,
        // Unity KHÔNG tự dọn nó khi nhân vật bị huỷ. Không xoá tay thì mỗi lần chết đi
        // sống lại / vào ra trận lại bỏ lại một profile rác trong bộ nhớ.
        if (_blurProfile != null) Destroy(_blurProfile);
    }
}
