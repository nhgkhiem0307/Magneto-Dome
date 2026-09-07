using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Viewmodel góc nhìn thứ nhất: hai cánh tay hiện trước mắt, tạo dáng HOÀN TOÀN bằng code.
///
/// GẮN LÊN Player.prefab (cùng cấp với FPSMovement).
///
/// ⚠️ ĐÃ THỬ VÀ BỎ HAI CÁCH TRƯỚC ĐÓ. Đừng quay lại:
///   1. Hiện thân thật + giấu đầu, dùng animation góc nhìn thứ ba
///   2. Ghì xương tay bằng cách CỘNG THÊM góc xoay vào animation đó
///
/// Cả hai đều hỏng vì cùng một lý do: animation Mixamo làm để NHÌN TỪ XA 10m. Kéo sát vào
/// tầm mắt là lộ hết cái mềm oặt, chủ project gọi đúng tên là "tay ma trôi". Không có con
/// số nào chỉnh được điều đó, vì lỗi nằm ở dữ liệu nguồn.
///
/// Cách làm chuyên nghiệp - và là cách MỌI game bắn súng dùng:
///   - Một bản sao mẫu nhân vật riêng, KHÔNG chạy animation nào cả
///   - Mọi xương không phải cánh tay đều bị thu nhỏ về 0
///   - Tư thế tay do code giải IK hai xương, nên không phụ thuộc quy ước trục của rig
///   - Vẽ bằng CAMERA RIÊNG, FOV riêng, nên tay không bao giờ xuyên tường
/// </summary>
public class FirstPersonViewmodel : MonoBehaviour
{
    [Header("Bật/tắt")]
    [Tooltip("Tắt để quay lại trạng thái không có tay trước mắt.")]
    public bool enableViewmodel = true;

    [Tooltip("Layer dành riêng cho viewmodel. Đã thêm sẵn ở slot 7 trong Tags & Layers.\n\n" +
             "Sai tên ở đây là camera phụ không lọc được gì và tay sẽ hiện trong thế giới " +
             "thật, to như người khổng lồ.")]
    public string viewmodelLayerName = "Viewmodel";

    [Header("Camera riêng")]
    [Tooltip("Góc nhìn của camera viewmodel. LUÔN HẸP HƠN camera chính.\n\n" +
             "Camera chính để 60. Tay vẽ ở 60 sẽ méo và bé tí ở rìa màn hình - mọi game " +
             "bắn súng đều dùng FOV hẹp hơn cho tay, thường 40-50.")]
    public float viewmodelFov = 45f;

    [Tooltip("Mặt phẳng cắt gần của camera viewmodel. Rất nhỏ vì tay ở ngay sát mặt.")]
    public float viewmodelNearClip = 0.01f;

    [Tooltip("Tự đo vị trí VAI từ xương rồi đặt khung cho đúng, thay vì gõ số tay.\n\n" +
             "⚠️ Bản đầu gõ tay -1.45 và SAI HAI LẦN: (1) đặt vai NGANG tầm mắt trong khi " +
             "người thật có vai thấp hơn mắt ~25cm, (2) quên model đang scale 1.2 nên vai " +
             "thật ở 1.74m chứ không phải 1.45m. Kết quả là ngực nằm ngay trong mặt và tay " +
             "mọc ra từ hư không - nhìn như tay bị cắt rời.\n\n" +
             "Đo từ xương thì đổi scale hay thay nhân vật khác cũng tự đúng.")]
    public bool autoPlaceBody = true;

    [Tooltip("Vai thấp hơn mắt bao nhiêu mét. Người thật khoảng 0.22-0.28.\n\n" +
             "Đây là con số làm tay TRÔNG CÓ GỐC: thấy được vai và bắp tay trên nối vào " +
             "khung hình thì não hiểu tay thuộc về mình. Vai quá thấp thì tay lại thành " +
             "hai que gỗ trôi từ dưới lên.")]
    public float shoulderBelowEye = 0.25f;

    [Tooltip("Lùi khung ra sau bao nhiêu mét, để ngực không thò vào mép dưới màn hình.")]
    public float bodyBehind = 0.12f;

    [Tooltip("Dùng khi tắt Auto Place Body. Vị trí khung so với camera, gõ tay.")]
    public Vector3 bodyOffset = new Vector3(0f, -2f, -0.12f);

    [Header("Tư thế nghỉ (toạ độ theo CAMERA)")]
    [Tooltip("Vị trí bàn tay TRÁI khi không làm gì. x = trái/phải, y = trên/dưới, z = xa/gần.")]
    public Vector3 leftHandRest = new Vector3(-0.26f, -0.24f, 0.42f);

    [Tooltip("Vị trí bàn tay PHẢI khi không làm gì.")]
    public Vector3 rightHandRest = new Vector3(0.28f, -0.26f, 0.4f);

    [Tooltip("Hướng KHUỶU TAY chống về. Âm ở Y nghĩa là khuỷu chúc xuống.\n\n" +
             "Thiếu gợi ý này thì bộ giải IK chọn tư thế khuỷu tuỳ ý miễn bàn tay tới đúng " +
             "chỗ - và nó rất hay chọn kiểu bẻ ngược khớp, nhìn như tay gãy.")]
    public Vector3 elbowHint = new Vector3(0.6f, -1f, -0.35f);

    [Tooltip("Tự co tư thế nghỉ lại cho vừa TẦM VỚI thật của cánh tay.' + NL + NL + '" +
             "⚠️ ĐÂY LÀ LÝ DO 'chỉ thấy hút, tăng số to mấy cũng vô ích'. Tư thế nghỉ gõ " +
             "tay đặt bàn tay cách vai đúng 0.55m, mà cánh tay người cũng chỉ dài 0.54m - " +
             "tức tay ĐÃ DUỖI CỨNG khi đứng yên.' + NL + NL + '" +
             "Bộ giải IK gặp đích xa hơn tầm với thì kẹp lại ở tầm với (SolveArm, dòng " +
             "maxReach). Kẹp rồi thì đẩy tay ra thêm 1cm hay 100cm đều cho ra ĐÚNG MỘT " +
             "tư thế duỗi thẳng - không có gì nhúc nhích.' + NL + NL + '" +
             "Hút đạn thấy được vì nó là động tác duy nhất kéo tay VỀ, tức đi vào vùng " +
             "còn co duỗi được. Đấm/bắn/laze đều đẩy RA nên chết hết.")]
    public bool autoScaleRest = true;

    [Tooltip("Tư thế nghỉ đặt tay ở bao nhiêu phần trăm tầm với. 1.0 = duỗi cứng.' + NL + NL + '" +
             "Phần dư ra chính là chỗ để đấm và bắn vung tay vào. 0.68 chừa lại khoảng " +
             "17cm - đủ cho một cú đấm nhìn ra hình.")]
    [Range(0.3f, 0.95f)]
    public float restReach01 = 0.68f;

    [Header("Rung camera")]
    [Tooltip("Tay hứng bao nhiêu phần rung của camera. 1 = dính cứng, 0 = đứng im hoàn toàn.\n\n" +
             "Tay là con của camera nên mặc định nó ăn TRỌN 100% cú rung - lắc 3.5 độ ở " +
             "25 lần/giây. Vấn đề là cả khung hình lẫn bàn tay rung y hệt nhau, nên mắt " +
             "không thấy tay rung, chỉ thấy tay DÁN CHẾT vào màn hình. Ngố đúng chỗ đó.\n\n" +
             "Ghì lại còn một phần thì tay hơi trễ so với khung hình, và chính độ trễ đó " +
             "làm tay trông có khối lượng thật. Mọi game bắn súng đều tách hai thứ này ra.")]
    [Range(0f, 1f)]
    public float shakeFollow = 0.4f;

    [Header("Ngón tay")]
    [Tooltip("Cuộn ngón tay bằng code. Avatar Mixamo đang dùng đã map đủ 30 xương ngón " +
             "(3 đốt x 5 ngón x 2 tay) nên không cần thêm asset nào.")]
    public bool enableFingers = true;

