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

    [Header("Juggling / Toss Settings")]
    public KeyCode tossKey = KeyCode.V;
    public float tossUpForce = 0.5f;
    public float tossForwardForce = 0.2f;

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
        targetRb.isKinematic = false;
        targetRb.useGravity = false;

        Vector3 pullDirection = (holdPoint.position - magObj.transform.position).normalized;
        targetRb.linearVelocity = pullDirection * pullForce;

        // Chỉ khi bay đến sát tay (< 0.7 mét) mới khóa cứng lại
        if (Vector3.Distance(magObj.transform.position, holdPoint.position) < 0.7f)
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
            if (movement != null) movement.AddImpact(grappleDirection, grapplePullForce);

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

    void TossObjectUp(Vector3 aimDirection)
    {
        MagneticObject objToToss = grabbedObject;
        Rigidbody rbToToss = grabbedRb;
        if (objToToss == null || rbToToss == null) return;

        // Trả cỡ gốc ngay, cùng lý do như FireGrabbedObject
        objToToss.RestoreScale();

        // Bật lại va chạm trước khi rời tay
        Collider playerCol = GetComponent<Collider>();
        Collider objCol = objToToss.GetComponent<Collider>();
        if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, false);

        // Giải phóng găng tay
        grabbedObject = null;

        // Trả lại vật lý
        rbToToss.isKinematic = false;
        rbToToss.useGravity = true;
        rbToToss.linearDamping = 0.05f;

        // Hất lên và xoay nhẹ
        Vector3 tossDirection = Vector3.up * tossUpForce + aimDirection * tossForwardForce;
        rbToToss.AddForce(tossDirection, ForceMode.Impulse);
        rbToToss.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
    }

    void FireGrabbedObject(Vector3 aimDirection)
    {
        // Giữ tạm ra biến cục bộ trước khi buông tay, vì grabbedObject/grabbedRb
        // sẽ trả về null ngay sau khi xoá ID đi.
        MagneticObject objToFire = grabbedObject;
        Rigidbody rbToFire = grabbedRb;
        if (objToFire == null || rbToFire == null) return;

        // Trả cỡ gốc NGAY, không đợi LateUpdate. Vật lý chạy trong tick mạng vốn xảy ra
        // trước LateUpdate, nên chậm một nhịp là vật bay với collider bé tí một khoảnh khắc.
        objToFire.RestoreScale();

        objToFire.CurrentDamage = originalBaseDamage;
        objToFire.LaunchAsBullet(this);

        // Bật lại va chạm trước khi bắn
        Collider playerCol = GetComponent<Collider>();
        Collider objCol = objToFire.GetComponent<Collider>();
        if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, false);

        // Buông tay
        grabbedObject = null;

        rbToFire.isKinematic = false;
        rbToFire.useGravity = true;
        rbToFire.linearDamping = 0f;

        Vector3 fireDirection = aimDirection;
        fireDirection.y = 0.05f;
        fireDirection.Normalize();

        rbToFire.AddForce(fireDirection * pushForce, ForceMode.Impulse);
    }

    void ReleaseGrabbedObject()
    {
        if (grabbedObject != null)
        {
            Collider playerCol = GetComponent<Collider>();
            Collider objCol = grabbedObject.GetComponent<Collider>();
            if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, false);

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

        // TẮT VA CHẠM: Để vật thể không đè lên người chơi
        Collider playerCol = GetComponent<Collider>();
        Collider objCol = targetObj.GetComponent<Collider>();
        if (playerCol != null && objCol != null) Physics.IgnoreCollision(playerCol, objCol, true);
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi cú cận chiến
    private void OnMeleePerformed()
    {
        AudioManager.MeleePunch(transform.position);
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
