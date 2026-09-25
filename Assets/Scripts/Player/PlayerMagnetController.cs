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

    [Tooltip("Tốc độ ĐẨY vật thể trong môi trường ra xa (chuột trái, cùng dấu), mét/giây. " +
             "Là VẬN TỐC chứ không phải lực, nên vật nặng vật nhẹ bay đi như nhau. " +
             "KHÔNG dùng cho cú bắn vật đang cầm - cái đó dùng fireSpeed bên dưới.")]
    public float pushSpeed = 125f;

    [Tooltip("Độ chếch LÊN của cú đẩy. MẶC ĐỊNH LÀ 0 - CỐ Ý.\n\n" +
             "Trọng lực project chỉ -3 nên quãng đường để vật rơi lại độ cao ban đầu là " +
             "d = 2*v_doc*v_ngang/g. Với pushSpeed 125 và chếch 0.18 thì d = 906m, trong khi " +
             "map chỉ ~56m - vật chưa bao giờ kịp rơi xuống, nó chỉ leo lên rồi bay qua đầu địch.\n\n" +
             "Việc vượt gờ đất do Push Lift Offset lo. Đừng dùng ô này cho việc đó.")]
    [Range(0f, 1f)]
    public float pushUpwardBias = 0f;

    [Tooltip("Nâng vật lên bao nhiêu mét trước khi đẩy. ĐÂY MỚI LÀ THỨ GIÚP VƯỢT GỜ ĐẤT: " +
             "nâng lên tầm ngực rồi bắn ngang thì mọi gờ thấp hơn mức này đều không chạm " +
             "tới, mà đạn vẫn ở đúng tầm trúng người suốt quãng đường.")]
    public float pushLiftOffset = 0.45f;

    [Tooltip("Tốc độ BẮN vật đang cầm trên tay, tính bằng mét/giây. " +
             "Đây là VẬN TỐC chứ không phải lực, nên vật nặng vật nhẹ đều bay đi như nhau. " +
             "Cao hơn = đường đạn thẳng hơn, ít rơi hơn, dễ ngắm hơn.")]
    public float fireSpeed = 45f;

    [Tooltip("BẬT: đường đạn luôn nằm ngang, bất kể đang ngắm cao hay thấp. Dễ đoán nhất. " +
             "TẮT: bắn đúng theo hướng đang nhìn, ngắm được lên xuống nhưng khó canh hơn.")]
    public bool lockFireToHorizontal = true;

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

    [Tooltip("Bề NGANG tối đa của vật cầm trên tay, tính bằng mét. Đây là ô chữa lỗi " +
             "'cầm đá thì che hết màn hình, cầm cây thì không sao'.\n\n" +
             "⚠️ VÌ SAO CẦN THÊM Ô NÀY: ô trên chỉ thu CẠNH DÀI NHẤT về một cỡ. Cái cây dài " +
             "3m x dày 0.3m thu về dài 1.2m thì bề ngang chỉ còn 0.12m - mảnh như que, che " +
             "gần như không đáng kể. Nhưng tảng đá thì ba chiều gần bằng nhau, nên thu cạnh " +
             "dài nhất về 1.2m là ra một khối vuông 1.2m lơ lửng ngay trước mũi.\n\n" +
             "Cùng một con số 'cạnh dài nhất' mà vật dẹt và vật vuông che màn hình khác nhau " +
             "một trời một vực - thứ quyết định độ che là BỀ NGANG, không phải chiều dài.\n\n" +
             "Code lấy hệ số thu NHỎ HƠN trong hai ô, nên vật dài mảnh vẫn giữ nguyên như " +
             "cũ, chỉ vật mập mới bị thu thêm. Để 0 là tắt, quay về cách cũ.")]
    public float heldObjectThickness = 0.55f;

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

    [Tooltip("Vật đang bị HÚT về tay cũng tính là đạn, gây sát thương cho ai đứng chắn " +
             "giữa bạn và nó.\n\n" +
             "Không hại chính mình: MagneticObject đã có sẵn dòng bỏ qua va chạm với người " +
             "bắn ra nó, nên vật về tới tay là lọt vào tay chứ không đập vào mặt.\n\n" +
             "⚠️ ẢNH HƯỞNG TỚI THÙNG TNT: TNT nổ khi va vào BẤT CỨ THỨ GÌ. Hút một thùng TNT " +
             "về mà nó quệt phải gốc cây giữa đường thì nó nổ ngay cạnh bạn. Đây là hệ quả " +
             "thật, không phải lỗi - nhưng nếu thấy quá khắc nghiệt thì tắt ô này.")]
    public bool pullDealsDamage = true;

    [Header("Grapple - Kéo áp sát")]
    [Tooltip("Lực kéo bản thân bay về phía đối thủ khi cận chiến ở tầm 3-8m và trái dấu điện tích.")]
    public float grapplePullForce = 100f;

    [Header("Hỗ trợ ngắm cận chiến")]
    [Tooltip("Bán kính vùng ngắm rộng, tính bằng mét. Tia thẳng trượt thì mới quét tới vùng này.\n\n" +
             "Đặt 0 để tắt hẳn, quay lại kiểu bắt buộc ngắm chính xác từng pixel.\n\n" +
             "1.2 ≈ rộng bằng một thân người. Đừng để quá 2 - rộng hơn thế thì grapple " +
             "trúng cả người đứng lệch hẳn sang bên, cảm giác như game tự chơi hộ.")]
    public float aimAssistRadius = 1.2f;

    [Tooltip("Mục tiêu được phép lệch khỏi tâm ngắm tối đa bao nhiêu ĐỘ.\n\n" +
             "Đây là cái hãm quan trọng nhất. Bán kính ở trên là con số cố định theo mét, " +
             "nên ở khoảng cách gần nó ứng với một góc rất rộng - địch đứng ngay bên hông " +
             "cũng lọt vào. Giới hạn theo góc chặn đúng chỗ đó lại.")]
    public float aimAssistAngle = 12f;

    [Tooltip("Góc hỗ trợ riêng cho CỰ LY GẦN (trong tầm đấm). Rộng hơn hẳn góc ở trên.\n\n" +
             "⚠️ Vì sao phải có góc riêng: giới hạn 12 độ ở trên đúng cho tầm xa nhưng vô " +
             "dụng ở tầm đấm. Địch đứng cách 1.5m mà lệch sang bên 0.4m đã là 15 độ - trượt. " +
             "Mà 0.4m ở khoảng cách đó thì hai người gần như đang chạm vào nhau.\n\n" +
             "Góc lệch không phải thước đo tốt ở cự ly gần: cùng một khoảng lệch bên hông, " +
             "càng lại gần thì góc càng phình to. 70 độ nghe rất rộng nhưng ở 1.5m nó chỉ " +
             "ứng với khoảng 4m bề ngang - và bước quét này vốn đã bị chặn trong tầm đấm rồi.")]
    [Range(10f, 120f)]
    public float closeAssistAngle = 70f;

    [Header("Rung camera")]
    [Tooltip("Độ mạnh cú rung khi bắn vật đang cầm đi, thang 0..1. Đặt 0 để tắt.")]
    public float fireShakeTrauma = 0.4f;

    [Tooltip("Cú GIẬT khi bắn vật. 1 = đúng bằng Kick Angles trên CameraShake.\n\n" +
             "Đây mới là thứ tạo cảm giác dứt khoát; ô Fire Shake Trauma ở trên chỉ là " +
             "phần xóc nền. Đặt 0 để tắt riêng phần giật.")]
    public float fireKickStrength = 1f;

    [Tooltip("Độ mạnh cú rung khi đấm cận chiến, thang 0..1.")]
    public float meleeShakeTrauma = 0.45f;

    [Tooltip("Cú GIẬT khi đấm cận chiến.\n\n" +
             "Để mạnh hơn lúc bắn vì đấm là dồn cả người vào cú đánh, còn bắn thì chỉ có " +
             "vật thể rời khỏi tay. Chênh lệch này là thứ giúp phân biệt hai đòn qua cảm giác.")]
    public float meleeKickStrength = 1.3f;

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

    /// <summary>
    /// Đếm số lần VUNG TAY - khác hẳn MeleeCount là số lần ĐẤM TRÚNG.
    ///
    /// ⚠️ VÌ SAO PHẢI TÁCH RA (thêm 09/09):
    /// HandleRightClickMelee() thoát ngay ở dòng đầu khi không tìm thấy mục tiêu, nên
    /// MeleeCount chỉ tăng lúc đấm TRÚNG ai đó. Đấm vào không khí thì tuyệt đối không có
    /// gì xảy ra: không tiếng, không rung camera, không animation, không cả cử động tay.
    ///
    /// Nghĩa là test một mình trong scene không có đối thủ thì KHÔNG BAO GIỜ thấy cú đấm.
    /// Bắn laze thì thấy, vì quanh map lúc nào cũng có vật để bắn.
    ///
    /// Mọi game bắn súng đều tách hai thứ này: vung tay là thứ NGƯỜI ĐẤM luôn thấy, còn
    /// tiếng đấm và rung camera là phản hồi CHỈ có khi chạm được vào ai.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnMeleeSwing))]
    private int MeleeSwingCount { get; set; }

    /// <summary>
    /// Chỗ vừa đấm trúng, và cú đó là đấm hay grapple.
    ///
    /// ⚠️ MeleeCount chỉ báo "vừa trúng", không nói trúng Ở ĐÂU - máy khác không có cách
    /// nào biết mà vẽ quyền khí bay tới. Dùng TOẠ ĐỘ chứ không dùng ID như tia laze, vì
    /// bù nhìn tập đấm (DummyMagnetTarget) là MonoBehaviour thường, không có ID mạng.
    ///
    /// Cần cờ grapple vì grapple CŨNG tăng MeleeCount. Không tách ra thì mỗi lần kéo áp
    /// sát lại có một luồng quyền khí bay xa 8 mét, trông vô lý.
    /// </summary>
    [Networked] private Vector3 MeleeHitPoint { get; set; }
    [Networked] private NetworkBool MeleeWasGrapple { get; set; }

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

    // Đếm số lần bắn tia laze vào một vật CHƯA CÓ ĐIỆN, để mọi máy phát tiếng laze.
    //
    // Cùng lý do với FireCount: HandleLeftClickMagnet() nằm sau dòng
    // "if (!HasStateAuthority) return;" nên chỉ chạy trên Host. Gọi AudioManager thẳng
    // trong đó thì chỉ mình Host nghe thấy, người chơi ở máy Client bắn mà im lặng.
    //
    // Tiếng này KHÁC với sfxCharge của MagneticObject, cố ý để cả hai cùng kêu:
    //   - sfxLaser  : phát ra ở TAY người bắn, là tiếng cây súng
    //   - sfxCharge : phát ra ở CHỖ VẬT THỂ, là tiếng vật nhiễm điện
    // Hai nguồn ở hai vị trí khác nhau nên tai nghe ra được khoảng cách tới mục tiêu.
    [Networked, OnChangedRender(nameof(OnLaserFired))]
    private int LaserCount { get; set; }

    /// <summary>
    /// Vật vừa bị bắn laze, để mọi máy biết vẽ tia TỚI ĐÂU.
    ///
    /// ⚠️ Bộ đếm LaserCount chỉ nói "vừa bắn", không nói "trúng chỗ nào". Thiếu ô này thì
    /// mỗi máy phải tự raycast lại, mà vật có thể đã nhúc nhích - hai máy vẽ tia đi hai
    /// hướng khác nhau.
    ///
    /// Gửi ID thay vì toạ độ vì rẻ hơn (4 byte so với 12) và tia còn BÁM ĐƯỢC vật nếu nó
    /// đang bay.
    /// </summary>
    [Networked] private NetworkBehaviourId LaserTargetId { get; set; }

    /// <summary>
    /// Đếm số lần ĐẨY vật đi bằng chuột trái (cùng dấu).
    ///
    /// ⚠️ Nhánh đẩy trong HandleLeftClickMagnet() trước đây KHÔNG có bộ đếm nào cả - nó
    /// đặt vận tốc cho vật rồi return. Nên đây là hành động duy nhất của găng tay không
    /// hề có phản hồi nào về phía người chơi: không tiếng, không rung, không cử động tay.
    /// Mà đẩy vật lại chính là đòn tấn công cơ bản nhất của game.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnObjectPushed))]
    private int PushCount { get; set; }

    /// <summary>
    /// Đang giữ chuột hút một vật về tay.
    ///
    /// Khác ba bộ đếm ở trên: đây là TRẠNG THÁI LIÊN TỤC, không phải sự kiện một nhịp.
    /// Viewmodel đọc ô này để giữ tư thế hai tay kéo về ngực suốt thời gian hút.
    ///
    /// [Networked] vì nhánh hút chỉ chạy trên Host - Client cần biết để tạo dáng tay.
    /// </summary>
    [Networked] public NetworkBool IsPulling { get; set; }

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

    // Viewmodel g\u00f3c nh\u00ecn th\u1ee9 nh\u1ea5t. Ch\u1ec9 kh\u00e1c null tr\u00ean m\u00e1y c\u1ee7a ch\u1ee7 nh\u00e2n v\u1eadt.
    private FirstPersonViewmodel viewmodel;
    private PlayerHealth health;
    private PlayerAnimatorDriver animatorDriver;

    // Vật đang bị THU NHỎ ở máy này. Khác với grabbedObject ở chỗ nó là trạng thái
    // hình ảnh cục bộ, dùng để biết lúc nào cần trả lại kích thước gốc.
    private MagneticObject _visuallyHeld;

    public override void Spawned()
    {
        movement = GetComponent<FPSMovement>();
        viewmodel = GetComponent<FirstPersonViewmodel>();
        health = GetComponent<PlayerHealth>();
        animatorDriver = GetComponent<PlayerAnimatorDriver>();

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

        // CHỈ ĐỂ TEST: phím K tự sát để thử vòng lặp round một mình. Chỉ có trong Editor.
        //
        // Bọc cả ở đầu NHẬN (Host), không chỉ đầu gửi: lỡ có ai sửa bản build để gửi
        // lệnh này thì Host của bản build cũng không có dòng code nào thực hiện nó.
#if UNITY_EDITOR
        if (HasStateAuthority && pressed.IsSet((int)InputButton.DebugSuicide) && health != null)
        {
            health.Die();
        }
#endif

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

        // XOÁ CỜ HÚT ở đầu mỗi tick. Nhánh hút bên dưới sẽ bật lại nếu còn đang hút.
        //
        // Phải xoá chủ động thế này chứ không tìm chỗ "khi thôi hút" để tắt: người chơi
        // thôi hút bằng RẤT NHIỀU đường - nhả chuột, vật vào tay, vật bị cất túi, quay
        // mặt đi chỗ khác, chết. Bắt hết từng đường thì chắc chắn sót một cái, mà sót là
        // tay kẹt tư thế hút vĩnh viễn.
        IsPulling = false;

        // ĐANG CHOÁNG -> không đánh đấm gì được.
        //
        // Đặt SAU phần xoay người và tính hướng ngắm ở trên, để người bị choáng vẫn ngó
        // nghiêng được. Cướp quyền nhìn là thứ gây ức chế nhất trong game bắn súng.
        //
        // Cố ý KHÔNG buông vật đang cầm: đang giơ cái bàn lên thì ăn một cú đấm, làm rơi
        // luôn cái bàn nghe hợp lý nhưng chơi thì bực - mất cả công đi nhặt. Choáng đã là
        // hình phạt đủ rồi.
        if (movement != null && movement.IsStunned)
        {
            if (grabbedObject != null) KeepObjectInHand(aimRotation);

            PreviousButtons = input.Buttons;
            return;
        }

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
            // Tăng TRƯỚC khi gọi, vì HandleRightClickMelee thoát ngay khi trượt. Đây là
            // "đã vung tay", trúng hay trượt tính sau.
            MeleeSwingCount++;

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
            if (justPressed)
            {
                magObj.SetPolarity(currentGlovePolarity);

                // Ghi mục tiêu TRƯỚC khi tăng bộ đếm. Fusion gửi cả gói trạng thái cùng
                // một tick, nhưng đặt đúng thứ tự thì đọc code không phải đoán.
                LaserTargetId = magObj.Id;

                // Tiếng tia laze bắn ra từ găng. Đặt TRONG nhánh justPressed nên chỉ kêu
                // đúng một lần lúc bấm xuống, không kêu liên tục khi giữ chuột.
                LaserCount++;
            }
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

            // NÂNG VẬT LÊN TẦM NGỰC RỒI BẮN NGANG. Sửa 16/08.
            //
            // Vấn đề gốc: vật nằm trên mặt đất bị đẩy ngang tuyệt đối sẽ trượt lê trên nền,
            // và mọi gờ đất phía trước là một bức tường ngay tầm của nó -> đâm vào là dừng.
            //
            // ĐÃ THỬ VÀ BỎ: chếch đường bay lên 10 độ. Nó vượt được gờ đất thật, nhưng
            // với pushSpeed 50 và trọng lực hiệu dụng -6 thì đạn đạt đỉnh ở tận 72m -
            // tức là trên map này nó CHỈ ĐI LÊN. Ở khoảng cách 20m đạn đã cao 3m, bay qua
            // đầu địch. Đứng xa là an toàn tuyệt đối. Đổi lỗi này lấy lỗi khác.
            //
            // Cách đúng: nâng ĐIỂM XUẤT PHÁT lên tầm ngực, rồi bắn gần như nằm ngang.
            // Bay ngang ở độ cao 0.9m thì mọi gờ thấp hơn mức đó đều không chạm tới,
            // mà đạn vẫn ở đúng tầm trúng người suốt quãng đường.
            targetRb.position += Vector3.up * pushLiftOffset;

            Vector3 pushDirection = aimDirection;
            pushDirection.y = pushUpwardBias;
            pushDirection.Normalize();

            // ĐẶT THẲNG VẬN TỐC thay cho AddForce, cùng lý do như cú bắn và cú tung:
            // AddForce chia cho khối lượng, nên prefab Heavy (mass 3-5) sẽ bị đẩy chậm
            // bằng 1/5 và dừng ngay tại chỗ.
            targetRb.linearVelocity = pushDirection * pushSpeed;
            targetRb.angularVelocity = Vector3.zero;

            PushCount++; // để tay người đẩy vung ra, xem OnObjectPushed
            return;
        }

        // TRÁI DẤU -> HÚT về phía mình

        // BÓNG NẶNG ĐANG BAY: không hút về tay được, nhưng CẢN lại được.
        // Đây là nước phòng thủ: bạn không cướp được vật, nhưng làm nó chậm và yếu đi.
        // ⚠️ Điều kiện "shooterOwner != this" LÀ BẮT BUỘC từ khi bật pullDealsDamage.
        //
        // Vì hút cũng đánh dấu vật là đạn, nên nếu không loại trừ vật của CHÍNH MÌNH thì:
        // tick đầu bạn bắt đầu hút Heavy -> nó thành đạn -> tick thứ hai rơi vào đúng
        // nhánh này -> "không hút về tay được" -> bạn không bao giờ hút nổi Heavy nữa.
        //
        // Về mặt luật chơi thì đây cũng là điều đúng: không ai đi cản đạn của chính mình.
        if (magObj.CurrentType == MagneticObject.ObjectType.Heavy
            && magObj.isMovingAsBullet
            && magObj.shooterOwner != this)
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
        // Cũng phải loại trừ vật của chính mình, cùng lý do với nhánh Heavy ở trên -
        // mà ở đây hậu quả còn nặng hơn: không có dòng này thì hút Spike một cái là
        // TỰ PHẠT CHÍNH MÌNH x2 sát thương ngay tick thứ hai, dù chẳng hút nhầm gì cả.
        if (magObj.CurrentType == MagneticObject.ObjectType.Spike
            && magObj.isMovingAsBullet
            && magObj.shooterOwner != this)
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

        // VẬT ĐANG HÚT CŨNG LÀ ĐẠN: ai đứng chắn giữa bạn và nó thì lãnh đủ.
        //
        // An toàn cho chính người hút nhờ dòng chặn sẵn có trong MagneticObject:
        // "bỏ qua va chạm với chính người bắn ra nó" - mà MarkAsPulledBullet đặt
        // shooterOwner = mình, nên vật về tới tay không hề trừ điện của mình.
        if (pullDealsDamage) magObj.MarkAsBullet(this);

        // Báo cho viewmodel biết đang hút, để nó giữ tư thế hai tay kéo về ngực.
        //
        // Đặt ở đây - trong nhánh HÚT MƯỢT - chứ không đặt ở đầu HandleLeftClickMagnet:
        // đầu hàm còn có nhánh nạp điện, đẩy vật, cản Heavy, hút nhầm Spike. Bật cờ ở đó
        // thì tay làm tư thế hút cả khi đang đẩy vật ra xa, ngược hẳn hành động.
        IsPulling = true;

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

    /// <summary>
    /// Tìm mục tiêu cho chuột phải (đấm hoặc Grapple), có hỗ trợ ngắm.
    ///
    /// HAI BƯỚC, thứ tự quan trọng:
    ///   1. Tia thẳng đúng tâm ngắm. Ngắm chuẩn thì LUÔN thắng, không bị hỗ trợ ngắm
    ///      cướp mất mục tiêu và kéo sang một người khác đứng gần đó.
    ///   2. Trượt rồi mới quét vùng rộng hơn, chọn người LỆCH TÂM NGẮM ÍT NHẤT.
    ///
    /// Vì sao dùng SphereCast chứ không duyệt PlayerHealth.AllPlayers: bù nhìn tập bắn
    /// (DummyMagnetTarget) không nằm trong danh sách đó. Quét theo hình học thì người thật
    /// và bù nhìn đều bắt được, không phải viết hai đường riêng.
    /// </summary>
    /// <summary>
    /// Tìm đối thủ đang đứng trong tầm đấm, chọn người gần tâm ngắm nhất.
    ///
    /// Dùng góc rộng closeAssistAngle chứ không dùng aimAssistAngle: xem ghi chú ở ô đó.
    /// </summary>
    Transform FindClosestInSphere(Vector3 aimOrigin, Vector3 aimDirection)
    {
        Collider[] hits = Physics.OverlapSphere(aimOrigin, meleeRange);

        Transform best = null;
        float bestAngle = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag("Player")) continue;
            if (hit.transform == transform) continue;

            // Đồng đội không phải mục tiêu. Góc hỗ trợ ở đây rộng tới 70°, nên đồng đội
            // đứng sát bên thường lại là người "gần tâm ngắm nhất" - nhắm vào địch mà cú
            // đấm bẻ sang đồng đội. Xem PlayerHealth.AreTeammates.
            if (IsTeammate(hit.transform)) continue;

            Vector3 toTarget = hit.transform.position - aimOrigin;
            if (toTarget.sqrMagnitude < 0.0001f) continue;

            float angle = Vector3.Angle(aimDirection, toTarget);
            if (angle > closeAssistAngle) continue;

            // Không cho đấm xuyên tường. Bỏ qua vật cản là chính mục tiêu hoặc chính mình -
            // tia xuất phát từ camera nên nó nằm ngay trong người mình.
            if (Physics.Linecast(aimOrigin, hit.bounds.center, out RaycastHit blocker)
                && blocker.transform != hit.transform
                && blocker.transform != transform)
            {
                continue;
            }

            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = hit.transform;
            }
        }

        return best;
    }

    /// <summary>Người này có phải đồng đội của mình không (mình thì không tính).</summary>
    private bool IsTeammate(Transform other)
    {
        return other != null && PlayerHealth.AreTeammates(gameObject, other.gameObject);
    }

    Transform FindMeleeTarget(Vector3 aimOrigin, Vector3 aimDirection)
    {
        // BƯỚC 0 - CỰ LY CỰC GẦN, quét chồng lấn
        //
        // ⚠️ BẮT BUỘC PHẢI CÓ BƯỚC RIÊNG NÀY. Physics.SphereCastAll ở bước 2 KHÔNG BÁO
        // những collider đã chồng lấn quả cầu NGAY TỪ ĐẦU đường quét. Quả cầu bán kính
        // 1.2m xuất phát từ camera, nên mọi đối thủ đứng gần hơn 1.2m đều rơi đúng vào
        // vùng mù đó - không bao giờ được trả về.
        //
        // Tức là hỗ trợ ngắm hỏng đúng ở khoảng cách mà cú đấm hay xảy ra nhất. Càng áp
        // sát càng vô dụng, ngược hẳn với thứ người chơi mong đợi.
        //
        // OverlapSphere không có vùng mù đó vì nó hỏi "có gì đang nằm trong quả cầu này",
        // không phải "quả cầu quét qua trúng gì".
        Transform close = FindClosestInSphere(aimOrigin, aimDirection);
        if (close != null) return close;

        // BƯỚC 1 - tia thẳng, ưu tiên tuyệt đối
        // Tia trúng thẳng đồng đội thì bỏ qua, xuống bước 2 tìm địch quanh đó.
        if (Physics.Raycast(aimOrigin, aimDirection, out RaycastHit precise, dashLockRange)
            && precise.collider.CompareTag("Player")
            && !IsTeammate(precise.transform))
        {
            return precise.transform;
        }

        if (aimAssistRadius <= 0f) return null;

        // BƯỚC 2 - quét hình trụ rộng hơn
        //
        // Dùng SphereCastAll chứ không SphereCast: SphereCast chỉ trả về vật CHẠM ĐẦU TIÊN,
        // mà thứ chạm đầu tiên thường là mặt đất hay thân cây nằm chệch sang bên. Lấy hết
        // rồi tự lọc thì mới tìm được người đứng sau mấy thứ vụn vặt đó.
        RaycastHit[] candidates = Physics.SphereCastAll(aimOrigin, aimAssistRadius, aimDirection, dashLockRange);

        Transform best = null;
        float bestAngle = float.MaxValue;

        foreach (RaycastHit candidate in candidates)
        {
            if (!candidate.collider.CompareTag("Player")) continue;

            // Không tự ngắm chính mình. Quả cầu quét bắt đầu ngay trong người nên
            // collider của bản thân gần như chắc chắn nằm trong danh sách trả về.
            if (candidate.transform == transform) continue;

            // Không grapple về phía đồng đội.
            if (IsTeammate(candidate.transform)) continue;

            Vector3 toTarget = candidate.transform.position - aimOrigin;

            float angle = Vector3.Angle(aimDirection, toTarget);
            if (angle > aimAssistAngle) continue;

            // Có tường chắn giữa không? Không cho grapple xuyên vách.
            //
            // Chấp nhận hai trường hợp: tia thông suốt tới đúng mục tiêu, hoặc nó chạm
            // vào chính mình trước (hay xảy ra vì tia xuất phát từ camera, nằm trong người).
            Vector3 targetCenter = candidate.collider.bounds.center;
            if (Physics.Linecast(aimOrigin, targetCenter, out RaycastHit blocker)
                && blocker.transform != candidate.transform
                && blocker.transform != transform)
            {
                continue;
            }

            // Chọn theo GÓC LỆCH, không chọn theo khoảng cách gần nhất.
            // Chọn theo khoảng cách thì người đứng sát bên hông sẽ luôn thắng người đang
            // nằm đúng giữa tâm ngắm ở xa hơn - trái hẳn ý định người chơi.
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = candidate.transform;
            }
        }

        return best;
    }

    void HandleRightClickMelee(Vector3 aimOrigin, Vector3 aimDirection)
    {
        Transform target = FindMeleeTarget(aimOrigin, aimDirection);
        if (target == null) return;

        // Đọc điện tích găng tay của mục tiêu
        MagneticObject.Polarity targetPolarity;

        PlayerMagnetController enemyGlove = target.GetComponent<PlayerMagnetController>();
        DummyMagnetTarget dummyTarget = target.GetComponent<DummyMagnetTarget>();

        if (enemyGlove != null) targetPolarity = enemyGlove.currentGlovePolarity;
        else if (dummyTarget != null) targetPolarity = dummyTarget.currentGlovePolarity;
        else return;

        float distance = Vector3.Distance(transform.position, target.position);

        // TẦM 3-8m + TRÁI DẤU -> KÉO ÁP SÁT (Grapple)
        if (distance > meleeRange && currentGlovePolarity != targetPolarity)
        {
            Vector3 grappleDirection = (target.position - transform.position).normalized;

            // scaleByCharge = false: mình tự kéo mình về phía địch, không phải bị đẩy.
            // Nếu nhân theo điện tích thì người sắp thua sẽ lao vọt qua đầu đối thủ.
            if (movement != null) movement.AddImpact(grappleDirection, grapplePullForce, false);

            MeleeCooldownTimer = TickTimer.CreateFromSeconds(Runner, meleeCooldown);

            // Ghi TRƯỚC khi tăng bộ đếm, cùng khuôn với LaserTargetId.
            MeleeHitPoint = TargetCenter(target);
            MeleeWasGrapple = true;

            MeleeCount++;
            return;
        }

        // Ngoài tầm đấm mà cùng dấu thì không làm gì cả
        if (distance > meleeRange) return;

        // TẦM < 3m
        Vector3 pushDirection = (target.position - transform.position).normalized;
        pushDirection.y = 0.3f; // Hất tung nhẹ

        FPSMovement enemyMove = target.GetComponent<FPSMovement>();
        DummyGravity dummyGrav = target.GetComponent<DummyGravity>();

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

        MeleeHitPoint = TargetCenter(target);
        MeleeWasGrapple = false;

        MeleeCount++;
    }

    /// <summary>Tâm thân người trúng đòn - để luồng khí đập vào ngực chứ không vào chân.</summary>
    private static Vector3 TargetCenter(Transform t)
    {
        Collider c = t.GetComponentInChildren<Collider>();
        // ⚠️ KHÔNG LẤY TÂM HỘP VA CHẠM. Hộp của CharacterController bọc từ chân tới đầu,
        // nên tâm của nó rơi ngang HÔNG - luồng khí đập vào bụng dưới, trông thấp hẳn.
        // Nhích lên 40% nửa chiều cao là tới ngực, đúng chỗ mắt chờ cú đấm trúng.
        if (c != null) return c.bounds.center + Vector3.up * c.bounds.extents.y * 0.4f;
        return t.position + Vector3.up * 1.4f;
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
            held.ApplyHeldScale(heldObjectSize, heldObjectThickness, onlyShrinkLargeObjects);
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
    /// Bắn bằng chuột phải không lộ lỗi này vì tốc độ bắn quá lớn, lực gỡ kẹt bị lấn át.
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
        // Bắn thẳng vốn không lộ lỗi lệch hướng vì tốc độ quá lớn, nhưng dùng chung
        // vẫn có lợi: hết cảnh vật to bị kẹt trong người rồi vọt ra sai đường.
        PrepareForRelease(objToFire, aimDirection);

        objToFire.CurrentDamage = originalBaseDamage;
        objToFire.LaunchAsBullet(this);

        // Buông tay
        grabbedObject = null;

        rbToFire.isKinematic = false;
        rbToFire.useGravity = true;
        rbToFire.linearDamping = 0f;

        Vector3 fireDirection = aimDirection.normalized;

        // KHOÁ ĐƯỜNG ĐẠN VỀ PHƯƠNG NGANG (chủ ý thiết kế của chủ project).
        //
        // Ép y = 0 CHÍNH XÁC, không phải 0.05 như bản cũ. Con số 0.05 nghe như bằng không
        // nhưng sau khi Normalize() nó thành một góc chếch lên ~3 độ - đủ để vật bay vồng
        // lên rồi rơi xuống theo đường cong, khiến điểm chạm đất khó đoán.
        // Bằng 0 tuyệt đối thì vật rời tay ở đúng độ cao đó và giữ nguyên phương ngang.
        if (lockFireToHorizontal)
        {
            fireDirection.y = 0f;

            // Ngắm thẳng đứng lên trời thì thành phần ngang bằng 0, không có hướng nào
            // để bắn - lấy tạm hướng thân người để khỏi chia cho 0.
            if (fireDirection.sqrMagnitude < 0.0001f) fireDirection = transform.forward;

            fireDirection.Normalize();
        }

        // ĐẶT THẲNG VẬN TỐC, không dùng AddForce.
        //
        // AddForce(..., Impulse) cho ra Δv = lực / khối lượng. Hiện mọi prefab đạn đều
        // mass = 1 nên chưa lộ, nhưng prefab Heavy sắp tới sẽ đặt mass 3-5 - lúc đó cùng
        // một cú bắn sẽ khiến nó bay chậm bằng 1/5 và rơi xuống đất ngay trước mặt.
        //
        // Đặt thẳng vận tốc thì vật nặng vật nhẹ bay đi y hệt nhau. Đây cũng chính là
        // cách đã dùng cho cú tung V, và cùng một lý do.
        rbToFire.linearVelocity = fireDirection * fireSpeed;
        rbToFire.angularVelocity = Vector3.zero;

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

    // Chạy trên MỌI máy, đúng một lần cho mỗi lần VUNG TAY - kể cả vung trượt.
    //
    // Chỉ phần NHÌN THẤY ĐƯỢC nằm ở đây. Tiếng đấm và rung camera vẫn ở OnMeleePerformed,
    // vì chúng là phản hồi của việc CHẠM được vào ai đó - đấm hụt mà vẫn nghe tiếng thịch
    // và rung màn hình thì người chơi tưởng mình vừa trúng.
    private void OnMeleeSwing()
    {
        // Animation đấm của thân người, thứ mà người khác nhìn thấy.
        // Để trống ô Punch Trigger Param bên kia thì nó tự bỏ qua.
        if (animatorDriver != null) animatorDriver.TriggerPunch();

        // Vung tay trước mắt. Tự lọc: viewmodel chỉ tồn tại trên máy chủ nhân vật.
        if (viewmodel != null) viewmodel.PlayPunch();

        // VÒNG KHÍ NÉN Ở NẮM ĐẤM, mỗi lần vung tay - kể cả đấm trượt. Mọi máy đều thấy.
        // Nhỏ và rất ngắn: nó chỉ báo "vừa tung cú đấm", phần kịch tính để dành cho lúc trúng.
        Vector3 fist = GetFistOrigin(out int fistLayer, false);
        CombatVFX.Flash(fist, GetPunchForward(), QiColor, 0.34f, 0.16f, fistLayer);
    }

    /// <summary>
    /// Vẽ quyền khí (đấm) hoặc tia nối (grapple) tới chỗ vừa trúng.
    ///
    /// Không trễ nhịp theo động tác rút tay của viewmodel, dù nắm đấm lúc này còn đang lùi
    /// về sát mặt. Lý do: nạn nhân BỊ HẤT VĂNG NGAY lúc trúng. Luồng khí mà đợi tay duỗi
    /// ra mới bay thì nó tới nơi khi người ta đã văng đi mấy mét, đâm vào khoảng không.
    /// Khớp với KẾT QUẢ quan trọng hơn khớp với động tác tay.
    /// </summary>
    private void PlayMeleeHitVfx()
    {
        Color c = QiColor;
        Vector3 from = GetFistOrigin(out int _, true);
        Vector3 to = MeleeHitPoint;

        if ((to - from).sqrMagnitude < 0.0001f) return;

        if (MeleeWasGrapple)
        {
            // Grapple: tia nối tay với đối thủ, như sợi xích từ trường kéo mình lao tới.
            CombatVFX.Beam(from, to, c, 0.3f, 0.12f);
            return;
        }

        CombatVFX.QiStrike(from, to, c);
    }

    /// <summary>
    /// Điểm nắm đấm phải.
    ///
    /// worldAligned = true: dùng cho hiệu ứng bay vào THẾ GIỚI - xem ghi chú ở
    /// FirstPersonViewmodel.TryGetScreenAlignedWorldPoint về chuyện hai camera lệch FOV.
    /// worldAligned = false: dùng cho hiệu ứng DÍNH vào nắm đấm, vẽ chung layer với tay.
    /// </summary>
    private Vector3 GetFistOrigin(out int layer, bool worldAligned)
    {
        if (viewmodel != null)
        {
            Transform fist = viewmodel.RightFist;
            if (fist != null)
            {
                // Kéo điểm xuất phát 25% về phía tâm màn hình. Nắm đấm nằm tít góc dưới bên
                // phải, nên luồng khí mọc thẳng từ đó ra thì cả vệt nằm sát mép dưới khung
                // hình - trông thấp và lệch. Kéo vào một chút thì vệt đi qua gần tâm ngắm,
                // mà vòng khí nhỏ ở nắm đấm vẫn che được chỗ nối.
                if (worldAligned && viewmodel.TryGetScreenAlignedWorldPoint(fist, out Vector3 w, 0.25f))
                {
                    layer = 0;
                    return w;
                }

                layer = fist.gameObject.layer;
                return fist.position;
            }
        }

        layer = 0;

        if (_cachedAnimator == null) _cachedAnimator = GetComponentInChildren<Animator>();
        if (_cachedAnimator != null && _cachedAnimator.isHuman)
        {
            Transform hand = _cachedAnimator.GetBoneTransform(HumanBodyBones.RightHand);

            // Nhích lên và ra trước: xương bàn tay nằm ở CỔ TAY, thấp và lùi so với nắm
            // đấm mà người khác nhìn thấy.
            if (hand != null) return hand.position + Vector3.up * 0.1f + transform.forward * 0.15f;
        }

        return transform.position + Vector3.up * 1.4f + transform.forward * 0.4f;
    }

    // Màu quyền khí: TRẮNG, không theo điện tích găng (chủ project chốt). Hơi ngả lạnh
    // một chút - trắng tuyệt đối khi bị Bloom khuếch đại dễ ám vàng, trông như tia lửa hàn.
    private static readonly Color QiColor = new Color(0.9f, 0.95f, 1f, 1f);

    private Vector3 GetPunchForward()
    {
        if (viewmodel != null && viewmodel.RightFist != null) return viewmodel.CameraForward;
        return transform.forward;
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi cú cận chiến TRÚNG ĐÍCH
    private void OnMeleePerformed()
    {
        AudioManager.MeleePunch(transform.position);

        // Rung + giật camera của NGƯỜI ĐẤM.
        //
        // Hàm này chạy trên mọi máy (vì MeleeCount là [Networked]), nhưng ShakeCamera và
        // KickCamera đều tự lọc bằng HasInputAuthority nên chỉ màn hình của chính người
        // vừa đấm mới rung. Không phải thêm điều kiện gì ở đây.
        if (movement != null)
        {
            movement.ShakeCamera(meleeShakeTrauma);
            movement.KickCamera(meleeKickStrength);
        }

        PlayMeleeHitVfx();
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi phát bắn.
    // Bản thân FPSMovement.ShakeCamera() đã tự lọc để chỉ rung camera của chủ nhân vật,
    // nên gọi thẳng ở đây không sợ rung nhầm màn hình người khác.
    private void OnObjectFired()
    {
        if (movement == null) return;

        movement.ShakeCamera(fireShakeTrauma);
        movement.KickCamera(fireKickStrength);

        if (viewmodel != null) viewmodel.PlayFire();
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi cú đẩy vật.
    private void OnObjectPushed()
    {
        if (viewmodel != null) viewmodel.PlayPush();

        // Giật camera nhẹ hơn cú bắn vật cầm trên tay (0.6 lần): đẩy một vật ở xa thì
        // phản lực dội về người ít hơn hẳn so với ném thứ đang cầm trong tay.
        if (movement != null) movement.KickCamera(fireKickStrength * 0.6f);
    }

    // Chạy trên MỌI máy, đúng một lần cho mỗi phát laze vào vật chưa có điện.
    //
    // Phát ở vị trí NGƯỜI BẮN chứ không phải vị trí vật trúng: đây là tiếng của cây súng,
    // không phải tiếng vật bị bắn. Tiếng ở phía vật đã có sfxCharge lo rồi.
    private void OnLaserFired()
    {
        AudioManager.Laser(transform.position);

        if (viewmodel != null) viewmodel.PlayLaser();

        DrawLaserBeam();
    }

    /// <summary>
    /// Vẽ tia laze từ ngón trỏ tới vật vừa bị nạp điện.
    ///
    /// Chạy trên MỌI máy, vì OnChangedRender là vậy - và đúng như mong muốn: tia laze là
    /// thứ tất cả mọi người phải thấy, không riêng người bắn.
    /// </summary>
    private void DrawLaserBeam()
    {
        if (Runner == null) return;
        if (!Runner.TryFindBehaviour(LaserTargetId, out MagneticObject target)) return;
        if (target == null) return;

        Vector3 from = GetLaserOrigin();
        int originLayer = _laserOriginLayer;
        Vector3 to = target.transform.position;

        // Màu theo điện tích VỪA NẠP, nên nhìn tia là biết vừa nạp âm hay dương. Đây là
        // thông tin có ích chứ không chỉ trang trí: người chơi cần biết vật kia giờ mang
        // dấu gì để quyết định hút hay đẩy.
        Color c = CombatVFX.PolarityColor(target.currentPolarity);

        // CHỚP SÁNG TRƯỚC, TIA SAU.
        //
        // Thiếu chớp sáng thì tia laze tự nhiên xuất hiện giữa không khí, không rõ từ đâu
        // ra - mắt không nối được bàn tay với thứ vừa bắn. Cục sáng ở ngay đầu ngón tay
        // chính là cái mắt cần để tin rằng chính bàn tay đó đã phóng ra tia.
        // TRUYỀN LAYER CỦA CHỖ PHÓNG TIA.
        //
        // Với chính mình, tia phóng từ đầu ngón của VIEWMODEL - mà viewmodel nằm trên
        // layer riêng và được vẽ bằng camera Overlay riêng. Cục sáng để ở layer mặc định
        // thì camera chính vẽ nó TRƯỚC, rồi camera viewmodel vẽ bàn tay ĐÈ LÊN - cục sáng
        // biến mất sạch.
        //
        // Cho nó cùng layer với bàn tay thì hai thứ được vẽ chung một lượt, và chiều sâu
        // mới so sánh được với nhau.
        CombatVFX.Flash(from, (to - from), c, 0.5f, 0.22f, originLayer);

        CombatVFX.Beam(from, to, c);
    }

    /// <summary>
    /// Điểm phóng tia. Ba mức, rơi dần khi mức trên không có.
    ///
    /// Với chính mình thì lấy ĐẦU NGÓN TRỎ của viewmodel - mà tư thế chỉ trỏ đó đã được
    /// dựng sẵn cho động tác bắn laze, nên tia phóng ra đúng từ ngón đang chỉ. Với người
    /// khác thì lấy xương bàn tay phải của mô hình thật.
    /// </summary>
    private Vector3 GetLaserOrigin()
    {
        if (viewmodel != null)
        {
            Transform tip = viewmodel.RightIndexTip;
            if (tip != null)
            {
                _laserOriginLayer = tip.gameObject.layer;
                return tip.position;
            }
        }

        _laserOriginLayer = gameObject.layer;

        if (_cachedAnimator == null) _cachedAnimator = GetComponentInChildren<Animator>();

        if (_cachedAnimator != null && _cachedAnimator.isHuman)
        {
            Transform hand = _cachedAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null) return hand.position;
        }

        // Không có xương nào dùng được thì bắn từ ngang ngực, còn hơn bắn từ dưới chân.
        return transform.position + Vector3.up * 1.4f + transform.forward * 0.3f;
    }

    private Animator _cachedAnimator;

    // Layer của chỗ vừa phóng tia. Xem ghi chú ở DrawLaserBeam.
    private int _laserOriginLayer;

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
