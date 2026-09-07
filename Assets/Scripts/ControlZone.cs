using UnityEngine;

/// <summary>
/// Khu chiếm đóng - vùng người chơi phải đứng vào để thắng round.
///
/// ĐẶT MỘT OBJECT RỖNG VÀO GIỮA MAP RỒI GẮN SCRIPT NÀY LÊN.
///
/// CỐ Ý LÀ MONOBEHAVIOUR THUẦN, KHÔNG PHẢI NetworkBehaviour.
///
/// Phần LUẬT CHƠI của script này không giữ trạng thái gì cả - nó chỉ trả lời câu hỏi
/// "ai đang đứng trong khu". Toàn bộ tiến độ chiếm nằm ở GameManager, vốn đã là
/// NetworkObject sẵn.
///
/// Vì sao làm vậy: đặt một NetworkObject sẵn trong scene là thêm một chỗ có thể hỏng
/// (Fusion phải bake ID cho scene object). Tiến độ chỉ là hai con số float, nhét vào
/// GameManager rẻ hơn nhiều. Đúng nguyên tắc "chỉ đồng bộ thứ BẮT BUỘC" của project.
///
/// Phần HÌNH ẢNH (biển lửa) thuần cục bộ - mỗi máy tự dựng, không gửi gì qua mạng.
/// </summary>
public class ControlZone : MonoBehaviour
{
    /// <summary>Để GameManager tìm thấy mà không cần kéo thả Inspector, giống GameManager.Instance.</summary>
    public static ControlZone Instance { get; private set; }

    [Header("Kích thước vùng")]
    [Tooltip("Bán kính tính theo phương NGANG, đơn vị mét.")]
    public float radius = 7f;

    [Tooltip("Chiều cao vùng tính từ tâm lên và xuống. Cần vì khu chiếm ở trên cao, " +
             "người đứng dưới chân dốc không được tính là đang giữ.")]
    public float halfHeight = 3f;

    // ==================== BIỂN LỬA ====================
    //
    // Trước 01/09 khu chiếm đóng CHỈ có Gizmo, mà Gizmo chỉ hiện trong Scene view của
    // Editor - trong game nó VÔ HÌNH hoàn toàn. Người chơi mới không có cách nào biết
    // mục tiêu là đứng vào giữa vòng.
    //
    // Đây không phải trang trí. Nó là thứ DẠY LUẬT CHƠI mà không cần chữ hướng dẫn.
    //
    // ⚠️ ĐÃ THỬ VÀ BỎ HAI CÁCH, đừng quay lại:
    //   1. LineRenderer vẽ vòng lượn -> chỉ ra một SỢI DÂY méo. LineRenderer vẽ được cái
    //      VIỀN của ngọn lửa, không tô được phần ruột. Nhìn ra "vòng tròn", không ra lửa.
    //   2. Dải mesh dựng đứng ở viền -> vẫn chỉ là cái viền, giữa khu trống trơn.
    //
    // Cách đang dùng: RẢI HÀNG TRĂM LƯỠI LỬA KHẮP MẶT SÀN của khu. Mỗi lưỡi là một tam
    // giác quay mặt về camera, cao thấp theo nhiễu Perlin, mờ dần lên đỉnh.

    [Header("Biển lửa")]
    [Tooltip("Tự dựng biển lửa bằng code lúc chạy. KHÔNG cần tạo prefab hay material nào.\n\n" +
             "Dựa vào Bloom (đã bật sẵn 0.7 ở DefaultVolumeProfile): màu HDR nhỉnh hơn 1 " +
             "sẽ tự loang hào quang. Đó là lý do các ô màu bên dưới chỉ hơi vượt 1 - " +
             "không cần cao hơn, cao hơn là chói.")]
    public bool buildVisualAtRuntime = true;

    [Tooltip("Số lưỡi lửa rải khắp khu.\n\n" +
             "Toàn bộ nằm trong MỘT mesh nên chỉ tốn một lượt vẽ, số lượng lớn không đáng " +
             "ngại. 220 lưỡi trên khu bán kính 7m cho mật độ vừa - thưa quá thì thành mấy " +
             "đốm lẻ loi, dày quá thì thành một tấm thảm đặc không còn thấy lưỡi.")]
    [Range(40, 600)]
    public int tongueCount = 300;

    [Tooltip("Chiều cao trung bình của lưỡi lửa, mét.\n\n" +
             "Cố ý rất thấp. Người chơi phải NHÌN XUYÊN QUA được biển lửa để thấy đối thủ " +
             "đứng bên kia khu - cao quá thì lửa thành bức tường che tầm nhìn, và đó là " +
             "phá hoại chứ không phải trang trí.")]
    public float flameHeight = 1.7f;

    [Tooltip("Chiều cao lửa khi đội dẫn đã chiếm gần xong.\n\n" +
             "Lửa bốc cao dần theo tiến độ là cách báo 'sắp mất round' mà không cần thêm " +
             "thứ gì lên màn hình. Đừng quá 1.2 - qua tầm ngực là bắt đầu che mắt.")]
    public float flameHeightAtFull = 2.6f;

    [Tooltip("Bề ngang trung bình một lưỡi lửa, mét.")]
    public float tongueWidth = 1.1f;

    [Tooltip("Tốc độ lửa nhảy. Thấp = lửa lười, cao = lửa gắt.")]
    public float flameSpeed = 1.6f;

    [Tooltip("Lưỡi lửa ngả nghiêng bao nhiêu mét ở phần ngọn. Đây là thứ làm lửa 'mềm' - " +
             "lưỡi đứng thẳng đơ trông như hàng rào cọc nhọn.")]
    public float flameSway = 0.45f;

    [Tooltip("Lưỡi ở SÁT VIỀN cao hơn lưỡi ở giữa bao nhiêu lần.\n\n" +
             "Trên 1 thì viền khu tự nổi lên thành một vành lửa cao hơn, giúp nhìn ra ranh " +
             "giới 'trong hay ngoài' mà không cần vẽ thêm cái vòng riêng nào.")]
    [Range(0.5f, 3f)]
    public float edgeHeightBoost = 1.6f;

