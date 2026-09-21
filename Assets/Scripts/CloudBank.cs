using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Biển mây bao quanh hòn đảo lơ lửng.
///
/// ⚠️ ĐÂY KHÔNG PHẢI TRANG TRÍ. Game có ĐÚNG MỘT cách chết: rơi khỏi đảo. Mà rìa vực lại
/// hoàn toàn không có dấu hiệu gì - người chơi chỉ biết mình sắp rơi khi đã rơi rồi.
/// Dải mây là thứ nói "qua đây là hết", và nó nói bằng hình ảnh nên không cần ai giải
/// thích.
///
/// Ba việc nó làm, theo thứ tự quan trọng:
///   1. Đánh dấu ranh giới chết
///   2. Che khoảng trống bên dưới (hiện chỉ thấy skybox)
///   3. Che mặt đáy hòn đảo - model Meshy AI đã giảm tam giác, mặt đáy không đẹp
///
/// ⚠️ VÌ SAO KHÔNG DÙNG SƯƠNG MÙ CỦA UNITY: URP chỉ có fog theo KHOẢNG CÁCH, không có
/// fog theo ĐỘ CAO. Bật lên là mờ luôn cả sân đấu và che mất đối thủ - sai công cụ.
///
/// ⚠️ VÌ SAO KHÔNG DÙNG PARTICLE SYSTEM: vài trăm hạt trong suốt to là thủ phạm giết FPS
/// kinh điển (overdraw - mỗi pixel bị tính lại một lần cho mỗi lớp chồng lên). Chủ project
/// vừa vất vả kéo từ 20 lên 56 FPS, không đánh đổi lại được.
///
/// Cách ở đây: MỘT lưới duy nhất chứa toàn bộ cụm mây, MỘT vật liệu, MỘT lệnh vẽ. Đúng bộ
/// máy đã chạy được ở ControlZone, chỉ khác một điểm cốt lõi - lửa dùng blend CỘNG SÁNG vì
/// nó là ánh sáng, mây dùng blend ALPHA vì nó CHE ánh sáng.
///
/// CÁCH ĐẶT: tạo một GameObject rỗng ngay TÂM hòn đảo, cao ngang MẶT ĐẢO, gắn script này.
/// Không cần kéo thả gì thêm.
/// </summary>
[ExecuteAlways]
public class CloudBank : MonoBehaviour
{
    [Header("Kích thước")]
    [Tooltip("Bán kính hòn đảo, mét. Mọi con số khác suy ra từ đây.")]
    public float islandRadius = 40f;

    [Tooltip("Vành mây bắt đầu ở bao nhiêu phần bán kính đảo. Dưới 1 nghĩa là THỤT VÀO " +
             "dưới mép đảo.\n\n" +
             "Phải thụt vào, không được bắt đầu đúng ở mép: mây là các tấm phẳng rời rạc " +
             "nên rìa trong của nó lởm chởm. Giấu cái rìa đó xuống dưới mặt đảo thì đứng " +
             "trên nhìn ra chỉ thấy một biển mây liền mạch.")]
    [Range(0.5f, 1f)]
    public float innerFactor = 0.85f;

    [Tooltip("Vành mây toả ra tới đâu, mét. Đủ xa để phủ kín chân trời.\n\n" +
             "⚠️ PHẢI RẤT LỚN SO VỚI ĐẢO, ÍT NHẤT GẤP 10. Lý do là góc nhìn:\n\n" +
             "Mắt người chơi cao 1.77m, mặt mây thấp hơn mặt đảo 2.5m - tức mắt ở trên " +
             "biển mây 4.3m. Nhìn ra mép ngoài của biển mây ở 160m thì nó chỉ nằm THẤP " +
             "HƠN ĐƯỜNG CHÂN TRỜI 1.5 ĐỘ. Mắt thấy ngay một đường tròn cắt ngang, và bên " +
             "ngoài nó là trời trống - lộ hẳn ra rằng biển mây chỉ là một cái đĩa.\n\n" +
             "Ở 450m thì góc đó còn 0.5 độ, tức mép mây trùng luôn vào đường chân trời và " +
             "không ai nhận ra nó kết thúc ở đâu.\n\n" +
             "Không sợ bị cắt: mặt phẳng cắt xa của camera là 1000m.")]
    public float outerRadius = 150f;

    [Tooltip("Mặt trên biển mây nằm THẤP HƠN gốc object này bao nhiêu mét.\n\n" +
             "Đặt gốc object ngang mặt đảo rồi để số này khoảng 2-3. Mây đúng ngang mặt " +
             "đất thì nó TRÀN VÀO rìa sân đấu, che mất đối thủ đứng gần mép - mà mép lại " +
             "đúng chỗ giao tranh căng nhất.")]
    public float topBelowSurface = 2.5f;

    [Tooltip("Biển mây dày bao nhiêu mét tính từ mặt trên xuống.\n\n" +
             "45 là dày tới mức NGẬP HẲN: rơi vào là trắng xoá, không còn thấy gì cho tới " +
             "lúc chạm KillZone ở -20. Hạ xuống 15 thì chỉ là một lớp mỏng nhìn xuyên qua " +
             "được.")]
    public float thickness = 45f;

    [Header("Mật độ")]
    [Tooltip("Số cụm mây. ĐÂY LÀ Ô CỨU HỘ KHI TỤT FPS - hạ xuống là nhẹ ngay.\n\n" +
             "Chi phí của mây không nằm ở số tam giác (180 cụm chỉ là 1080 tam giác, không " +
             "đáng kể) mà ở OVERDRAW: mỗi pixel màn hình bị tính lại một lần cho mỗi lớp " +
             "mây chồng lên nó. Nhìn xuống biển mây là lúc chồng nhiều lớp nhất.")]
    [Range(20, 900)]
    public int puffCount = 420;

    [Tooltip("Cỡ cụm mây gần đảo nhất, mét.")]
    public float puffSizeNear = 48f;

