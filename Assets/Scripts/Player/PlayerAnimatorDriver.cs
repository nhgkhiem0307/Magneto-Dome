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

    [Tooltip("BẬT (mặc định từ 31/08): nhìn xuống THẤY THÂN MÌNH — ngực, hai tay, hai chân.\n\n" +
             "Chỉ giấu riêng cái đầu, vì đầu nằm ngay chỗ camera nên để nguyên sẽ che kín " +
             "màn hình bằng mặt trong của hộp sọ.\n\n" +
             "TẮT: quay lại kiểu cũ - vô hình hoàn toàn với chính mình, chỉ còn cái bóng.")]
    public bool showOwnBodyInFirstPerson = false;

    [Tooltip("Thu nhỏ xương đầu còn bấy nhiêu lần để nó biến mất.\n\n" +
             "CỐ Ý KHÔNG ĐỂ 0. Scale bằng 0 tạo ma trận suy biến, một số đường tính da " +
             "(skinning) sẽ cho ra pháp tuyến NaN và mặt bị nháy đen hoặc kéo dài vô tận. " +
             "Số rất nhỏ thì mắt không phân biệt được với 0 mà toán học vẫn hợp lệ.")]
    public float headHideScale = 0.001f;

    [Tooltip("Đổi mặt phẳng cắt gần của camera thành bấy nhiêu mét. Đặt 0 để không đụng tới.\n\n" +
             "CẦN THIẾT khi bật thân thật. Mặc định Unity là 0.3m - nghĩa là mọi thứ gần hơn " +
             "30cm đều bị cắt bỏ. Cúi đầu nhìn xuống thì ngực mình nằm trong khoảng đó, " +
             "nên bạn sẽ nhìn XUYÊN QUA ngực thấy nội tạng rỗng thay vì thấy áo.\n\n" +
             "0.08 đủ gần để hết cắt. Đừng hạ xuống quá thấp (0.01) - mặt phẳng cắt gần " +
             "càng nhỏ thì độ chính xác của bộ đệm chiều sâu càng kém, sinh ra hiện tượng " +
             "hai mặt phẳng tranh nhau nhấp nháy ở xa.")]
    public float firstPersonNearClip = 0.08f;

    [Header("Bàn tay góc nhìn thứ nhất - IK (CHỈ máy của chính mình)")]
    [Tooltip("Dùng IK ĐẶT THẲNG bàn tay vào một điểm cố định trước mặt, bỏ qua hoàn toàn " +
             "tư thế tay của animation.\n\n" +
             "Khác hẳn cách 'ghì tay' cũ bên dưới. Cách cũ CỘNG THÊM góc xoay vào tư thế " +
             "animation, nên cái mềm oặt của animation gốc vẫn lộ ra - đó chính là cảm giác " +
             "'ma trôi'. IK thì GHI ĐÈ: animation vung tay thế nào cũng mặc kệ, bàn tay luôn " +
             "nằm đúng chỗ ta chỉ định, và mọi chuyển động thấy được đều do code này tạo ra " +
             "nên kiểm soát được hoàn toàn.\n\n" +
             "⚠️ Cần bật IK Pass trên Base Layer của AC_Player (đã bật sẵn 31/08).\n\n" +
             "CHỈ chạy trên nhân vật của mình - người khác vẫn thấy bạn vung tay bình thường.")]
    public bool useFirstPersonHandIK = false;

    [Tooltip("Vị trí bàn tay TRÁI so với camera, tính bằng mét. " +
             "x = trái/phải, y = trên/dưới, z = xa/gần.")]
    public Vector3 leftHandOffset = new Vector3(-0.28f, -0.32f, 0.55f);

    [Tooltip("Vị trí bàn tay PHẢI so với camera.")]
    public Vector3 rightHandOffset = new Vector3(0.3f, -0.3f, 0.5f);

    [Tooltip("Sức mạnh IK, 0..1. 1 = bàn tay bám đúng điểm chỉ định tuyệt đối.\n\n" +
             "Hạ xuống 0.7-0.8 nếu muốn animation gốc còn ảnh hưởng một phần, " +
             "nhưng như vậy sẽ kéo lại cảm giác mềm oặt.")]
    [Range(0f, 1f)]
    public float handIKWeight = 1f;

    [Tooltip("Có điều khiển luôn HƯỚNG XOAY của bàn tay không, 0..1.\n\n" +
             "CẦN THIẾT. Nếu chỉ đặt vị trí mà bỏ mặc hướng xoay, cổ tay vẫn giữ góc của " +
             "animation - mà animation đang cho tay buông thõng bên hông. Kéo bàn tay ra " +
             "trước mặt trong khi cổ tay vẫn xoay kiểu buông thõng thì bàn tay chìa ngang " +
             "hoặc lật ngửa, nhìn như cổ tay bị gãy.\n\n" +
             "Hạ về 0 nếu muốn thử để animation tự lo phần xoay.")]
    [Range(0f, 1f)]
    public float handRotationWeight = 0.8f;

    [Tooltip("Xoay thêm bàn tay bấy nhiêu độ so với hướng camera.\n\n" +
             "⚠️ Ô NÀY GẦN NHƯ CHẮC CHẮN PHẢI CHỈNH. Mỗi rig định nghĩa 'hướng gốc' của " +
             "bàn tay một kiểu khác nhau, và tôi không chạy được Unity để xem kết quả.\n\n" +
             "Nếu lòng bàn tay quay sai phía, xoay thử từng trục 90 độ một: (90,0,0), " +
             "(0,90,0), (0,0,90)... cho tới khi thuận mắt.")]
    public Vector3 handRotationOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Biên độ nhấp nhô tay khi đi bộ, tính bằng mét.")]
    public float handBobAmount = 0.05f;

    [Tooltip("Nhịp nhấp nhô tay, số chu kỳ mỗi giây khi chạy hết tốc.")]
    public float handBobSpeed = 5f;

    [Tooltip("Tay TRÔI LẠI bao nhiêu khi bạn quay đầu, tính bằng mét trên mỗi độ xoay.\n\n" +
             "Đây là thứ tạo ra SỨC NẶNG. Quay người mà bàn tay dính cứng vào camera thì " +
             "trông như hai cái sticker dán trên màn hình. Cho tay trôi chậm lại một nhịp " +
             "rồi mới đuổi kịp thì mắt đọc ra là 'tay có khối lượng'.")]
    public float handSwayAmount = 0.012f;

    [Tooltip("Tay trôi tối đa bấy nhiêu mét, để quay ngoắt 180 độ không làm tay bay ra sau lưng.")]
    public float handSwayMax = 0.09f;

    [Tooltip("Tốc độ tay đuổi về đúng chỗ sau khi trôi. Thấp = nặng và ì, cao = nhẹ và bám sát.")]
    public float handSwayRecover = 7f;

    [Tooltip("Khi ĐẤM, tạm buông IK trong bấy nhiêu giây để animation đấm hiện ra.\n\n" +
             "BẮT BUỘC PHẢI CÓ. IK chạy SAU khi các layer animation đã trộn xong, nên nó " +
             "đè luôn cả Punch Layer - không có ô này thì ở góc nhìn thứ nhất bạn đấm mà " +
             "hai tay đứng im như tượng, dù đối thủ vẫn bay đi.\n\n" +
             "Sức mạnh IK tụt về 0 ngay lúc ra đòn rồi lớn dần trở lại, nên cú đấm hiện " +
             "trọn vẹn mà tay vẫn tự về đúng chỗ sau đó. Đặt 0 để tắt.")]
    public float punchIKReleaseTime = 0.35f;

    [Header("Ghì tay vào tầm nhìn - CÁCH CŨ, đã thay bằng IK ở trên")]
    [Tooltip("Kéo hai cánh tay ra trước để chúng nằm trong khung hình, khỏi phải cúi xuống mới thấy.\n\n" +
             "🔴 ĐANG TẮT (chốt 31/08 sau khi chạy thử). Cơ chế chạy đúng, nhưng KẾT QUẢ XẤU - " +
             "và nguyên nhân không nằm ở mấy con số bên dưới nên chỉnh chúng cũng vô ích.\n\n" +
             "Lý do thật: animation gốc (Mixamo) đưa tay rất nhẹ và mượt, không có sức nặng. " +
             "Ở góc nhìn thứ ba xa 10m thì không ai để ý, nhưng kéo sát vào tầm mắt thì cái " +
             "mềm oặt đó lộ hết - trông như tay ma trôi lơ lửng. Ghì tay chỉ khuếch đại " +
             "khuyết điểm sẵn có của animation chứ không tạo ra nó.\n\n" +
             "Muốn bật lại thì phải sửa GỐC trước: thay bằng animation chạy có lực hơn, hoặc " +
             "tự tạo tư thế tay riêng cho góc nhìn thứ nhất. Vặn Arm Swing Angle / Speed thì " +
             "chỉ làm tay ma trôi nhanh hơn và rộng hơn thôi.\n\n" +
             "Phần cơ chế vẫn giữ nguyên trong code, bật lại là chạy: CHỈ tác động lên bản sao " +
             "nhân vật TRÊN MÁY NÀY, người chơi khác vẫn thấy bạn vung tay chạy bộ bình thường.")]
    public bool raiseArmsInFirstPerson = false;

    [Tooltip("Nâng cánh tay ra TRƯỚC bao nhiêu độ. Số dương = ra trước.\n\n" +
             "Nếu chạy thử mà thấy tay vung RA SAU lưng thì đổi ô này thành số âm - " +
             "hướng xoay phụ thuộc quy ước trục xương của rig, không đoán trước được chắc chắn.")]
    public float armLiftAngle = 38f;

    [Tooltip("Gập khuỷu tay bao nhiêu độ, để bàn tay lọt vào khung hình thay vì chỉ thấy " +
             "hai khuỷu tay chìa ra hai bên.")]
    public float elbowBendAngle = 55f;

    [Tooltip("Biên độ vung tay khi đi bộ, tính bằng độ. Hai tay vung ngược pha nhau.\n\n" +
             "Đặt 0 thì tay đứng yên cứng đờ trước mặt - có tay trong khung hình nhưng " +
             "không có cảm giác đang đi.")]
    public float armSwingAngle = 26f;

    [Tooltip("Nhịp vung tay, số chu kỳ mỗi giây khi chạy hết tốc. CAO = chu kỳ ngắn, vung nhanh.\n\n" +
             "Người chạy ngoài đời vung khoảng 2-3 vòng/giây, nhưng nhân vật này chạy " +
             "13 m/s - nhanh hơn cả vận động viên - nên nhịp tay phải dồn hơn thực tế " +
             "mới không có cảm giác lướt trên băng.\n\n" +
             "⚠️ Đừng đẩy lên tới 8 cho khớp với Bob Frequency bên CameraShake. Hai ô đó " +
             "đếm đơn vị KHÁC NHAU: Bob đếm BƯỚC CHÂN, còn ô này đếm VÒNG VUNG TAY, mà một " +
             "vòng vung ứng với hai bước. Để 8 ở đây là tay rung bần bật.")]
    public float armSwingSpeed = 6f;

    private Vector3 _lastPosition;
    private float _smoothedSpeed01;
    private bool _bodyHidden;
    private FPSMovement _movement;

    // Xương đầu đã bị thu nhỏ, giữ lại để ép lại kích thước mỗi khung hình.
    // Chỉ khác null trên nhân vật của CHÍNH MÌNH - xem LateUpdate().
    private Transform _hiddenHead;

    // Đây có phải nhân vật của người ngồi trước máy này không.
    //
    // Tách riêng khỏi _hiddenHead vì hai việc độc lập: người chơi có thể tắt phần
    // hiện thân mình mà vẫn muốn ghì tay, hoặc ngược lại.
    private bool _isOwnBody;

    // Pha nhịp vung tay, cộng dồn theo thời gian. Phải cộng dồn chứ không tính thẳng
    // từ Time.time - đứng lại rồi đi tiếp mà tính từ Time.time sẽ nhảy vào giữa một
    // nhịp vung bất kỳ và hai tay giật một cái.
    private float _armPhase;

    // --- IK BÀN TAY ---
    private Transform _camera;

    // Độ trôi hiện tại của tay khi quay đầu, tính trong hệ toạ độ CAMERA.
    private Vector3 _handSway;

    // Góc nhìn ở khung hình trước, để biết vừa quay bao nhiêu độ.
    private float _lastYaw;
    private float _lastPitch;
    private bool _hasLastLook;

    // Đếm ngược thời gian buông IK cho cú đấm. Xem punchIKReleaseTime.
    private float _punchIKTimer;

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

        // Buông IK ngay cả khi chưa gán tên Trigger. Hai việc độc lập: người dùng có thể
        // chưa làm animation đấm, nhưng nếu đã làm rồi mà quên dòng này thì cú đấm bị IK
        // đè mất và không ai hiểu vì sao tay đứng im.
        _punchIKTimer = punchIKReleaseTime;

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

        _isOwnBody = true;

        // CÁCH MỚI (31/08): giữ nguyên thân, chỉ giấu riêng cái đầu.
        //
        // Nhìn xuống thấy ngực, hai tay, hai chân của chính mình - góc nhìn thứ nhất có
        // thân thật, thay vì camera bay lơ lửng không gắn với cái gì.
        //
        // Vì sao chỉ cần giấu ĐẦU mà không phải cắt gọt gì thêm: camera nằm ở
        // y = 0.77 so với gốc, tức 1.77m trên mặt chân - đúng tầm mắt, tức là nằm
        // NGAY TRONG hộp sọ. Để nguyên đầu thì toàn màn hình là mặt trong của sọ.
        // Mọi bộ phận còn lại đều nằm dưới hoặc sau camera nên không che gì cả.
        //
        // Vì sao thu nhỏ XƯƠNG thay vì tắt renderer của đầu: nhân vật là một tấm da liền
        // (một SkinnedMeshRenderer duy nhất cho cả người), không tách đầu ra tắt riêng được.
        // Thu nhỏ xương thì mọi đỉnh bám vào xương đó co về một điểm và biến mất, phần còn
        // lại của tấm da không hề bị ảnh hưởng. Tóc, mũ... gắn vào xương đầu cũng co theo.
        if (showOwnBodyInFirstPerson && TryHideHeadBone())
        {
            ApplyFirstPersonNearClip();
            return;
        }

        // CÁCH CŨ - dùng khi tắt tuỳ chọn trên, hoặc rig không phải Humanoid nên
        // không tìm được xương đầu. Giấu sạch, chỉ chừa lại cái bóng.
        //
        // Dùng ShadowsOnly chứ không tắt renderer.enabled, vì PlayerHealth.ApplyAliveState()
        // cũng bật/tắt đúng thuộc tính đó khi chết và hồi sinh - đụng nhau thì hồi sinh xong
        // thân mình hiện lại.
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;

            // Bỏ qua thứ vốn không đổ bóng (hiệu ứng, UI trong thế giới...)
            if (r.shadowCastingMode == ShadowCastingMode.Off) continue;

            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    /// <summary>
    /// Thu nhỏ xương đầu cho nó biến khỏi tầm nhìn. Trả về false nếu không làm được
    /// (chưa gán Animator, hoặc rig không phải Humanoid nên không có khái niệm "xương đầu").
    /// </summary>
    private bool TryHideHeadBone()
    {
        if (animator == null || !animator.isHuman) return false;

        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null) return false;

        _hiddenHead = head;
        _hiddenHead.localScale = Vector3.one * headHideScale;
        return true;
    }

    private void ApplyFirstPersonNearClip()
    {
        if (firstPersonNearClip <= 0f) return;
        if (_movement == null || _movement.cameraTransform == null) return;

        Camera cam = _movement.cameraTransform.GetComponent<Camera>();
        if (cam != null) cam.nearClipPlane = firstPersonNearClip;
    }

    /// <summary>
    /// Ép lại kích thước xương đầu SAU KHI Animator đã ghi xong tư thế của khung hình này.
    ///
    /// Vì sao phải làm mỗi khung hình chứ không đặt một lần: Animator ghi đè transform của
    /// mọi xương humanoid ở mỗi khung. Đa số animation không có đường cong scale nên đặt một
    /// lần thường là đủ - nhưng "thường" không phải "luôn luôn", và nếu có một clip nào đó
    /// đụng tới scale thì cái đầu sẽ bung ra che kín màn hình đúng lúc đang đánh nhau.
    /// Ghi đè lại ở LateUpdate là một phép gán, rẻ tới mức không đáng đo.
    /// </summary>
    private void LateUpdate()
    {
        if (_hiddenHead != null)
        {
            _hiddenHead.localScale = Vector3.one * headHideScale;
        }

        ApplyFirstPersonArms();
    }

    /// <summary>
    /// Kéo hai cánh tay ra trước cho lọt vào khung hình, kèm nhịp vung khi đi bộ.
    ///
    /// ⚠️ CHỈ CHẠY TRÊN NHÂN VẬT CỦA CHÍNH MÌNH. Đây là điều kiện làm cho cả ý tưởng này
    /// khả thi mà không cần model tay riêng:
    ///
    /// Animation KHÔNG được truyền qua mạng dưới dạng tư thế xương. Mỗi máy tự chạy
    /// Animator của riêng nó, dựa trên mấy con số [Networked] (WalkSpeed01, IsGrounded,
    /// MovingBackward). Nên sửa tư thế xương ở máy này KHÔNG hề lan sang máy khác - trên
    /// màn hình họ, bạn vẫn vung tay chạy bộ đúng kiểu góc nhìn thứ ba.
    ///
    /// Cùng nguyên tắc với CameraShake: thứ chỉ mình mình thấy thì không đồng bộ.
    ///
    /// Phải nằm trong LateUpdate vì Animator ghi đè toàn bộ xương ở mỗi khung hình.
    /// Sửa ở Update thì Animator ghi đè lại ngay sau đó, không thấy gì thay đổi cả.
    /// </summary>
    private void ApplyFirstPersonArms()
    {
        if (!raiseArmsInFirstPerson) return;

        // HAI CÁCH KHÔNG ĐƯỢC CHẠY CÙNG LÚC.
        //
        // Cách này chạy ở LateUpdate, tức là SAU khi IK đã đặt xong bàn tay - nên nó sẽ
        // xoay đè lên kết quả IK và kéo tay ra khỏi chỗ vừa đặt. Kết quả là tay giật lung
        // tung, mà nhìn vào thì tưởng IK hỏng chứ không nghĩ là bị cái khác phá.
        if (useFirstPersonHandIK) return;

        if (animator == null || !animator.isHuman) return;

        // Tự xác định thay vì tin vào cờ do TryHideOwnBody đặt: hàm đó thoát ngay nếu
        // ô Hide Own Body bị tắt, nên cờ sẽ không bao giờ được gán.
        if (!ResolveOwnBody()) return;

        float speed01 = _movement != null ? Mathf.Clamp01(_movement.LocalWalkSpeed01) : 0f;

        // Nhịp vung nhanh dần theo tốc độ đi. Đứng yên thì pha đứng im, tay giữ nguyên
        // tư thế giơ trước chứ không vung - giống người đang thủ thế.
        _armPhase += Time.deltaTime * armSwingSpeed * speed01 * Mathf.PI * 2f;

        // Biên độ cũng nhân theo tốc độ, nên lúc chuyển từ đứng sang chạy thì tay
        // lớn dần chứ không bật ra đột ngột.
        float swing = Mathf.Sin(_armPhase) * armSwingAngle * speed01;

        // Hai tay NGƯỢC PHA nhau: tay trái ra trước thì tay phải ra sau.
        // Cùng pha thì trông như đang bơi ếch chứ không phải đi bộ.
        RotateBoneForward(HumanBodyBones.LeftUpperArm, armLiftAngle + swing);
        RotateBoneForward(HumanBodyBones.RightUpperArm, armLiftAngle - swing);

        RotateBoneForward(HumanBodyBones.LeftLowerArm, elbowBendAngle);
        RotateBoneForward(HumanBodyBones.RightLowerArm, elbowBendAngle);
    }

    /// <summary>
    /// Đặt hai bàn tay vào đúng tầm nhìn bằng IK.
    ///
    /// Unity gọi hàm này GIỮA lúc tính animation, sau khi đã dựng xong tư thế gốc nhưng
    /// TRƯỚC khi chốt lại. Nhờ vậy IK ghi đè được tay mà không phá phần còn lại của cơ thể.
    /// Chỉ được gọi khi layer có bật IK Pass - đã bật sẵn trên Base Layer của AC_Player.
    ///
    /// VÌ SAO DÙNG IK THAY VÌ TỰ XOAY XƯƠNG:
    ///
    /// Bản trước xoay thẳng xương cánh tay, và nó thất bại vì hai lẽ. Một là nó CỘNG THÊM
    /// vào tư thế animation, nên cái mềm oặt của animation gốc vẫn lộ nguyên - đúng cái
    /// "ma trôi". Hai là mỗi rig đặt trục xương một kiểu, viết mà không nhìn được kết quả
    /// thì chỉ có đoán mò.
    ///
    /// IK lật ngược bài toán: ta nói "để bàn tay ở ĐIỂM NÀY", Unity tự tính xem vai và
    /// khuỷu phải xoay bao nhiêu. Vị trí thì đo bằng mét và hình dung được ngay, không
    /// phụ thuộc quy ước trục của rig. Và vì nó GHI ĐÈ chứ không cộng thêm, animation gốc
    /// yếu tới đâu cũng không lọt ra được - mọi chuyển động thấy trên màn hình đều do
    /// đoạn code này sinh ra, nên muốn nặng tay tới đâu cũng làm được.
    /// </summary>
    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != 0) return;
        if (!useFirstPersonHandIK) return;
        if (animator == null || !animator.isHuman) return;

        if (!ResolveOwnBody()) return;
        if (!ResolveCamera()) return;

        float speed01 = _movement != null ? Mathf.Clamp01(_movement.LocalWalkSpeed01) : 0f;

        UpdateHandSway();

        // Nhấp nhô theo bước chân. Trục dọc chạy nhanh gấp đôi trục ngang vì mỗi chu kỳ
        // có HAI bước chân - cùng lý do đã ghi ở CameraShake, bỏ số 2 đi thì thành đi cà nhắc.
        _armPhase += Time.deltaTime * handBobSpeed * speed01 * Mathf.PI * 2f;

        float bobY = Mathf.Sin(_armPhase * 2f) * handBobAmount * speed01;
        float bobX = Mathf.Cos(_armPhase) * handBobAmount * 0.6f * speed01;

        // Hai tay lệch pha nhau nửa vòng, nên tay này lên thì tay kia xuống.
        // Cùng pha thì hai tay dập lên dập xuống như đang bơi.
        Vector3 leftBob = new Vector3(bobX, bobY, 0f);
        Vector3 rightBob = new Vector3(bobX, -bobY, 0f);

        // BUÔNG IK TRONG LÚC ĐẤM, để animation đấm hiện ra.
        //
        // Sức mạnh tụt về 0 ngay lúc ra đòn rồi lớn dần trở lại: t chạy từ 1 xuống 0,
        // nên hệ số (1 - t) chạy từ 0 lên 1.
        float weight = handIKWeight;

        if (_punchIKTimer > 0f && punchIKReleaseTime > 0f)
        {
            _punchIKTimer -= Time.deltaTime;

            float t = Mathf.Clamp01(_punchIKTimer / punchIKReleaseTime);
            weight *= 1f - t;
        }

        ApplyHandIK(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow,
                    leftHandOffset + leftBob + _handSway, weight);
        ApplyHandIK(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow,
                    rightHandOffset + rightBob + _handSway, weight);
    }

    /// <summary>
    /// Tính độ TRÔI LẠI của tay khi người chơi quay đầu.
    ///
    /// Đây là chi tiết tạo ra sức nặng. Tay dính cứng vào camera thì trông như hai cái
    /// sticker dán trên màn hình; cho nó trôi chậm lại một nhịp rồi mới đuổi kịp thì mắt
    /// tự đọc ra là "tay có khối lượng".
    ///
    /// Trôi NGƯỢC chiều quay: quay sang phải thì tay tụt lại bên trái.
    /// </summary>
    private void UpdateHandSway()
    {
        float yaw = _camera.eulerAngles.y;
        float pitch = _camera.eulerAngles.x;

        if (!_hasLastLook)
        {
            _lastYaw = yaw;
            _lastPitch = pitch;
            _hasLastLook = true;
            return;
        }

        // DeltaAngle chứ không phải phép trừ thường: góc Euler nhảy từ 359 về 0 sẽ cho ra
        // chênh lệch 359 độ thay vì 1 độ, và tay bị quăng ra ngoài màn hình mỗi lần quay
        // qua mốc đó.
        float deltaYaw = Mathf.DeltaAngle(_lastYaw, yaw);
        float deltaPitch = Mathf.DeltaAngle(_lastPitch, pitch);

        _lastYaw = yaw;
        _lastPitch = pitch;

        _handSway.x -= deltaYaw * handSwayAmount;
        _handSway.y += deltaPitch * handSwayAmount;

        _handSway = Vector3.ClampMagnitude(_handSway, handSwayMax);

        // Đuổi về 0. Không có dòng này thì tay trôi đi rồi ở lại đó vĩnh viễn.
        _handSway = Vector3.Lerp(_handSway, Vector3.zero, handSwayRecover * Time.deltaTime);
    }

    /// <summary>
    /// Đặt một bàn tay vào điểm chỉ định, kèm gợi ý hướng khuỷu tay.
    /// </summary>
    /// <param name="localOffset">Vị trí mong muốn, tính trong hệ toạ độ CAMERA.</param>
    private void ApplyHandIK(AvatarIKGoal goal, AvatarIKHint hint, Vector3 localOffset, float weight)
    {
        Vector3 target = _camera.TransformPoint(localOffset);

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKPosition(goal, target);

        // Gợi ý cho khuỷu tay chúc XUỐNG và ra ngoài, thay vì chìa ngang như cánh gà.
        //
        // Thiếu bước này thì bộ giải IK chọn tư thế khuỷu tuỳ ý miễn là bàn tay tới đúng
        // chỗ - và nó rất hay chọn kiểu bẻ ngược khớp, nhìn như tay gãy.
        Vector3 elbowHint = target
                            + _camera.right * (goal == AvatarIKGoal.LeftHand ? -0.35f : 0.35f)
                            - _camera.up * 0.45f;

        animator.SetIKHintPositionWeight(hint, 0.7f * weight);
        animator.SetIKHintPosition(hint, elbowHint);

        // HƯỚNG XOAY BÀN TAY.
        //
        // Không đặt thì cổ tay giữ nguyên góc của animation - mà animation đang cho tay
        // buông thõng bên hông. Kéo bàn tay ra trước mặt với cổ tay xoay kiểu buông thõng
        // thì bàn tay chìa ngang hoặc lật ngửa, nhìn như cổ tay bị gãy.
        animator.SetIKRotationWeight(goal, handRotationWeight * weight);
        animator.SetIKRotation(goal, _camera.rotation * Quaternion.Euler(handRotationOffset));
    }

    private bool ResolveCamera()
    {
        if (_camera != null) return true;

        if (_movement == null || _movement.cameraTransform == null) return false;

        _camera = _movement.cameraTransform;
        return true;
    }

    /// <summary>
    /// Có phải nhân vật của người ngồi trước máy này không. Lưu lại sau lần đầu xác định được.
    /// </summary>
    private bool ResolveOwnBody()
    {
        if (_isOwnBody) return true;

        if (FPSMovement.Local == null) return false;
        if (FPSMovement.Local.gameObject != gameObject) return false;

        _isOwnBody = true;
        return true;
    }

    /// <summary>
    /// Xoay một xương RA TRƯỚC thêm bấy nhiêu độ, cộng vào tư thế Animator vừa ghi.
    ///
    /// Xoay quanh trục PHẢI CỦA NHÂN VẬT trong không gian thế giới, chứ không gán
    /// localRotation tuyệt đối. Lý do: mỗi rig đặt trục xương một kiểu - có rig để trục X
    /// dọc theo xương, rig khác để trục Y. Gán localRotation tuyệt đối thì phải biết đúng
    /// quy ước của rig này, đoán sai là tay xoắn quẹo. Còn "xoay quanh trục phải của thân
    /// người" thì luôn đúng nghĩa là nâng lên/hạ xuống, bất kể xương đặt thế nào.
    ///
    /// Cộng vào tư thế có sẵn (nhân bên trái) chứ không thay thế, nên animation gốc vẫn
    /// giữ được: đấm vẫn ra đấm, chỉ là toàn bộ cánh tay được nâng cao thêm.
    /// </summary>
    private void RotateBoneForward(HumanBodyBones bone, float degrees)
    {
        if (Mathf.Approximately(degrees, 0f)) return;

        Transform t = animator.GetBoneTransform(bone);
        if (t == null) return;

        // Dấu TRỪ vì trong hệ toạ độ của Unity, xoay dương quanh trục phải sẽ hất cánh
        // tay đang buông ra SAU lưng. Đổi dấu ở đây để ô Inspector đọc thuận: số dương
        // nghĩa là ra trước, đúng như tên gọi của nó.
        t.rotation = Quaternion.AngleAxis(-degrees, transform.right) * t.rotation;
    }
}
