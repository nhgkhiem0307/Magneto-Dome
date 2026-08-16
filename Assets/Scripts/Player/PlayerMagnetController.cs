using Fusion;
using UnityEngine;

public class PlayerMagnetController : NetworkBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform holdPoint;

    [Header("Magnet Settings")]
    public float shootRange = 100f;
    public float pullForce = 40f;
    public float pushForce = 300f;

    [Tooltip("Khoảng cách cơ bản để vật lọt vào tay. BÁN KÍNH CỦA VẬT sẽ được cộng thêm " +
             "vào con số này, nên vật càng to càng được bắt từ xa - nếu không thì vật to " +
             "chỉ nghiến vào người mà không bao giờ nắm được.")]
    public float grabDistance = 0.7f;

    [Header("Juggling / Toss Settings")]
    public KeyCode tossKey = KeyCode.V;

    // ĐỔI TỪ "LỰC" SANG "VẬN TỐC" (09/08).
    // AddForce(..., Impulse) cho ra thay đổi vận tốc = lực / khối lượng, nên cùng một
    // con số sẽ tung mỗi prefab một kiểu tuỳ khối lượng của nó. Đặt thẳng vận tốc thì
    // vật nặng vật nhẹ đều tung lên cao như nhau, dễ căn tay hơn nhiều.
    // Số mặc định tính theo trọng lực THẬT của project: Project Settings -> Physics
    // -> Gravity = (0, -3, 0), tức chỉ bằng ~1/3 mặc định Unity. Vật thể bay lơ lửng.
    //
    // Độ cao đạt được h = v^2 / (2g). Với g = 3 thì v = 3 cho ra apex 1.5m,
    // lơ lửng trên không khoảng 2 giây - vừa đủ để tung hứng.
    // CẨN THẬN: tính nhầm bằng g = 9.81 sẽ ra số cao gấp hơn 3 lần thực tế.
    [Tooltip("Tốc độ hất LÊN khi tung, mét/giây. Đây là VẬN TỐC chứ không phải lực. " +
             "Với gravity -3 của project: 3 m/s = cao 1.5m, 4.5 m/s = cao 3.4m.")]
    public float tossUpSpeed = 3f;

    [Tooltip("Tốc độ đẩy RA TRƯỚC khi tung, mét/giây. Giữ tỉ lệ 4:1 so với lực hất lên " +
             "như bản cũ, để vật rơi lại gần tầm tay chứ không trôi ra xa.")]
    public float tossForwardSpeed = 0.75f;

    [Tooltip("Độ xoáy khi tung. Để nhỏ thôi - xoáy mạnh cộng ma sát lúc chạm vật khác " +
             "sẽ đẩy vật chệch hướng.")]
    public float tossSpinStrength = 0.3f;

    [Tooltip("Khoảng hở thêm khi đẩy vật ra khỏi người lúc buông tay, tính bằng mét. " +
             "Tăng lên nếu vật vẫn còn dính vào người khi tung.")]
    public float releaseClearance = 0.15f;

    [Header("Melee & Ability Settings")]
    public float meleeRange = 4f;
    public float dashLockRange = 20f;
    public float meleePushForce = 100f;
    public float meleeCooldown = 1f;

    [Header("Kích thước vật cầm trên tay")]
    [Tooltip("Cạnh dài nhất của vật sau khi thu nhỏ, tính bằng mét. Mọi vật cầm lên đều về cỡ này.")]
    public float heldObjectSize = 0.6f;

    [Tooltip("Bật: vật vốn đã nhỏ hơn cỡ trên thì giữ nguyên. Tắt: mọi vật đều về đúng một cỡ.")]
    public bool onlyShrinkLargeObjects = true;

    [Header("Luật riêng từng loại đạn")]
    [Tooltip("Cản bóng nặng đang bay: tốc độ còn lại bao nhiêu phần. 0.5 = mất nửa tốc.")]
    public float heavyBlockSlowFactor = 0.5f;

    [Tooltip("Cản bóng nặng đang bay: sát thương còn lại bao nhiêu phần.")]
    public float heavyBlockDamageFactor = 0.5f;

    [Tooltip("Hút nhầm bóng gai: nhân sát thương lên bấy nhiêu lần. Theo GDD là 25 -> 50 HP.")]
    public float spikeDamageMultiplier = 2f;

    [Tooltip("Hút nhầm bóng gai: lực kéo nó lao về phía mình, nhân với pullForce.")]
    public float spikePullBoost = 1.5f;

    [Header("Grapple - Kéo áp sát")]
    [Tooltip("Lực kéo bản thân bay về phía đối thủ khi cận chiến ở tầm 3-8m và trái dấu điện tích.")]
    public float grapplePullForce = 100f;

    [Header("Rung camera")]
    [Tooltip("Độ mạnh cú rung khi bắn vật đang cầm đi, thang 0..1. Đặt 0 để tắt.")]
    public float fireShakeTrauma = 0.4f;

    // --- TRẠNG THÁI ĐỒNG BỘ QUA MẠNG ---

    // Cố ý giữ nguyên tên viết thường 'currentGlovePolarity' dù giờ nó là property,
    // để mọi chỗ đang gọi tới nó (cận chiến, DummyMagnetTarget) không phải sửa gì.
    //
    // Bắt buộc [Networked] vì đối thủ PHẢI nhìn thấy màu găng của bạn thì luật
    // khắc chế cùng dấu / trái dấu mới tính ra đúng kết quả trên mọi máy.
    [Networked] public MagneticObject.Polarity currentGlovePolarity { get; set; }

    // Đồng hồ hồi chiêu cận chiến. Dùng TickTimer thay cho Time.time vì Time.time
    // chạy theo máy từng người, còn TickTimer chạy theo tick mạng nên mọi máy khớp nhau.
    [Networked] private TickTimer MeleeCooldownTimer { get; set; }

    // Trạng thái nút ở tick trước, để phát hiện khoảnh khắc "vừa bấm xuống".
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    // Đếm số cú cận chiến, để mọi máy phát tiếng.
    // Không phát thẳng trong FixedUpdateNetwork được vì Fusion tua lại nhiều tick,
    // một cú đấm sẽ kêu chồng lên nhau nhiều lần.
    [Networked, OnChangedRender(nameof(OnMeleePerformed))]
    private int MeleeCount { get; set; }

    // Đếm số vật đã bắn đi, để rung camera đúng một lần mỗi phát.
    //
    // Vì sao phải [Networked] cho một hiệu ứng thuần cục bộ: FireGrabbedObject() nằm SAU
    // dòng "if (!HasStateAuthority) return;" ở FixedUpdateNetwork, nghĩa là nó CHỈ chạy
    // trên máy Host. Nếu rung thẳng trong đó thì người chơi ở máy Client bắn vật sẽ
    // không thấy camera rung gì cả — mà Client mới là phần lớn người chơi.
    // Cho con số đi qua mạng thì máy nào cũng nhận được tín hiệu "vừa có phát bắn".
    //
    // Đây đúng khuôn mẫu của MeleeCount / DashCount / LaunchCount, không phải cách làm mới.
    [Networked, OnChangedRender(nameof(OnObjectFired))]
    private int FireCount { get; set; }

    // Đang cầm sẵn Chai Xăng Tẩy Chế trên tay hay không.
    // Dùng xong một lần là hết, phải rút chai khác từ túi.
    [Networked] public NetworkBool HasGasolineEquipped { get; set; }

    // ID của vật đang cầm trên tay.
    //
    // Vì sao lưu ID chứ không lưu thẳng tham chiếu? Vì Client cần tự đoán trước hành động
    // cầm/ném để bớt độ trễ. Nếu để là biến thường, Fusion sẽ không tua lại được nó khi
    // resimulation -> Client kẹt ở trạng thái "đang cầm" trong khi Host thì không.
    // Đúng loại bệnh đã gặp với CharacterController.
    // NetworkBehaviourId là con số định danh mà mọi máy đều hiểu giống nhau.
    [Networked] private NetworkBehaviourId GrabbedObjectId { get; set; }

    // Sát thương gốc của vật lúc vừa cầm lên, để trả lại khi bắn ra.
    [Networked] private float originalBaseDamage { get; set; }

    // Lớp bọc cho tiện dùng: đọc/ghi như một biến bình thường,
    // bên dưới thực chất là tra ngược từ GrabbedObjectId ra vật thật.
    private MagneticObject grabbedObject
    {
        get
        {
            if (Runner == null) return null;
            if (!Runner.TryFindBehaviour(GrabbedObjectId, out MagneticObject obj)) return null;
            return obj;
        }
        set
        {
            GrabbedObjectId = value != null ? value.Id : default;
        }
    }

    // Rigidbody của vật đang cầm. Lấy tươi mỗi lần dùng thay vì lưu sẵn,
    // để không bị giữ lại tham chiếu cũ sau khi Fusion tua lại trạng thái.
    private Rigidbody grabbedRb
    {
        get
        {
            MagneticObject obj = grabbedObject;
            return obj != null ? obj.GetComponent<Rigidbody>() : null;
        }
    }

    private FPSMovement movement;
    private PlayerHealth health;

    // Vật đang bị THU NHỎ ở máy này. Khác với grabbedObject ở chỗ nó là trạng thái
    // hình ảnh cục bộ, dùng để biết lúc nào cần trả lại kích thước gốc.
    private MagneticObject _visuallyHeld;

    public override void Spawned()
    {
        movement = GetComponent<FPSMovement>();
        health = GetComponent<PlayerHealth>();

        // Chỉ Host đặt giá trị khởi đầu, Client nhận về qua mạng
        if (HasStateAuthority)
        {
            currentGlovePolarity = MagneticObject.Polarity.Positive;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        // GetPressed lọc ra những nút VỪA được bấm xuống ở tick này,
        // tương đương Input.GetKeyDown / GetMouseButtonDown của bản cũ.
        NetworkButtons pressed = input.Buttons.GetPressed(PreviousButtons);
        PreviousButtons = input.Buttons;

        // --- 0. CÁC TRƯỜNG HỢP BỊ KHOÁ THAO TÁC ---

        // CHỈ ĐỂ TEST - XOÁ TRƯỚC KHI NỘP BÀI. Phím K tự sát để thử vòng lặp round một mình.
        if (HasStateAuthority && pressed.IsSet((int)InputButton.DebugSuicide) && health != null)
        {
            health.Die();
        }

        // Đã bị loại khỏi round thì không đánh đấm gì được nữa
        if (health != null && !health.IsAlive) return;

        // Ngoài pha Combat (đang chuẩn bị, hoặc round vừa kết thúc) thì cấm chiến đấu.
        // Nếu chưa có GameManager thì cứ cho đánh, để còn test được khi chạy thẳng TestScene.
        if (GameManager.Instance != null && !GameManager.Instance.IsCombatAllowed()) return;

        // --- 1. ĐỔI ĐIỆN TÍCH GĂNG TAY ---
        // Không chặn theo Host: để Client tự đổi ngay tại máy mình cho phản hồi tức thì,
        // Fusion sẽ tự đối chiếu lại với Host sau. Đây là trạng thái của chính mình
        // nên đoán trước cũng không gây lệch.
        if (pressed.IsSet((int)InputButton.PolarityPositive))
        {
            currentGlovePolarity = MagneticObject.Polarity.Positive;
        }
        if (pressed.IsSet((int)InputButton.PolarityNegative))
        {
            currentGlovePolarity = MagneticObject.Polarity.Negative;
        }

        // --- 2. DỰNG LẠI HƯỚNG NGẮM ---
        // KHÔNG dùng cam.transform.forward được. Lý do: trên máy Host, camera của những
        // người chơi khác đã bị tắt và không xoay theo chuột của họ, nên sẽ ngắm sai hoàn toàn.
        // Góc nhìn thật đã được gửi kèm trong input, nên dựng lại từ đó mới chính xác.
        Quaternion aimRotation = Quaternion.Euler(input.Pitch, input.Yaw, 0f);
        Vector3 aimDirection = aimRotation * Vector3.forward;
        Vector3 aimOrigin = cam != null ? cam.transform.position : transform.position + Vector3.up * 1.6f;

        // --- 3. THAO TÁC VỚI VẬT THỂ VÀ CẬN CHIẾN ---
        //
        // ĐÃ THỬ VÀ BỎ (30/07): từng gỡ dòng dưới đây để Client tự đoán trước cho bớt độ trễ.
        // Kết quả TỆ HƠN HẲN - vật cầm trên tay bị giật và trễ nặng hơn, nhất là khi
        // vừa cầm vừa di chuyển. Độ trễ đều đặn dễ quen tay hơn là giật ngẫu nhiên.
        // Đừng thử lại nếu không có cách xử lý sai lệch dự đoán tử tế hơn.
        if (!HasStateAuthority) return;

        // TRẠNG THÁI 1: ĐANG CÓ ĐỒ TRÊN TAY
        if (grabbedObject != null)
        {
            KeepObjectInHand(aimRotation);

            if (pressed.IsSet((int)InputButton.Toss))
            {
                TossObjectUp(aimDirection);
                return;
            }

            if (pressed.IsSet((int)InputButton.Melee))
            {
                FireGrabbedObject(aimDirection);
                return;
            }

            return;
        }

        // TRẠNG THÁI 2: TAY TRỐNG -> cho phép Hút/Đẩy hoặc Cận chiến
        if (input.Buttons.IsSet((int)InputButton.Fire))
        {
            HandleLeftClickMagnet(aimOrigin, aimDirection, pressed.IsSet((int)InputButton.Fire));
        }

        if (pressed.IsSet((int)InputButton.Melee) && MeleeCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            HandleRightClickMelee(aimOrigin, aimDirection);
        }
    }

    void HandleLeftClickMagnet(Vector3 aimOrigin, Vector3 aimDirection, bool justPressed)
    {
        if (!Physics.Raycast(aimOrigin, aimDirection, out RaycastHit hit, shootRange)) return;
        if (!hit.collider.CompareTag("Magnetic")) return;

        MagneticObject magObj = hit.collider.GetComponent<MagneticObject>();
        if (magObj == null) return;

        Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
        if (targetRb == null) return;

        // ĐANG CẦM CHAI XĂNG -> chế vật thường thành thùng TNT.
        // Xét TRƯỚC mọi thứ khác, vì lúc này chuột trái mang ý nghĩa khác hẳn.
        if (HasGasolineEquipped && justPressed)
        {
            if (magObj.ConvertToTNT())
            {
                HasGasolineEquipped = false; // dùng xong là hết chai
            }
            return;
        }

        // VẬT TRUNG TÍNH -> nạp điện cho nó
        if (magObj.currentPolarity == MagneticObject.Polarity.None)
        {
            if (justPressed) magObj.SetPolarity(currentGlovePolarity);
            return;
        }

        // CÙNG DẤU -> ĐẨY vật ra xa
        if (magObj.currentPolarity == currentGlovePolarity)
        {
            if (!justPressed) return;

            magObj.WakeUp(); // đang ngủ đông thì đánh thức, không thì lực đẩy bị PhysX bỏ qua
            targetRb.isKinematic = false;
            targetRb.useGravity = true;
            magObj.LaunchAsBullet(this);

            Vector3 pushDirection = aimDirection;
            pushDirection.y = 0f;
            pushDirection.Normalize();

            targetRb.linearVelocity = Vector3.zero;
            targetRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            return;
        }

        // TRÁI DẤU -> HÚT về phía mình

        // BÓNG NẶNG ĐANG BAY: không hút về tay được, nhưng CẢN lại được.
        // Đây là nước phòng thủ: bạn không cướp được vật, nhưng làm nó chậm và yếu đi.
        if (magObj.CurrentType == MagneticObject.ObjectType.Heavy && magObj.isMovingAsBullet)
        {
            if (justPressed && magObj.TryCounterInFlight())
            {
                targetRb.linearVelocity *= heavyBlockSlowFactor;
                magObj.CurrentDamage *= heavyBlockDamageFactor;

                Debug.Log($"<color=#88CCFF>[CẢN] Đã cản bóng nặng, sát thương còn {magObj.CurrentDamage}</color>");
            }
            return; // dù cản được hay không thì cũng không hút về tay được
        }

        // BÓNG GAI ĐANG BAY: hút nhầm là tự rước hoạ.
        // Vật lao nhanh hơn về phía bạn VÀ gây gấp đôi sát thương.
        // Đây là cái bẫy phản xạ - thấy vật bay tới mà theo bản năng hút lại thì thiệt nặng.
        if (magObj.CurrentType == MagneticObject.ObjectType.Spike && magObj.isMovingAsBullet)
        {
            if (justPressed && magObj.TryCounterInFlight())
            {
                targetRb.AddForce(-aimDirection * pullForce * spikePullBoost, ForceMode.Impulse);
                magObj.CurrentDamage *= spikeDamageMultiplier;

                Debug.Log($"<color=red><b>[HÚT NHẦM] Bóng gai lao tới! Sát thương {magObj.CurrentDamage}</b></color>");
            }
            return;
        }

        // KÉO VẬT THỂ MƯỢT MÀ VỀ TAY (Không Teleport)
        magObj.WakeUp(); // đang ngủ đông thì đánh thức, không thì hút mãi không nhúc nhích
        targetRb.isKinematic = false;
        targetRb.useGravity = false;

        Vector3 pullDirection = (holdPoint.position - magObj.transform.position).normalized;
        targetRb.linearVelocity = pullDirection * pullForce;

        // NGƯỠNG BẮT VÀO TAY PHẢI CỘNG THÊM BÁN KÍNH VẬT.
        //
        // Trước đây là con số cứng 0.7m đo từ TÂM vật tới holdPoint - và đó là lỗi khiến
        // vật to không bao giờ nắm được. Cái bàn rộng 3m thì collider của nó đụng người
        // chơi khi tâm còn cách hơn 2m, không tài nào xuống dưới 0.7m được. Nó cứ nghiến
        // vào người mãi mà không lọt vào tay.
        //
        // Cộng bán kính vào thì vật to được bắt từ xa hơn, đúng bằng phần nó "to ra".
        float grabThreshold = grabDistance + magObj.GetBoundingRadius();

        if (Vector3.Distance(magObj.transform.position, holdPoint.position) < grabThreshold)
        {
            ForceGrabObject(magObj);
        }
    }

    void HandleRightClickMelee(Vector3 aimOrigin, Vector3 aimDirection)
    {
        if (!Physics.Raycast(aimOrigin, aimDirection, out RaycastHit hit, dashLockRange)) return;
        if (!hit.collider.CompareTag("Player")) return;

        // Đọc điện tích găng tay của mục tiêu
        MagneticObject.Polarity targetPolarity;

        PlayerMagnetController enemyGlove = hit.collider.GetComponent<PlayerMagnetController>();
        DummyMagnetTarget dummyTarget = hit.collider.GetComponent<DummyMagnetTarget>();

        if (enemyGlove != null) targetPolarity = enemyGlove.currentGlovePolarity;
        else if (dummyTarget != null) targetPolarity = dummyTarget.currentGlovePolarity;
        else return;

        float distance = Vector3.Distance(transform.position, hit.transform.position);

        // TẦM 3-8m + TRÁI DẤU -> KÉO ÁP SÁT (Grapple)
        if (distance > meleeRange && currentGlovePolarity != targetPolarity)
        {
            Vector3 grappleDirection = (hit.transform.position - transform.position).normalized;

            // scaleByCharge = false: mình tự kéo mình về phía địch, không phải bị đẩy.
            // Nếu nhân theo điện tích thì người sắp thua sẽ lao vọt qua đầu đối thủ.
            if (movement != null) movement.AddImpact(grappleDirection, grapplePullForce, false);

            MeleeCooldownTimer = TickTimer.CreateFromSeconds(Runner, meleeCooldown);
            MeleeCount++;
            return;
        }

        // Ngoài tầm đấm mà cùng dấu thì không làm gì cả
        if (distance > meleeRange) return;

        // TẦM < 3m
        Vector3 pushDirection = (hit.transform.position - transform.position).normalized;
        pushDirection.y = 0.3f; // Hất tung nhẹ

        FPSMovement enemyMove = hit.collider.GetComponent<FPSMovement>();
        DummyGravity dummyGrav = hit.collider.GetComponent<DummyGravity>();

        if (currentGlovePolarity == targetPolarity)
        {
            Debug.Log("<color=red>CÙNG DẤU! Đấm văng đối thủ!</color>");

            if (enemyMove != null) enemyMove.AddImpact(pushDirection, meleePushForce);
            if (dummyGrav != null) dummyGrav.AddImpact(pushDirection, meleePushForce);
        }
        else
        {
            Debug.Log("<color=yellow>KHÁC DẤU! Nảy bật nhẹ!</color>");

            if (enemyMove != null) enemyMove.AddImpact(pushDirection, 15f);
            if (dummyGrav != null) dummyGrav.AddImpact(pushDirection, 15f);
            if (movement != null) movement.AddImpact(-pushDirection, 15f);
        }

        MeleeCooldownTimer = TickTimer.CreateFromSeconds(Runner, meleeCooldown);
        MeleeCount++;
    }

    // Đặt vật vào tay NGAY TẠI MÁY NÀY, mỗi khung hình, trên MỌI máy.
    //
    // Vì sao cần: khi đang cầm, vị trí của vật KHÔNG phải thứ cần truyền qua mạng.
    // Thứ duy nhất cần truyền là "người này đang cầm vật nào" - đã có GrabbedObjectId lo.
    // Trước đây chỉ Host đặt vị trí rồi truyền sang, nên ở máy Client bàn tay đi một đằng
    // (dự đoán tại chỗ, tức thì) còn vật đi một nẻo (dữ liệu mạng, trễ một vòng) -> nhìn rất dị.
    //
    // Dùng LateUpdate vì nó chạy sau mọi Update, nên vị trí đặt ở đây là vị trí cuối cùng
    // người chơi nhìn thấy trong khung hình đó.
    private void LateUpdate()
    {
        MagneticObject held = grabbedObject;

        // Vật vừa rời tay (bắn đi, tung lên, cất túi) -> trả lại kích thước gốc.
        // Phải làm ở đây thay vì trong các hàm bắn/tung, vì những hàm đó chỉ chạy trên Host
        // còn việc thu nhỏ thì máy nào cũng tự làm.
        if (_visuallyHeld != null && _visuallyHeld != held)
        {
            _visuallyHeld.RestoreScale();
            _visuallyHeld = null;
        }

        if (held == null || holdPoint == null) return;

        // Vừa cầm lên -> thu về cỡ chuẩn, làm đúng một lần
        if (_visuallyHeld != held)
        {
            _visuallyHeld = held;
            held.ApplyHeldScale(heldObjectSize, onlyShrinkLargeObjects);
        }

        held.transform.position = holdPoint.position;
        held.transform.rotation = holdPoint.rotation;
    }

    // Chỉ chạy trên Host, mỗi tick mạng. Nhiệm vụ giờ KHÁC với LateUpdate ở trên:
    //   - LateUpdate lo phần HÌNH ẢNH: đặt vật vào tay mượt mà ở từng máy, mỗi khung hình.
    //   - Hàm này lo phần TRẠNG THÁI MẠNG: giữ cho vị trí chính thức của vật bám theo tay,
    //     để lúc buông ra vật không bị nhảy giật về một chỗ khác.
    void KeepObjectInHand(Quaternion aimRotation)
    {
        Rigidbody rb = grabbedRb;
        if (rb == null) return;

        rb.isKinematic = true;

        grabbedObject.transform.position = holdPoint.position;
        grabbedObject.transform.rotation = aimRotation;
    }

    /// <summary>
    /// Chuẩn bị cho một vật rời tay: trả cỡ gốc, DỜI RA KHỎI NGƯỜI, rồi mới bật lại va chạm.
    ///
    /// VÌ SAO PHẢI DỜI RA - đây là nguyên nhân lỗi "tung V bị lệch ngẫu nhiên":
    ///
    /// Lúc cầm, vật bị thu nhỏ còn heldObjectSize (0.6m) nên nằm gọn ở holdPoint trước mặt.
    /// RestoreScale() làm nó phình lại cỡ thật NGAY TẠI CHỖ ĐÓ - một cái bàn 3m sẽ lồng
    /// xuyên qua người chơi. Ngay sau đó va chạm được bật lại và vật giao cho PhysX;
    /// PhysX thấy hai collider chồng nhau rất sâu nên bắn ra một LỰC GỠ KẸT (depenetration)
    /// để tách chúng ra.
    ///
    /// Lực gỡ kẹt đó lớn hơn lực tung nhiều lần, và hướng của nó phụ thuộc vào hình dạng
    /// chỗ chồng lấn - nên vật bay đi mỗi lần một kiểu.
    ///
    /// Bắn bằng chuột phải không lộ lỗi này vì pushForce (300) quá lớn, lực gỡ kẹt bị lấn át.
    /// Tung lên thì lực bé nên lỗi hiện ra rõ mồn một.
    /// </summary>
    private void PrepareForRelease(MagneticObject obj, Vector3 aimDirection)
    {
        // Trả cỡ gốc TRƯỚC, để đo được bán kính thật ở bước sau
        obj.RestoreScale();

        // Bán kính vật SAU khi đã phình lại cỡ thật.
        // Dùng GetBoundingRadius() thay cho bounds của collider vì bounds đổi theo góc xoay.
        float objRadius = obj.GetBoundingRadius();

        float playerRadius = 0.5f;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) playerRadius = cc.radius;

        // Đẩy vật ra trước mặt đủ xa để hai collider không còn chạm nhau.
        // Chỉ đẩy theo phương NGANG và giữ nguyên độ cao điểm cầm, nếu không thì
        // ngước lên trời tung vật sẽ làm nó xuất hiện ngay trên đầu.
        Vector3 flatAim = new Vector3(aimDirection.x, 0f, aimDirection.z);
        if (flatAim.sqrMagnitude < 0.0001f) flatAim = transform.forward;
        flatAim.Normalize();

        float safeDistance = playerRadius + objRadius + releaseClearance;
        float holdHeight = holdPoint != null ? holdPoint.position.y : transform.position.y + 1f;

        obj.transform.position =
            new Vector3(transform.position.x, holdHeight, transform.position.z) + flatAim * safeDistance;

        // GIỜ mới bật lại va chạm, khi vật đã đứng ở chỗ không chồng lấn với ai
        SetIgnoreCollisionWithPlayer(obj, false);
    }

    /// <summary>
    /// Bật/tắt va chạm giữa người chơi này và MỌI collider của vật thể.
    ///
    /// VÌ SAO PHẢI DUYỆT HẾT chứ không lấy một cái:
    /// trước đây chỗ này viết là GetComponent&lt;Collider&gt;() - số ít - và nó chỉ trả về
    /// collider ĐẦU TIÊN tìm thấy. Với vật thể một collider thì không sao, nhưng cây cối
    /// và đá dùng NHIỀU BoxCollider ghép lại (một cho thân, một cho vòm lá) để thay cho
    /// MeshCollider lõm - thứ mà Unity không cho đi cùng Rigidbody động.
    ///
    /// Sót một collider là dính lại đúng BẪY SỐ 8 trong CLAUDE.md: cái collider chưa được
    /// tắt nằm chồng trong người chơi, PhysX bắn ra lực gỡ kẹt để tách hai vật ra, và vật
    /// bay đi mỗi lần một hướng. Lần đó mất rất nhiều thời gian mới tìm ra thủ phạm.
    /// </summary>
    private void SetIgnoreCollisionWithPlayer(MagneticObject obj, bool ignore)
    {
        if (obj == null) return;

        // CharacterController cũng là một loại Collider nên nằm luôn trong danh sách này.
        //
        // Dùng InChildren để khớp với cachedColliders của MagneticObject (nó cũng quét
        // cả con). Nhờ vậy mai này có tách phần mesh ra GameObject con - chẳng hạn để
        // gắn hiệu ứng gió lay lá riêng - thì chỗ này không phải sửa lại.
        Collider[] playerCols = GetComponentsInChildren<Collider>();
        Collider[] objCols = obj.GetComponentsInChildren<Collider>();

        foreach (Collider pc in playerCols)
        {
            // Bỏ qua collider đang tắt: Unity báo lỗi đỏ nếu gọi IgnoreCollision lên
            // collider đã disable. Vật nằm trong túi bị tắt hết collider (ApplyStoredState).
            if (pc == null || !pc.enabled || pc.isTrigger) continue;

            foreach (Collider oc in objCols)
            {
                if (oc == null || !oc.enabled || oc.isTrigger) continue;

                Physics.IgnoreCollision(pc, oc, ignore);
            }
        }
    }

    void TossObjectUp(Vector3 aimDirection)
    {
        MagneticObject objToToss = grabbedObject;
        Rigidbody rbToToss = grabbedRb;
        if (objToToss == null || rbToToss == null) return;

        // Trả cỡ gốc + dời ra khỏi người + bật va chạm. Xem PrepareForRelease().
        PrepareForRelease(objToToss, aimDirection);

        // Giải phóng găng tay
        grabbedObject = null;

        // Trả lại vật lý
        rbToToss.isKinematic = false;
        rbToToss.useGravity = true;
        rbToToss.linearDamping = 0.05f;

        // ĐẶT THẲNG VẬN TỐC, không dùng AddForce.
        //
        // Hai cái lợi: (1) vật nặng vật nhẹ tung lên cao như nhau, (2) phép gán này
        // GHI ĐÈ mọi vận tốc rác còn sót lại, nên dù có lực gỡ kẹt nào lọt qua thì
        // cũng bị xoá sạch ngay tại đây.
        rbToToss.linearVelocity = Vector3.up * tossUpSpeed + aimDirection.normalized * tossForwardSpeed;
        rbToToss.angularVelocity = Vector3.zero;

        // Xoáy nhẹ cho đẹp mắt
        rbToToss.AddTorque(Random.insideUnitSphere * tossSpinStrength, ForceMode.Impulse);
    }

    void FireGrabbedObject(Vector3 aimDirection)
    {
        // Giữ tạm ra biến cục bộ trước khi buông tay, vì grabbedObject/grabbedRb
        // sẽ trả về null ngay sau khi xoá ID đi.
        MagneticObject objToFire = grabbedObject;
        Rigidbody rbToFire = grabbedRb;
        if (objToFire == null || rbToFire == null) return;

        // Trả cỡ gốc + dời ra khỏi người + bật va chạm, dùng chung hàm với tung V.
        //
        // Bắn thẳng vốn không lộ lỗi lệch hướng vì pushForce quá lớn, nhưng dùng chung
        // vẫn có lợi: hết cảnh vật to bị kẹt trong người rồi vọt ra sai đường.
        PrepareForRelease(objToFire, aimDirection);

        objToFire.CurrentDamage = originalBaseDamage;
        objToFire.LaunchAsBullet(this);

        // Buông tay
        grabbedObject = null;

        rbToFire.isKinematic = false;
        rbToFire.useGravity = true;
        rbToFire.linearDamping = 0f;

        Vector3 fireDirection = aimDirection;
        fireDirection.y = 0.05f;
        fireDirection.Normalize();

        rbToFire.AddForce(fireDirection * pushForce, ForceMode.Impulse);

        FireCount++; // để máy của người bắn rung camera, xem OnObjectFired
    }

    /// <summary>
    /// Buông vật đang cầm mà không bắn cũng không tung. GameManager gọi khi reset map
    /// đầu round mới, để vật không bị bàn tay giữ lại rồi kéo giật về.
    /// </summary>
    public void ReleaseGrabbedObject()
    {
        if (grabbedObject != null)
        {
            SetIgnoreCollisionWithPlayer(grabbedObject, false);

            grabbedObject.ResetBulletState();
        }

        if (grabbedRb != null)
        {
            grabbedRb.isKinematic = false;
            grabbedRb.useGravity = true;
            grabbedRb.linearDamping = 0f;
        }
        grabbedObject = null;
    }

    public void ForceGrabObject(MagneticObject targetObj)
    {
        if (grabbedObject != null)
        {
            ReleaseGrabbedObject();
        }

        Rigidbody targetRb = targetObj.GetComponent<Rigidbody>();
        if (targetRb == null) return;

        grabbedObject = targetObj;
        originalBaseDamage = targetObj.CurrentDamage;

        grabbedObject.transform.position = holdPoint.position;
        grabbedObject.transform.rotation = holdPoint.rotation;

        grabbedRb.isKinematic = true;
        grabbedRb.useGravity = false;
        grabbedRb.linearDamping = 10f;

        // TẮT VA CHẠM: Để vật thể không đè lên người chơi.
        // Phải tắt HẾT mọi collider của vật, xem ghi chú ở SetIgnoreCollisionWithPlayer().
        SetIgnoreCollisionWithPlayer(targetObj, true);
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi cú cận chiến
    private void OnMeleePerformed()
    {
        AudioManager.MeleePunch(transform.position);
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi phát bắn.
    // Bản thân FPSMovement.ShakeCamera() đã tự lọc để chỉ rung camera của chủ nhân vật,
    // nên gọi thẳng ở đây không sợ rung nhầm màn hình người khác.
    private void OnObjectFired()
    {
        if (movement != null) movement.ShakeCamera(fireShakeTrauma);
    }

    /// <summary>
    /// Rút Chai Xăng Tẩy Chế ra tay. Lần bấm chuột trái kế tiếp vào một vật thường
    /// sẽ biến nó thành thùng TNT.
    ///
    /// Trả về false nếu đang cầm sẵn một chai rồi, để không phí chai thứ hai.
    /// </summary>
    public bool EquipGasoline()
    {
        if (!HasStateAuthority) return false;
        if (HasGasolineEquipped) return false;

        HasGasolineEquipped = true;

        Debug.Log("<color=#FF6600>[CHAI XĂNG] Đã cầm chai xăng. Bấm chuột trái vào một vật thường để chế thành TNT.</color>");
        return true;
    }

    // --- CÁC HÀM GETTER ĐỂ INVENTORY GỌI ---
    public MagneticObject GetGrabbedObject()
    {
        return grabbedObject;
    }

    public void ClearGrabbedObjectWithoutReset()
    {
        grabbedObject = null;
    }
}