    [Tooltip("Độ cuộn mặc định. 1 = nắm đấm.\n\n" +
             "Đây là găng tay từ tính, không phải bàn tay không - nắm hờ suốt trận vừa " +
             "hợp nhân vật vừa đỡ lộ ngón tay xấu ở khoảng cách gần.")]
    [Range(0f, 1f)]
    public float idleCurl = 1f;

    [Tooltip("Tay trái vươn ra bao nhiêu phần trăm tầm với khi khống chế vật lơ lửng.\n\n" +
             "Lớn hơn Rest Reach 01 một chút thì mới ra dáng đang với tới vật.")]
    [Range(0.4f, 0.98f)]
    public float controlExtend01 = 0.82f;

    [Tooltip("Nhân toàn bộ góc cuộn. Tăng nếu nắm tay chưa kín, giảm nếu ngón đâm xuyên " +
             "vào lòng bàn tay.")]
    [Range(0.3f, 1.6f)]
    public float fingerCurlScale = 1f;

    [Tooltip("Đảo chiều cuộn ngón nếu rig này dựng ngược. CHỈ bật khi thấy CẢ HAI bàn tay " +
             "cùng nắm ra mu bàn tay - một bên đúng một bên sai thì không phải lỗi này.")]
    public bool flipFingerCurl = false;

    [Tooltip("Tốc độ nắm/mở, đơn vị 0-1 mỗi giây. Cao = đóng phắt, thấp = từ từ.")]
    public float fistSpeed = 9f;

    [Tooltip("Nắm chặt bao nhiêu giây quanh cú đấm. Nắm tay phải đóng TRƯỚC lúc chạm và " +
             "còn giữ một nhịp sau đó, không mở ra ngay giữa cú đấm.")]
    public float punchFistHold = 0.3f;

    [Tooltip("Nâng cả hai bàn tay lên bấy nhiêu mét so với tư thế nghỉ đã tính.\n\n" +
             "Ô riêng để chỉnh độ cao mà không phải đụng vào Left/Right Hand Rest - hai ô " +
             "kia còn quyết định cả hướng vươn nên sửa vào đó dễ lệch thứ khác.")]
    public float handRaise = 0.06f;

    [Header("Chuyển động nền")]
    [Tooltip("Biên độ nhấp nhô LÊN XUỐNG khi đi bộ, mét.")]
    public float walkBobAmount = 0.05f;

    [Tooltip("Biên độ ĐÁNH TAY trước-sau khi đi bộ, mét. Hai tay so le nhau.\n\n" +
             "⚠️ ĐỪNG NHẦM VỚI Walk Bob Amount. Ô kia chỉ làm tay nhún lên xuống - mắt gần " +
             "như không nhận ra, và đó là lý do bản đầu 'đi mà không thấy đánh tay'.\n\n" +
             "Đánh tay trước-sau mới là chuyển động người ta THẤY khi đi bộ. Phải so le: " +
             "tay trái ra trước thì tay phải ra sau. Cùng pha thì trông như đang bơi ếch.")]
    public float walkSwingAmount = 0.24f;

    [Tooltip("Biên độ tay lắc SANG NGANG theo mỗi bước, mét. Nhỏ thôi - đây chỉ là gia vị " +
             "để chuyển động không nằm gọn trong một mặt phẳng.")]
    public float walkLateralAmount = 0.05f;

    [Tooltip("Xoay CẢ BỘ KHUNG theo nhịp bước, tính bằng độ.\n\n" +
             "Đây là thứ bản đầu thiếu hẳn, và là lý do 'đi mà không thấy đánh tay': dời " +
             "bàn tay 11cm thì phần lớn chuyển động bị chính bàn tay che mất, vì bàn tay to " +
             "gần bằng chừng đó.\n\n" +
             "Xoay cả khung thì VAI cũng nhấp nhô theo, và mắt bắt được ngay - đó mới là " +
             "cách cơ thể thật chuyển động khi đi: xoay quanh cột sống, không phải dời tay.")]
    public float walkRollAmount = 2.2f;

    [Tooltip("Số nhịp nhấp nhô mỗi giây khi chạy hết tốc.")]
    public float walkBobSpeed = 5f;

    [Tooltip("Biên độ thở khi đứng yên, mét. Rất nhỏ - đây chỉ là để tay không chết cứng.")]
    public float breatheAmount = 0.012f;

    [Tooltip("Tay TRÔI LẠI khi quay đầu, mét trên mỗi độ xoay.\n\n" +
             "Đây là thứ tạo ra SỨC NẶNG. Tay dính cứng vào camera thì trông như hai cái " +
             "sticker dán trên màn hình.")]
    public float swayAmount = 0.006f;

    [Tooltip("Tay trôi tối đa bấy nhiêu mét, để quay ngoắt 180 độ không làm tay bay ra sau lưng.")]
    public float swayMax = 0.07f;

    [Tooltip("Tốc độ tay đuổi về chỗ cũ. Thấp = nặng và ì, cao = nhẹ và bám sát.")]
    public float swayRecover = 6f;

    [Header("Lò xo chuyển tư thế")]
    [Tooltip("Độ cứng lò xo kéo tay về tư thế đích. CAO = dứt khoát, THẤP = nhũn.\n\n" +
             "Đây là ô quyết định cảm giác 'chuyên nghiệp'. Dưới 60 thì mọi động tác đều " +
             "nhão như bơi trong dầu.")]
    public float poseStiffness = 140f;

    [Tooltip("Ma sát lò xo. Thấp = nảy qua nảy lại, cao = về thẳng không nảy.\n\n" +
             "18-24 cho ra đúng một nhịp nảy ngược nhẹ - chính nhịp đó làm động tác có " +
             "'điểm kết' thay vì trôi tuột về chỗ cũ.")]
    public float poseDamping = 20f;

    [Header("Động tác — ĐẤM")]
    [Tooltip("Tay phải thọc ra trước bao nhiêu mét.")]
    public float punchThrust = 0.55f;

    [Tooltip("Tay phải hạ xuống bao nhiêu khi thọc (cú đấm đi hơi chúc xuống, không nằm ngang).")]
    public float punchDrop = 0.06f;

    [Tooltip("Giữ tư thế thọc tay bao nhiêu giây.\n\n" +
             "⚠️ Ô này là thứ làm cú đấm NHÌN THẤY ĐƯỢC. Bản trước chỉ cộng một xung vào " +
             "vận tốc rồi thả cho lò xo lo - đỉnh vung tay đến sau 0.09 giây rồi tan ngay, " +
             "tức khoảng 5 khung hình ở 60fps. Quá nhanh để mắt bắt được, nhất là khi " +
             "camera đang giật cùng lúc.\n\n" +
             "Bắn laze thấy được ngay từ đầu chính vì nó CÓ ô giữ này (Laser Hold). Đấm và " +
             "bắn thì không, nên mới mất tăm.")]
    public float punchHold = 0.13f;

    [Header("Động tác — BẮN VẬT")]
    [Tooltip("Hai tay đẩy ra trước bao nhiêu mét khi bắn.")]
    public float firePush = 0.3f;

    [Tooltip("Rồi giật ngược lại bao nhiêu. Chính cú giật ngược tạo cảm giác có phản lực.")]
    public float fireRecoil = 0.22f;

    [Tooltip("Bao nhiêu giây sau cú đẩy thì tay giật ngược lại.\n\n" +
             "Đẩy ra và giật về là HAI NHỊP NỐI TIẾP. Bản đầu làm sai chỗ này: nó lấy " +
             "firePush TRỪ fireRecoil rồi đẩy một lần duy nhất, ra 0.30 - 0.22 = 0.08m - " +
             "nhỏ hơn cú đấm tám lần. Đó là lý do 'bắn không thấy gì': tay chỉ nhích 4cm, " +
             "mà bàn tay còn to hơn chừng đó nên nó tự che mất chuyển động của chính mình.")]
    public float fireRecoilDelay = 0.09f;

    [Tooltip("Giữ tư thế đẩy tay bao nhiêu giây. Xem ghi chú ở Punch Hold.")]
    public float fireHold = 0.1f;

    [Header("Động tác — BẮN LAZE")]
    [Tooltip("Tay trái vươn ra trước bao nhiêu mét.")]
    public float laserExtend = 0.42f;

