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

    [Header("Tư thế CẢNH GIÁC khi đứng yên (toạ độ theo CAMERA)")]
    [Tooltip("Bàn tay TRÁI - tay PHỤ, hạ thấp và đẩy ra xa cho nhỏ bớt.\n\n" +
             "⚠️ Hai ô cũ Left/Right Hand Rest đã bỏ. Chúng đặt hai tay ĐỐI XỨNG NHAU " +
             "hoàn toàn - cùng độ cao, cùng độ vươn - và thứ đó luôn trông vô hồn, bất kể " +
             "chỉnh số thế nào. Cơ thể người ở trạng thái sẵn sàng KHÔNG BAO GIỜ đối xứng: " +
             "luôn có một tay dẫn và một tay thủ, như tư thế thủ của võ sĩ.\n\n" +
             "Chia vai trò theo ĐÚNG THỨ MỖI TAY LÀM trong code, không theo cảm tính:\n\n" +
             "  Tay PHẢI làm ba trong bốn động tác - đấm, bắn laze, giữ vật lơ lửng. Nó là " +
             "tay bận rộn nên nằm sẵn ở tư thế dẫn: cao, gần người, thấy rõ.\n\n" +
             "  Tay TRÁI chỉ có mỗi cú đẩy. Nó lùi xuống thấp và ra xa, chỉ đủ hiện diện.\n\n" +
             "⚠️ ĐỪNG ĐỘNG VÀO TAY PHẢI khi chủ project chỉ than phiền về tay trái. Đã " +
             "mắc lỗi này một lần: được nhờ hạ tay trái xuống, lại đi nâng tay phải lên, " +
             "nên trên màn hình nhìn y như cũ - chỉ đổi xem tay nào cao hơn tay nào.")]
    public Vector3 alertLeftHand = new Vector3(-0.2f, -0.2f, 0.46f);

    [Tooltip("Bàn tay PHẢI - tay DẪN, đưa cao và giữ gần người, sẵn sàng bung ra.")]
    public Vector3 alertRightHand = new Vector3(0.25f, -0.15f, 0.38f);

    [Tooltip("Tay TRÁI vươn xa thêm bao nhiêu lần so với Rest Reach 01. Trên 1 là ra xa, nhỏ lại.\n\n" +
             "⚠️ ĐÂY MỚI LÀ Ô LÀM TAY NHỎ LẠI TRÊN MÀN HÌNH, không phải toạ độ ở ô trên.\n\n" +
             "Lý do: MeasureRest chỉ lấy HƯỚNG từ ô toạ độ kia rồi ép khoảng cách về đúng " +
             "Rest Reach 01. Nên gõ z lớn hơn chỉ làm tay chếch ra trước nhiều hơn, chứ " +
             "khoảng cách tới mắt gần như không đổi - và kích thước trên màn hình cũng vậy.\n\n" +
             "Muốn tay nhỏ đi thì phải cho nó ra XA MẮT hơn thật, tức tăng ô này.")]
    [Range(0.6f, 1.4f)]
    public float alertLeftDraw = 1.35f;

    [Tooltip("Tay PHẢI thu về còn bao nhiêu phần. Dưới 1 là gần người hơn, tức to và rõ hơn.\n\n" +
             "Chênh lệch ĐỘ VƯƠN mới là thứ tạo chiều sâu. Chỉ khác độ cao thôi thì hai " +
             "tay vẫn nằm trên cùng một mặt phẳng, nhìn vẫn phẳng lì.")]
    [Range(0.5f, 1.2f)]
    public float alertRightDraw = 0.92f;

    [Tooltip("Độ nắm của bàn tay TRÁI lúc cảnh giác. Thấp hơn Idle Curl của tay phải.\n\n" +
             "Tay trái nắm hờ buông xuôi, tay phải (theo ô Idle Curl) nắm chặt thành đấm " +
             "sẵn sàng. Hai bàn tay khác thế là một tầng bất đối xứng nữa.")]
    [Range(0f, 1f)]
    public float alertLeftCurl = 0.55f;

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
    [Tooltip("Bật thì tay TỰ GHÌ LẠI một phần rung của camera, theo ô Shake Follow bên dưới.\n\n" +
             "⚠️ MẶC ĐỊNH TẮT, và nên để tắt. Ghì một phần nghĩa là tay chuyển động NGƯỢC " +
             "chiều camera đúng phần chênh - lúc đứng yên ăn một cú đấm thì hay, nhưng lúc " +
             "ĐI BỘ thì camera nhấp nhô liên tục theo bước chân, và tay cứ lắc ngược lại " +
             "từng nhịp. Mắt đọc thành RUNG GIẬT chứ không thành sức nặng.\n\n" +
             "Tắt thì tay là con của camera đúng nghĩa: camera đi đâu tay theo đó, tuyệt " +
             "đối không rung tương đối. Nhịp đi bộ của tay đã có Walk Roll Amount lo riêng.")]
    public bool decoupleFromShake = false;

    [Tooltip("Chỉ có tác dụng khi bật Decouple From Shake. Tay hứng bao nhiêu phần rung " +
             "của camera. 1 = dính cứng, 0 = đứng im hoàn toàn.\n\n" +
             "Tay là con của camera nên mặc định nó ăn TRỌN 100% cú rung - lắc 3.5 độ ở " +
             "25 lần/giây. Vấn đề là cả khung hình lẫn bàn tay rung y hệt nhau, nên mắt " +
             "không thấy tay rung, chỉ thấy tay DÁN CHẾT vào màn hình. Ngố đúng chỗ đó.\n\n" +
             "Ghì lại còn một phần thì tay hơi trễ so với khung hình, và chính độ trễ đó " +
             "làm tay trông có khối lượng thật. Mọi game bắn súng đều tách hai thứ này ra.")]
    [Range(0f, 1f)]
    public float shakeFollow = 0.4f;

    [Header("Tia điện trên găng")]
    [Tooltip("Tia điện chạy trên găng tay, màu theo điện tích đang mang.\n\n" +
             "⚠️ CHỈ NGƯỜI CHƠI NÀY THẤY. Đối thủ nhìn sang thì thấy hiệu ứng khác hẳn - " +
             "quả cầu sáng trong lòng bàn tay, do PlayerVisuals lo. Hai thứ độc lập.\n\n" +
             "Lý do phải làm riêng: quả cầu của PlayerVisuals gắn vào xương của THÂN THẬT, " +
             "mà thân thật bị giấu đi ở góc nhìn thứ nhất (Hide Own Body). Còn bản sao " +
             "viewmodel thì bị xoá sạch mọi script lúc dựng, nên nó không mang theo gì cả. " +
             "Kết quả là chủ nhân vật KHÔNG hề thấy điện tích găng của chính mình.")]
    public bool enableGloveArcs = true;

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
    [Tooltip("Biên độ ĐÁNH TAY khi đi bộ, mét. Đây là ô DUY NHẤT chỉnh nhịp đi bộ.\n\n" +
             "⚠️ NĂM Ô CŨ (Walk Bob / Swing / Lateral / Vertical Swing / Motion Scale) ĐÃ " +
             "BỎ HẲN, vì chúng dính bẫy giá trị Inspector đè giá trị code.\n\n" +
             "Chuyện đã xảy ra: code nâng Walk Swing Amount lên 0.24, nhưng prefab vẫn giữ " +
             "0.11 từ trước và không ai sửa tay. Rồi Walk Motion Scale 0.45 nhân vào nữa, " +
             "còn 0.05m - năm centimet, nhỏ hơn chính bàn tay. Đó là lý do 'không thấy " +
             "đánh tay gì'.\n\n" +
             "Ô này MỚI HOÀN TOÀN nên chắc chắn lấy đúng số ở đây. Ba thành phần còn lại " +
             "(nhún dọc, lắc ngang, nhấp so le, xoay khung) suy ra theo tỉ lệ cố định từ nó, nên chỉnh " +
             "một chỗ là cả nhịp đi bộ to nhỏ theo, không lệch nhau.")]
    [Range(0f, 0.5f)]
    public float walkSwingReach = 0.2f;

    [Tooltip("Đổi Walk Swing Reach ra GÓC RƠI XUỐNG của cánh tay: bao nhiêu độ trên mỗi mét.\n\n" +
             "⚠️ ĐÂY LÀ CUNG RƠI MỘT CHIỀU, không phải biên độ hai chiều. Điểm CAO NHẤT " +
             "của nhịp vung luôn đúng bằng tư thế nghỉ - tay không bao giờ vung lên cao hơn " +
             "chỗ nó đứng lúc rảnh.\n\n" +
             "Bản trước dao động đều hai bên tư thế nghỉ, nên nửa nhịp nào tay cũng nhấc " +
             "cao hơn lúc đứng yên - vừa che tầm nhìn vừa không giống người đi bộ.\n\n" +
             "Mặc định 0.2m x 200 = 40 độ. Hai tay so le: tay này ở đỉnh (ngang tư thế " +
             "nghỉ) thì tay kia đang ở đáy, chúc xuống khỏi khung hình. Đó chính là cảnh " +
             "mắt người thật nhìn xuống lúc đi: mỗi lúc chỉ thấy MỘT tay.")]
    public float walkSwingDegPerMeter = 200f;

    [Tooltip("Nhịp đánh tay bằng bao nhiêu phần nhịp nhấp nhô của camera. 0.5 = chậm bằng nửa.\n\n" +
             "⚠️ Tần số GỐC không nằm ở đây mà ở CameraShake > Bob Frequency (đang để 5), vì " +
             "hai bên dùng CHUNG một pha - đó là cách đã chữa lỗi tay rung lắc khi đi bộ.\n\n" +
             "Nhưng dùng chung nguyên xi thì tay đánh 5 nhịp mỗi giây, nhanh như đang bơi. " +
             "Người thật đi bộ đánh tay khoảng 1 nhịp/giây, vì MỘT chu kỳ đánh tay có HAI " +
             "bước chân.\n\n" +
             "Ô này chia nhỏ nhịp tay xuống mà VẪN GIỮ KHOÁ PHA - vì nó nhân vào cùng một " +
             "pha chứ không đếm riêng một đồng hồ khác. Nên đặt số nào cũng không quay lại " +
             "cảnh rung lắc. Muốn đúng kiểu người thật thì để 0.5.")]
    [Range(0.1f, 1f)]
    public float walkSwingRate = 0.5f;

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

    [Tooltip("Ba động tác thọc tay (đấm / bắn / đẩy) nhắm VƯỢT QUA đích bấy nhiêu lần.\n\n" +
             "⚠️ ĐÂY LÀ Ô LÀM ĐỘNG TÁC DỨT KHOÁT. Lò xo cần khoảng 0.085 giây để đi được " +
             "63% quãng đường; đặt đích đúng chỗ tay duỗi thẳng thì trong 0.13 giây giữ tư " +
             "thế nó mới tới được chừng 78% - tay KHÔNG BAO GIỜ thẳng hẳn, và động tác " +
             "trông nhũn.\n\n" +
             "Nhắm ra xa hơn tầm tay thì lò xo phóng nhanh hơn hẳn và ĐI QUA điểm duỗi " +
             "thẳng. Không sợ tay dài ra: SolveArm kẹp mọi đích ở đúng tầm với, nên phần " +
             "vượt quá chỉ biến thành TỐC ĐỘ chứ không thành độ dài.")]
    [Range(1f, 2f)]
    public float poseOvershoot = 1.55f;

    [Header("Động tác — ĐẤM")]
    [Tooltip("Tay phải thọc ra trước bao nhiêu mét.")]
    public float punchThrust = 0.55f;


    [Tooltip("Cả thân người chồm ra trước bấy nhiêu mét khi đấm.\n\n" +
             "Đây là thứ làm cú đấm CÓ SỨC NẶNG. Chỉ duỗi tay thì chỉ có cánh tay đấm; " +
             "chồm cả người ra thì VAI cũng tiến lên, và mắt đọc ra là cả cơ thể dồn vào " +
             "cú đó - đúng cách người thật đấm.")]
    public float punchLunge = 0.07f;

    [Tooltip("Hướng cú đấm, tính theo CAMERA. (0,0,1) là thẳng trước mặt.\n\n" +
             "⚠️ ĐO TỪ MẮT, KHÔNG ĐO TỪ VAI - đây là chỗ chữa bệnh 'đấm xuống đất'.\n\n" +
             "Bản trước lấy hướng từ VAI. Mà vai nằm thấp hơn mắt 25cm, nên đấm nằm ngang " +
             "từ vai thì nắm đấm dừng ở 24cm dưới tầm mắt - tức 29 độ dưới trục nhìn, " +
             "trong khi khung hình chỉ mở 22.5 độ mỗi bên. Nắm đấm rơi HẲN XUỐNG DƯỚI mép " +
             "màn hình, nhìn y như đang đấm xuống đất.\n\n" +
             "Ô Punch Rise cũ càng làm tệ hơn: nó nâng VAI lên, mà đích thì vẫn đứng yên " +
             "một chỗ, nên cánh tay chúc xuống dốc hơn nữa. Đó là lý do đặt 0.21 thì hỏng. " +
             "Ô đó đã bỏ hẳn.\n\n" +
             "Đo từ mắt thì gõ hướng nào là nắm đấm đi đúng chỗ đó trên màn hình, bất kể " +
             "vai nằm đâu.")]
    public Vector3 punchAim = new Vector3(0.16f, -0.1f, 1f);

    [Tooltip("Lúc rút, nắm đấm cách MẮT bao nhiêu mét. Nhỏ = sát mặt, to trên màn hình.")]
    public float punchWindupDist = 0.2f;

    [Tooltip("Lúc thọc hết, nắm đấm cách MẮT bao nhiêu mét.\n\n" +
             "Để xa hơn tầm tay là cố ý: bộ giải IK tự kẹp lại ở tầm với, nên phần dư chỉ " +
             "biến thành tốc độ và giữ cho tay duỗi thẳng cứng suốt nhịp thọc.")]
    public float punchStrikeDist = 0.62f;

    [Tooltip("Cả thân người VẶN sang trái bấy nhiêu độ khi đấm.\n\n" +
             "Đây là ô làm THẤY ĐƯỢC CẢ CÁNH TAY. Vai phải nằm thấp và lệch hẳn sang bên " +
             "phải khung hình, nên khi tay duỗi thẳng ra trước thì phần lớn bắp tay và " +
             "cẳng tay nằm ngoài mép màn hình - chỉ mỗi nắm đấm lọt vào.\n\n" +
             "Vặn người sang trái là đưa VAI PHẢI ra trước và vào trong, kéo theo cả cánh " +
             "tay vào giữa khung hình. Cơ thể thật cũng vặn hông khi đấm, chính vì thế.")]
    public float punchTwist = 16f;

    [Tooltip("Góc nhìn camera viewmodel NỞ RA bấy nhiêu độ ở đỉnh cú đấm.\n\n" +
             "Mẹo của ống kính góc rộng: FOV càng rộng thì thứ ở gần càng bị kéo giãn về " +
             "phía người xem. Nở FOV đúng lúc nắm đấm lao tới làm cả cánh tay VỌT RA " +
             "trong một nhịp - hiệu ứng mạnh hơn nhiều so với việc duỗi tay thêm vài " +
             "centimet, mà lại không tốn tầm với nào cả.\n\n" +
             "Chỉ đụng camera viewmodel, không đụng camera chính - thế giới đứng yên, chỉ " +
             "cánh tay vọt lên.")]
    public float punchFovPunch = 7f;

    [Tooltip("Rút tay về bao nhiêu giây TRƯỚC khi thọc ra. Cộng thêm vào Punch Hold.\n\n" +
             "⚠️ ĐÂY LÀ THỨ QUYẾT ĐỊNH CÚ ĐẤM CÓ LỰC HAY KHÔNG, và là lý do bản trước " +
             "'chỉ đưa tay ra một chút như khều nhẹ'.\n\n" +
             "Nguyên nhân là HÌNH HỌC, không phải biên độ: cú thọc đi dọc theo TRỤC NHÌN " +
             "của camera, mà chuyển động dọc trục nhìn gần như không dời đi đâu trên màn " +
             "hình - nó chỉ làm vật to lên hay nhỏ đi. Tệ hơn nữa, vai nằm THẤP VÀ SAU " +
             "camera, nên duỗi tay ra là nắm đấm đi XA MẮT HƠN, tức là NHỎ LẠI. Duỗi hết " +
             "sức mà hình ảnh thu nhỏ dần thì không thể nào ra vẻ mạnh được.\n\n" +
             "Cách chữa là pha rút tay: kéo nắm đấm về sát mặt trước đã. Lúc đó nó chiếm " +
             "một mảng lớn màn hình, rồi lao ra và nhỏ dần. Chính cú ĐỔI KÍCH THƯỚC LỚN " +
             "cộng với quãng đường dài trên màn hình mới là thứ mắt đọc thành sức mạnh.")]
    public float punchWindupHold = 0.1f;

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

    [Tooltip("Rút hai tay về bao nhiêu giây trước khi đẩy ra. Cộng thêm vào Fire Hold.\n\n" +
             "Cùng lý do với Punch Windup Hold, và cũng chữa cùng một bệnh: không có nhịp " +
             "rút thì tay xuất phát từ CHỖ NÓ ĐANG ĐỨNG - mà lúc đang giữ vật lơ lửng, chỗ " +
             "đó là đâu đó trên cao chỉ vào vật. Lao từ trên cao xuống đường bắn thì ra " +
             "một VÒNG CUNG, chứ không phải một cú đẩy thẳng.\n\n" +
             "Có nhịp rút thì mọi cú bắn đều bắt đầu từ CÙNG MỘT CHỖ, nằm ngay trên đường " +
             "đẩy - nên nó luôn là đường thẳng, bất kể trước đó tay ở đâu.")]
    public float fireWindupHold = 0.07f;

    [Tooltip("Lúc rút, hai tay co về còn bao nhiêu phần trăm tầm với. Nhỏ = co sát người.")]
    [Range(0.2f, 0.8f)]
    public float fireWindup01 = 0.4f;

    [Tooltip("Khi bắn/đẩy, hai tay vươn tới bao nhiêu phần trăm tầm với.\n\n" +
             "⚠️ Phải ĐO TỪ VAI chứ không cộng thêm vào tư thế nghỉ. Lý do: lúc đang giữ " +
             "vật lơ lửng, tay phải đã vươn sẵn 82% rồi - cộng thêm một đoạn nhỏ vào đó " +
             "gần như không dời đi đâu cả, và cú bắn biến mất.\n\n" +
             "Đo từ vai thì dù tay đang ở đâu, nó cũng bị kéo thẳng tới đúng chỗ này.")]
    [Range(0.5f, 1f)]
    public float fireExtend01 = 0.92f;

    [Header("Động tác — ĐẨY VẬT")]
    [Tooltip("Tay TRÁI vươn tới bao nhiêu phần trăm tầm với khi đẩy vật.")]
    [Range(0.5f, 1f)]
    public float pushExtend01 = 0.98f;

    [Tooltip("Tay trái dang RA NGOÀI bao nhiêu khi đẩy, tính theo phần của tầm với.\n\n" +
             "Đẩy mà tay đi thẳng trước mặt thì trông y hệt cú bắn. Dang ra ngoài mới ra " +
             "dáng xoè bàn tay chặn và hất một vật đang lơ lửng.")]
    [Range(0f, 0.5f)]
    public float pushSpread = 0.18f;

    [Tooltip("Giữ tư thế đẩy bao nhiêu giây.")]
    public float pushHold = 0.16f;

    [Tooltip("Cả thân chồm ra trước bấy nhiêu mét khi bắn hoặc đẩy.\n\n" +
             "⚠️ Đây là cách DUY NHẤT đưa tay ra trước thêm được nữa. Lúc bắn, tay đã " +
             "duỗi thẳng hết cỡ rồi (SolveArm kẹp ở đúng tầm với), nên tăng Fire Extend 01 " +
             "hay Pose Overshoot lên bao nhiêu cũng ra cùng một tư thế - không dài thêm " +
             "một milimet nào.\n\n" +
             "Muốn bàn tay đi xa hơn thì phải dời cả VAI ra trước. Cùng nguyên lý với " +
             "Punch Lunge.")]
    public float fireLunge = 0.06f;

    [Header("Động tác — BẮN LAZE")]
    [Tooltip("Tay trái vươn ra trước bao nhiêu mét.")]
    public float laserExtend = 0.42f;

    [Tooltip("Giữ tư thế vươn tay bao nhiêu giây sau mỗi phát.")]
    public float laserHold = 0.18f;

    [Header("Động tác — HÚT ĐẠN")]
    [Tooltip("Khi hút, hai tay ở bao nhiêu phần trăm tầm với. Nhỏ = co sát người.\n\n" +
             "Thay cho ô Pull Draw cũ vốn CỘNG một đoạn lùi vào tư thế nghỉ. Cộng lùi thì " +
             "tay chỉ thụt về dọc trục nhìn, mà lùi dọc trục nhìn thì trên màn hình gần " +
             "như không thấy gì - đúng lý do cú hút trông yếu ớt.\n\n" +
             "Đo từ vai thì đặt được hai tay lên NGANG MẶT và DANG RỘNG, tức là dịch " +
             "chuyển thật sự trên màn hình chứ không phải chỉ đổi độ sâu.")]
    [Range(0.3f, 0.9f)]
    public float pullExtend01 = 0.62f;

    [Tooltip("Biên độ ghì run khi đang hút, phần của tầm với. Nhỏ thôi.\n\n" +
             "Giữ nguyên một tư thế chết cứng thì trông như ảnh chụp. Rung nhẹ theo nhịp " +
             "mới ra vẻ đang GẮNG SỨC ghì một vật nặng.")]
    public float pullStrain = 0.045f;

    [Tooltip("Số nhịp ghì run mỗi giây khi hút.")]
    public float pullStrainSpeed = 9f;

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

    // Pha ĐÁNH TAY - chậm hơn pha nhấp nhô theo walkSwingRate. ApplyBodyRoll cũng đọc ô này.
    private float _swingPhase;
    private float _laserTimer;
    private float _fireRecoilTimer;
    private float _punchTimer, _fireTimer, _pushTimer;

    // Pháp tuyến LÒNG BÀN TAY, ghi theo toạ độ bàn tay. Dùng để xoay bàn tay hướng lòng
    // về phía vật khi đẩy.
    private Vector3 _lPalmLocal, _rPalmLocal;

    // Tư thế GỐC của bàn tay trái, và độ mạnh của phép xoay lòng bàn tay (0-1).
    //
    // ⚠️ CÙNG CÁI BẪY ĐÃ GẶP Ở NGÓN TAY: xoay bàn tay là ghi đè localRotation của nó, mà
    // bộ giải IK chỉ xoay bắp tay và cẳng tay - không ai trả bàn tay về chỗ cũ. Nên cú
    // xoay lúc đẩy CÒN NGUYÊN mãi mãi sau đó, bàn tay vẹo một góc lớn, da bị kéo giãn
    // thành hình thù vô nghĩa. Phải chụp tư thế gốc rồi trả về mỗi khung hình.
    private Quaternion _lHandRest = Quaternion.identity;
    private Quaternion _rHandRest = Quaternion.identity;

    // Tư thế GỐC của bốn xương tay. Xem ResetArmPose() để biết vì sao bắt buộc phải có.
    private Quaternion _lUpperRest = Quaternion.identity, _lLowerRest = Quaternion.identity;
    private Quaternion _rUpperRest = Quaternion.identity, _rLowerRest = Quaternion.identity;
    private float _pushPalm;

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

    /// <summary>
    /// Đầu ngón trỏ phải - chỗ tia laze phóng ra. Null nếu chưa dựng xong hoặc rig thiếu xương.
    ///
    /// Chỉ số 5 là đốt cuối ngón trỏ, theo đúng thứ tự khai trong RightFingerBones.
    /// </summary>
    public Transform RightIndexTip =>
        _built && _rFingers != null && _rFingers.Length > 5 ? _rFingers[5] : null;

    /// <summary>Vị trí nắm đấm phải. Null nếu viewmodel chưa dựng (tức không phải nhân vật mình).</summary>
    public Transform RightFist => RightIndexTip != null ? RightIndexTip : (_built ? _rHand : null);

    /// <summary>Hướng camera đang nhìn.</summary>
    public Vector3 CameraForward => _camera != null ? _camera.forward : transform.forward;

    /// <summary>
    /// Đổi một điểm thuộc viewmodel ra điểm trong THẾ GIỚI trùng đúng chỗ đó TRÊN MÀN HÌNH.
    ///
    /// ⚠️ VÌ SAO KHÔNG DÙNG THẲNG position: viewmodel được vẽ bằng camera FOV 45, còn thế
    /// giới vẽ bằng camera chính FOV 60. Cùng một toạ độ mà chiếu qua hai camera thì rơi
    /// vào HAI CHỖ KHÁC NHAU trên màn hình - lệch khoảng 28% về phía tâm.
    ///
    /// Nên hiệu ứng nào xuất phát từ nắm đấm mà bay vào thế giới (quyền khí) thì phải lấy
    /// điểm xuất phát kiểu này. Lấy thẳng position là luồng khí mọc ra từ một chỗ trống
    /// cách nắm đấm cả gang tay trên màn hình.
    ///
    /// Cách đổi: hỏi camera viewmodel "điểm này nằm ở đâu trên màn hình, sâu bao nhiêu",
    /// rồi hỏi camera chính "điểm nào nằm đúng chỗ đó, đúng độ sâu đó".
    /// </summary>
    public bool TryGetScreenAlignedWorldPoint(Transform t, out Vector3 world, float towardCenter = 0f)
    {
        world = default;
        if (!_built || t == null || _viewmodelCam == null || _camera == null) return false;

        Camera main = _camera.GetComponent<Camera>();
        if (main == null) return false;

        Vector3 vp = _viewmodelCam.WorldToViewportPoint(t.position);
        if (vp.z <= 0.01f) return false;

        // towardCenter: kéo toạ độ màn hình về tâm (0.5, 0.5) bấy nhiêu phần, giữ độ sâu.
        if (towardCenter > 0f)
        {
            vp.x = Mathf.Lerp(vp.x, 0.5f, towardCenter);
            vp.y = Mathf.Lerp(vp.y, 0.5f, towardCenter);
        }

        world = main.ViewportToWorldPoint(vp);
        return true;
    }

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
        BuildGloveArcs();

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

        if (_lHand  != null) _lHandRest  = _lHand.localRotation;
        if (_rHand  != null) _rHandRest  = _rHand.localRotation;
        if (_lUpper != null) _lUpperRest = _lUpper.localRotation;
        if (_lLower != null) _lLowerRest = _lLower.localRotation;
        if (_rUpper != null) _rUpperRest = _rUpper.localRotation;
        if (_rLower != null) _rLowerRest = _rLower.localRotation;

        CaptureFingers(anim, LeftFingerBones,  ref _lFingers, ref _lFingerRest,
                       _lHand, -1f, ref _lCurlAxis, ref _lCurlSign, ref _lPalmLocal);
        CaptureFingers(anim, RightFingerBones, ref _rFingers, ref _rFingerRest,
                       _rHand, +1f, ref _rCurlAxis, ref _rCurlSign, ref _rPalmLocal);

        DetachArms(anim, copy.transform);
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
                                ref Vector3 axisLocal, ref float sign, ref Vector3 palmLocal)
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

        // PHÁP TUYẾN LÒNG BÀN TAY, suy từ cùng bộ dữ liệu.
        //
        // Tích có hướng của (hướng ngón) và (trục ngang khớp) cho ra pháp tuyến của mặt
        // bàn tay. Nó chỉ về phía MU BÀN TAY hay LÒNG BÀN TAY thì tuỳ tay trái hay phải -
        // đúng cùng lý do đối xứng gương đã gặp ở chiều cuộn ngón, nên dùng lại handSign.
        Vector3 palmWorld = Vector3.Cross(fingerDir, axisWorld) * -handSign;
        if (palmWorld.sqrMagnitude > 0.000001f)
        {
            palmLocal = hand.InverseTransformDirection(palmWorld.normalized);
        }

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

        // TAY TRÁI phụ hoạ, và lúc rảnh thì HÉ MỞ chứ không nắm chặt như tay phải -
        // xem ghi chú ở ô Alert Left Curl.
        float lFistWant = alertLeftCurl;
        if (_pushTimer > 0f)   lFistWant = 0f;
        else if (pulling)      lFistWant = 0.1f;

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

    /// <summary>
    /// Xoay bàn tay TRÁI sao cho LÒNG BÀN TAY hướng về phía đang đẩy.
    ///
    /// Bộ giải IK chỉ lo bàn tay ĐẾN ĐÚNG CHỖ, hoàn toàn không quản nó úp hay ngửa -
    /// hướng đó thừa hưởng từ cẳng tay. Nên tay có thể tới đúng vị trí mà lòng bàn tay
    /// lại quay lên trời, nhìn như đang bê khay chứ không phải đang đẩy.
    ///
    /// Cách xoay dùng lại đúng mẹo của AimBone: đo pháp tuyến lòng bàn tay ĐANG có, rồi
    /// xoay đúng phần chênh tới hướng mong muốn. Không cần biết quy ước trục của rig.
    ///
    /// Chạy SAU ApplyFingers vì xoay bàn tay là xoay theo cả chùm ngón - cuộn ngón trước
    /// rồi xoay bàn sau thì ngón vẫn giữ nguyên thế cuộn, đúng thứ tự cần có.
    /// </summary>
    private void AimPalmForPush(float dt)
    {
        if (_lHand == null) return;

        // Không phải tự trả bàn tay về gốc nữa - ResetArmPose() đã lo, cho cả sáu xương.
        if (_lPalmLocal.sqrMagnitude < 0.000001f) return;

        // Tắt dần chứ không cắt phụt: hết giờ đẩy mà bàn tay bật về tư thế cũ trong đúng
        // một khung hình thì trông như lỗi hiển thị.
        _pushPalm = Mathf.MoveTowards(_pushPalm, _pushTimer > 0f ? 1f : 0f, fistSpeed * dt);
        if (_pushPalm <= 0.001f) return;

        // Hướng đẩy, quy về thế giới. Cùng công thức với tư thế tay ở BuildTargets nên
        // lòng bàn tay luôn vuông góc với đường tay đang vươn tới.
        Vector3 want = _camera.TransformDirection(new Vector3(-pushSpread, 0.1f, 1f).normalized);
        Vector3 palm = _lHand.TransformDirection(_lPalmLocal);

        Quaternion full = Quaternion.FromToRotation(palm, want);
        _lHand.rotation = Quaternion.Slerp(Quaternion.identity, full, _pushPalm) * _lHand.rotation;
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

    /// <summary>
    /// Tách hai cánh tay ra khỏi cột sống, gắn thẳng vào gốc viewmodel.
    ///
    /// ⚠️ VÌ SAO PHẢI TÁCH: đây là điều kiện để giấu được THÂN NGƯỜI.
    ///
    /// Bản sao viewmodel là một cơ thể đầy đủ, không phải cặp tay rời. Bản trước chỉ thu
    /// nhỏ đầu và hai chân, còn hông - cột sống - ngực thì vẫn nguyên vẹn ngay dưới
    /// camera. Nhìn thẳng thì chúng nằm dưới mép dưới nên không thấy; NGƯỚC XUỐNG thì
    /// khối ngực đó lọt vào khung hình, và vì camera nằm bên trong nó nên ta nhìn thấy
    /// MẶT TRONG của lồng ngực - đúng cảm giác "camera bị dồn trong người".
    ///
    /// Không thu nhỏ thẳng xương hông được, vì hai cánh tay là CON của nó: thu nhỏ hông
    /// là tay teo theo. Đó là lỗi đã gặp hồi đầu, khi thu nhỏ Hips/Spine/Chest làm cả bộ
    /// da suy biến thành một mảng kín màn hình.
    ///
    /// Tách ra rồi thì thu nhỏ hông vô hại: toàn bộ thân, đầu, hai chân biến mất trong
    /// một nhát, còn hai cánh tay không còn liên quan gì tới nó nữa. Đây chính là cách
    /// mọi game bắn súng làm - viewmodel của họ CHỈ CÓ hai cánh tay, không có thân.
    ///
    /// Dùng SetParent(..., true) để giữ nguyên vị trí và hướng trong thế giới, nên phép
    /// tách này không dời cánh tay đi đâu cả. Bộ da bám theo ma trận THẾ GIỚI của xương
    /// nên cũng không hề hấn gì.
    /// </summary>
    private void DetachArms(Animator anim, Transform root)
    {
        // Ưu tiên xương bả vai; rig nào không có thì lấy bắp tay trên.
        Transform lArm = anim.GetBoneTransform(HumanBodyBones.LeftShoulder) ?? _lUpper;
        Transform rArm = anim.GetBoneTransform(HumanBodyBones.RightShoulder) ?? _rUpper;

        if (lArm != null) lArm.SetParent(root, true);
        if (rArm != null) rArm.SetParent(root, true);
    }

    private void HideNonArmBones(Animator anim)
    {
        // Hông là gốc của mọi thứ còn lại sau khi đã tách tay ra: cột sống, ngực, cổ,
        // đầu, hai chân. Thu nhỏ mỗi nó là sạch cả người.
        Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);

        if (hips != null)
        {
            hips.localScale = Vector3.one * 0.0001f;
            return;
        }

        // Dự phòng khi rig không khai báo xương hông: giấu từng phần như bản cũ. Vẫn còn
        // thân người, nhưng ít ra không lộ đầu và chân.
        HumanBodyBones[] hide =
        {
            HumanBodyBones.Head, HumanBodyBones.Neck, HumanBodyBones.Jaw,
            HumanBodyBones.LeftEye, HumanBodyBones.RightEye,
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
        _lRest = alertLeftHand;
        _rRest = alertRightHand;

        MeasureArm(_lUpper, _lLower, _lHand, ref _lShoulder, ref _lReach, ref _lRest,
                   alertLeftDraw);
        MeasureArm(_rUpper, _rLower, _rHand, ref _rShoulder, ref _rReach, ref _rRest,
                   alertRightDraw);

        // Nâng sau khi đã co cho vừa tầm với, để phép co không kéo tụt lại.
        _lRest.y += handRaise;
        _rRest.y += handRaise;

        // In ra SỐ THẬT của tư thế nghỉ.
        //
        // Chỉnh viewmodel bằng cách nhìn màn hình rồi đoán là cách chắc chắn sai: các ô
        // toạ độ chỉ quyết định HƯỚNG, còn khoảng cách tới mắt - thứ quyết định tay to hay
        // nhỏ trên màn hình - lại do ô Draw quyết. Hai thứ đó không nhìn ra được bằng mắt.
        //
        // Có dòng này thì mở Console là đọc được ngay tay nào cao hơn, tay nào gần hơn,
        // thay vì thử từng số một.
        Debug.Log($"<color=#66CCFF>[Viewmodel] Tư thế nghỉ (toạ độ camera):" +
                  $"\nTRÁI  cao {_lRest.y:F3}m | cách mắt {_lRest.magnitude:F3}m | tầm với {_lReach:F3}m" +
                  $"\nPHẢI  cao {_rRest.y:F3}m | cách mắt {_rRest.magnitude:F3}m | tầm với {_rReach:F3}m" +
                  $"\nCách mắt CÀNG LỚN thì tay trên màn hình CÀNG NHỎ.</color>");
    }

    private void MeasureArm(Transform upper, Transform lower, Transform hand,
                            ref Vector3 shoulder, ref float reach, ref Vector3 rest,
                            float drawScale)
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

        // drawScale < 1 là tay này thu về gần người hơn tay kia - xem ô Alert Right Draw.
        float want = reach * restReach01 * drawScale;

        rest = shoulder + armVec * (want / dist);
    }

    /// <summary>
    /// Gắn tia điện lên găng. Mốc bám là CỔ TAY và NĂM ĐẦU NGÓN của mỗi bàn.
    ///
    /// Chỉ số 2, 5, 8, 11, 14 là đốt cuối của ngón cái, trỏ, giữa, áp út, út - theo đúng
    /// thứ tự khai trong LeftFingerBones / RightFingerBones. Đổi thứ tự mảng đó thì phải
    /// đổi cả mấy con số này.
    /// </summary>
    private void BuildGloveArcs()
    {
        if (!enableGloveArcs || _body == null || _magnet == null) return;

        GameObject host = new GameObject("GloveArcs");
        host.transform.SetParent(_camera, false);
        host.layer = _layer;

        GloveArcs arcs = host.AddComponent<GloveArcs>();
        arcs.Init(_magnet,
                  AnchorsOf(_lHand, _lFingers, _lPalmLocal),
                  AnchorsOf(_rHand, _rFingers, _rPalmLocal),
                  _layer);
    }

    [Tooltip("Điểm neo tia điện nhô ra khỏi xương về phía MU TAY bao nhiêu mét.\n\n" +
             "⚠️ Bản đầu neo thẳng vào XƯƠNG cổ tay và đầu ngón. Xương nằm BÊN TRONG bàn " +
             "tay, mà bàn tay lúc nghỉ lại đang NẮM - đầu ngón cuộn vào lòng. Nên mọi tia " +
             "chạy xuyên trong lòng nắm đấm, bị da mu tay che kín, chỉ lộ ra phía lòng bàn " +
             "tay. Mà ở góc nhìn thứ nhất, thứ ta nhìn thấy nhiều nhất lại chính là mu tay.\n\n" +
             "Nhô điểm neo ra đúng bề mặt da thì tia bò TRÊN mu tay, luôn thấy được.")]
    public float arcSurfaceOffset = 0.022f;

    /// <summary>
    /// Các điểm neo cho tia điện của một bàn tay.
    ///
    /// Gồm: giữa mu tay, bốn khớp đốt ngón (trỏ, giữa, áp út, út) - tất cả đều NHÔ RA mặt
    /// mu tay; cộng thêm đầu ngón trỏ và ngón giữa để lúc bàn tay xoè ra (giữ vật, đẩy)
    /// tia còn chạy dọc ra tới đầu ngón.
    ///
    /// Điểm neo là GameObject rỗng gắn làm CON của xương, nên ngón tay cuộn hay bàn tay
    /// xoay kiểu gì nó cũng đi theo - không phải tính lại mỗi khung hình.
    /// </summary>
    private Transform[] AnchorsOf(Transform hand, Transform[] fingers, Vector3 palmLocal)
    {
        if (hand == null) return new Transform[0];

        // Hướng MU TAY = ngược hướng lòng bàn tay. Lòng bàn tay đã đo sẵn lúc bắt xương
        // ngón (và đã được kiểm chứng đúng chiều qua động tác đẩy xoè tay).
        Vector3 dorsal = palmLocal.sqrMagnitude > 0.000001f
                         ? -hand.TransformDirection(palmLocal).normalized
                         : hand.up;

        System.Collections.Generic.List<Transform> list =
            new System.Collections.Generic.List<Transform>();

        // Giữa mu tay: đi từ cổ tay về phía khớp ngón giữa gần nửa đường.
        Transform midKnuckle = (fingers != null && fingers.Length > 6) ? fingers[6] : null;
        Vector3 back = midKnuckle != null
                       ? Vector3.Lerp(hand.position, midKnuckle.position, 0.45f)
                       : hand.position;
        list.Add(MakeAnchor(hand, back + dorsal * arcSurfaceOffset));

        if (fingers != null)
        {
            // Bốn khớp đốt: đốt gốc của trỏ (3), giữa (6), áp út (9), út (12).
            int[] knuckles = { 3, 6, 9, 12 };
            foreach (int k in knuckles)
            {
                if (k < fingers.Length && fingers[k] != null)
                    list.Add(MakeAnchor(fingers[k], fingers[k].position + dorsal * arcSurfaceOffset));
            }

            // Đầu ngón trỏ (5) và giữa (8), cũng nhô ra mặt trên.
            int[] tips = { 5, 8 };
            foreach (int k in tips)
            {
                if (k < fingers.Length && fingers[k] != null)
                    list.Add(MakeAnchor(fingers[k], fingers[k].position + dorsal * arcSurfaceOffset * 0.6f));
            }
        }

        return list.ToArray();
    }

    private static Transform MakeAnchor(Transform bone, Vector3 worldPos)
    {
        GameObject go = new GameObject("ArcAnchor");
        go.transform.SetParent(bone, false);

        // Đặt theo toạ độ THẾ GIỚI sau khi đã gắn làm con: Unity tự quy đổi ra toạ độ
        // cục bộ, kể cả khi xương đang bị scale 1.2 - tự tính tay thì dễ lệch đúng chỗ đó.
        go.transform.position = worldPos;
        return go.transform;
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

        // Tổng thời gian = rút tay + thọc ra. BuildTargets so _punchTimer với punchHold
        // để biết đang ở pha nào, nên không cần biến đếm thứ hai.
        _punchTimer = punchWindupHold + punchHold;

        // Xung ĐI NGƯỢC, về phía sau. Nghe lạ nhưng đúng: nhịp đầu của cú đấm là RÚT
        // TAY VỀ, nên xung phải giúp nó rút nhanh. Xung đẩy ra trước như bản cũ thì nó
        // đánh nhau với pha rút tay và triệt tiêu mất cả hai.
        // Xung đi DỌC ĐƯỜNG ĐẤM, không lệch ngang. Trước để x = +0.1 nên nắm đấm bị hất
        // sang phải lúc rút rồi mới quăng vào trong - thêm một tầng quét ngang nữa.
        _rVelocity += new Vector3(0.04f, -0.01f, -0.4f) * poseStiffness * 0.05f;

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

        _fireTimer = fireWindupHold + fireHold;

        // Hẹn nhịp giật ngược, tính từ lúc THẢ tư thế đẩy chứ không phải từ lúc bấm -
        // giật ngược trong khi tay còn đang giữ thế đẩy thì hai lực đè nhau, mất cả hai.
        _fireRecoilTimer = fireHold + fireRecoilDelay;
    }

    /// <summary>Đẩy vật đi bằng găng: tay TRÁI xoè bàn, dang ra ngoài và hất mạnh.</summary>
    public void PlayPush()
    {
        if (!_built) return;

        _pushTimer = fireWindupHold + pushHold;

        // Xung VỪA PHẢI thôi. Trước đây để (-0.35, 0.08, 0.9) x 9.8 = vận tốc ngang 3.4
        // m/s SANG TRÁI, cộng thêm cái đích vốn đã lệch trái - tay vọt hẳn ra ngoài khung
        // hình. Giờ độ vươn do đích lo (xem poseOvershoot), xung chỉ còn là gia vị cho
        // nhịp khởi động.
        _lVelocity += new Vector3(-0.08f, 0.04f, 0.35f) * poseStiffness * 0.05f;
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
        AimPalmForPush(dt);
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

        // ⚠️ DÙNG CHUNG PHA VỚI CAMERA, KHÔNG TỰ ĐẾM.
        //
        // Tự đếm thì viewmodel chạy 5 nhịp/giây còn camera chạy 8 - hai nhịp chồng lên
        // nhau thành phách 3 nhịp/giây, lúc cùng chiều lúc ngược chiều. Đó chính là cảnh
        // tay rung lắc dữ dội khi đi bộ, và nó KHÔNG chữa được bằng cách hạ biên độ.
        //
        // Dùng chung pha thì hai bên đồng bộ tuyệt đối. Cũng lấy luôn tốc độ đã làm mượt
        // bên đó, nên lúc bắt đầu và dừng lại tay không giật.
        if (_shake != null && _shake.enableHeadBob)
        {
            _bobPhase = _shake.BobPhase;
            speed01 = Mathf.Clamp01(_shake.SmoothedSpeed01);
        }
        else
        {
            _bobPhase += dt * walkBobSpeed * Mathf.Max(speed01, 0.18f) * Mathf.PI * 2f;
        }

        // Ba tỉ lệ dưới đây lấy theo tỉ lệ tay người thật khi đi: nhún dọc và lắc ngang
        // đều nhỏ hơn hẳn biên độ đánh tay trước-sau.
        float amp = Mathf.Lerp(breatheAmount, walkSwingReach * 0.18f, speed01);

        // Trục DỌC: nhún theo mỗi bước chân, nên chạy NHANH GẤP ĐÔI nhịp đánh tay -
        // một chu kỳ đánh tay có hai bước chân. Bỏ số 2 đi thì thành đi cà nhắc một bên.
        float bobY = Mathf.Sin(_bobPhase * 2f) * amp;

        // Nhịp đánh tay chậm hơn nhịp bước chân - xem ô Walk Swing Rate.
        _swingPhase = _bobPhase * walkSwingRate;

        // Trục NGANG: lắc nhẹ, cùng pha với đánh tay.
        float lateral = Mathf.Cos(_swingPhase) * walkSwingReach * 0.2f * speed01;

        // ĐÁNH TAY LÀ XOAY QUANH VAI, KHÔNG PHẢI TRƯỢT TỚI TRƯỢT LUI.
        //
        // ⚠️ Bản trước cộng một đoạn thẳng vào trục z, tức bàn tay trượt tới trượt lui
        // NGANG TẦM MẮT. Trượt dọc trục nhìn thì trên màn hình gần như không thấy gì -
        // đúng cái bệnh đã gặp ở cú đấm và cú hút.
        //
        // Tay người thật QUAY quanh khớp vai: ra trước là nhấc lên, ra sau là chúc hẳn
        // xuống dưới. Chính vì thế mà nhìn xuống lúc đang đi, mỗi lúc ta chỉ thấy MỘT
        // tay - tay kia đã khuất xuống dưới tầm nhìn.
        //
        // Xoay quanh trục X của camera cho ra đúng cung đó. Độ dài cánh tay giữ nguyên
        // nên không cần lo IK kẹp.
        // RƠI MỘT CHIỀU, KHÔNG DAO ĐỘNG HAI BÊN.
        //
        // dropL và dropR chạy trong khoảng 0 -> 1 và ngược pha nhau. Số 0 nghĩa là "đứng
        // đúng tư thế nghỉ", số 1 nghĩa là "chúc xuống hết cung". Nên tay KHÔNG BAO GIỜ
        // nhấc lên cao hơn chỗ nó đứng lúc rảnh - đúng như người đi bộ thật: buông tay
        // xuống rồi đưa về, chứ không nhấc tay lên khỏi ngực.
        //
        // Nhân speed01 nên lúc đứng yên cung rơi bằng 0, tay về đúng tư thế cảnh giác mà
        // không cần thêm đoạn chuyển nào.
        float span = walkSwingReach * walkSwingDegPerMeter * speed01;

        float dropL = (1f - Mathf.Sin(_swingPhase)) * 0.5f * span;
        float dropR = (1f + Mathf.Sin(_swingPhase)) * 0.5f * span;

        Vector3 lArm = _lRest - _lShoulder;
        Vector3 rArm = _rRest - _rShoulder;

        // Góc DƯƠNG hạ tay xuống và ra sau. Hai tay ngược pha nên luôn có một tay ở đỉnh.
        Vector3 lSwung = Quaternion.AngleAxis(dropL, Vector3.right) * lArm;
        Vector3 rSwung = Quaternion.AngleAxis(dropR, Vector3.right) * rArm;

        _lTarget = _lShoulder + lSwung + _sway + new Vector3(lateral, bobY, 0f);
        _rTarget = _rShoulder + rSwung + _sway + new Vector3(-lateral, bobY, 0f);

        // --- ĐANG HÚT ĐẠN: kéo hai tay về ngực và tách rộng ---
        //
        // Đây là trạng thái GIỮ LIÊN TỤC nên tác động thẳng vào ĐÍCH, không phải cộng
        // xung vào vận tốc như các cú đánh một nhịp. Cộng xung liên tục thì tay bay mất.
        if (_magnet != null && _magnet.IsPulling)
        {
            // GHÌ RUN: đổi độ vươn qua lại quanh mức chính, để tư thế không chết cứng.
            float strain = Mathf.Sin(Time.time * pullStrainSpeed) * pullStrain;
            float reachIn = Mathf.Max(0.15f, pullExtend01 + strain);

            // Chếch RA NGOÀI và LÊN TRÊN, không phải lùi thẳng về sau. Hai tay vì thế
            // nằm ngang tầm mặt và dang rộng - dịch chuyển thật trên màn hình, thay vì
            // chỉ thụt lùi dọc trục nhìn như bản cũ.
            Vector3 dirL = new Vector3(-(0.35f + pullSpread), 0.30f, 0.84f).normalized;
            Vector3 dirR = new Vector3(0.35f + pullSpread, 0.30f, 0.84f).normalized;

            if (_lReach > 0.01f) _lTarget = _lShoulder + dirL * (_lReach * reachIn) + _sway;
            if (_rReach > 0.01f) _rTarget = _rShoulder + dirR * (_rReach * reachIn) + _sway;
        }

        // --- ĐANG "CẦM" VẬT: TAY PHẢI KHỐNG CHẾ, TAY TRÁI NGHỈ ---
        // ⚠️ KHỐI NÀY PHẢI ĐỨNG TRƯỚC BA KHỐI ĐỘNG TÁC BÊN DƯỚI.
        //
        // Nó GÁN ĐÈ _rTarget chứ không cộng thêm, nên đặt sau là nó xoá sạch mọi tư thế
        // đấm / bắn / laze vừa tính. Giữ vật là TRẠNG THÁI NỀN, ba cái kia là HÀNH ĐỘNG -
        // hành động luôn thắng trạng thái, giống đúng thứ tự ưu tiên ở UpdateFists().
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

        // --- GIỮ TƯ THẾ ĐẤM ---
        //
        // Cộng thẳng vào ĐÍCH chứ không phải vào vận tốc. Xung vận tốc cho ra một đỉnh
        // nhọn rồi tắt; cộng vào đích thì tay ở NGUYÊN đó suốt punchHold giây, đủ lâu để
        // nhìn ra. Xung vận tốc vẫn giữ ở PlayPunch để cú vung có độ nảy.
        //
        // Không sợ vượt tầm tay: SolveArm tự kẹp lại ở tầm với, nên punchThrust to là ra
        // tay duỗi thẳng hết cỡ - đúng cái mình muốn cho một cú đấm.
        // ⚠️ Đếm ngược NGOÀI phần kiểm _rReach. Để đồng hồ bên trong thì lỡ đo tầm tay
        // hỏng (_rReach = 0) là nó không bao giờ giảm, và động tác kẹt lại vĩnh viễn.
        if (_punchTimer > 0f)
        {
            _punchTimer -= dt;

            // MỘT TIA DUY NHẤT xuất phát từ MẮT, cho cả nhịp rút lẫn nhịp thọc. Chỉ khác
            // ĐỘ DÀI, nên nắm đấm chạy trên một đường thẳng và giữ nguyên chỗ trên màn
            // hình, chỉ to lên rồi nhỏ đi. Đó là cú thọc thẳng của mọi game bắn súng.
            Vector3 aim = punchAim.sqrMagnitude > 0.0001f
                          ? punchAim.normalized
                          : Vector3.forward;

            // Còn dư nhiều hơn punchHold nghĩa là vẫn đang ở nhịp RÚT TAY.
            float dist = _punchTimer > punchHold ? punchWindupDist : punchStrikeDist;

            _rTarget = aim * dist + _sway;

            _lTarget += new Vector3(0f, 0.02f, -0.1f);   // tay trái lùi giữ thăng bằng
        }

        // --- GIỮ TƯ THẾ BẮN / ĐẨY ---
        //
        // GÁN ĐÈ, đo từ vai - xem ghi chú ở ô Fire Extend 01 để biết vì sao không cộng
        // thêm vào tư thế hiện tại.
        //
        // Khác cú đấm ở hai điểm nên mắt phân biệt được ngay: đây là HAI TAY cùng đẩy,
        // và chỉ vươn 92% nên khuỷu còn cong; cú đấm là MỘT TAY và duỗi thẳng hết cỡ.
        if (_fireTimer > 0f)
        {
            _fireTimer -= dt;

            // Hơi chếch lên, vì đẩy một vật nặng thì tay đi từ dưới lên chứ không nằm ngang.
            // Bớt chếch lên (0.14 -> 0.07): phần nào dồn vào trục Y là phần đó KHÔNG
            // dồn ra trước được, mà tầm với thì cố định.
            Vector3 shoveDir = new Vector3(0f, 0.07f, 1f).normalized;

            // Rút và đẩy đi trên CÙNG MỘT ĐƯỜNG, chỉ khác độ dài - nên quỹ đạo là một
            // đoạn thẳng vào rồi ra, không phải vòng cung.
            float reachOut = _fireTimer > fireHold
                             ? fireWindup01
                             : fireExtend01 * poseOvershoot;

            if (_rReach > 0.01f) _rTarget = _rShoulder + shoveDir * (_rReach * reachOut) + _sway;
            if (_lReach > 0.01f) _lTarget = _lShoulder + shoveDir * (_lReach * reachOut) + _sway;
        }

        // --- GIỮ TƯ THẾ ĐẨY VẬT ---
        //
        // Chỉ TAY TRÁI, và dang ra ngoài. Ba điểm khác cú bắn để mắt phân biệt được:
        // một tay thay vì hai, chếch ra ngoài thay vì thẳng trước mặt, và bàn tay xoè
        // hết (lo ở UpdateFists) thay vì nắm hờ.
        if (_pushTimer > 0f)
        {
            _pushTimer -= dt;

            if (_lReach > 0.01f)
            {
                // x âm là ra ngoài, vì tay trái nằm bên trái.
                Vector3 pushDir = new Vector3(-pushSpread, 0.05f, 1f).normalized;

                float reachOut = _pushTimer > pushHold
                                 ? fireWindup01
                                 : pushExtend01 * poseOvershoot;

                _lTarget = _lShoulder + pushDir * (_lReach * reachOut) + _sway;
            }
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
        if (_shake != null && _shake.enableHeadBob) speed01 = Mathf.Clamp01(_shake.SmoothedSpeed01);

        // Xoay cả khung, suy từ cùng một ô. Hệ số 11 độ trên mỗi mét cho ra 2.2 độ ở
        // mức mặc định 0.2m - đúng con số đã dùng trước đây.
        float rollDeg = walkSwingReach * 11f;

        // Nghiêng người theo nhịp ĐÁNH TAY, gật đầu theo nhịp BƯỚC CHÂN - hai nhịp khác
        // nhau, đúng như cơ thể thật.
        float roll = Mathf.Sin(_swingPhase) * rollDeg * speed01;
        float pitch = Mathf.Sin(_bobPhase * 2f) * rollDeg * 0.35f * speed01;

        // Đường cong của cú đấm: 0 -> 1 -> 0 trong suốt thời gian đấm, dùng chung cho cả
        // phần chồm tới lẫn phần vặn người.
        float punch01 = 0f;
        float punchTotal = punchWindupHold + punchHold;

        if (_punchTimer > 0f && punchTotal > 0.0001f)
        {
            punch01 = Mathf.Sin(Mathf.Clamp01(1f - _punchTimer / punchTotal) * Mathf.PI);
        }

        // Vặn sang TRÁI (yaw âm) để đưa vai phải ra trước - xem ghi chú ở ô Punch Twist.
        Quaternion pose = Quaternion.Euler(pitch, -punchTwist * punch01, roll);

        // Nở góc nhìn cho cánh tay vọt ra. Gán thẳng chứ không cộng dồn, nên hết cú đấm
        // là nó tự về đúng viewmodelFov, không cần nhớ giá trị cũ.
        if (_viewmodelCam != null)
        {
            _viewmodelCam.fieldOfView = viewmodelFov + punchFovPunch * punch01;
        }

        // CẢ THÂN CHỒM RA TRƯỚC KHI ĐẤM. Chỉ duỗi tay thì chỉ có cánh tay đấm; dời cả
        // khung thì VAI cũng tiến lên, và đó mới là thứ mắt đọc thành "dồn sức vào cú đấm".
        // Chồm người: cú đấm và cú bắn/đẩy dùng chung một chỗ dời, cộng lại được vì
        // không bao giờ trùng nhau quá một nhịp.
        float fireTotal = fireWindupHold + fireHold;
        float pushTotal = fireWindupHold + pushHold;

        float fire01 = _fireTimer > 0f && fireTotal > 0.0001f
                       ? Mathf.Sin(Mathf.Clamp01(1f - _fireTimer / fireTotal) * Mathf.PI) : 0f;
        float push01 = _pushTimer > 0f && pushTotal > 0.0001f
                       ? Mathf.Sin(Mathf.Clamp01(1f - _pushTimer / pushTotal) * Mathf.PI) : 0f;

        float lunge = punch01 * punchLunge + Mathf.Max(fire01, push01) * fireLunge;

        if (!decoupleFromShake || _shake == null || shakeFollow >= 0.999f)
        {
            _body.localRotation = pose;
            _body.localPosition = _bodyBasePos + new Vector3(0f, 0f, lunge);
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

        _body.localPosition = _bodyBasePos + new Vector3(0f, 0f, lunge)
                              - _camera.InverseTransformDirection(off) * cancel;
    }

    /// <summary>
    /// Trả bốn xương tay và hai bàn tay về tư thế gốc trước mỗi lần giải IK.
    ///
    /// ⚠️ THIẾU CÁI NÀY THÌ TAY TỰ XOẮN DẦN, CÀNG ĐẤM NHIỀU CÀNG HỎNG.
    ///
    /// AimBone chỉ ép HƯỚNG của xương, hoàn toàn không nói gì về việc xương xoay quanh
    /// trục của chính nó. Góc xoắn đó thừa hưởng từ khung hình trước, nên nó là một số
    /// tự do trôi nổi.
    ///
    /// Và nó KHÔNG trôi ngẫu nhiên - nó trôi có hệ thống. Quaternion.FromToRotation cho
    /// ra phép xoay ngắn nhất, mà xoay ngắn nhất đi vòng quanh một chu trình khép kín
    /// trên mặt cầu thì để lại một góc xoắn thừa đúng bằng diện tích chu trình đó. Mỗi
    /// cú đấm là một chu trình khép kín (nghỉ -> rút -> thọc -> nghỉ), nên MỖI CÚ ĐẤM
    /// CỘNG THÊM một chút xoắn, không bao giờ tự trả lại.
    ///
    /// Đấm vài lần thì chưa thấy. Đấm nhiều lần thì cẳng tay xoắn hẳn đi, và bàn tay
    /// cùng chùm ngón bị vặn theo - đúng cái "sau khi đấm nhiều lần thì tay phải bị lỗi
    /// ngón tay".
    ///
    /// Trả về gốc thì tư thế mỗi khung hình chỉ còn phụ thuộc vào ĐÍCH, không phụ thuộc
    /// lịch sử. Đây cũng đúng cách đã chữa cho ngón tay và cho bàn tay lúc đẩy.
    /// </summary>
    private void ResetArmPose()
    {
        if (_lUpper != null) _lUpper.localRotation = _lUpperRest;
        if (_lLower != null) _lLower.localRotation = _lLowerRest;
        if (_lHand  != null) _lHand.localRotation  = _lHandRest;

        if (_rUpper != null) _rUpper.localRotation = _rUpperRest;
        if (_rLower != null) _rLower.localRotation = _rLowerRest;
        if (_rHand  != null) _rHand.localRotation  = _rHandRest;
    }

    private void ApplyIK()
    {
        ResetArmPose();

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
