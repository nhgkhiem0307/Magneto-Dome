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

    public override void Spawned()
    {
        movement = GetComponent<FPSMovement>();

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
            magObj.isMovingAsBullet = true;
            magObj.shooterOwner = this;

            Vector3 pushDirection = aimDirection;
            pushDirection.y = 0f;
            pushDirection.Normalize();

            targetRb.linearVelocity = Vector3.zero;
            targetRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            return;
        }

        // TRÁI DẤU -> HÚT về phía mình
        //
        // Ghi chú thiết kế (30/07): tạm BỎ hết luật riêng của từng loại vật thể.
        // Trước đây Spike bị hút nhầm lúc đang bay thì x2 sát thương, còn Heavy thì
        // không hút được khi đang bay và bị giảm 50% lực. Giờ cả 3 loại
        // Normal / Heavy / Spike hút đẩy y hệt nhau, chỉ khác con số sát thương.
        // Sẽ thêm lại các luật này sau.

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
        if (holdPoint == null) return;

        MagneticObject held = grabbedObject;
        if (held == null) return;

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

        objToFire.isMovingAsBullet = true;
        objToFire.shooterOwner = this;
        objToFire.CurrentDamage = originalBaseDamage;

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