    [Tooltip("Giữ tư thế vươn tay bao nhiêu giây sau mỗi phát.")]
    public float laserHold = 0.18f;

    [Header("Động tác — HÚT ĐẠN")]
    [Tooltip("Hai tay kéo về gần ngực bao nhiêu mét khi đang hút. " +
             "Đây là trạng thái GIỮ LIÊN TỤC, không phải cú đánh một nhịp.")]
    public float pullDraw = 0.26f;

    [Tooltip("Hai tay tách rộng ra bao nhiêu khi hút - như đang ôm lấy luồng từ trường.")]
    public float pullSpread = 0.1f;

    // ==================== TRẠNG THÁI ====================

    private FPSMovement _movement;
    private PlayerMagnetController _magnet;
    private Transform _camera;
    private Camera _viewmodelCam;
    private int _layer = -1;

    // Bộ xương cánh tay của BẢN SAO viewmodel, không phải của nhân vật thật.
    private Transform _lUpper, _lLower, _lHand;
    private Transform _rUpper, _rLower, _rHand;

    // Gốc của cả bộ khung viewmodel, để xoay theo nhịp bước.
    private Transform _body;
    private Vector3 _bodyBasePos;
    private CameraShake _shake;

    private bool _built;

    // Lò xo cho từng tay: độ lệch hiện tại và vận tốc của nó.
    private Vector3 _lOffset, _lVelocity;
    private Vector3 _rOffset, _rVelocity;

    // Đích mà lò xo đang kéo về, tính lại mỗi khung hình từ các động tác đang diễn ra.
    private Vector3 _lTarget, _rTarget;

    // Trôi tay khi quay đầu.
    private Vector3 _sway;
    private float _lastYaw, _lastPitch;
    private bool _hasLastLook;

    private float _bobPhase;
    private float _laserTimer;
    private float _fireRecoilTimer;
    private float _punchTimer, _fireTimer;

    // Tư thế nghỉ ĐÃ CO cho vừa tầm với, đo một lần lúc dựng. BuildTargets dùng hai ô này
    // chứ không dùng thẳng leftHandRest/rightHandRest.
    private Vector3 _lRest, _rRest;

    // Ngón tay: 15 đốt mỗi bàn, xếp theo LeftFingerBones/RightFingerBones bên dưới.
    private Transform[] _lFingers, _rFingers;

    // Tư thế GỐC của từng đốt ngón, chụp một lần lúc dựng.
    //
    // ⚠️ THIẾU CÁI NÀY LÀ NGÓN TAY QUAY TÍT VÔ TẬN. Phép cuộn là xoay THÊM một góc vào
    // tư thế đang có; không trả về gốc trước thì mỗi khung hình lại cộng thêm 50 độ nữa,
    // sau một giây là xương xoắn thành vô nghĩa và lưới vỡ ra thành vệt sáng.
    //
    // Bộ giải IK ở tay không dính lỗi này vì AimBone ĐO hướng hiện tại rồi xoay đúng phần
    // chênh - chạy bao nhiêu lần cũng ra một kết quả. Cuộn ngón thì không tự sửa được.
    private Quaternion[] _lFingerRest, _rFingerRest;

    // Trục cuộn ngón, ghi theo TOẠ ĐỘ BÀN TAY nên xoay tay kiểu gì nó cũng đúng.
    private Vector3 _lCurlAxis, _rCurlAxis;
    private float _lCurlSign = 1f, _rCurlSign = 1f;

    // Độ nắm hiện tại 0-1, làm mượt dần chứ không nhảy phắt.
    private float _lFist, _rFist;

    // Độ "chìa ngón trỏ" 0-1. Tách khỏi độ nắm vì hai thứ chồng lên nhau: bàn tay vẫn
    // nắm, riêng ngón trỏ duỗi thẳng ra.
    private float _lPoint, _rPoint;

    private float _punchFistTimer;

    // Vai (toạ độ camera) và tầm với của từng tay, đo một lần lúc dựng.
    private Vector3 _lShoulder, _rShoulder;
    private float _lReach, _rReach;

    // Thứ tự BẮT BUỘC khớp với FingerCurlAngles bên dưới: mỗi ngón ba đốt, gốc -> đầu.
    private static readonly HumanBodyBones[] LeftFingerBones =
    {
        HumanBodyBones.LeftThumbProximal,  HumanBodyBones.LeftThumbIntermediate,  HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,  HumanBodyBones.LeftIndexIntermediate,  HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,   HumanBodyBones.LeftRingIntermediate,   HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal,
    };

    private static readonly HumanBodyBones[] RightFingerBones =
    {
        HumanBodyBones.RightThumbProximal,  HumanBodyBones.RightThumbIntermediate,  HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,  HumanBodyBones.RightIndexIntermediate,  HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,   HumanBodyBones.RightRingIntermediate,   HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal,
    };

    // Góc cuộn tối đa của từng đốt, độ.
    //
    // ĐỐT GIỮA CONG NHIỀU NHẤT - đó là khớp gập sâu nhất của bàn tay người. Cho ba đốt
    // cùng một góc thì ngón cong đều như ống nước, không ra nắm đấm.
    //
    // NGÓN CÁI CONG RẤT ÍT vì nó không gập vào lòng bàn tay mà VẮT NGANG qua các ngón
    // khác, quanh một trục hoàn toàn khác. Cho nó góc lớn theo trục chung là ngón cái
    // đâm xuyên qua bàn tay. Để nhỏ thì nó chỉ khép lại, nhìn vẫn đúng.
    private static readonly float[] FingerCurlAngles =
    {
        22f, 20f, 18f,   // cái
        48f, 62f, 48f,   // trỏ
        50f, 65f, 50f,   // giữa
        48f, 62f, 48f,   // áp út
        45f, 60f, 45f,   // út
    };

    private void Awake()
    {
        _movement = GetComponent<FPSMovement>();
        _magnet = GetComponent<PlayerMagnetController>();
    }

    // ==================== DỰNG ====================

    /// <summary>
    /// Dựng viewmodel. FPSMovement gọi vào đây trong Spawned(), CHỈ với nhân vật của mình.
    /// </summary>
    public void Build(Transform cameraTransform, Animator sourceAnimator)
    {
        if (!enableViewmodel || _built) return;
        if (cameraTransform == null || sourceAnimator == null) return;
        if (!sourceAnimator.isHuman) return;

        _camera = cameraTransform;

        _layer = LayerMask.NameToLayer(viewmodelLayerName);
        if (_layer < 0)
        {
            Debug.LogError($"[Viewmodel] Chưa có layer '{viewmodelLayerName}' trong " +
                           $"Tags & Layers. Viewmodel sẽ không hiện.", this);
            return;
        }

        if (!BuildArmRig(sourceAnimator)) return;

        BuildCamera();
        MeasureRest();

        _built = true;
    }

    /// <summary>
    /// Nhân bản mẫu nhân vật làm viewmodel, rồi xoá sạch mọi thứ không phải cánh tay.
    ///
    /// Vì sao nhân bản thay vì dùng luôn thân thật: thân thật do Animator điều khiển và
    /// được đồng bộ qua mạng. Đụng vào nó là đụng vào thứ người khác cũng nhìn thấy.
    /// Bản sao thì hoàn toàn của riêng máy này, muốn bẻ kiểu gì cũng được.
    /// </summary>
    private bool BuildArmRig(Animator sourceAnimator)
    {
        // TẮT BẢN GỐC TRƯỚC KHI NHÂN BẢN, rồi bật lại ngay.
        //
        // ⚠️ BẮT BUỘC. Destroy() bị HOÃN tới cuối khung hình, nên nếu nhân bản một object
        // đang bật thì mọi script trên bản sao KỊP CHẠY Awake/Start trước khi bị xoá:
        // PlayerVisuals đi tìm FPSMovement.Local rồi tô màu đội và gắn viền QuickOutline
        // lên viewmodel, PlayerAnimatorDriver đi giấu xương... Toàn những việc rất lạ trên
        // một object đáng lẽ là mô hình câm.
        //
        // Nhân bản một object ĐANG TẮT thì Awake không bao giờ chạy. Dọn xong mới bật lên.
        GameObject source = sourceAnimator.gameObject;
        bool sourceWasActive = source.activeSelf;

        source.SetActive(false);
        GameObject copy = Instantiate(source, _camera, false);
        source.SetActive(sourceWasActive);

        copy.name = "Viewmodel";

        // TẮT ANIMATOR bằng cách bỏ controller, KHÔNG phải bằng enabled = false.
        //
        // Bỏ controller thì Animator vẫn sống (GetBoneTransform còn dùng được) nhưng
        // không ghi vào xương nào cả - nên tư thế do code đặt sẽ nằm yên.
        // Tắt hẳn enabled thì GetBoneTransform có thể trả về null tuỳ phiên bản Unity.
        Animator anim = copy.GetComponent<Animator>();
        if (anim == null) { Destroy(copy); return false; }

        anim.runtimeAnimatorController = null;
        anim.applyRootMotion = false;

        // Dọn mọi script đi kèm. Bản sao mang theo cả PlayerAnimatorDriver, PlayerVisuals...
        // và chúng sẽ đi tìm FPSMovement.Local rồi làm những việc rất lạ.
        foreach (MonoBehaviour mb in copy.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) Destroy(mb);
        }