    [Tooltip("Cỡ cụm mây ngoài xa, mét.\n\n" +
             "⚠️ PHẢI TĂNG CÙNG VỚI Outer Radius, không được để lệch. Mật độ cụm rải đều " +
             "theo DIỆN TÍCH, mà diện tích tăng theo bình phương bán kính - nới bán kính " +
             "từ 160 lên 450 là diện tích tăng 8 lần, cùng số cụm đó trải ra thì khoảng " +
             "cách giữa hai cụm vọt lên khoảng 60m.\n\n" +
             "Cụm chỉ to 42m thì chúng KHÔNG CÒN CHẠM NHAU nữa, và biển mây liền mạch biến " +
             "thành một đám đốm trắng rời rạc. 110m thì chúng vẫn chồng lên nhau.\n\n" +
             "Cụm to ở xa gần như không tốn thêm gì: ở 400m thì một cụm 110m trông vẫn nhỏ " +
             "trên màn hình, nên số pixel phải tô chẳng hơn bao nhiêu.")]
    public float puffSizeFar = 150f;

    [Header("Vẻ ngoài")]
    [Tooltip("Ảnh cụm mây của hoạ sĩ. ĐỂ TRỐNG thì script tự vẽ một cái bằng code.\n\n" +
             "⚠️ Đây là ô đáng dùng nhất nếu tìm được asset. Phần khó của biển mây KHÔNG " +
             "phải cái ảnh - mà là: gom tất cả vào MỘT lưới một lệnh vẽ, rải cụm theo khối " +
             "để có khe hở, tô đậm nhạt theo độ sâu, và quay tấm theo camera mà trục dọc " +
             "vẫn bám trời. Toàn bộ phần đó giữ nguyên, chỉ thay ảnh.\n\n" +
             "Ảnh cần: NỀN TRONG SUỐT (PNG có kênh alpha), một cụm mây duy nhất, vuông, " +
             "512 hoặc 1024. Trong Unity đặt Alpha Is Transparency = bật.\n\n" +
             "⚠️ Nếu asset đi kèm SHADER riêng thì shader đó thường của pipeline cũ và sẽ " +
             "ra màu hồng trong URP - chỉ lấy ẢNH, bỏ shader và material của họ đi.")]
    public Texture2D customPuffTexture;

    [Tooltip("Ảnh trên chia thành lưới mấy x mấy? 1 = một hình duy nhất, 2 = atlas 2x2 " +
             "(bốn hình).\n\n" +
             "⚠️ Bốn hình khác nhau là bước nhảy lớn hơn nhiều so với việc tô màu cho đẹp. " +
             "Cùng MỘT hình lặp lại 180 lần thì dù xoay trở thế nào mắt cũng nhận ra ngay " +
             "đó là một cái tem dán đi dán lại - và một khi đã nhận ra thì không bỏ qua " +
             "được nữa.\n\n" +
             "File CloudPuffs.png kèm theo là atlas 2x2, để 2.")]
    [Range(1, 4)]
    public int puffAtlasGrid = 2;

    [Tooltip("Màu mây. Hơi ngả xanh lạnh thì ra vẻ ở trên cao.")]
    public Color cloudColor = new Color(0.88f, 0.91f, 0.97f, 1f);

    [Tooltip("Độ đục của cụm mây dày nhất, 0-1.\n\n" +
             "Để cao. Mây ĐẶC thì mới là mây; mây trong suốt thì mắt đọc thành SƯƠNG MÙ, " +
             "và sương mù không bao giờ có hình khối.")]
    [Range(0f, 1f)]
    public float opacity = 0.94f;

    [Tooltip("Số CỤM LỚN mà mây gom vào.\n\n" +
             "⚠️ ĐÂY LÀ Ô QUYẾT ĐỊNH 'GIỐNG MÂY' HAY 'GIỐNG SƯƠNG'.\n\n" +
             "Rải đều khắp vành thì ra một tấm thảm trắng dày đều - đó chính là sương mù, " +
             "dù có tô đẹp cỡ nào. Mây thật thì TỤ THÀNH KHỐI: chỗ dày chỗ thưa, có khe " +
             "hở nhìn xuyên xuống được, và chính những khe hở đó mới cho mắt thấy khối mây " +
             "có hình dạng.\n\n" +
             "Số nhỏ = vài khối mây to. Số lớn = vụn ra, dần thành thảm đều.")]
    [Range(1, 60)]
    public int clusterCount = 26;

    [Tooltip("Cụm con bám quanh tâm khối rộng bao nhiêu, tính theo phần cỡ khối.")]
    [Range(0.2f, 2f)]
    public float clusterSpread = 1.1f;

    [Tooltip("Mây trải đều theo CHIỀU CAO tới mức nào. 0 = dồn hết lên mặt trên, " +
             "1 = rải đều khắp bề dày.\n\n" +
             "⚠️ ĐÂY LÀ Ô LÀM MÂY CHỒNG TẦNG LÊN NHAU. Bản trước dùng bình phương số " +
             "ngẫu nhiên, tức gần như mọi cụm đều nằm sát mặt trên - biển mây thành một " +
             "TẤM MỎNG, dù ô Thickness có để 45 đi nữa.\n\n" +
             "Kéo lên cao thì các cụm nằm rải từ nóc xuống đáy, che nhau, và nhìn từ mép " +
             "đảo xuống mới thấy nhiều lớp chồng lên - đó là thứ cho cảm giác SÂU.")]
    [Range(0f, 1f)]
    public float verticalSpread = 0.75f;

    [Tooltip("Màu MẶT TRÊN cụm mây, nơi hứng nắng.\n\n" +
             "⚠️ ĐỪNG LẤY ĐIỂM SÁNG CHÓI NHẤT của mây trong skybox làm số này. Đã sai đúng " +
             "chỗ đó: đo được đốm chói là (0.87, 0.96, 0.99) rồi nhét thẳng vào đây, mà vì " +
             "Depth Shading thấp nên GẦN NHƯ MỌI cụm đều mang màu ấy - cả biển mây trắng " +
             "bệch, mất hết sắc lơ.\n\n" +
             "Phải lấy màu của PHẦN LỚN THÂN MÂY, tức vùng sáng vừa. Đốm chói chỉ chiếm " +
             "một mảng nhỏ trên nóc, nó không đại diện cho màu của đám mây.\n\n" +
             "Một quy luật hữu ích: trong tranh, mây càng tối thì càng NGẢ LƠ XANH, vì " +
             "phần khuất nắng chỉ còn nhận được ánh sáng từ bầu trời.\n\n" +
             "⚠️ Chênh lệch sáng-tối giữa mặt trên và mặt dưới LÀ THỨ QUAN TRỌNG NHẤT để " +
             "ra được mây. Sương mù xám đều một màu; mây thì trắng chói ở nóc và xám xanh " +
             "ở bụng, vì nắng chỉ tới được mặt trên. Không có chênh lệch này thì mọi thứ " +
             "khác đều vô ích.")]
    public Color topColor = new Color(0.77f, 0.92f, 0.98f, 1f);