    [Tooltip("Độ đục của chân lửa, 0..1.\n\n" +
             "Đừng để quá cao. Biển lửa phủ kín mặt sàn mà đục thì người chơi đứng trong khu " +
             "không nhìn thấy chân đối thủ, và vật thể lăn qua khu cũng bị che mất.")]
    [Range(0f, 1f)]
    public float flameAlpha = 0.85f;

    [Tooltip("Màu LÕI NÓNG ở chân lửa. Lửa thật nóng nhất ở gốc nên chỗ đó ngả trắng, " +
             "càng lên cao càng ngả về màu của ngọn.\n\n" +
             "Đây là thứ tách biệt 'mấy cái tam giác màu cam' với 'ngọn lửa'. Lửa một màu " +
             "từ gốc tới ngọn thì mắt đọc ra ngay là hình vẽ.")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color hotCoreColor = new Color(2.6f, 2.3f, 1.9f);

    [Tooltip("Bao nhiêu phần chân lửa ngả về màu lõi nóng. 0 = một màu suốt, 1 = gốc trắng hẳn.")]
    [Range(0f, 1f)]
    public float hotCoreAmount = 0.5f;

    [Tooltip("Lửa BỐC LÊN rồi tan, thay vì chỉ nhấp nhô tại chỗ.\n\n" +
             "Đây là khác biệt lớn nhất giữa 'lửa' và 'cỏ rung'. Lửa thật luôn có hướng: " +
             "sinh ra ở gốc, vươn lên, mờ dần rồi biến mất. Tắt ô này thì mấy lưỡi lửa chỉ " +
             "phập phồng lên xuống như rong biển.")]
    public bool risingFlames = true;

    [Tooltip("Một lưỡi lửa sống bao nhiêu giây từ lúc sinh tới lúc tan hẳn.")]
    public float flameLifetime = 1.1f;

    [Tooltip("Ngọn lửa CHỤM VÀO TÂM khi vươn lên, bao nhiêu phần.\n\n" +
             "Đây là quy luật của mọi đám cháy thật: khí nóng bốc lên kéo không khí lạnh " +
             "từ xung quanh vào, nên cả khối lửa thu hẹp dần lên trên thành hình nón. " +
             "Lửa mọc thẳng đứng song song trông như một cánh đồng cỏ phát sáng.\n\n" +
             "0 = mọc thẳng, 1 = chụm mạnh.")]
    [Range(0f, 1f)]
    public float convergeToCenter = 0.35f;

    [Header("Quầng sáng dưới đất")]
    [Tooltip("Đĩa sáng loang trên mặt đất dưới chân lửa. Đặt 0 để bỏ.\n\n" +
             "Rẻ mà hiệu quả bậc nhất: lửa không có quầng sáng dưới chân trông như dán lơ " +
             "lửng lên mặt đất. Có quầng thì mắt tin là ngọn lửa đang THẬT SỰ chiếu sáng " +
             "chỗ nó đứng.")]
    [Range(0f, 1f)]
    public float groundGlowAlpha = 0.5f;

    [Tooltip("Quầng sáng rộng gấp bao nhiêu lần bán kính khu.")]
    public float groundGlowScale = 1.15f;

    [Header("Tàn lửa bay lên")]
    [Tooltip("Số đốm tàn lửa bay lên rồi tắt. Đặt 0 để bỏ.\n\n" +
             "Vài chục đốm sáng nhỏ trôi lên là thứ khiến ngọn lửa trông SỐNG. Mắt người " +
             "bám theo chuyển động của vật nhỏ mạnh hơn nhiều so với mảng lớn.")]
    [Range(0, 200)]
    public int emberCount = 70;

    [Tooltip("Tàn lửa bay cao bao nhiêu mét trước khi tắt.\n\n" +
             "Được phép cao hơn ngọn lửa nhiều: đốm nhỏ không che tầm nhìn, mà lại kéo mắt " +
             "người chơi từ xa nhìn về phía khu.")]
    public float emberRiseHeight = 5f;

    [Tooltip("Cỡ một đốm tàn lửa, mét.")]
    public float emberSize = 0.09f;

    [Tooltip("Một đốm sống bao nhiêu giây.")]
    public float emberLifetime = 2.2f;

    // ⚠️ BỐN MÀU DƯỚI ĐÂY CỐ Ý RẤT NHẠT.
    //
    // Bloom đang bật 0.7, nên màu HDR chỉ cần nhỉnh hơn 1 là đã loang hào quang rõ.
    // Để 3.0 thì cả khu chói loà, vừa che mất người đứng trong đó, vừa nuốt mất màu viền
    // găng tay - thứ quan trọng nhất phải đọc được trong game này.
    //
    // Khu chiếm đóng là thông tin NỀN: cần thấy được, không cần giành sự chú ý.

    [Header("Màu theo đội đang giữ — để NHẠT")]
    [Tooltip("Chưa ai đứng trong khu. Trắng ngà, nhạt nhất trong bốn màu.")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color neutralColor = new Color(1.6f, 1.55f, 1.4f);

    [Tooltip("Đội Đỏ đang giữ. Dùng ĐÚNG màu đội, không phải màu 'phe mình/phe địch' - " +
             "khu là vật thể trong thế giới, mọi người cùng nhìn thấy một màu.")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color redTeamColor = new Color(2.2f, 0.7f, 0.5f);

    [ColorUsage(showAlpha: false, hdr: true)]
    public Color blueTeamColor = new Color(0.6f, 1.1f, 2.2f);

    [Tooltip("Cả hai đội cùng đứng trong khu -> tiến độ đóng băng.")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color contestedColor = new Color(2.2f, 1.7f, 0.7f);

    [Tooltip("Tốc độ chuyển màu khi đổi chủ. Thấp = mượt, cao = đổi phắt.\n\n" +
             "Đừng để quá cao: hai người chạy ra chạy vào liên tục sẽ làm khu nhấp nháy " +
             "đỏ xanh loạn xạ, vừa xấu vừa khó đọc.")]
    public float colorBlendSpeed = 4f;

    [Header("Đèn hắt sáng")]
    [Tooltip("(Tuỳ chọn) Tự tạo nếu để trống. Hắt sáng nền đất quanh khu.")]
    public Light zoneLight;

    [Tooltip("Tầm chiếu, mét. Nên lớn hơn bán kính khu để ánh sáng loang ra ngoài mép.")]
    public float lightRange = 18f;

    [Tooltip("Độ mạnh đèn. Để 0 để bỏ hẳn đèn - đèn thời gian thực là thứ đắt nhất trong " +
             "cụm này, và map hiện chỉ có đúng 1 đèn.")]
    public float lightIntensity = 2.5f;

    [Header("Hiển thị trong Editor")]
    public Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.25f);

    // --- TRẠNG THÁI HÌNH ẢNH ---

    private Color _currentColor;
    private bool _hasColor;

    // Màu lõi nóng, tính MỘT LẦN mỗi khung hình thay vì 300 lần trong vòng lặp.
    private Color _hotColorCached;

    // --- QUẦNG SÁNG DƯỚI ĐẤT ---
    private Mesh _glowMesh;
    private Color[] _glowColors;

    // --- TÀN LỬA ---
    private Mesh _emberMesh;
    private Vector3[] _emberVerts;
    private Color[] _emberColors;
    private Vector3[] _emberBase;   // chỗ đốm sinh ra
    private float[] _emberPhase;
    private float[] _emberDrift;    // lệch ngang khi bay lên
    private float[] _emberSizeVar;

    private Material _glowMaterial;
    private Texture2D _flameTexture;
    private Mesh _fireMesh;
    private Camera _cam;

    // Mọi mảng dưới đây cấp phát MỘT LẦN rồi dùng lại mỗi khung hình.
    //
    // Cấp phát mới mỗi khung là ném rác cho bộ thu gom: 220 lưỡi × 3 đỉnh × 60 khung/giây
    // = gần 40 nghìn Vector3 mỗi giây. Đúng kiểu rác gây khựng hình định kỳ mà ta vừa
    // mất cả buổi đi dọn.
    private Vector3[] _verts;
    private Color[] _colors;

    // Thông số cố định của từng lưỡi, bốc ngẫu nhiên MỘT LẦN lúc dựng.
    private Vector3[] _tongueBase;    // chân lưỡi, toạ độ cục bộ
    private float[] _tongueWidthArr;
    private float[] _tongueHeightArr; // hệ số nhân chiều cao
    private float[] _tonguePhase;     // lệch pha, để chúng không nhảy đồng loạt

    private void Awake()
    {
        Instance = this;

        if (buildVisualAtRuntime) BuildVisual();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Material và Mesh tạo bằng new không tự mất khi object bị huỷ - Unity chỉ dọn
        // component, còn tài nguyên thì để lại làm rác trong bộ nhớ.
        if (_glowMaterial != null) Destroy(_glowMaterial);
        if (_fireMesh != null) Destroy(_fireMesh);
        if (_flameTexture != null) Destroy(_flameTexture);
        if (_glowMesh != null) Destroy(_glowMesh);
        if (_emberMesh != null) Destroy(_emberMesh);
    }

    // ==================== DỰNG BẰNG CODE ====================

    private void BuildVisual()
    {
        // ⚠️ PHẢI DÙNG SHADER CỦA URP. Sửa 01/09 - đây là lý do bản trước KHÔNG HIỆN GÌ.
        //
        // Bản đầu dùng "Sprites/Default", shader của pipeline CŨ (Built-in). Project này
        // chạy URP, mà URP chỉ vẽ những shader có pass đúng chuẩn của nó - shader lạ thì
        // bị bỏ qua IM LẶNG, không lỗi, không cảnh báo, chỉ đơn giản là không thấy gì.
        //
        // Cần shader có ĐỦ BA tính chất, và Particles/Unlit là loại duy nhất có cả ba:
        //   - Unlit           : lửa không bị ám màu theo ánh sáng cảnh
        //   - Ăn MÀU ĐỈNH     : để chân đặc ngọn trong. URP/Unlit thường KHÔNG có,
        //                       chỉ dòng Particles mới đọc màu đỉnh
        //   - Chỉnh được blend: để đặt chế độ cộng sáng
        Shader glowShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (glowShader == null) glowShader = Shader.Find("Particles/Standard Unlit");
        if (glowShader == null) glowShader = Shader.Find("Sprites/Default");

        if (glowShader == null)
        {
            Debug.LogError("[ControlZone] Không tìm thấy shader nào để vẽ lửa.", this);
            return;
        }

        _glowMaterial = new Material(glowShader) { name = "ZoneFire (runtime)" };

        // CỘNG SÁNG (Additive) thay vì hoà trộn alpha thường.
        //
        // Hai lý do:
        //   1. Lửa là ÁNH SÁNG, mà ánh sáng thì cộng vào cảnh chứ không che khuất cảnh.
        //      Cộng sáng cho ra đúng cảm giác đó, còn alpha thường cho ra cảm giác dán
        //      một tấm nhựa mờ lên màn hình.
        //   2. Cộng sáng thì KHÔNG BAO GIỜ vô hình trên nền tối - nó chỉ có thể làm chỗ
        //      đó sáng lên. An toàn hơn hẳn khi ta không xem được kết quả.
        _glowMaterial.SetFloat("_Surface", 1f);   // Transparent
        _glowMaterial.SetFloat("_Blend", 2f);     // Additive
        _glowMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glowMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        _glowMaterial.SetFloat("_ZWrite", 0f);
        _glowMaterial.SetFloat("_Cull", 0f);      // hai mặt, nhìn từ phía nào cũng thấy

        _glowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _glowMaterial.DisableKeyword("_ALPHATEST_ON");

        // Hàng đợi Transparent, nếu không nó vẽ trước địa hình rồi bị ghi đè.
        _glowMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // ẢNH LƯỠI LỬA MỀM — đây là bước nhảy chất lượng lớn nhất.
        //
        // Trước đây dùng ảnh trắng đặc, nên mỗi lưỡi là một TAM GIÁC CẠNH SẮC. Mắt người
        // nhận ra hình học ngay lập tức, và cả biển lửa trông như một đống mảnh giấy cắt.
        //
        // Ảnh có mép mềm biến mỗi tam giác thành một vệt sáng nhoè, và hàng trăm vệt nhoè
        // chồng lên nhau mới ra được cảm giác lửa. Cùng một hình học, khác hẳn kết quả.
        _flameTexture = CreateFlameTexture();

        _glowMaterial.mainTexture = _flameTexture;
        if (_glowMaterial.HasProperty("_BaseMap")) _glowMaterial.SetTexture("_BaseMap", _flameTexture);
        if (_glowMaterial.HasProperty("_BaseColor")) _glowMaterial.SetColor("_BaseColor", Color.white);
        _glowMaterial.color = Color.white;

        BuildGroundGlow();
        BuildFireMesh();
        BuildEmbers();
        BuildLight();
    }

    /// <summary>
    /// Đĩa sáng loang trên mặt đất dưới chân lửa.
    ///
    /// Dựng bằng quạt tam giác: một đỉnh ở tâm, còn lại rải quanh vành. Màu đỉnh tâm đục,
    /// đỉnh vành trong suốt - ra một vệt sáng toả đều, không cần texture riêng.
    ///
    /// Hình học ĐỨNG YÊN, chỉ màu đổi. Nên không phải tính lại đỉnh mỗi khung hình như
    /// biển lửa - chỉ ghi lại mảng màu.
    /// </summary>
    private void BuildGroundGlow()
    {
        if (groundGlowAlpha <= 0f) return;

        const int rim = 48;

        GameObject go = new GameObject("ZoneGroundGlow");
        go.transform.SetParent(transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _glowMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        Vector3[] verts = new Vector3[rim + 1];
        Vector2[] uvs = new Vector2[rim + 1];
        _glowColors = new Color[rim + 1];
        int[] tris = new int[rim * 3];

        float r = radius * groundGlowScale;

        // Nhấc lên 5cm khỏi mặt đất.
        //
        // Nằm đúng y=0 thì đĩa và mặt đất tranh nhau cùng một độ sâu, và card đồ hoạ sẽ
        // vẽ loang lổ chỗ này chỗ kia - hiện tượng z-fighting. 5cm là đủ để tách hẳn mà
        // mắt không nhận ra nó lơ lửng.
        verts[0] = Vector3.up * 0.05f;

        // Tâm lấy giữa ảnh (nơi đặc nhất), vành lấy góc dưới ảnh (nơi loãng).
        uvs[0] = new Vector2(0.5f, 0f);

        for (int i = 0; i < rim; i++)
        {
            float a = i / (float)rim * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
            uvs[i + 1] = new Vector2(0.5f, 0.95f);

            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i + 1) % rim + 1;
        }

        _glowMesh = new Mesh { name = "ZoneGroundGlow (runtime)" };
        _glowMesh.MarkDynamic();
        _glowMesh.vertices = verts;
        _glowMesh.triangles = tris;
        _glowMesh.uv = uvs;
        _glowMesh.bounds = new Bounds(Vector3.zero, new Vector3(r * 2.2f, 1f, r * 2.2f));

        mf.sharedMesh = _glowMesh;
    }

    /// <summary>
    /// Các đốm tàn lửa bay lên rồi tắt.
    ///
    /// Mỗi đốm là một hình vuông nhỏ quay mặt về camera. Gộp hết vào một mesh, cùng lý do
    /// với biển lửa: 70 object riêng là 70 lượt vẽ.
    /// </summary>
    private void BuildEmbers()
    {
        if (emberCount <= 0) return;

        GameObject go = new GameObject("ZoneEmbers");
        go.transform.SetParent(transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _glowMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        int n = emberCount;

        _emberVerts = new Vector3[n * 4];
        _emberColors = new Color[n * 4];
        _emberBase = new Vector3[n];
        _emberPhase = new float[n];
        _emberDrift = new float[n];
        _emberSizeVar = new float[n];

        Vector2[] uvs = new Vector2[n * 4];
        int[] tris = new int[n * 6];

        for (int i = 0; i < n; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Mathf.Sqrt(Random.value) * radius;

            _emberBase[i] = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            _emberPhase[i] = Random.value;
            _emberDrift[i] = Random.Range(-1f, 1f);
            _emberSizeVar[i] = Random.Range(0.6f, 1.5f);

            int v = i * 4;

            // Lấy vùng ĐẶC NHẤT của ảnh lửa (giữa-dưới) cho cả bốn góc, để đốm ra một
            // chấm sáng tròn đều thay vì mang hình lưỡi lửa thu nhỏ.
            uvs[v + 0] = new Vector2(0.35f, 0.1f);
            uvs[v + 1] = new Vector2(0.65f, 0.1f);
            uvs[v + 2] = new Vector2(0.35f, 0.35f);
            uvs[v + 3] = new Vector2(0.65f, 0.35f);

            tris[i * 6 + 0] = v + 0; tris[i * 6 + 1] = v + 2; tris[i * 6 + 2] = v + 1;
            tris[i * 6 + 3] = v + 1; tris[i * 6 + 4] = v + 2; tris[i * 6 + 5] = v + 3;
        }

        _emberMesh = new Mesh { name = "ZoneEmbers (runtime)" };
        _emberMesh.MarkDynamic();
        _emberMesh.vertices = _emberVerts;
        _emberMesh.triangles = tris;
        _emberMesh.uv = uvs;
        _emberMesh.bounds = new Bounds(Vector3.up * emberRiseHeight * 0.5f,
                                       new Vector3(radius * 2.5f, emberRiseHeight * 2.5f, radius * 2.5f));

        mf.sharedMesh = _emberMesh;
    }

    /// <summary>
    /// Vẽ ra ảnh một lưỡi lửa mềm, ngay trong bộ nhớ. Không cần file ảnh nào.
    ///
    /// Hình dáng: sáng nhất ở GIỮA-DƯỚI, nhoè dần ra hai bên và lên trên.
    ///
    /// Hai tính chất phải có, thiếu cái nào cũng lộ ra hình tam giác:
    ///   - Mép NGANG mềm: nếu cắt phẳng ở hai bên thì thấy ngay cạnh thẳng của tam giác
    ///   - Mép TRÊN tan dần: lửa phải tan vào không khí, không được cắt ngang
    /// </summary>
    private Texture2D CreateFlameTexture()
    {
        const int size = 64;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ZoneFlameTex (runtime)",

            // Clamp chứ không Repeat: Repeat thì mép trái nối vào mép phải và sinh ra một
            // đường sáng dọc ở rìa mỗi lưỡi.
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);   // 0 = chân, 1 = ngọn

            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);

                // Khoảng cách tới trục giữa, quy về 0..1.
                float distFromCenter = Mathf.Abs(u - 0.5f) * 2f;

                // Lưỡi lửa THON DẦN lên trên: càng cao thì phần sáng càng hẹp.
                // Không thu hẹp thì ra hình chữ nhật nhoè, không ra lưỡi.
                float widthAtHeight = Mathf.Lerp(1f, 0.45f, v);
                float horizontal = 1f - Mathf.Clamp01(distFromCenter / widthAtHeight);

                // Luỹ thừa 1.6: chuyển từ sáng sang trong nhanh hơn tuyến tính, cho mép
                // gọn mà vẫn mềm. Tuyến tính thì nhoè lều bều như sương.
                horizontal = Mathf.Pow(horizontal, 1.1f);

                // Theo chiều dọc: đậm ở gốc, tan dần lên ngọn.
                float vertical = Mathf.Pow(1f - v, 0.7f);

                float a = horizontal * vertical;

                // Màu trắng, chỉ dùng kênh alpha. Màu thật do MÀU ĐỈNH quyết định, nên
                // cùng một ảnh này dùng được cho cả đỏ, xanh lẫn trắng.
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return tex;
    }

    /// <summary>
    /// Rải lưỡi lửa khắp mặt sàn khu và gộp tất cả vào MỘT mesh.
    ///
    /// Mỗi lưỡi là một TAM GIÁC: hai đỉnh dưới ghim mặt đất, một đỉnh trên là ngọn lửa.
    /// Tam giác rẻ hơn hình chữ nhật (1 mặt thay vì 2) và hình dáng thon nhọn của nó
    /// vốn đã giống ngọn lửa sẵn, không phải gọt gì thêm.
    ///
    /// Gộp hết vào một mesh chứ không tạo 220 object: 220 object là 220 lượt vẽ, đủ để
    /// gấp đôi số Batches của cả scene.
    /// </summary>
    private void BuildFireMesh()
    {
        GameObject go = new GameObject("ZoneFire");
        go.transform.SetParent(transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        mr.sharedMaterial = _glowMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Vật tự phát sáng: không nhận ánh sáng dò và phản chiếu, nếu không màu sẽ bị ám
        // theo cảnh và không còn nhạt như đã cân.
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        int n = tongueCount;

        _verts = new Vector3[n * 3];
        _colors = new Color[n * 3];
        _tongueBase = new Vector3[n];
        _tongueWidthArr = new float[n];
        _tongueHeightArr = new float[n];
        _tonguePhase = new float[n];

        int[] tris = new int[n * 3];

        // UV — BẮT BUỘC PHẢI CÓ khi đã dùng ảnh.
        //
        // Mesh không có UV thì mọi đỉnh đọc ảnh ở toạ độ (0,0) - tức là góc dưới trái,
        // nơi ảnh lửa gần như trong suốt hoàn toàn. Kết quả: cả biển lửa vô hình.
        // Đây đúng là loại lỗi im lặng đã làm mất mấy vòng thử vừa rồi.
        //
        // Đặt MỘT LẦN vì UV không đổi: chỉ vị trí đỉnh mới đổi mỗi khung hình.
        Vector2[] uvs = new Vector2[n * 3];

        for (int i = 0; i < n; i++)
        {
            // RẢI ĐỀU TRONG HÌNH TRÒN, không phải rải đều theo bán kính.
            //
            // Lấy thẳng Random(0, radius) sẽ dồn cục ở giữa, vì vòng ngoài có diện tích
            // lớn hơn nhiều mà lại nhận cùng số điểm. Căn bậc hai sửa đúng độ lệch đó.
            float angle = Random.value * Mathf.PI * 2f;
            float dist = Mathf.Sqrt(Random.value) * radius;

            _tongueBase[i] = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            // Lưỡi sát viền cao hơn -> vành lửa ngoài tự nổi lên, không phải vẽ vòng riêng.
            float edge = dist / Mathf.Max(radius, 0.01f);
            float edgeFactor = Mathf.Lerp(1f, edgeHeightBoost, edge * edge);

            _tongueWidthArr[i] = tongueWidth * Random.Range(0.6f, 1.4f);
            _tongueHeightArr[i] = Random.Range(0.55f, 1.45f) * edgeFactor;
            _tonguePhase[i] = Random.value * 100f;

            tris[i * 3 + 0] = i * 3 + 0;
            tris[i * 3 + 1] = i * 3 + 1;
            tris[i * 3 + 2] = i * 3 + 2;

            // Hai đỉnh dưới lấy hai góc đáy của ảnh, đỉnh trên lấy chính giữa mép trên.
            uvs[i * 3 + 0] = new Vector2(0f, 0f);
            uvs[i * 3 + 1] = new Vector2(1f, 0f);
            uvs[i * 3 + 2] = new Vector2(0.5f, 1f);
        }

        _fireMesh = new Mesh { name = "ZoneFire (runtime)" };
        _fireMesh.MarkDynamic(); // báo cho Unity biết đỉnh sẽ đổi mỗi khung hình

        // Đặt đỉnh tạm để mesh hợp lệ trước khi gán tam giác.
        _fireMesh.vertices = _verts;
        _fireMesh.triangles = tris;
        _fireMesh.uv = uvs;

        // Đặt hộp bao BẰNG TAY.
        //
        // Cần vì đỉnh đổi mỗi khung hình: để Unity tự tính lại là tốn công vô ích, mà
        // hộp bao sai thì Unity CULL MẤT cả biển lửa khi camera nhìn nghiêng - lửa biến
        // mất không rõ lý do, rất khó truy.
        _fireMesh.bounds = new Bounds(Vector3.up * flameHeightAtFull,
                                      new Vector3(radius * 2.2f, flameHeightAtFull * 4f, radius * 2.2f));

        mf.sharedMesh = _fireMesh;

        // CHẨN ĐOÁN — in một lần lúc dựng xong.
        //
        // Thêm vì đã ba lần sửa mà vẫn "không thấy gì", mỗi lần đoán một nguyên nhân khác
        // nhau. Dòng này trả lời dứt điểm: mesh có được dựng không, dựng ở đâu, bằng shader
        // nào. Xoá đi khi hiệu ứng đã chạy ổn.
        Debug.Log($"<color=cyan>[ControlZone] Đã dựng biển lửa</color>\n" +
                  $"  shader   = {_glowMaterial.shader.name}\n" +
                  $"  lưỡi     = {n} (đỉnh {_verts.Length}, tam giác {n})\n" +
                  $"  vị trí   = {go.transform.position} (bán kính {radius})\n" +
                  $"  camera   = {(Camera.main != null ? Camera.main.name : "CHƯA CÓ")}", this);
    }

    private void BuildLight()
    {
        if (lightIntensity <= 0f) return;
        if (zoneLight != null) return; // đã gán tay thì tôn trọng

        GameObject lightObj = new GameObject("ZoneLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.up * 1.2f;

        zoneLight = lightObj.AddComponent<Light>();
        zoneLight.type = LightType.Point;
        zoneLight.range = lightRange;
        zoneLight.intensity = lightIntensity;

        // KHÔNG cho đèn này đổ bóng.
        //
        // Đèn phụ có bóng là thứ đắt nhất trong toàn bộ phần đồ hoạ, và ta vừa mất cả
        // buổi để kéo FPS từ 20 lên 56. Một đèn không bóng gần như miễn phí; cũng đèn đó
        // mà bật bóng thì phải vẽ lại toàn bộ hình học trong tầm chiếu.
        zoneLight.shadows = LightShadows.None;
    }

    // ==================== HOẠT CẢNH ====================

    private void Update()
    {
        if (_fireMesh == null) return;

        UpdateColor();
        UpdateFire();
    }

    private void UpdateColor()
    {
        CountPlayersInside(out int redInside, out int blueInside);

        // Đọc theo NGƯỜI ĐANG ĐỨNG chứ không theo tiến độ. Người chơi cần phản hồi TỨC THÌ
        // cho hành động vừa làm ("tôi vừa bước vào, lửa đổi màu"). Đổi màu theo tiến độ sẽ
        // trễ vài giây và không ai nối được nhân quả.
        Color target;

        if (redInside > 0 && blueInside > 0) target = contestedColor;
        else if (redInside > 0) target = redTeamColor;
        else if (blueInside > 0) target = blueTeamColor;
        else target = neutralColor;

        if (!_hasColor)
        {
            _currentColor = target;
            _hasColor = true;
        }
        else
        {
            _currentColor = Color.Lerp(_currentColor, target, colorBlendSpeed * Time.deltaTime);
        }

        if (zoneLight != null)
        {
            zoneLight.color = _currentColor;
            zoneLight.intensity = lightIntensity;
        }
    }

    /// <summary>
    /// Tính lại toàn bộ đỉnh của biển lửa.
    ///
    /// BA THỨ TẠO RA CẢM GIÁC LỬA:
    ///   1. Mỗi lưỡi có PHA RIÊNG -> chúng nhấp nhô lệch nhau. Cùng pha thì cả biển lửa
    ///      phập phồng đồng loạt như một tấm bạt, không ai đọc ra là lửa.
    ///   2. Ngọn NGẢ NGHIÊNG theo nhiễu -> lưỡi đứng thẳng đơ trông như hàng rào cọc nhọn.
    ///   3. Đỉnh trên ALPHA 0 -> lửa tan vào không khí thay vì kết thúc bằng đường cắt phẳng.
    /// </summary>
    private void UpdateFire()
    {
        // Lưỡi lửa quay mặt về camera, nếu không nhìn nghiêng sẽ thấy chúng mỏng dính.
        //
        // ⚠️ KHÔNG return khi thiếu camera - dùng hướng dự phòng.
        //
        // Bản trước thoát hàm luôn nếu Camera.main null, mà thoát nghĩa là đỉnh mesh nằm
        // nguyên ở (0,0,0): cả 300 tam giác co về một điểm, KHÔNG VẼ RA GÌ CẢ. Một thứ
        // chỉ dùng cho hiệu ứng quay mặt lại có thể làm biến mất toàn bộ ngọn lửa - đó là
        // phụ thuộc sai chỗ. Thà lửa quay hơi lệch còn hơn không có lửa.
        if (_cam == null || !_cam.isActiveAndEnabled) _cam = Camera.main;

        Vector3 right;

        if (_cam != null)
        {
            // Đổi sang hệ toạ độ cục bộ, vì đỉnh mesh là toạ độ cục bộ.
            right = transform.InverseTransformDirection(_cam.transform.right);
            right.y = 0f;
        }
        else
        {
            right = Vector3.right;
        }

        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();

        float ratio = GetLeadingProgressRatio();
        float baseHeight = Mathf.Lerp(flameHeight, flameHeightAtFull, ratio);

        float t = Time.time * flameSpeed;

        Color bottom = _currentColor;
        bottom.a = flameAlpha;

        Color top = _currentColor;
        top.a = 0f;

        // Tính màu lõi nóng MỘT LẦN cho cả 300 lưỡi, không tính lại trong vòng lặp.
        //
        // Chuẩn hoá màu đội về sắc thuần (chia cho kênh sáng nhất) rồi nhân vào lõi nóng.
        // Nhờ vậy lõi giữ được SẮC của đội trong khi vẫn sáng chói như lửa nóng thật.
        Color tint = _currentColor;
        float maxChannel = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b));

        if (maxChannel > 0.001f)
        {
            tint.r /= maxChannel;
            tint.g /= maxChannel;
            tint.b /= maxChannel;
        }

        // Lerp về 1: kéo sắc thuần lại gần trắng một nửa, để lõi vẫn ra "nóng" chứ không
        // thành ra chỉ là màu đội sáng hơn.
        Color hotTinted = new Color(hotCoreColor.r * Mathf.Lerp(1f, tint.r, 0.55f),
                                    hotCoreColor.g * Mathf.Lerp(1f, tint.g, 0.55f),
                                    hotCoreColor.b * Mathf.Lerp(1f, tint.b, 0.55f));

        _hotColorCached = Color.Lerp(bottom, hotTinted, hotCoreAmount);

        float now = Time.time;

        for (int i = 0; i < tongueCount; i++)
        {
            float phase = _tonguePhase[i];

            // Nhiễu Perlin chứ không phải Random: Random cho ra lửa run bần bật như nhiễu
            // tín hiệu, Perlin mới ra được kiểu bốc lên hạ xuống liền mạch của lửa thật.
            float n1 = Mathf.PerlinNoise(phase, t);
            float n2 = Mathf.PerlinNoise(phase + 5.7f, t * 0.8f);

            // --- VÒNG ĐỜI: SINH -> VƯƠN LÊN -> TAN ---
            //
            // life chạy 0 -> 1 rồi quay lại 0. Mỗi lưỡi lệch pha riêng nên cả biển lửa
            // không bao giờ đồng loạt sinh hay đồng loạt tắt.
            float life = 1f;
            float lifeFade = 1f;

            if (risingFlames && flameLifetime > 0.01f)
            {
                life = Mathf.Repeat(now / flameLifetime + phase, 1f);

                // Vươn lên NHANH ở đầu rồi chậm dần: căn bậc hai cho ra đúng nhịp đó.
                // Tuyến tính thì lưỡi trồi lên đều đều như thang máy.
                float rise = Mathf.Sqrt(life);

                // Mờ dần ở NỬA SAU vòng đời. Mờ ngay từ đầu thì lưỡi chưa kịp thấy đã tan.
                lifeFade = 1f - Mathf.Clamp01((life - 0.45f) / 0.55f);
                lifeFade *= lifeFade; // bình phương: tan nhanh ở cuối, tự nhiên hơn

                life = rise;
            }

            // Không để tụt hẳn về 0: lưỡi co về một điểm rồi bung ra trông như nhấp nháy lỗi.
            float h = baseHeight * _tongueHeightArr[i] * (0.45f + 0.55f * n1) * life;

            // Lưỡi HẸP DẦN khi vươn cao, giống lửa thật thu lại ở ngọn.
            float halfW = _tongueWidthArr[i] * 0.5f * Mathf.Lerp(1f, 0.78f, life);

            Vector3 b = _tongueBase[i];

            // Ngọn ngả theo hướng ngang MÀN HÌNH, để độ ngả luôn nhìn thấy được dù đứng
            // ở góc nào. Ngả theo trục thế giới cố định thì có hướng nhìn sẽ thấy lửa
            // đứng thẳng đơ.
            //
            // Nhân thêm life: lưỡi càng cao càng ngả nhiều, vì phần ngọn nhẹ hơn và bị
            // không khí cuốn. Ngả đều từ gốc lên thì thành cả lưỡi bị nghiêng như cái cột.
            // CHỤM VÀO TÂM khi vươn lên.
            //
            // Quy luật của mọi đám cháy thật: khí nóng bốc lên kéo không khí lạnh từ xung
            // quanh vào, nên cả khối lửa thu hẹp dần lên trên thành hình nón. Không có nó
            // thì hàng trăm lưỡi mọc thẳng song song, nhìn ra cánh đồng cỏ phát sáng chứ
            // không ra một đám cháy.
            Vector3 inward = -b.normalized * (h * convergeToCenter);
            if (b.sqrMagnitude < 0.01f) inward = Vector3.zero; // lưỡi ngay giữa thì không chụm đi đâu

            Vector3 tip = b + Vector3.up * h + inward
                          + right * ((n2 - 0.5f) * 2f * flameSway * life);

            int v = i * 3;
            _verts[v + 0] = b - right * halfW;
            _verts[v + 1] = b + right * halfW;
            _verts[v + 2] = tip;

            // --- MÀU: LÕI NÓNG Ở GỐC, MÀU ĐỘI Ở NGỌN ---
            //
            // Lửa thật nóng nhất ở gốc nên chỗ đó ngả sáng. Một màu suốt từ gốc tới ngọn
            // là dấu hiệu rõ nhất của "hình vẽ" chứ không phải lửa.
            //
            // ⚠️ LÕI NÓNG PHẢI GIỮ SẮC CỦA ĐỘI - sửa 01/09.
            //
            // Bản trước trộn thẳng sang màu trắng, mà chân lửa lại là phần SÁNG VÀ ĐỤC
            // NHẤT. Kết quả: đội nào đứng trong khu thì lửa cũng trắng như nhau, màu đội
            // bị nuốt gần hết - đúng lỗi "không thấy lửa đổi màu".
            //
            // Cách chữa: lấy lõi nóng nhân với SẮC THUẦN của màu đội (đã chuẩn hoá về
            // cùng độ sáng). Lõi vẫn sáng chói như lửa nóng, nhưng là trắng-ngả-đỏ hoặc
            // trắng-ngả-xanh chứ không phải trắng vô hồn.
            _colors[v + 0] = _hotColorCached;
            _colors[v + 1] = _hotColorCached;

            Color baseColor = _colors[v + 0];
            baseColor.a = bottom.a * lifeFade;

            _colors[v + 0] = baseColor;
            _colors[v + 1] = baseColor;
            _colors[v + 2] = top;   // ngọn: màu đội thuần, alpha 0
        }

        _fireMesh.vertices = _verts;
        _fireMesh.colors = _colors;

        UpdateGroundGlow();
        UpdateEmbers(right, now);
    }

    private void UpdateGroundGlow()
    {
        if (_glowMesh == null || _glowColors == null) return;

        // Quầng sáng THỞ CHẬM hơn ngọn lửa nhiều.
        //
        // Ánh sáng hắt lên mặt đất mà nhấp nháy theo nhịp lửa thì cả khu chớp tắt, rất
        // khó chịu lúc đánh nhau. Chậm và nhẹ thì nó chỉ là cái nền ấm áp.
        float breathe = 1f + Mathf.Sin(Time.time * 0.8f) * 0.12f;

        Color center = _hotColorCached;
        center.a = groundGlowAlpha * breathe;

        Color rim = _currentColor;
        rim.a = 0f;

        _glowColors[0] = center;
        for (int i = 1; i < _glowColors.Length; i++) _glowColors[i] = rim;

        _glowMesh.colors = _glowColors;
    }

    private void UpdateEmbers(Vector3 right, float now)
    {
        if (_emberMesh == null) return;

        // Hướng "lên" của màn hình, để đốm luôn là hình vuông quay mặt về camera.
        // Dùng tích có hướng thay vì Vector3.up: nhìn từ trên xuống mà lấy Vector3.up
        // thì đốm bẹp thành một đường.
        Vector3 up = _cam != null
            ? transform.InverseTransformDirection(_cam.transform.up)
            : Vector3.up;

        for (int i = 0; i < emberCount; i++)
        {
            // Vòng đời riêng của từng đốm, lệch pha nên chúng không bay thành từng đợt.
            float life = Mathf.Repeat(now / Mathf.Max(emberLifetime, 0.1f) + _emberPhase[i], 1f);

            Vector3 b = _emberBase[i];

            // Bay lên CHẬM DẦN: khí nóng mất đà khi xa nguồn. Luỹ thừa 0.75 cho ra đúng
            // nhịp đó, tuyến tính thì đốm bay đều như tên lửa.
            float rise = Mathf.Pow(life, 0.75f) * emberRiseHeight;

            // Trôi ngang tăng dần và có lắc: đốm càng lên cao càng bị gió cuốn đi xa.
            float drift = _emberDrift[i] * life * 1.2f;
            float wobble = Mathf.Sin(now * 2.2f + _emberPhase[i] * 20f) * 0.25f * life;

            Vector3 pos = b + Vector3.up * rise
                          + new Vector3(drift + wobble, 0f, drift * 0.7f - wobble * 0.6f);

            // Sáng lên nhanh rồi tắt dần. Không có đoạn sáng lên thì đốm bật ra đột ngột
            // ngay giữa ngọn lửa, nhìn như lỗi nhấp nháy.
            float fadeIn = Mathf.Clamp01(life / 0.12f);
            float fadeOut = 1f - Mathf.Clamp01((life - 0.35f) / 0.65f);
            float alpha = fadeIn * fadeOut * fadeOut;

            // Đốm NHỎ DẦN khi nguội - tàn lửa tan ra chứ không biến mất nguyên cỡ.
            float s = emberSize * _emberSizeVar[i] * Mathf.Lerp(1f, 0.35f, life);

            Vector3 r2 = right * s;
            Vector3 u2 = up * s;

            int v = i * 4;
            _emberVerts[v + 0] = pos - r2 - u2;
            _emberVerts[v + 1] = pos + r2 - u2;
            _emberVerts[v + 2] = pos - r2 + u2;
            _emberVerts[v + 3] = pos + r2 + u2;

            // Đốm mới thì nóng trắng, đốm già nguội về màu đội rồi tắt.
            Color c = Color.Lerp(_hotColorCached, _currentColor, life);
            c.a = alpha;

            _emberColors[v + 0] = c;
            _emberColors[v + 1] = c;
            _emberColors[v + 2] = c;
            _emberColors[v + 3] = c;
        }

        _emberMesh.vertices = _emberVerts;
        _emberMesh.colors = _emberColors;
    }

    /// <summary>Tiến độ của đội đang DẪN, quy về 0..1. Trả 0 nếu chưa vào trận.</summary>
    private float GetLeadingProgressRatio()
    {
        GameManager gm = GameManager.Instance;

        // gm.Object.IsValid: Host thoát thì object còn nằm đó thêm vài khung hình nhưng
        // mạng đã chết - đọc vào sẽ ném lỗi mỗi khung. Cùng cái bẫy đã sửa ở HUDController.
        if (gm == null || gm.Object == null || !gm.Object.IsValid) return 0f;
        if (gm.zoneProgressToWin <= 0f) return 0f;

        float red = gm.RedZoneProgress / gm.zoneProgressToWin;
        float blue = gm.BlueZoneProgress / gm.zoneProgressToWin;

        return Mathf.Clamp01(Mathf.Max(red, blue));
    }

    // ==================== LUẬT CHƠI ====================

    /// <summary>
    /// Người này có đang đứng trong khu không.
    ///
    /// Dùng hình TRỤ chứ không phải hình cầu: bán kính xét theo phương ngang, chiều cao
    /// xét riêng. Hình cầu sẽ khiến người đứng sát mép mà thấp hơn một chút bị loại oan,
    /// còn người nhảy cao ngay giữa tâm lại vẫn được tính.
    /// </summary>
    public bool Contains(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - transform.position;

        if (Mathf.Abs(offset.y) > halfHeight) return false;

        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius;
    }

    /// <summary>
    /// Đếm số người CÒN SỐNG của mỗi đội đang đứng trong khu.
    /// Người đã bị loại không giữ được khu - nếu không thì cái xác sẽ chiếm hộ.
    /// </summary>
    public void CountPlayersInside(out int redCount, out int blueCount)
    {
        redCount = 0;
        blueCount = 0;

        foreach (PlayerHealth p in PlayerHealth.AllPlayers)
        {
            if (p == null || !p.IsAlive) continue;
            if (!Contains(p.transform.position)) continue;

            if (p.Team == 0) redCount++;
            else blueCount++;
        }
    }

    // ==================== GIZMO ====================

    // Vẽ vùng trong Scene view để căn vị trí cho dễ. Chỉ chạy trong Editor.
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        DrawCircle(transform.position + Vector3.up * halfHeight);
        DrawCircle(transform.position - Vector3.up * halfHeight);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 0.5f;
            Vector3 edge = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(transform.position + edge + Vector3.up * halfHeight,
                            transform.position + edge - Vector3.up * halfHeight);
        }
    }

    private void DrawCircle(Vector3 center)
    {
        const int segments = 32;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