        foreach (Collider col in copy.GetComponentsInChildren<Collider>(true))
        {
            if (col != null) Destroy(col);
        }

        _lUpper = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _lLower = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);

        _rUpper = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rLower = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);

        if (_lUpper == null || _rUpper == null) { Destroy(copy); return false; }

        CaptureFingers(anim, LeftFingerBones,  ref _lFingers, ref _lFingerRest,
                       _lHand, -1f, ref _lCurlAxis, ref _lCurlSign);
        CaptureFingers(anim, RightFingerBones, ref _rFingers, ref _rFingerRest,
                       _rHand, +1f, ref _rCurlAxis, ref _rCurlSign);

        HideNonArmBones(anim);
        SetLayerRecursive(copy.transform, _layer);

        // Đặt bản sao sao cho VAI nằm ngay dưới camera, thân tụt xuống khỏi khung hình.
        //
        // Con số này căn theo mẫu Mixamo đang dùng: vai ở khoảng 1.45m tính từ chân, nên
        // hạ 1.45m thì vai về ngang camera. Lùi thêm 0.1m ra sau để ngực không thò vào
        // mép dưới màn hình.
        //
        // Nếu thấy tay quá cao hoặc quá thấp thì đây là ô đầu tiên cần chỉnh - xem
        // bodyOffset trong Inspector.
        copy.transform.localRotation = Quaternion.identity;

        if (autoPlaceBody)
        {
            // Đặt tạm về 0 rồi ĐO vai đang ở đâu so với camera, sau đó bù lại đúng lượng
            // cần thiết. Cách này không cần biết chiều cao mẫu hay scale bao nhiêu -
            // nó tự đo cái đang có.
            copy.transform.localPosition = Vector3.zero;

            Transform shoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder)
                                 ?? anim.GetBoneTransform(HumanBodyBones.RightUpperArm);

            if (shoulder != null)
            {
                float shoulderLocalY = _camera.InverseTransformPoint(shoulder.position).y;

                // Muốn vai nằm ở -shoulderBelowEye, hiện nó đang ở shoulderLocalY.
                copy.transform.localPosition = new Vector3(0f,
                                                           -shoulderBelowEye - shoulderLocalY,
                                                           -bodyBehind);
            }
            else
            {
                copy.transform.localPosition = bodyOffset;
            }
        }
        else
        {
            copy.transform.localPosition = bodyOffset;
        }

        foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            // Tắt cắt bỏ theo hộp bao.
            //
            // Tấm da của nhân vật có hộp bao tính theo tư thế GỐC. Ta bẻ tay đi rất xa
            // tư thế đó, nên hộp bao cũ có thể nằm ngoài tầm nhìn camera phụ và Unity
            // CẮT MẤT cả cánh tay dù nó đang ở ngay trước mặt.
            SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
            if (smr != null) smr.updateWhenOffscreen = true;
        }

        // Bật lên SAU KHI đã dọn sạch. Từ đây trở đi bản sao chỉ là mô hình câm.
        copy.SetActive(true);
        _body = copy.transform;
        _bodyBasePos = copy.transform.localPosition;
        _shake = _camera.GetComponent<CameraShake>();

        return true;
    }

    /// <summary>
    /// Giấu đầu và hai chân. GIỮ NGUYÊN thân và vai.
    ///
    /// ⚠️ ĐÂY LÀ CHỖ BẢN ĐẦU LÀM SAI, và sai rất nặng - đọc kỹ trước khi "tối ưu" lại.
    ///
    /// Bản đầu thu nhỏ MỌI xương không phải cánh tay, gồm cả Hips, Spine, Chest.
    /// Nhưng SCALE TRONG UNITY LAN XUỐNG TOÀN BỘ CON: cánh tay là con của Chest, nên
    /// thu nhỏ Chest là thu nhỏ luôn cả tay. Tệ hơn, xương biến dạng làm phép tính da
    /// (skinning) cho ra đỉnh vô cực, và tấm da bung thành một đa giác khổng lồ phủ kín
    /// màn hình - đúng cái mảng màu che hết mọi thứ.
    ///
    /// Chỉ được giấu những xương KHÔNG PHẢI TỔ TIÊN của cánh tay:
    ///   - Đầu và cổ  (nhánh riêng đi lên từ Chest)
    ///   - Hai chân   (nhánh riêng đi xuống từ Hips)
    ///
    /// Thân vẫn còn, nhưng không sao: nó nằm dưới và sau camera nên rơi ra ngoài khung
    /// hình. Đó cũng là cách các game FPS có "thân thật" làm - thấy ngực khi cúi xuống
    /// là điều mong muốn, không phải lỗi.
    ///
    /// Dùng 0.0001 chứ không phải 0: scale bằng 0 tạo ma trận suy biến, một số đường tính
    /// da cho ra pháp tuyến NaN và mặt bị nháy đen hoặc kéo dài vô tận.
    /// </summary>
    /// <summary>
    /// Nhặt 15 xương ngón của một bàn tay, rồi TỰ TÌM RA trục cuộn và chiều cuộn.
    ///
    /// ⚠️ VÌ SAO PHẢI TỰ TÌM CHỨ KHÔNG GÕ SẴN "cuộn quanh trục X":
    /// mỗi rig đặt trục xương một kiểu. Gõ sẵn là đoán, đoán sai thì ngón xoè ngược ra
    /// sau lưng bàn tay. Đây đúng cái bẫy mà SolveArm đã tránh - làm nốt cho ngón tay.
    ///
    /// TRỤC: ngón tay gập quanh hàng khớp đốt, tức trục chạy NGANG BÀN TAY từ ngón trỏ
    /// sang ngón út. Đo thẳng bằng hai xương đó là ra, không cần biết quy ước rig.
    ///
    /// CHIỀU: thử xoay đầu ngón giữa 30 độ quanh trục vừa tìm. Chiều nào kéo đầu ngón
    /// LẠI GẦN CỔ TAY hơn thì đó là chiều gập vào lòng bàn tay.
    /// </summary>
    private void CaptureFingers(Animator anim, HumanBodyBones[] table, ref Transform[] slot,
                                ref Quaternion[] restSlot,
                                Transform hand, float handSign,
                                ref Vector3 axisLocal, ref float sign)
    {
        if (!enableFingers || hand == null) return;

        Transform[] bones = new Transform[table.Length];
        for (int i = 0; i < table.Length; i++) bones[i] = anim.GetBoneTransform(table[i]);

        Transform index  = bones[3];    // trỏ, đốt gốc
        Transform little = bones[12];   // út,  đốt gốc
        Transform midRoot = bones[6];
        Transform midTip  = bones[8];

        if (index == null || little == null || midRoot == null || midTip == null)
        {
            Debug.LogWarning("[Viewmodel] Rig thiếu xương ngón tay, bỏ qua phần nắm tay.", this);
            return;
        }

        Vector3 arm = midTip.position - midRoot.position;
        if (arm.sqrMagnitude < 0.000001f) return;

        Vector3 fingerDir = arm.normalized;

        // TRỤC PHẢI VUÔNG GÓC VỚI NGÓN TAY.
        //
        // Đường nối khớp trỏ-út là hướng đúng, nhưng lòng bàn tay không phẳng tuyệt đối
        // nên nó thường XIÊN một chút so với ngón. Xoay quanh trục xiên thì ngón vừa gập
        // xuống vừa quẹo sang bên. Bỏ đi phần chạy dọc theo ngón thì còn lại đúng thành
        // phần gập.
        Vector3 axisWorld = little.position - index.position;
        axisWorld -= fingerDir * Vector3.Dot(axisWorld, fingerDir);

        if (axisWorld.sqrMagnitude < 0.000001f) return;
        axisWorld.Normalize();

        // ⚠️ CHIỀU CUỘN SUY RA TỪ TAY TRÁI/PHẢI, KHÔNG ĐO ĐƯỢC.
        //
        // Bản trước thử bằng cách xoay đầu ngón giữa rồi xem nó lại gần cổ tay hơn không.
        // PHÉP THỬ ĐÓ VÔ NGHĨA: gập vào lòng bàn tay hay bẻ ngược ra mu bàn tay thì đầu
        // ngón đều lại gần cổ tay NHƯ NHAU - cổ tay nằm ngay trên đường kéo dài của ngón,
        // nên nó cách đều cả hai phía. Kết quả gần như bốc thăm, và đó là lý do một bàn
        // đúng một bàn nắm ngược ra mu tay.
        //
        // Chiều đúng suy được thẳng từ hình học, không cần đo:
        //   Tay PHẢI, lòng bàn úp xuống, ngón chỉ ra trước -> ngón út nằm bên phải, nên
        //   trục trỏ->út chỉ sang PHẢI. Xoay dương quanh trục sang phải thì đầu ngón đi
        //   XUỐNG, tức về phía lòng bàn tay. Đúng chiều, sign = +1.
        //
        //   Tay TRÁI là ảnh gương: ngón út nằm bên trái nên trục quay ngược 180 độ, cùng
        //   phép xoay dương lại hất ngón LÊN phía mu bàn tay. sign = -1.
        //
        // Hai bàn tay đối xứng gương nên CÙNG MỘT ĐOẠN CODE PHẢI RA HAI DẤU KHÁC NHAU -
        // đúng như chủ project đoán.
        sign = handSign * (flipFingerCurl ? -1f : 1f);
        axisLocal = hand.InverseTransformDirection(axisWorld);

        Quaternion[] rest = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            rest[i] = bones[i] != null ? bones[i].localRotation : Quaternion.identity;

        slot = bones;
        restSlot = rest;
    }

    /// <summary>
    /// Quyết định hai bàn tay đang nắm bao nhiêu, rồi tiến dần về đó.
    ///
    /// Làm mượt chứ không gán thẳng: bàn tay đóng phắt trong một khung hình trông như
    /// lỗi hiển thị chứ không giống động tác.
    /// </summary>
    private void UpdateFists(float dt)
    {
        if (_punchFistTimer > 0f) _punchFistTimer -= dt;

        bool holding = _magnet != null && _magnet.GetGrabbedObject() != null;
        bool pulling = _magnet != null && _magnet.IsPulling;

        // TAY PHẢI - tay tấn công.
        // Đấm thắng laze khi trùng nhau: nắm đấm là động tác dứt khoát hơn, chìa ngón trỏ
        // giữa lúc đang đấm thì vừa sai vừa trông như gãy ngón.
        // Thứ tự ưu tiên có chủ ý: đấm thắng laze (nắm đấm dứt khoát hơn, chìa ngón trỏ
        // giữa lúc đang đấm thì trông như gãy ngón), laze thắng khống chế (bắn laze là
        // hành động chủ động, giữ vật chỉ là trạng thái nền).
        float rFistWant  = idleCurl;
        float rPointWant = 0f;

        if (_punchFistTimer > 0f)
        {
            rFistWant = 1f;
        }
        else if (_laserTimer > 0f)
        {
            rFistWant = 1f;     // cả bàn nắm chặt...
            rPointWant = 1f;    // ...riêng ngón trỏ duỗi thẳng chỉ vào mục tiêu
        }
        else if (holding)
        {
            rFistWant = 0f;     // xoè bàn khống chế vật lơ lửng
        }
        else if (pulling)
        {
            rFistWant = 0.1f;
        }

        // TAY TRÁI chỉ phụ hoạ. Hút đạn là động tác hai tay nên nó cũng xoè ra, còn lại
        // để nguyên nắm hờ.
        float lFistWant = pulling ? 0.1f : idleCurl;

        _rFist  = Mathf.MoveTowards(_rFist,  rFistWant,  fistSpeed * dt);
        _lFist  = Mathf.MoveTowards(_lFist,  lFistWant,  fistSpeed * dt);
        _rPoint = Mathf.MoveTowards(_rPoint, rPointWant, fistSpeed * dt);
        _lPoint = Mathf.MoveTowards(_lPoint, 0f,         fistSpeed * dt);
    }

    /// <summary>
    /// Cuộn ngón tay. PHẢI chạy SAU ApplyIK vì IK xoay bắp tay và cẳng tay, tức là dời
    /// luôn cả bàn tay - trục cuộn lấy từ bàn tay nên phải đợi nó về đúng chỗ đã.
    /// </summary>
    private void ApplyFingers()
    {
        CurlHand(_lFingers, _lFingerRest, _lHand, _lCurlAxis, _lCurlSign, _lFist, _lPoint);
        CurlHand(_rFingers, _rFingerRest, _rHand, _rCurlAxis, _rCurlSign, _rFist, _rPoint);
    }

    private void CurlHand(Transform[] bones, Quaternion[] rest, Transform hand,
                          Vector3 axisLocal, float sign, float fist, float point)
    {
        if (bones == null || rest == null || hand == null) return;

        // TRẢ MỌI ĐỐT VỀ TƯ THẾ GỐC TRƯỚC. Phải làm hết một lượt rồi mới cuộn, không xen
        // kẽ: trả gốc cho đốt cha là đốt con cũng xoay theo, nên cuộn xong đốt cha rồi mới
        // trả gốc đốt con thì phần vừa cuộn bị xoá mất.
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] != null) bones[i].localRotation = rest[i];

        Vector3 axis = hand.TransformDirection(axisLocal);

        // Duyệt từ gốc ngón ra đầu ngón. Xoay đốt gốc là cả ngón đi theo, rồi đốt sau
        // cộng thêm phần của nó - đúng cách ngón tay thật gập, và cũng là lý do thứ tự
        // trong LeftFingerBones/RightFingerBones không được đảo.
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;

            float curl = fist;

            // Ngón trỏ (đốt 3-5) DUỖI THẲNG khi đang chỉ, dù cả bàn vẫn nắm.
            if (i >= 3 && i <= 5) curl *= 1f - point;

            // Ngón cái (đốt 0-2) chỉ ngóc lên một phần - bàn tay chỉ trỏ thật vẫn giữ
            // ngón cái hơi khép, duỗi hẳn ra thành "khẩu súng ngón tay" thì hơi trẻ con.
            else if (i <= 2) curl *= 1f - point * 0.6f;

            float angle = FingerCurlAngles[i] * fingerCurlScale * curl * sign;
            bones[i].rotation = Quaternion.AngleAxis(angle, axis) * bones[i].rotation;
        }
    }

    private void HideNonArmBones(Animator anim)
    {
        HumanBodyBones[] hide =
        {
            // Đầu: camera nằm ngay trong sọ.
            HumanBodyBones.Head, HumanBodyBones.Neck, HumanBodyBones.Jaw,
            HumanBodyBones.LeftEye, HumanBodyBones.RightEye,

            // Hai chân: không bao giờ lọt vào khung hình viewmodel, mà để đó thì lủng lẳng
            // dưới camera và có thể thò vào rìa màn hình lúc nhìn xuống.
            HumanBodyBones.LeftUpperLeg,  HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,  HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot,      HumanBodyBones.RightFoot,
            HumanBodyBones.LeftToes,      HumanBodyBones.RightToes,
        };

        foreach (HumanBodyBones b in hide)
        {
            Transform t = anim.GetBoneTransform(b);
            if (t != null) t.localScale = Vector3.one * 0.0001f;
        }
    }

    /// <summary>
    /// Co tư thế nghỉ lại cho vừa tầm với thật của cánh tay.
    ///
    /// GIỮ NGUYÊN HƯỚNG mà chủ project đã chỉnh ở leftHandRest/rightHandRest - chỉ rút
    /// ngắn KHOẢNG CÁCH từ vai tới bàn tay. Nên chỉnh tay trái/phải, cao/thấp, rộng/hẹp
    /// ở Inspector vẫn ăn như thường, chỉ có độ vươn là bị ghì lại.
    ///
    /// Đo bằng xương thật nên đổi scale model hay thay nhân vật khác cũng tự đúng.
    /// </summary>
    private void MeasureRest()
    {
        _lRest = leftHandRest;
        _rRest = rightHandRest;

        MeasureArm(_lUpper, _lLower, _lHand, ref _lShoulder, ref _lReach, ref _lRest);
        MeasureArm(_rUpper, _rLower, _rHand, ref _rShoulder, ref _rReach, ref _rRest);

        // Nâng sau khi đã co cho vừa tầm với, để phép co không kéo tụt lại.
        _lRest.y += handRaise;
        _rRest.y += handRaise;
    }

    private void MeasureArm(Transform upper, Transform lower, Transform hand,
                            ref Vector3 shoulder, ref float reach, ref Vector3 rest)
    {
        if (upper == null || lower == null || hand == null) return;

        reach = Vector3.Distance(upper.position, lower.position)
              + Vector3.Distance(lower.position, hand.position);
        if (reach < 0.01f) return;

        // Vai quy về TOẠ ĐỘ CAMERA, vì tư thế nghỉ cũng tính theo camera.
        shoulder = _camera.InverseTransformPoint(upper.position);

        if (!autoScaleRest) return;

        Vector3 armVec = rest - shoulder;
        float dist = armVec.magnitude;
        if (dist < 0.01f) return;

        float want = reach * restReach01;
        if (dist <= want) return;   // đã đủ co rồi thì để yên

        rest = shoulder + armVec * (want / dist);
    }

    private void BuildCamera()
    {
        Camera mainCam = _camera.GetComponent<Camera>();
        if (mainCam == null) return;

        // Camera CHÍNH thôi vẽ layer viewmodel.
        //
        // Thiếu dòng này thì tay bị vẽ HAI LẦN: một lần bởi camera chính (nằm trong thế
        // giới thật, xuyên qua tường) và một lần bởi camera phụ. Kết quả là bóng ma tay
        // lồng vào nhau.
        mainCam.cullingMask &= ~(1 << _layer);

        GameObject camObj = new GameObject("ViewmodelCamera");
        camObj.transform.SetParent(_camera, false);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;

        _viewmodelCam = camObj.AddComponent<Camera>();

        _viewmodelCam.cullingMask = 1 << _layer;
        _viewmodelCam.fieldOfView = viewmodelFov;
        _viewmodelCam.nearClipPlane = viewmodelNearClip;
        _viewmodelCam.farClipPlane = 12f;   // tay ở ngay trước mặt, không cần nhìn xa

        // ⚠️ URP DÙNG CAMERA STACKING, KHÔNG DÙNG depth + Clear Flags.
        //
        // Đây là lý do bản trước cho ra MÀN HÌNH XANH KÍN. Ở pipeline cũ, cách làm là đặt
        // depth cao hơn và Clear Flags = Depth Only, rồi camera thứ hai vẽ đè lên.
        // URP KHÔNG hiểu cách đó: camera thứ hai mặc định là một BASE CAMERA, nghĩa là nó
        // XOÁ SẠCH MÀN HÌNH bằng màu nền của chính nó rồi mới vẽ. Màu nền mặc định của
        // Camera trong Unity là xanh xám (49,77,121) - đúng cái màu xanh phủ kín kia.
        // Và vì đó là màu MẶC ĐỊNH CỦA UNITY chứ không phải màu đội, nên đổi đội cũng
        // không đổi màu.
        //
        // Cách đúng của URP: khai camera phụ là OVERLAY rồi THÊM VÀO NGĂN XẾP của camera
        // chính. Overlay không xoá gì cả, chỉ vẽ chồng lên - và vì nó có bộ đệm chiều sâu
        // riêng nên tay vẫn không bao giờ bị tường che.
        UniversalAdditionalCameraData overlayData = _viewmodelCam.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData baseData = mainCam.GetUniversalAdditionalCameraData();

        if (overlayData == null || baseData == null)
        {
            Debug.LogError("[Viewmodel] Không lấy được dữ liệu camera URP. " +
                           "Viewmodel sẽ không hiện đúng.", this);
            return;
        }

        overlayData.renderType = CameraRenderType.Overlay;

        // Overlay không cần hậu kỳ riêng - nó đã nằm trong ảnh của camera chính rồi.
        // Bật lên là chạy bloom/tonemapping hai lần, vừa tốn vừa sai màu.
        overlayData.renderPostProcessing = false;

        // Xoá bản cũ nếu hồi sinh hay dựng lại, tránh chồng nhiều overlay vào một ngăn xếp.
        baseData.cameraStack.Remove(_viewmodelCam);
        baseData.cameraStack.Add(_viewmodelCam);

        // Không cần nghe âm thanh - AudioListener thứ hai làm Unity báo lỗi.
        AudioListener listener = camObj.GetComponent<AudioListener>();
        if (listener != null) Destroy(listener);
    }

    private static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
    }

    // ==================== ĐỘNG TÁC ====================
    //
    // Bốn hàm dưới đây được gọi từ các bộ đếm [Networked] đã có sẵn:
    // OnMeleePerformed, OnObjectFired, OnLaserFired. Chúng chạy đúng một lần mỗi hành
    // động, trên mọi máy - nhưng viewmodel chỉ tồn tại trên máy chủ nhân nên tự lọc.

    /// <summary>Cú đấm: tay phải thọc thẳng ra trước.</summary>
    public void PlayPunch()
    {
        if (!_built) return;

        // Cộng vào VẬN TỐC lò xo, không cộng vào vị trí.
        //
        // Cộng vào vị trí thì tay nhảy phắt tới đích rồi trườn về - trông như bị giật.
        // Cộng vào vận tốc thì nó phóng đi rồi lò xo kéo lại, ra đúng đường cong của một
        // cú đấm thật: bung nhanh, chạm đỉnh, thu về.
        // Nắm tay lại quanh cú đấm. Đấm bằng bàn tay xoè là thứ mắt bắt lỗi ngay.
        _punchFistTimer = punchFistHold;
        _punchTimer = punchHold;

        _rVelocity += new Vector3(-0.08f, -punchDrop, punchThrust) * poseStiffness * 0.06f;

        // Tay trái hơi lùi lại để giữ thăng bằng - cơ thể thật luôn phản lực như vậy.
        _lVelocity += new Vector3(0f, 0.02f, -0.1f) * poseStiffness * 0.04f;
    }

    /// <summary>Bắn vật đang cầm: hai tay đẩy ra rồi giật ngược.</summary>
    public void PlayFire()
    {
        if (!_built) return;

        // Xung tính theo firePush ĐẦY ĐỦ, không trừ fireRecoil - xem ghi chú ở ô
        // Fire Recoil Delay để biết vì sao phép trừ kia làm động tác biến mất.
        //
        // Hệ số 0.09 chọn để đỉnh vung tay rơi vào khoảng 0.26m, ngang với động tác hút
        // đạn - vốn là động tác duy nhất chủ project nhìn thấy được ở bản trước.
        Vector3 push = new Vector3(0f, 0.03f, firePush) * poseStiffness * 0.09f;

        _rVelocity += push;
        _lVelocity += push * 0.7f;

        _fireTimer = fireHold;

        // Hẹn nhịp giật ngược, tính từ lúc THẢ tư thế đẩy chứ không phải từ lúc bấm -
        // giật ngược trong khi tay còn đang giữ thế đẩy thì hai lực đè nhau, mất cả hai.
        _fireRecoilTimer = fireHold + fireRecoilDelay;
    }

    /// <summary>Bắn laze: tay trái vươn ra và giữ một nhịp.</summary>
    public void PlayLaser()
    {
        if (!_built) return;

        // TAY PHẢI chìa ngón trỏ ra bắn, không phải tay trái. Ngón trỏ là cử chỉ "chỉ vào"
        // ai cũng đọc được ngay, và để nó ở tay thuận thì trùng với tay đấm - người chơi
        // hiểu ngay tay phải là tay tấn công, tay trái là tay khống chế.
        _laserTimer = laserHold;
        _rVelocity += new Vector3(-0.05f, 0.04f, laserExtend) * poseStiffness * 0.05f;
    }

    // ==================== TẠO DÁNG MỖI KHUNG HÌNH ====================

    private void LateUpdate()
    {
        if (!_built) return;

        // Chỉ chạy cho nhân vật của người ngồi trước máy này. Bản sao viewmodel chỉ được
        // dựng cho họ, nhưng kiểm lại cho chắc.
        if (FPSMovement.Local != _movement) return;

        float dt = Time.deltaTime;

        UpdateSway(dt);
        BuildTargets(dt);
        StepSprings(dt);
        ApplyBodyRoll();
        ApplyIK();

        UpdateFists(dt);
        ApplyFingers();
    }

    private void UpdateSway(float dt)
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
        float dYaw = Mathf.DeltaAngle(_lastYaw, yaw);
        float dPitch = Mathf.DeltaAngle(_lastPitch, pitch);

        _lastYaw = yaw;
        _lastPitch = pitch;

        _sway.x -= dYaw * swayAmount;
        _sway.y += dPitch * swayAmount;

        _sway = Vector3.ClampMagnitude(_sway, swayMax);
        _sway = Vector3.Lerp(_sway, Vector3.zero, swayRecover * dt);
    }

    /// <summary>
    /// Tính đích của hai tay từ tư thế nghỉ, nhấp nhô, trôi, và các trạng thái đang giữ.
    /// </summary>
    private void BuildTargets(float dt)
    {
        float speed01 = _movement != null ? Mathf.Clamp01(_movement.LocalWalkSpeed01) : 0f;

        // Nhấp nhô theo bước chân, và thở nhẹ khi đứng yên. Trộn hai cái theo tốc độ nên
        // lúc dừng lại nhịp chuyển mượt sang thở chứ không tắt phụt.
        _bobPhase += dt * walkBobSpeed * Mathf.Max(speed01, 0.18f) * Mathf.PI * 2f;

        float amp = Mathf.Lerp(breatheAmount, walkBobAmount, speed01);

        // Trục DỌC: nhún theo mỗi bước chân, nên chạy NHANH GẤP ĐÔI nhịp đánh tay -
        // một chu kỳ đánh tay có hai bước chân. Bỏ số 2 đi thì thành đi cà nhắc một bên.
        float bobY = Mathf.Sin(_bobPhase * 2f) * amp;

        // Trục TRƯỚC-SAU: đánh tay. Đây mới là chuyển động người ta THẤY khi đi bộ.
        // Chỉ có khi đang thật sự di chuyển, đứng yên thì không đánh tay.
        float swing = Mathf.Sin(_bobPhase) * walkSwingAmount * speed01;

        // Trục NGANG: lắc nhẹ, cùng pha với đánh tay.
        float lateral = Mathf.Cos(_bobPhase) * walkLateralAmount * speed01;

        // HAI TAY SO LE NHAU: tay trái ra trước thì tay phải ra sau.
        //
        // Đây là điểm mấu chốt. Cùng pha thì hai tay cùng đưa ra rồi cùng thu về, trông
        // như đang bơi ếch chứ không phải đi bộ. Dấu trừ ở tay phải làm tất cả khác biệt.
        _lTarget = _lRest + _sway
                   + new Vector3(lateral, bobY, swing);

        _rTarget = _rRest + _sway
                   + new Vector3(-lateral, bobY, -swing);

        // --- ĐANG HÚT ĐẠN: kéo hai tay về ngực và tách rộng ---
        //
        // Đây là trạng thái GIỮ LIÊN TỤC nên tác động thẳng vào ĐÍCH, không phải cộng
        // xung vào vận tốc như các cú đánh một nhịp. Cộng xung liên tục thì tay bay mất.
        if (_magnet != null && _magnet.IsPulling)
        {
            _lTarget += new Vector3(-pullSpread, 0.05f, -pullDraw);
            _rTarget += new Vector3(pullSpread, 0.05f, -pullDraw);
        }

        // --- GIỮ TƯ THẾ ĐẤM ---
        //
        // Cộng thẳng vào ĐÍCH chứ không phải vào vận tốc. Xung vận tốc cho ra một đỉnh
        // nhọn rồi tắt; cộng vào đích thì tay ở NGUYÊN đó suốt punchHold giây, đủ lâu để
        // nhìn ra. Xung vận tốc vẫn giữ ở PlayPunch để cú vung có độ nảy.
        //
        // Không sợ vượt tầm tay: SolveArm tự kẹp lại ở tầm với, nên punchThrust to là ra
        // tay duỗi thẳng hết cỡ - đúng cái mình muốn cho một cú đấm.
        if (_punchTimer > 0f)
        {
            _punchTimer -= dt;
            _rTarget += new Vector3(-0.06f, -punchDrop, punchThrust);
            _lTarget += new Vector3(0f, 0.02f, -0.08f);   // tay trái lùi giữ thăng bằng
        }

        // --- GIỮ TƯ THẾ BẮN ---
        //
        // Nhẹ hơn cú đấm (0.55 lần) để hai động tác không giống hệt nhau: đấm là duỗi
        // thẳng hết cỡ, bắn là đẩy hai tay ra nhưng khuỷu còn cong.
        if (_fireTimer > 0f)
        {
            _fireTimer -= dt;
            Vector3 shove = new Vector3(0f, 0.03f, firePush * 0.55f);
            _rTarget += shove;
            _lTarget += shove;
        }

        // --- NHỊP GIẬT NGƯỢC SAU KHI BẮN ---
        //
        // Nhịp thứ hai của cú bắn: đẩy ra xong thì tay bị hất về sau. Phải là một XUNG
        // riêng đến muộn vài phần trăm giây, chứ gộp vào nhịp đẩy thì hai lực triệt tiêu
        // nhau và không còn động tác nào cả.
        if (_fireRecoilTimer > 0f)
        {
            _fireRecoilTimer -= dt;
            if (_fireRecoilTimer <= 0f)
            {
                Vector3 kick = new Vector3(0f, 0.02f, -fireRecoil) * poseStiffness * 0.06f;
                _rVelocity += kick;
                _lVelocity += kick * 0.7f;
            }
        }

        // --- GIỮ TƯ THẾ LAZE ---
        if (_laserTimer > 0f)
        {
            _laserTimer -= dt;
            _rTarget += new Vector3(-0.04f, 0.03f, laserExtend * 0.55f);
        }

        // --- ĐANG "CẦM" VẬT: TAY PHẢI KHỐNG CHẾ, TAY TRÁI NGHỈ ---
        //
        // Nhân vật không bưng cục đá trên tay - vô lý với một tảng đá hay cái bàn. Nó
        // giữ vật LƠ LỬNG bằng từ trường, nên tay chìa về phía vật và xoè bàn ra.
        //
        // Hướng lấy từ VỊ TRÍ THẬT của vật chứ không gõ cứng: vật lơ lửng hơi lệch trái
        // hay lệch phải thì tay cũng nghiêng theo, và người chơi thấy tay mình thật sự
        // đang trỏ vào nó.
        if (_magnet != null && _rReach > 0.01f)
        {
            MagneticObject held = _magnet.GetGrabbedObject();
            if (held != null)
            {
                Vector3 objLocal = _camera.InverseTransformPoint(held.transform.position);
                Vector3 toObj = objLocal - _rShoulder;

                if (toObj.sqrMagnitude > 0.0001f)
                {
                    _rTarget = _rShoulder + toObj.normalized * (_rReach * controlExtend01)
                               + _sway + new Vector3(0f, bobY * 0.4f, 0f);
                }
            }
        }
    }

    /// <summary>
    /// Chạy lò xo đưa tay về đích.
    ///
    /// Lò xo THẬT (khối lượng - lò xo - giảm chấn), không phải Lerp. Lerp chỉ trườn về một
    /// chiều và không bao giờ vọt quá; lò xo cho phép nảy ngược, và chính nhịp nảy đó làm
    /// động tác có "điểm kết" thay vì trôi tuột.
    /// </summary>
    private void StepSprings(float dt)
    {
        // Chia nhỏ bước thời gian. BẮT BUỘC: lò xo cứng (140) tích phân bằng Euler sẽ NỔ
        // TUNG nếu dt lớn - máy khựng một cái là tay văng ra vô cực. Cùng cái bẫy đã ghi
        // ở CameraShake.
        int steps = Mathf.Clamp(Mathf.CeilToInt(dt / (1f / 120f)), 1, 8);
        float sdt = dt / steps;

        for (int i = 0; i < steps; i++)
        {
            StepOne(ref _lOffset, ref _lVelocity, _lTarget, sdt);
            StepOne(ref _rOffset, ref _rVelocity, _rTarget, sdt);
        }
    }

    private void StepOne(ref Vector3 pos, ref Vector3 vel, Vector3 target, float sdt)
    {
        Vector3 accel = (target - pos) * poseStiffness - vel * poseDamping;
        vel += accel * sdt;
        pos += vel * sdt;
    }

    /// <summary>
    /// Xoay cả bộ khung viewmodel theo nhịp bước.
    ///
    /// PHẢI chạy TRƯỚC ApplyIK. Xoay khung là dời luôn vị trí vai, mà bộ giải IK đọc vị
    /// trí vai để tính chỗ khuỷu - xoay sau thì IK tính trên số liệu cũ và tay lệch khỏi
    /// đích một nhịp.
    /// </summary>
    private void ApplyBodyRoll()
    {
        if (_body == null) return;

        float speed01 = _movement != null ? Mathf.Clamp01(_movement.LocalWalkSpeed01) : 0f;

        // Nghiêng người sang hai bên theo bước chân, và gật nhẹ theo nhịp nhún.
        float roll = Mathf.Sin(_bobPhase) * walkRollAmount * speed01;
        float pitch = Mathf.Sin(_bobPhase * 2f) * walkRollAmount * 0.35f * speed01;

        Quaternion pose = Quaternion.Euler(pitch, 0f, roll);

        if (_shake == null || shakeFollow >= 0.999f)
        {
            _body.localRotation = pose;
            _body.localPosition = _bodyBasePos;
            return;
        }

        // TRỪ BỚT PHẦN RUNG MÀ CAMERA CHA ĐÃ ÁP.
        //
        // Camera cha xoay đủ 100% cú rung, tay là con nên ăn theo hết. Xoay NGƯỢC lại
        // (1 - shakeFollow) ở đây thì phần còn dính vào tay đúng bằng shakeFollow.
        float cancel = 1f - shakeFollow;

        Quaternion undo = Quaternion.Slerp(Quaternion.identity,
                                           Quaternion.Inverse(Quaternion.Euler(_shake.RotationOffset)),
                                           cancel);

        _body.localRotation = undo * pose;

        // Làm y hệt với phần TỊNH TIẾN. Độ lệch vị trí do CameraShake tính trong hệ toạ độ
        // THÂN nhân vật (xem cameraBaseLocalPosition trong FPSMovement), nên phải đưa về
        // hệ của camera rồi mới trừ - trừ thẳng là sai trục khi đang ngẩng hay cúi đầu.
        Vector3 off = _shake.PositionOffset;
        if (_camera.parent != null) off = _camera.parent.TransformDirection(off);

        _body.localPosition = _bodyBasePos - _camera.InverseTransformDirection(off) * cancel;
    }

    private void ApplyIK()
    {
        Vector3 pole = _camera.TransformDirection(elbowHint.normalized);

        SolveArm(_lUpper, _lLower, _lHand, _camera.TransformPoint(_lOffset),
                 _camera.TransformDirection(new Vector3(-elbowHint.x, elbowHint.y, elbowHint.z).normalized));

        SolveArm(_rUpper, _rLower, _rHand, _camera.TransformPoint(_rOffset), pole);
    }

    /// <summary>
    /// Giải IK hai xương cho một cánh tay: vai -> khuỷu -> bàn tay.
    ///
    /// ⚠️ VÌ SAO TỰ VIẾT THAY VÌ ĐẶT GÓC XOAY XƯƠNG:
    ///
    /// Mỗi rig đặt trục xương một kiểu - có rig để trục X dọc theo xương, rig khác để
    /// trục Y. Gán localRotation tuyệt đối thì phải biết đúng quy ước của rig này, đoán
    /// sai là tay xoắn quẹo. Bản trước đã dính đúng lỗi đó và phải dặn "nếu tay vung ra
    /// sau thì đổi dấu".
    ///
    /// Cách ở đây KHÔNG cần biết quy ước nào cả: nó đo hướng xương ĐANG có, tính hướng
    /// MONG MUỐN, rồi xoay một góc delta giữa hai hướng. Đúng với mọi rig, không phải thử.
    ///
    /// Toán: biết ba cạnh của tam giác vai-khuỷu-tay thì định lý cosin cho ra góc ở vai.
    /// Từ đó dựng lại vị trí khuỷu, rồi hướng hai xương theo đó.
    /// </summary>
    private void SolveArm(Transform upper, Transform lower, Transform hand,
                          Vector3 target, Vector3 poleDir)
    {
        if (upper == null || lower == null || hand == null) return;

        float upperLen = Vector3.Distance(upper.position, lower.position);
        float lowerLen = Vector3.Distance(lower.position, hand.position);

        if (upperLen < 0.0001f || lowerLen < 0.0001f) return;

        Vector3 shoulder = upper.position;
        Vector3 toTarget = target - shoulder;
        float dist = toTarget.magnitude;

        if (dist < 0.0001f) return;

        // Với tay không: mục tiêu xa hơn tầm tay thì duỗi thẳng, đừng cố kéo dài xương.
        float maxReach = (upperLen + lowerLen) * 0.999f;
        dist = Mathf.Clamp(dist, Mathf.Abs(upperLen - lowerLen) + 0.001f, maxReach);

        Vector3 dir = toTarget.normalized;

        // Định lý cosin: góc giữa cánh tay trên và đường thẳng vai->đích.
        float cos = (upperLen * upperLen + dist * dist - lowerLen * lowerLen) / (2f * upperLen * dist);
        float angle = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f));

        // Trục để bẻ khuỷu ra khỏi đường thẳng. Lấy theo hướng gợi ý, và bỏ phần song song
        // với dir - nếu không thì khi gợi ý trùng hướng đích, tích có hướng ra vector 0
        // và cả cánh tay sập thành đường thẳng.
        Vector3 bendAxis = Vector3.Cross(dir, poleDir);
        if (bendAxis.sqrMagnitude < 0.0001f) bendAxis = Vector3.Cross(dir, Vector3.up);
        if (bendAxis.sqrMagnitude < 0.0001f) bendAxis = Vector3.right;
        bendAxis.Normalize();

        Vector3 upperDir = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, bendAxis) * dir;
        Vector3 elbow = shoulder + upperDir * upperLen;

        AimBone(upper, lower, upperDir);
        AimBone(lower, hand, (target - elbow).normalized);
    }

    /// <summary>
    /// Xoay một xương sao cho con của nó chỉ theo hướng mong muốn.
    /// Không cần biết trục cục bộ của xương - chỉ đo hướng hiện tại rồi xoay delta.
    /// </summary>
    private static void AimBone(Transform bone, Transform child, Vector3 desiredDir)
    {
        Vector3 currentDir = child.position - bone.position;
        if (currentDir.sqrMagnitude < 0.000001f) return;

        bone.rotation = Quaternion.FromToRotation(currentDir.normalized, desiredDir) * bone.rotation;
    }
}