    [Tooltip("Màu MẶT DƯỚI cụm mây, nơi khuất nắng.\n\n" +
             "⚠️ HAI Ô NÀY LÀ NƠI DUY NHẤT MÂY CÓ MÀU. Ảnh CloudPuffs.png cố ý chỉ có " +
             "THANG XÁM - vì giữ màu gốc trong ảnh thì viền bán trong suốt lộ nền trời " +
             "xanh ra. Nên nếu hai ô này để trắng xám thì cả biển mây trắng xám theo.\n\n" +
             "Số mặc định đo THẲNG từ FS000_Day_03.png: phần sáng nhất của mây trong đó " +
             "là (0.85, 0.95, 0.99) và bụng mây là (0.56, 0.85, 0.96) - tức mây ngả LƠ " +
             "XANH rất mạnh, vì cả bầu trời hắt ánh sáng xanh lên chúng.\n\n" +
             "Đổi skybox khác thì phải đo lại, không thì mây lạc tông với nền trời ngay.")]
    public Color bottomColor = new Color(0.56f, 0.85f, 0.96f, 1f);

    [Tooltip("Màu MÀN SƯƠNG giữa mắt và mây ở xa. Mây càng xa càng nhoà về màu này.\n\n" +
             "⚠️ ĐÂY LÀ Ô CHỮA 'MÂY Ở XA BỊ TỐI'. Nguyên nhân không phải màu sai, mà là " +
             "KÍCH THƯỚC: cụm mây ngoài xa to 150m, mà độ dốc sáng-tối lại trải đúng một " +
             "lần trên mỗi tấm - nên nửa dưới của một tấm khổng lồ là một mảng xanh đậm " +
             "rộng mấy chục mét, đập thẳng vào mắt. Cụm nhỏ ở gần thì mảng tối ấy bé và " +
             "lẫn vào nhau nên không ai để ý.\n\n" +
             "Nhoà dần về màu trời vừa xoá mảng tối đó, vừa đúng hiện tượng thật: càng xa " +
             "thì lớp không khí giữa ta và vật càng dày, nó kéo mọi thứ về màu nền trời và " +
             "làm mất tương phản. Hoạ sĩ gọi là phối cảnh không khí.\n\n" +
             "⚠️ ĐÂY KHÔNG PHẢI MÀU TRỜI PHÍA SAU MÂY - đừng lấy màu đo từ skybox nhét " +
             "vào đây. Đã làm sai đúng chỗ này một lần: lấy màu trời sát chân trời " +
             "(0.34, 0.74, 0.93), độ sáng 0.67, trong khi nóc mây gần sáng tới 0.93. Nhoà " +
             "75% về đó là mây xa TỐI ĐI 21% - càng xa càng tối, vô lý hoàn toàn.\n\n" +
             "Thứ cần dùng là màn sương NẰM GIỮA mắt và vật: ánh sáng bị lớp không khí đó " +
             "tán xạ rồi hắt vào mắt ta. Nó luôn NHẠT VÀ SÁNG, vì bản thân nó phát sáng " +
             "chứ không che. Vì vậy vật ở xa phải mờ nhạt đi, KHÔNG PHẢI tối đi.\n\n" +
             "Số mặc định là màu trời chân trời đã kéo mạnh về trắng.")]
    public Color horizonColor = new Color(0.72f, 0.88f, 0.98f, 1f);

    [Tooltip("Cụm ở rìa ngoài cùng nhoà về màu chân trời bao nhiêu phần. 0 = không nhoà.")]
    [Range(0f, 1f)]
    public float distanceFade = 0.75f;

    [Tooltip("Cụm nằm sâu bị tối đi mạnh tới mức nào. 0 = mọi cụm sáng như nhau.\n\n" +
             "⚠️ ĐÂY LÀ Ô CHỮA 'CHỒNG NHIỀU LỚP THÌ ĐEN LẠI'. Thật ra chồng lớp KHÔNG làm " +
             "tối: phép trộn alpha hội tụ về đúng màu của mây, không bao giờ vượt quá nó.\n\n" +
             "Nhưng chồng lớp CHE MẤT bầu trời sáng phía sau, nên cái còn lại đúng bằng " +
             "màu cụm mây. Nhìn ngang ra chân trời thì xuyên qua rất nhiều lớp, alpha dồn " +
             "gần 1, và ta thấy trọn màu tối của những cụm nằm sâu.\n\n" +
             "Nên thứ cần hạ là ĐỘ TỐI CỦA CỤM SÂU, không phải số lớp. Hạ ô này là biển " +
             "mây sáng đều lên mà vẫn giữ nguyên độ dày.")]
    [Range(0f, 1f)]
    public float depthShading = 0.4f;

    [Tooltip("Cụm nhỏ được phép lệch độ sâu khỏi khối của nó bao nhiêu. 0 = cả khối nằm " +
             "cùng một tầng.\n\n" +
             "⚠️ Ô NÀY CHỮA 'CÓ ĐÁM TỐI HƠN HẲN ĐÁM KHÁC'. Bản trước bốc độ sâu ngẫu " +
             "nhiên cho TỪNG CỤM NHỎ, nên ngay trong cùng một khối mây đã có cụm nằm nóc " +
             "và cụm nằm đáy dính vào nhau - sáng tối chênh hẳn, nhìn như vá víu.\n\n" +
             "Mây thật thì cả một khối được chiếu sáng như MỘT vật: nóc khối sáng, đáy " +
             "khối tối, còn trong lòng khối thì đều. Nên độ sâu phải quyết định ở mức " +
             "KHỐI, cụm nhỏ chỉ nhấp nhô quanh đó một ít.")]
    [Range(0f, 1f)]
    public float depthVariance = 0.25f;

    [Tooltip("Tốc độ cả biển mây xoay quanh tâm đảo, độ mỗi giây. Rất chậm.\n\n" +
             "Có chuyển động thì mây mới sống. Nhưng phải chậm tới mức người chơi không " +
             "chỉ ra được nó đang quay - nhanh hơn là thành cái đĩa xoay, lộ ngay là giả.")]
    public float driftDegPerSecond = 0.6f;

    [Tooltip("Biên độ cụm mây dập dềnh lên xuống, mét.")]
    public float bobAmount = 1.2f;

