using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Nối chuyển động của nhân vật vào Animator, và giấu thân mình đi ở góc nhìn thứ nhất.
///
/// MonoBehaviour thuần. Nó KHÔNG tự đồng bộ gì cả — chỉ đọc ba giá trị [Networked]
/// mà FPSMovement đã tính sẵn: WalkSpeed01, IsGrounded, MovingBackward.
///
/// ⚠️ LỊCH SỬ - ĐỪNG QUAY LẠI CÁCH CŨ (sửa 16/08):
///
/// Bản đầu tự đo tốc độ bằng cách so vị trí giữa hai khung hình, với lý do "vị trí vốn đã
/// đồng bộ rồi nên khỏi tốn băng thông". Nghe hợp lý nhưng SAI trên máy Host.
///
/// FPSMovement.BeforeAllTicks() tắt/bật CharacterController cho mọi nhân vật mà máy này
/// có quyền mô phỏng. Trên Host, điều kiện đó đúng với TẤT CẢ nhân vật, kể cả của client.
/// Việc tắt/bật đó ép transform nhảy thẳng về vị trí thô của từng tick, phá nát phần nội
/// suy mượt của NetworkTransform. Đo quãng đường trên chuỗi vị trí giật cục đó ra tốc độ
/// dọc rất lớn -> trên màn hình Host, mọi đối thủ đều hiện animation rơi tự do dù đang
/// đứng yên. Máy Client thì không dính vì ở đó nhân vật Host là proxy, được nội suy đàng hoàng.
///
/// Bài học: "suy ra tại chỗ thay vì đồng bộ" là nguyên tắc tốt, nhưng chỉ đúng khi thứ
/// mình suy ra từ đó ĐÁNG TIN. Transform trên Host không đáng tin cho việc đo tốc độ.
/// Ba giá trị kia cộng lại chưa tới 6 byte mỗi tick - rẻ hơn nhiều so với một lỗi khó tìm.
/// </summary>
public class PlayerAnimatorDriver : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Để trống thì tự tìm Animator trong các object con.")]
    public Animator animator;

    [Header("Tên tham số trong Animator Controller")]
    [Tooltip("Tham số Float, chạy 0 (đứng yên) tới 1 (chạy hết tốc).")]
    public string speedParam = "Speed";

    [Tooltip("Tham số Bool, false khi đang bay trên không.")]
    public string groundedParam = "Grounded";

    [Tooltip("Tham số Trigger, bắn ra mỗi cú cận chiến. Để trống nếu chưa làm animation đấm.")]
    public string punchTriggerParam = "Punch";

    [Tooltip("Tham số Float điều khiển TỐC ĐỘ PHÁT của animation chạy. " +
             "Phải gán vào ô Multiplier của state Locomotion trong Animator thì mới ăn. " +
             "Để trống nếu không dùng.")]
    public string speedMultiplierParam = "SpeedMultiplier";

    [Header("Hiệu chỉnh")]
    [Tooltip("Tốc độ chạy tối đa, dùng để quy về thang 0..1. " +
             "PHẢI khớp với moveSpeed bên FPSMovement, nếu không chân sẽ chạy nhanh hoặc chậm hơn thân.")]
    public float maxSpeed = 13f;

    [Tooltip("Làm mượt con số tốc độ. Cao = phản ứng nhanh, thấp = chuyển mượt. " +
             "Cần có vì vị trí qua mạng hơi giật, lấy thô sẽ làm animation rung.")]
    public float smoothing = 12f;

    [Tooltip("Đi lên/xuống nhanh hơn mức này (m/s) thì coi như đang ở trên không.")]
    public float airborneVerticalSpeed = 2.5f;

    [Tooltip("Gom quãng đường trong bấy nhiêu giây rồi mới tính ra tốc độ, thay vì tính " +
             "từng khung hình. BẮT BUỘC phải có: nhân vật di chuyển theo TICK MẠNG chứ không " +
             "theo khung hình, nên đo từng khung hình sẽ cho ra tốc độ sai gấp mấy lần. " +
             "Để quá nhỏ thì lỗi quay lại, để quá lớn thì animation phản ứng chậm.")]
    public float sampleInterval = 0.1f;

    [Header("Tốc độ phát animation chạy")]
    [Tooltip("Nhân tốc độ phát của animation chạy. 1 = nguyên bản, 1.3 = nhanh hơn 30%. " +
             "Chỉnh ô này khi thấy chân quay chậm hơn tốc độ nhân vật thật sự di chuyển.")]
    public float animationSpeedScale = 1.3f;

    [Tooltip("Đi lùi thì phát animation chạy NGƯỢC, thay vì chạy tới trong khi thân lùi lại. " +
             "Đi ngang (strafe) vẫn phát xuôi bình thường.")]
    public bool reverseWhenMovingBackward = true;

    [Header("Góc nhìn thứ nhất")]
    [Tooltip("Giấu thân mình khỏi camera của chính mình. " +
             "Không tắt hẳn renderer mà chuyển sang chế độ CHỈ ĐỔ BÓNG — nên bạn vẫn thấy " +
             "bóng của mình dưới đất, và không tranh chấp với PlayerHealth (nó bật/tắt renderer khi chết).")]
    public bool hideOwnBody = true;

    private Vector3 _lastPosition;
    private float _smoothedSpeed01;
    private bool _bodyHidden;
    private FPSMovement _movement;

    // Gom quãng đường và thời gian lại, cứ đủ sampleInterval mới tính ra tốc độ một lần.
    // Xem chú thích ở ô sampleInterval để biết vì sao không đo từng khung hình.
    private Vector3 _accumulatedDelta;
    private float _accumulatedTime;

    // Tốc độ đo được ở lần chốt gần nhất, giữ nguyên cho tới lần chốt kế tiếp.
    private float _horizontalSpeed;
    private float _verticalSpeed;
    private bool _movingBackward;

    private void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        _movement = GetComponent<FPSMovement>();

        _lastPosition = transform.position;

        if (animator != null)
        {
            // ÉP TẮT ROOT MOTION.
            //
            // Bật root motion nghĩa là để animation tự dịch chuyển nhân vật. Nhưng ở game này
            // CharacterController mới là thứ quyết định vị trí, nên hai bên sẽ đánh nhau:
            // nhân vật trượt lung tung, hoặc đi một quãng rồi bị kéo giật lại.
            //
            // Ép ở code thay vì nhắc nhau nhớ bỏ tick trong Inspector, vì đây là lỗi
            // rất hay gặp mà triệu chứng lại khó đoán ra nguyên nhân.
            animator.applyRootMotion = false;
        }
    }

    private void Update()
    {
        if (animator == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // ĐƯỜNG CHÍNH: đọc thẳng số liệu đã đồng bộ từ FPSMovement.
        //
        // Sửa ngày 16/08. Trước đây script này tự đo quãng đường giữa hai khung hình,
        // và nó SAI trên máy Host khi nhìn sang nhân vật của client: FPSMovement
        // .BeforeAllTicks() tắt/bật CharacterController cho MỌI nhân vật trên Host
        // (vì Host có StateAuthority với tất cả), khiến transform của họ nhảy thô từng
        // tick thay vì được nội suy mượt. Đo trên chuỗi vị trí giật cục đó ra tốc độ
        // dọc rất lớn -> mọi đối thủ đều bị hiển thị là đang rơi tự do.
        //
        // Ba con số dưới đây được tính ở nơi CÓ MÔ PHỎNG THẬT rồi mới truyền đi,
        // nên máy nào đọc cũng đúng.
        if (_movement != null)
        {
            animator.SetFloat(speedParam, _movement.WalkSpeed01);
            animator.SetBool(groundedParam, _movement.IsGrounded);

            if (!string.IsNullOrEmpty(speedMultiplierParam))
            {
                float s = (reverseWhenMovingBackward && _movement.MovingBackward) ? -1f : 1f;
                animator.SetFloat(speedMultiplierParam, animationSpeedScale * s);
            }

            TryHideOwnBody();
            return;
        }

        // ĐƯỜNG DỰ PHÒNG: không có FPSMovement (ví dụ bù nhìn tập bắn) thì tự đo lấy.
        // GOM quãng đường lại, ĐỪNG tính tốc độ ngay từng khung hình.
        //
        // Nhân vật được di chuyển trong FixedUpdateNetwork, tức là theo TICK MẠNG.
        // Chạy 144 FPS với tick 60Hz thì vị trí chỉ nhảy ở một số khung hình - lấy quãng
        // đường của cả một tick chia cho thời gian của một khung hình sẽ ra tốc độ thổi
        // phồng gấp 2-3 lần. Chỉ cần rung vài centimet theo phương dọc là đủ vượt ngưỡng
        // và Animator tưởng nhân vật đang rơi trong khi nó đứng yên.
        Vector3 position = transform.position;
        _accumulatedDelta += position - _lastPosition;
        _lastPosition = position;
        _accumulatedTime += dt;

        if (_accumulatedTime >= sampleInterval)
        {
            Vector3 flat = new Vector3(_accumulatedDelta.x, 0f, _accumulatedDelta.z);

            _horizontalSpeed = flat.magnitude / _accumulatedTime;
            _verticalSpeed = Mathf.Abs(_accumulatedDelta.y) / _accumulatedTime;

            // ĐANG ĐI TỚI HAY ĐI LÙI?
            //
            // Chiếu hướng di chuyển lên hướng nhân vật đang QUAY MẶT. Âm nhiều nghĩa là
            // đang lùi. Dùng ngưỡng -0.3 chứ không phải 0, để đi ngang (strafe) - lúc đó
            // tích vô hướng xấp xỉ 0 - vẫn được coi là đi tới và phát animation xuôi.
            // Lấy đúng mốc 0 thì chỉ cần lệch một chút là animation lật xuôi ngược liên tục.
            if (flat.sqrMagnitude > 0.000001f)
            {
                float facingDot = Vector3.Dot(flat.normalized, transform.forward);
                _movingBackward = facingDot < -0.3f;
            }
            else
            {
                _movingBackward = false;
            }

            _accumulatedDelta = Vector3.zero;
            _accumulatedTime = 0f;
        }

        // --- TỐC ĐỘ NGANG -> tham số Speed ---
        // Luôn là số DƯƠNG, vì nó chỉ dùng để pha trộn Idle <-> Run trong Blend Tree.
        // Chuyện xuôi hay ngược do tham số Multiplier bên dưới lo.
        float speed01 = maxSpeed > 0f ? Mathf.Clamp01(_horizontalSpeed / maxSpeed) : 0f;
        _smoothedSpeed01 = Mathf.Lerp(_smoothedSpeed01, speed01, smoothing * dt);
        animator.SetFloat(speedParam, _smoothedSpeed01);

        // --- TỐC ĐỘ PHÁT ANIMATION -> tham số SpeedMultiplier ---
        // Số âm khiến Unity phát animation NGƯỢC. Nhờ vậy đi lùi trông đúng là đi lùi,
        // chứ không phải chạy tới trong khi thân thể trôi ngược về sau.
        if (!string.IsNullOrEmpty(speedMultiplierParam))
        {
            float sign = (reverseWhenMovingBackward && _movingBackward) ? -1f : 1f;
            animator.SetFloat(speedMultiplierParam, animationSpeedScale * sign);
        }

        // --- ĐANG BAY TRÊN KHÔNG? -> tham số Grounded ---
        //
        // Suy từ tốc độ THẲNG ĐỨNG chứ không đọc CharacterController.isGrounded, vì
        // isGrounded chỉ đúng trên máy đang mô phỏng nhân vật đó. Nhìn sang nhân vật
        // người khác thì nó vô nghĩa.
        //
        // Xét cả hai chiều: bị hất bay LÊN cũng là đang ở trên không, không riêng gì rơi xuống.
        animator.SetBool(groundedParam, _verticalSpeed < airborneVerticalSpeed);

        TryHideOwnBody();
    }

    /// <summary>
    /// Chạy animation đấm. PlayerMagnetController gọi vào mỗi khi có cú cận chiến.
    ///
    /// Cắm vào OnMeleePerformed() - chỗ ĐÃ CÓ SẴN để phát tiếng đấm. Không tự tạo đường
    /// mới, vì hàm đó đã được gọi đúng một lần mỗi cú trên MỌI máy (nhờ MeleeCount
    /// là [Networked] + OnChangedRender). Gọi thẳng trong FixedUpdateNetwork thì Fusion
    /// tua lại nhiều tick sẽ bắn trigger 5-6 lần cho một cú đấm.
    /// </summary>
    public void TriggerPunch()
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(punchTriggerParam)) return;

        animator.SetTrigger(punchTriggerParam);
    }

    /// <summary>
    /// Giấu thân mình khỏi camera của chính mình.
    ///
    /// Phải thử lại mỗi khung hình cho tới khi thành công, vì FPSMovement.Local chỉ được
    /// gán trong Spawned() của Fusion — có thể xảy ra SAU Start() của script này.
    ///
    /// Dùng ShadowsOnly chứ KHÔNG tắt renderer.enabled, vì hai lý do:
    ///   1. PlayerHealth.ApplyAliveState() cũng bật/tắt renderer.enabled khi chết và hồi sinh.
    ///      Nếu ở đây cũng đụng vào cùng thuộc tính đó thì hồi sinh xong thân mình sẽ hiện lại.
    ///   2. Giữ được bóng đổ của chính mình dưới đất — một chi tiết nhỏ nhưng làm góc nhìn
    ///      thứ nhất bớt cảm giác "camera bay lơ lửng".
    /// </summary>
    private void TryHideOwnBody()
    {
        if (!hideOwnBody || _bodyHidden) return;

        // Chưa biết ai là nhân vật của mình thì chờ khung hình sau
        if (FPSMovement.Local == null) return;

        _bodyHidden = true; // dù có phải mình hay không cũng thôi không xét lại nữa

        // Không phải nhân vật của mình thì để nguyên cho người ta nhìn thấy
        if (FPSMovement.Local.gameObject != gameObject) return;

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;

            // Bỏ qua thứ vốn không đổ bóng (hiệu ứng, UI trong thế giới...)
            if (r.shadowCastingMode == ShadowCastingMode.Off) continue;

            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