    [Header("Lòng chảo mây")]
    [Tooltip("Bên TRONG bán kính đảo, nóc mây phải nằm sâu dưới mặt đảo ít nhất bấy nhiêu mét.\n\n" +
             "⚠️ ĐÂY LÀ THỨ CHỮA 'THẤY CẠNH MÂY TRÊN ĐẢO'. Trước đây ô Top Below Surface chỉ " +
             "quy định TÂM cụm mây, mà một cụm cao 55m thì nửa trên của nó vươn lên +25m - " +
             "xuyên thẳng qua mặt đảo. Nới vòng trong cũng vô ích, vì từng cụm nhỏ còn bị " +
             "lệch ngẫu nhiên tới 116m theo mọi hướng, kể cả hướng vào trong.\n\n" +
             "Giờ mọi cụm được đặt theo MÉP TRÊN, và mép trên đó bị giới hạn theo khoảng " +
             "cách tới tâm đảo - trong đảo thì chìm hẳn, ở mép thì ôm sát vách, ra xa thì " +
             "được dâng lên. Mây dưới đảo vẫn còn nguyên, người rơi vẫn chìm vào mây.")]
    public float underIslandDepth = 12f;

    [Tooltip("Ở rìa ngoài cùng, nóc mây được phép dâng CAO HƠN mặt đảo bấy nhiêu mét.\n\n" +
             "Đây là thứ giữ cảm giác ĐẢO NẰM GIỮA BIỂN MÂY: mây phía xa dâng lên bao quanh, " +
             "đảo như ngồi trong một lòng chảo. Dâng dần theo bình phương khoảng cách, nên " +
             "gần mép gần như phẳng, càng xa càng dốc lên.\n\n" +
             "Không che trận đánh: mây ở cách xa trên 100m, mà mọi giao tranh đều trên đảo. " +
             "Đặt 0 nếu muốn biển mây phẳng lì.")]
    public float farRise = 12f;

    [Tooltip("Bật soft particles: mây tan dần khi chạm vào vách đảo thay vì bị cắt thành một " +
             "đường thẳng.\n\n" +
             "⚠️ Mỗi cụm mây là một tấm phẳng. Cắm vào địa hình thì GPU vẽ phần phía trên mặt " +
             "đất và bỏ phần bên dưới - chỗ bị cắt thành một đường sắc cạnh. Mà chỗ mây gặp " +
             "vách lại chính là chỗ tạo cảm giác 'đảo trên mây', nên đường cắt đó rất lộ.\n\n" +
             "Cần Depth Texture. Script tự bật nó cho camera người chơi, không cần sửa URP " +
             "asset. Tốn thêm một chút hiệu năng để đọc depth.")]
    public bool softParticles = true;

    [Tooltip("Mây bắt đầu tan khi còn cách bề mặt khác bấy nhiêu mét. Lớn thì chỗ giao mềm " +
             "và rộng, nhỏ thì sát và gọn.")]
    public float softFadeDistance = 6f;

    // ==================== TRẠNG THÁI ====================

    private Mesh _mesh;
    private MeshFilter _filter;
    private MeshRenderer _renderer;
    private Material _material;
    private Texture2D _puffTexture;

    private Vector3[] _vertices;
    private Color[] _colors;
    private Vector2[] _uv;

    // Dữ liệu gốc của từng cụm: vị trí phẳng, cỡ, pha dập dềnh.
    private Vector3[] _puffPos;
    private float[] _puffSize;
    private float[] _puffPhase;
    private int[] _puffCell;   // ô nào trong atlas
    private Color[] _puffTint;      // màu ĐỈNH TRÊN của cụm
    private Color[] _puffTintLow;   // màu ĐỈNH DƯỚI của cụm

    private Camera _camera;
    private int _builtCount = -1;
    private int _builtGrid = -1;

    private bool _dirty;

    private void OnEnable()
    {
        Build();
    }

    /// <summary>
    /// Unity gọi hàm này mỗi khi một ô trong Inspector bị sửa.
    ///
    /// ⚠️ THIẾU HÀM NÀY LÀ MỌI Ô ĐỀU VÔ DỤNG SAU LẦN DỰNG ĐẦU.
    ///
    /// Toàn bộ cấu hình chỉ được ĐỌC MỘT LẦN, trong Build(). Mà Build() chỉ chạy ở
    /// OnEnable. Nên kéo ảnh mây vào ô Custom Puff Texture sau khi đã gắn component thì
    /// nó chẳng có tác dụng gì - biển mây vẫn dùng ảnh script tự vẽ, y như cũ, và không
    /// có lỗi nào báo ra cả.
    ///
    /// Không dựng thẳng trong đây vì OnValidate chạy giữa lúc Unity đang tuần tự hoá;
    /// gọi AddComponent hay Destroy ở đó là Unity la ngay. Chỉ cắm cờ, LateUpdate dựng.
    /// </summary>
    private void OnValidate()
    {
        _dirty = true;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) Destroy(_material);
            // Chỉ huỷ ảnh do MÌNH sinh ra. Huỷ nhầm ảnh của hoạ sĩ là xoá luôn asset
            // khỏi bộ nhớ, mọi thứ khác dùng chung nó cũng hỏng theo.
            if (_puffTexture != null) Destroy(_puffTexture);
        }
    }

    private void Build()
    {
        _filter = GetComponent<MeshFilter>();
        if (_filter == null) _filter = gameObject.AddComponent<MeshFilter>();

        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<MeshRenderer>();

        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;

        // Mây không nhận ánh sáng động, không dính probe: nó là một tấm phẳng tô màu sẵn.
        // Bật lên chỉ tốn, mà còn làm mây ám màu theo đèn trong cảnh.
        _renderer.lightProbeUsage = LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        BuildMaterial();
        BuildPuffs();
        BuildMesh();

        UpdateMesh();
    }

    private void BuildMaterial()
    {
        // ⚠️ KHÔNG THOÁT SỚM KHI ĐÃ CÓ VẬT LIỆU.
        //
        // Bản trước viết "if (_material != null) return;" - nghĩa là vật liệu tạo xong
        // một lần rồi thôi, không bao giờ đọc lại ô ảnh nữa. Đó chính là lý do kéo ảnh
        // mây vào mà biển mây không đổi gì.
        //
        // Tạo vật liệu thì chỉ một lần thật, nhưng GÁN ẢNH thì phải làm mỗi lần dựng.
        if (_material == null) BuildMaterialOnce();
        if (_material == null) return;

        Texture2D tex = BuildPuffTexture();

        // Gán cả ba ô: mainTexture ghi vào "_MainTex" - tên của pipeline CŨ, còn shader
        // URP đọc "_BaseMap". Thiếu một trong hai là shader không nhận được ảnh.
        _material.mainTexture = tex;
        if (_material.HasProperty("_BaseMap")) _material.SetTexture("_BaseMap", tex);
        if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", Color.white);

        // SOFT PARTICLES. Đặt ở đây chứ không ở BuildMaterialOnce, để bật/tắt trong
        // Inspector là ăn ngay (OnValidate gọi dựng lại).
        //
        // Phải tự tính _SoftParticleFadeParams: vật liệu tạo bằng code không có giao
        // diện Inspector của URP, mà chính giao diện đó mới là thứ tính ô này. Thiếu nó
        // thì bật từ khoá cũng vô dụng - shader đọc (0, 0) và không tan gì cả. Công thức
        // chép đúng từ ParticleGUI.cs của URP: (gần, 1 / (xa - gần)).
        bool soft = softParticles && softFadeDistance > 0.01f;

        _material.SetFloat("_SoftParticlesEnabled", soft ? 1f : 0f);
        _material.SetFloat("_SoftParticlesNearFadeDistance", 0f);
        _material.SetFloat("_SoftParticlesFarFadeDistance", soft ? softFadeDistance : 1f);
        _material.SetVector("_SoftParticleFadeParams",
                            soft ? new Vector4(0f, 1f / softFadeDistance, 0f, 0f) : Vector4.zero);

        if (soft) _material.EnableKeyword("_SOFTPARTICLES_ON");
        else _material.DisableKeyword("_SOFTPARTICLES_ON");

        _renderer.sharedMaterial = _material;
    }

    private void BuildMaterialOnce()
    {
        // Cùng shader với ControlZone và CombatVFX, vì đây là loại duy nhất trong URP vừa
        // không nhận sáng, vừa đọc được MÀU ĐỈNH, vừa chỉnh được blend.
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
        {
            Debug.LogError("[CloudBank] Không tìm thấy shader để vẽ mây.", this);
            return;
        }

        _material = new Material(shader) { name = "CloudBank (runtime)" };

        // ⚠️ ALPHA, KHÔNG PHẢI CỘNG SÁNG - KHÁC HẲN LỬA Ở CONTROLZONE.
        //
        // Lửa là ánh sáng nên nó CỘNG vào cảnh. Mây thì ngược lại: nó CHE ánh sáng phía
        // sau. Dùng cộng sáng cho mây thì mây càng dày càng trắng xoá và càng trong suốt -
        // sai hoàn toàn cảm giác, và nó sẽ không giấu được cái hố bên dưới.
        _material.SetFloat("_Surface", 1f);   // Transparent
        _material.SetFloat("_Blend", 0f);     // Alpha
        _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        _material.SetFloat("_ZWrite", 0f);
        _material.SetFloat("_Cull", 0f);

        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.DisableKeyword("_ALPHATEST_ON");

        _material.renderQueue = (int)RenderQueue.Transparent;

        // Vật liệu do code sinh ra thì ĐỪNG để nó bị ghi vào file scene.
        _material.hideFlags = HideFlags.DontSave;
    }

    /// <summary>
    /// Ảnh một cụm mây.
    ///
    /// ⚠️ HAI THỨ LÀM NÊN "MÂY" THAY VÌ "SƯƠNG", VÀ CẢ HAI ĐỀU NẰM TRONG ẢNH NÀY:
    ///
    /// 1. VIỀN SÚP LƠ. Mây không tròn. Nó là nhiều bướu phình chồng lên nhau, mỗi bướu
    ///    một cỡ. Một hình tròn dù có làm mép mềm tới đâu cũng vẫn đọc ra là một đốm khói.
    ///    Ở đây gộp 7 bướu lệch tâm rồi lấy bướu nào đặc nhất - ra đúng cái viền gồ ghề
    ///    mà mắt nhận ngay là mây.
    ///
    /// 2. NẮNG TỪ TRÊN. Nóc cụm trắng chói, bụng cụm xám xanh. Đây là thứ QUAN TRỌNG NHẤT
    ///    và cũng là thứ bản trước thiếu hẳn - nó tô một màu đều nên dù hình có đẹp vẫn
    ///    phẳng lì như sương. Chênh lệch sáng tối là cái duy nhất cho mắt biết khối mây có
    ///    BỀ DÀY.
    ///
    /// Độ dốc sáng-tối ghi thẳng vào kênh RGB của ảnh, còn màu đỉnh của lưới nhân lên
    /// trên đó - nên mỗi cụm vừa có nắng riêng vừa có độ sâu riêng.
    /// </summary>
    private Texture2D BuildPuffTexture()
    {
        // Có ảnh của hoạ sĩ thì dùng luôn, khỏi vẽ.
        if (customPuffTexture != null) return customPuffTexture;

        if (_puffTexture != null) return _puffTexture;

        const int size = 192;
        _puffTexture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = "CloudPuff (runtime)",
            hideFlags = HideFlags.DontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        // Bảy bướu: một bướu chính ở giữa, sáu bướu con bám quanh vai và đỉnh. Toạ độ
        // lệch lên trên vì mây nở về phía nắng, đáy thì bằng và tan ra.
        Vector3[] lobes =
        {
            new Vector3( 0.00f,  0.02f, 0.46f),
            new Vector3(-0.30f, -0.06f, 0.30f),
            new Vector3( 0.31f, -0.04f, 0.32f),
            new Vector3(-0.15f,  0.26f, 0.26f),
            new Vector3( 0.18f,  0.24f, 0.24f),
            new Vector3(-0.42f, -0.20f, 0.18f),
            new Vector3( 0.44f, -0.18f, 0.19f),
        };

        float half = size * 0.5f;
        const float seed = 61.7f;

        for (int y = 0; y < size; y++)
        {
            float ny = (y + 0.5f - half) / half;

            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - half) / half;

                // Lấy bướu ĐẶC NHẤT tại điểm này, không cộng dồn. Cộng dồn thì chỗ giao
                // nhau của các bướu sáng vọt lên và viền lại thành một khối tròn trở lại.
                float a = 0f;
                for (int i = 0; i < lobes.Length; i++)
                {
                    float dx = nx - lobes[i].x;
                    float dy = ny - lobes[i].y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / lobes[i].z;
                    a = Mathf.Max(a, Mathf.Clamp01(1f - d));
                }

                // Nhiễu làm rách viền. Tác động vào ĐỘ ĐẶC ở vùng rìa thôi, chừa lõi lại
                // cho đặc - nếu không thì cả cụm lốm đốm như bọt biển.
                float n = Mathf.PerlinNoise(seed + x * 0.045f, seed + y * 0.045f) * 0.65f
                        + Mathf.PerlinNoise(seed + x * 0.13f, seed + y * 0.13f) * 0.35f;

                a *= Mathf.Lerp(0.35f + n * 0.9f, 1f, a);
                a = Mathf.Clamp01(a);

                // Mép tan nhanh, lõi giữ đặc.
                a = a * a * (3f - 2f * a);

                // NẮNG TỪ TRÊN: pha màu theo độ cao TRONG cụm. Cộng thêm một chút theo độ
                // đặc, nên phần lồi ra ngoài sáng hơn phần lõm vào trong.
                float lit = Mathf.Clamp01(ny * 0.5f + 0.5f);
                lit = Mathf.Lerp(lit, 1f, a * 0.25f);
                lit = Mathf.Pow(lit, 1.35f);

                float shade = Mathf.Lerp(0.34f, 1f, lit);

                _puffTexture.SetPixel(x, y, new Color(shade, shade * 1.01f, shade * 1.04f, a));
            }
        }

        _puffTexture.Apply();
        return _puffTexture;
    }

    private void BuildPuffs()
    {
        int n = Mathf.Max(1, puffCount);

        _puffPos = new Vector3[n];
        _puffSize = new float[n];
        _puffPhase = new float[n];
        _puffCell = new int[n];
        _puffTint = new Color[n];
        _puffTintLow = new Color[n];

        float inner = islandRadius * innerFactor;
        float outer = Mathf.Max(inner + 1f, outerRadius);

        // Hạt cố định: cùng một bố cục mỗi lần chạy, nên nhìn quen thuộc và test được.
        Random.State saved = Random.state;
        Random.InitState(20260911);

        // ---- Bước 1: rải TÂM KHỐI ----
        int clusters = Mathf.Max(1, clusterCount);
        Vector3[] centers = new Vector3[clusters];
        float[] centerSize = new float[clusters];
        float[] centerT = new float[clusters];   // 0 = sát mép đảo, 1 = rìa ngoài cùng
        float[] centerDepth = new float[clusters];

        for (int c = 0; c < clusters; c++)
        {
            // CĂN BẬC HAI để mật độ đều theo DIỆN TÍCH. Bốc bán kính đều tay thì vành
            // trong (diện tích nhỏ) bị nhồi dày còn vành ngoài thì thưa hoác.
            float t = Mathf.Sqrt(Random.value);
            float r = Mathf.Lerp(inner, outer, t);
            float ang = Random.value * Mathf.PI * 2f;

            centers[c] = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            centerSize[c] = Mathf.Lerp(puffSizeNear, puffSizeFar, t);
            centerT[c] = t;

            // Độ sâu của CẢ KHỐI. Trộn giữa "dồn lên nóc" và "rải đều" theo verticalSpread.
            float rd = Random.value;
            centerDepth[c] = Mathf.Lerp(rd * rd, rd, verticalSpread) * thickness;
        }

        // ---- Bước 2: rải cụm con quanh từng tâm khối ----
        for (int i = 0; i < n; i++)
        {
            int c = i % clusters;
            float baseSize = centerSize[c];

            // Lệch khỏi tâm khối theo phân bố dồn vào giữa (nhân hai số ngẫu nhiên), nên
            // khối đặc ở lõi và thưa dần ra rìa - đúng hình một đám mây thật.
            float off = Random.value * Random.value * baseSize * clusterSpread * 2.2f;
            float oa = Random.value * Mathf.PI * 2f;

            // Càng xuống sâu càng thưa: mặt trên biển mây đặc, đáy tan dần.
            // Lấy độ sâu của KHỐI rồi nhấp nhô quanh đó một ít - xem ô Depth Variance.
            float jitter = (Random.value - 0.5f) * thickness * depthVariance;
            float depth = Mathf.Clamp(centerDepth[c] + jitter, 0f, thickness);

            // Cụm con nhỏ hơn tâm khối, và chênh nhau nhiều - vài cụm to làm thân, nhiều
            // cụm nhỏ làm bướu quanh mép. Phải biết cỡ TRƯỚC khi đặt độ cao, vì độ cao giờ
            // tính theo mép trên của cụm.
            _puffSize[i] = baseSize * Random.Range(0.45f, 1.15f);

            float px = centers[c].x + Mathf.Cos(oa) * off;
            float pz = centers[c].z + Mathf.Sin(oa) * off;

            // CHẶN TỪNG CỤM, KHÔNG CHỈ TÂM KHỐI.
            //
            // Vòng trong chỉ quyết định chỗ đặt tâm khối; cụm con lệch ngẫu nhiên thì chui
            // thẳng vào giữa đảo. Không cho cụm nào lọt vào sâu hơn 70% vòng trong: dưới đó
            // thì nó bị thân đảo che kín từ mọi phía, vẽ ra chỉ tốn overdraw vô ích.
            float pr = Mathf.Sqrt(px * px + pz * pz);
            float minR = inner * 0.7f;
            if (pr < minR)
            {
                float k = pr > 0.001f ? minR / pr : 0f;
                if (pr > 0.001f) { px *= k; pz *= k; }
                else { px = Mathf.Cos(oa) * minR; pz = Mathf.Sin(oa) * minR; }
                pr = minR;
            }

            // ĐẶT THEO MÉP TRÊN: nóc cụm (tâm + nửa chiều cao + biên độ dập dềnh) không bao
            // giờ vượt quá trần của vùng đó. Hệ số 0.85 vì trong ô ảnh còn một viền trong
            // suốt - mép mây thật nằm thấp hơn mép tấm một chút.
            float visibleHalf = _puffSize[i] * 0.5f * 0.85f + bobAmount;

            _puffPos[i] = new Vector3(px, CeilingAt(pr) - visibleHalf - depth, pz);
            _puffPhase[i] = Random.value * Mathf.PI * 2f;

            int grid = Mathf.Max(1, puffAtlasGrid);
            _puffCell[i] = Random.Range(0, grid * grid);

            // Cụm nằm sâu thì tối hơn: nắng không lọt xuống được. Đây là thứ làm biển mây
            // có CHIỀU DÀY thay vì trông như một tấm decal phẳng.
            float depth01 = depth / Mathf.Max(0.01f, thickness);
            // Mũ 1.4 thay cho 0.6: trước đó mới sâu một phần tư bề dày đã tối tới 43%,
            // nên GẦN NHƯ MỌI cụm đều mang màu bụng. Mũ lớn hơn thì chỉ những cụm thật
            // sự nằm đáy mới tối, phần trên vẫn sáng.
            float dt = Mathf.Pow(depth01, 1.4f) * depthShading;
            Color c2 = Color.Lerp(topColor, bottomColor, dt);

            float alpha = opacity * Random.Range(0.82f, 1f);

            // ĐỘ DỐC NẮNG NGAY TRÊN BỐN ĐỈNH CỦA TẤM.
            //
            // ⚠️ VÌ SAO KHÔNG ĐỂ TRONG ẢNH: ảnh do script tự vẽ thì ghi độ dốc vào được,
            // nhưng dùng ảnh của hoạ sĩ (ô Custom Puff Texture) là mất sạch - ảnh ngoài
            // chỉ có một màu xám đều, và cả biển mây phẳng lì trở lại.
            //
            // Đặt ở màu đỉnh thì nó SỐNG SÓT qua mọi ảnh: hai đỉnh trên sáng, hai đỉnh
            // dưới tối, phần cờ nội suy ở giữa. Đổi ảnh nào cũng vẫn có nắng từ trên.
            // Kéo MẠNH về hai màu, không pha loãng.
            //
            // Bản trước để 0.45 / 0.55 nên màu bị hoà về giữa và gần như biến mất - cộng
            // thêm ảnh đã là thang xám nữa thì kết quả chỉ còn trắng xám.
            Color hi = Color.Lerp(c2, topColor, 0.7f);
            Color lo = Color.Lerp(c2, bottomColor, 0.92f);

            // PHỐI CẢNH KHÔNG KHÍ: càng xa càng nhoà về màu trời, và càng mất tương phản.
            //
            // centerT là vị trí của khối này trên đường từ mép đảo (0) ra rìa ngoài (1).
            // Mũ 1.6 để phần gần giữ nguyên nét, chỉ phần thật xa mới tan đi.
            float far = Mathf.Pow(centerT[c], 1.6f) * distanceFade;
            if (far > 0f)
            {
                hi = Color.Lerp(hi, horizonColor, far);
                lo = Color.Lerp(lo, horizonColor, far);
            }

            hi.a = alpha;
            lo.a = alpha;

            _puffTint[i] = hi;
            _puffTintLow[i] = lo;
        }

        Random.state = saved;
        _builtCount = n;
        _builtGrid = puffAtlasGrid;
    }

    /// <summary>
    /// Nóc mây được phép cao tới đâu, tuỳ khoảng cách R tới tâm đảo. Hình lòng chảo.
    ///
    ///   R trong vòng trong      : chìm hẳn, -underIslandDepth
    ///   vòng trong -> mép đảo   : dâng dần lên -topBelowSurface  (ôm sát vách)
    ///   mép đảo -> rìa ngoài    : dâng tiếp lên +farRise          (lòng chảo)
    /// </summary>
    private float CeilingAt(float r)
    {
        float inR = islandRadius * innerFactor;
        float rim = Mathf.Max(inR + 0.01f, islandRadius);

        if (r <= inR) return -underIslandDepth;

        if (r <= rim)
        {
            float t = (r - inR) / (rim - inR);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(-underIslandDepth, -topBelowSurface, t);
        }

        // Bình phương: sát mép gần như phẳng, càng xa càng dốc - mép đảo vẫn là chỗ mây
        // ôm vách, không bị dâng lên che vách.
        float k = Mathf.Clamp01((r - rim) / Mathf.Max(1f, outerRadius - rim));
        return Mathf.Lerp(-topBelowSurface, farRise, k * k);
    }

    private void BuildMesh()
    {
        int n = _builtCount;

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "CloudBank (runtime)" };
            _mesh.MarkDynamic();

            // ⚠️ ĐỪNG ĐỂ LƯỚI SINH RA LÚC CHẠY BỊ GHI VÀO FILE SCENE.
            //
            // Component có [ExecuteAlways] nên nó dựng lưới ngay trong Editor. Lưới đó
            // bị MeshFilter tham chiếu, mà Unity ghi mọi thứ được tham chiếu vào file
            // scene khi lưu - nên mỗi lần Ctrl+S là TestScene.unity phình thêm một lưới
            // 720 đỉnh và một ảnh 192x192 nhúng thẳng vào đó.
            //
            // DontSave nói với Unity: thứ này sống trong bộ nhớ thôi, đừng lưu.
            _mesh.hideFlags = HideFlags.DontSave;
        }

        _mesh.Clear();

        _vertices = new Vector3[n * 4];
        _colors = new Color[n * 4];
        _uv = new Vector2[n * 4];

        int[] tris = new int[n * 6];

        for (int i = 0; i < n; i++)
        {
            int v = i * 4;

            // UV cat đúng một ô của atlas. Lật ngang ngẫu nhiên nữa, nên bốn hình
            // thành tám dáng mà không tốn thêm pixel nào.
            int grid = Mathf.Max(1, puffAtlasGrid);
            float step = 1f / grid;

            int cx = _puffCell[i] % grid;
            int cy = _puffCell[i] / grid;

            float u0 = cx * step;
            float u1 = u0 + step;

            // Lật: dùng chính chỉ số cụm cho chẵn/lẻ, nên bố cục vẫn cố định giữa các
            // lần chạy - dựng lại scene là ra y hệt, dễ so sánh khi chỉnh.
            if ((i & 1) == 0) { float swap = u0; u0 = u1; u1 = swap; }

            // Ảnh trong Unity có gốc toạ độ ở DƯỚI, còn atlas ghép từ trên xuống -
            // nên hàng phải lật lại, không thì hàng trên và hàng dưới đổi chỗ cho nhau.
            float v1 = 1f - cy * step;
            float v0 = v1 - step;

            _uv[v + 0] = new Vector2(u0, v0);
            _uv[v + 1] = new Vector2(u1, v0);
            _uv[v + 2] = new Vector2(u1, v1);
            _uv[v + 3] = new Vector2(u0, v1);

            // Thứ tự đỉnh dựng ở UpdateMesh: 0 và 1 nằm DƯỚI, 2 và 3 nằm TRÊN.
            _colors[v + 0] = _puffTintLow[i];
            _colors[v + 1] = _puffTintLow[i];
            _colors[v + 2] = _puffTint[i];
            _colors[v + 3] = _puffTint[i];

            int t = i * 6;
            tris[t + 0] = v + 0;
            tris[t + 1] = v + 2;
            tris[t + 2] = v + 1;
            tris[t + 3] = v + 0;
            tris[t + 4] = v + 3;
            tris[t + 5] = v + 2;
        }

        _mesh.vertices = _vertices;
        _mesh.uv = _uv;
        _mesh.colors = _colors;
        _mesh.triangles = tris;

        // TỰ ĐẶT HỘP BAO. Đỉnh bị ghi lại mỗi khung hình nên Unity không tính kịp, mà
        // tính sai hộp bao thì cả biển mây bị cắt khỏi màn hình khi quay đầu.
        // Cộng thêm cả cỡ cụm lớn nhất: đỉnh của một cụm ở rìa ngoài còn thò ra quá
        // outerRadius thêm nửa chiều rộng cụm nữa.
        float span = (outerRadius + puffSizeFar) * 2.2f;

        _mesh.bounds = new Bounds(Vector3.zero,
                                  new Vector3(span, thickness * 4f + puffSizeFar + 40f, span));

        _filter.sharedMesh = _mesh;
    }

    private void LateUpdate()
    {
        if (_dirty || _builtCount != puffCount || _builtGrid != puffAtlasGrid)
        {
            _dirty = false;
            Build();
        }
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        if (_mesh == null || _puffPos == null) return;

        Camera cam = ResolveCamera();

        // ⚠️ KHÔNG THOÁT SỚM KHI CHƯA CÓ CAMERA.
        //
        // Bản đầu của ControlZone thoát sớm ở đây và để nguyên toàn bộ đỉnh ở gốc toạ độ -
        // kết quả là lửa vô hình mà không có lỗi nào cả. Không có camera thì dựng theo một
        // hướng mặc định, xấu còn hơn là mất tăm.
        Vector3 right, up;

        if (cam != null)
        {
            right = cam.transform.right;

            // ⚠️ TRỤC DỌC BÁM THEO TRỜI, KHÔNG BÁM THEO CAMERA.
            //
            // Độ dốc nắng nằm sẵn trong ảnh cụm mây: nóc trắng, bụng xám. Nếu tấm quay
            // theo trục dọc của camera thì nghiêng đầu một cái là cả biển mây nghiêng
            // theo - nắng đang từ trên bỗng chiếu từ bên hông, và mọi cảm giác khối
            // lượng tan biến.
            //
            // Lấy hướng lên của THẾ GIỚI rồi gạt bỏ phần dọc theo tia nhìn. Tấm vẫn quay
            // mặt về camera, nhưng "trên" của nó luôn là trên thật.
            Vector3 fwd = cam.transform.forward;
            up = Vector3.up - fwd * Vector3.Dot(fwd, Vector3.up);

            // Nhìn thẳng đứng xuống thì phép trên suy biến - lúc đó lấy tạm trục camera,
            // và cũng không sao: nhìn từ trên xuống thì chỉ thấy nóc mây.
            if (up.sqrMagnitude < 0.0001f) up = cam.transform.up;
            else up.Normalize();
        }
        else
        {
            right = Vector3.right;
            up = Vector3.up;
        }

        float time = Application.isPlaying ? Time.time : 0f;
        Quaternion drift = Quaternion.Euler(0f, time * driftDegPerSecond, 0f);

        for (int i = 0; i < _builtCount; i++)
        {
            Vector3 p = drift * _puffPos[i];
            p.y += Mathf.Sin(time * 0.25f + _puffPhase[i]) * bobAmount;

            float h = _puffSize[i] * 0.5f;
            Vector3 rx = right * h;
            Vector3 uy = up * h;

            int v = i * 4;
            _vertices[v + 0] = p - rx - uy;
            _vertices[v + 1] = p + rx - uy;
            _vertices[v + 2] = p + rx + uy;
            _vertices[v + 3] = p - rx + uy;
        }

        _mesh.vertices = _vertices;
    }

    /// <summary>
    /// Tìm camera đang dùng. Camera.main CHỈ tìm thấy camera đang BẬT và có tag MainCamera -
    /// mà camera của người chơi khác đều bị tắt, nên nó vẫn đúng ở đây.
    /// </summary>
    private Camera ResolveCamera()
    {
        if (_camera != null && _camera.isActiveAndEnabled) return _camera;

        _camera = Camera.main;

        // Soft particles đọc _CameraDepthTexture. Asset PC hiện đã bật sẵn Depth Texture,
        // nhưng asset Mobile thì TẮT - đổi quality level là mây bị cắt cạnh trở lại mà
        // không ai hiểu vì sao. Bật thẳng trên camera cho chắc, chỉ lúc đang chạy game để
        // khỏi gắn component lạ vào camera của Scene view.
        if (_camera != null && softParticles && Application.isPlaying)
        {
            UniversalAdditionalCameraData data = _camera.GetUniversalAdditionalCameraData();
            if (data != null) data.requiresDepthOption = CameraOverrideOption.On;
        }

#if UNITY_EDITOR
        if (_camera == null && !Application.isPlaying)
        {
            if (UnityEditor.SceneView.lastActiveSceneView != null)
                _camera = UnityEditor.SceneView.lastActiveSceneView.camera;
        }
#endif
        return _camera;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Vẽ hai vòng để đặt vị trí cho dễ: vành trong và vành ngoài của biển mây.
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.8f);

        Vector3 top = transform.position + Vector3.down * topBelowSurface;
        DrawCircle(top, islandRadius * innerFactor);
        DrawCircle(top, outerRadius);

        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.6f);
        DrawCircle(transform.position, islandRadius);
    }

    private static void DrawCircle(Vector3 center, float r)
    {
        const int seg = 64;
        Vector3 prev = center + new Vector3(r, 0f, 0f);

        for (int i = 1; i <= seg; i++)
        {
            float a = (float)i / seg * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
