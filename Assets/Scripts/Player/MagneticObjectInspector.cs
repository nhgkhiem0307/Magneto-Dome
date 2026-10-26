using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Soi vật thể từ tính đang nằm dưới tâm ngắm: làm nó sáng lên và cho HUD biết
/// đó là loại gì, đang mang điện tích nào.
///
/// GẮN LÊN Player.prefab (cùng cấp với FPSMovement).
///
/// ĐÂY LÀ MONOBEHAVIOUR THUẦN, CỐ Ý KHÔNG NETWORKED.
///
/// Bạn đang ngắm vào cái gì là chuyện của riêng bạn - đối thủ không cần biết, và
/// cũng không NÊN biết (nói cho họ biết chẳng khác nào chỉ điểm bạn sắp làm gì).
/// Gửi qua mạng vừa phí băng thông vừa lộ thông tin. Cùng nguyên tắc với CameraShake
/// và với phần ghì tay góc nhìn thứ nhất.
///
/// Script này KHÔNG tự đụng vào viền hay màu vật thể. Nó chỉ báo cho MagneticAura
/// "tôi đang nhìn vào anh", còn dựng vẻ ngoài thế nào là việc của MagneticAura.
/// Vì sao phải vòng vo vậy: MagneticAura cũng bật/tắt viền theo điện tích. Nếu ở đây
/// cũng tự ghi vào Outline thì hai bên tranh nhau, và thôi nhìn vào một vật đang nhiễm
/// điện sẽ xoá luôn viền điện tích của nó.
/// </summary>
[DefaultExecutionOrder(5000)]   // LateUpdate chạy SAU khi camera đã xoay xong khung hình này - khung ngắm không trễ một nhịp
public class MagneticObjectInspector : MonoBehaviour
{
    [Header("Tầm soi")]
    [Tooltip("Nhìn xa bao nhiêu mét thì còn soi được, tính từ camera.\n\n" +
             "Cố ý NGẮN HƠN tầm bắn (shootRange 100m). Soi được tận 100m thì cả bản đồ " +
             "lúc nào cũng có một vật đang sáng rực ở đằng xa, vừa rối mắt vừa vô dụng - " +
             "thông tin chỉ hữu ích khi vật đủ gần để thật sự tương tác được.")]
    public float inspectRange = 35f;

    [Tooltip("Bao nhiêu giây bắn tia một lần. 0 = mỗi khung hình.\n\n" +
             "0.05 (20 lần/giây) là đủ nhạy để mắt không nhận ra độ trễ, mà rẻ hơn hẳn " +
             "so với bắn tia 144 lần mỗi giây trong một scene có 4000 vật thể.")]
    public float scanInterval = 0.05f;

    [Header("Bảng phân tích kiểu kính công nghệ (24/10)")]
    [Tooltip("BẬT: dùng bảng phân tích trên màn hình (4 góc khung ngắm + thẻ thông tin) - xem lớp ScanOverlay.\n\n" +
             "TẮT: quay về bảng chữ nổi trên đỉnh vật như cũ. Bảng cũ bị bỏ vì vật to đứng gần thì nó bay vọt lên " +
             "cao hoặc che kín màn hình.")]
    public bool useScanOverlay = true;

    [Header("Bảng chữ nổi trên vật (bản cũ, chỉ dùng khi tắt Use Scan Overlay)")]
    [Tooltip("Object cha của bảng chữ (Canvas World Space). Nên gắn thêm component " +
             "Billboard lên chính nó để bảng luôn quay mặt về phía bạn.\n\n" +
             "Đặt làm CON của Player.prefab. Mỗi người chơi có bảng riêng, và bảng chỉ tồn " +
             "tại trên máy của chủ nhân - đối thủ không thấy bạn đang soi vật nào.")]
    public Transform labelRoot;

    [Tooltip("Dòng trên: loại vật thể.")]
    public TMP_Text labelTypeText;

    [Tooltip("Dòng dưới: điện tích. Tự đổi màu theo cực.")]
    public TMP_Text labelChargeText;

    [Tooltip("Bảng chữ nổi cao hơn ĐỈNH vật bao nhiêu mét.")]
    public float labelHeightOffset = 0.35f;

    [Tooltip("Thời gian (giây) để bảng chữ trượt tới độ cao mới khi đỉnh vật thay đổi.\n\n" +
             "Vật đang bay thì lộn vòng, đỉnh của nó lên xuống liên tục. Không làm mượt thì " +
             "bảng chữ giật theo từng vòng lộn. 0 = bám cứng, không làm mượt.")]
    public float labelHeightSmoothTime = 0.12f;

    [Tooltip("(Tuỳ chọn) Nền phía sau bảng chữ. Nếu gán, nền sẽ ĐỔI MÀU theo loại vật.\n\n" +
             "Đây mới là thứ cho nhận diện tức thì: một mảng màu to đọc được bằng thị giác " +
             "ngoại vi, còn chữ thì bắt buộc phải nhìn thẳng vào mới đọc nổi.")]
    public UnityEngine.UI.Image labelBackground;

    [Tooltip("Độ trong suốt của nền. 0.55 đủ để thấy màu mà không che mất vật phía sau.")]
    [Range(0f, 1f)]
    public float labelBackgroundAlpha = 0.55f;

    // ==================== MÀU THEO LOẠI VẬT ====================
    //
    // ⚠️ BỐN MÀU NÀY PHẢI TRÁNH: đỏ, xanh dương, xanh lá, vàng.
    //
    // Đỏ và xanh dương đã là cực điện tích; xanh lá là đồng đội; vàng là aura nhiễm điện.
    // Dùng lại bất kỳ màu nào trong số đó thì người chơi nhìn thấy màu mà không biết nó
    // đang nói về loại vật hay về điện tích - đúng cái bẫy đã ghi ở CLAUDE.md mục "nhãn quan".
    //
    // Tím, hồng, cam là ba khoảng màu còn trống, và chúng cách xa nhau đủ để phân biệt
    // được ngay cả bằng thị giác ngoại vi.

    [Header("Màu theo LOẠI vật")]
    [Tooltip("Vật thường - xám trắng, cố ý NHẠT NHẤT.\n\n" +
             "Loại này chiếm đa số trên bản đồ. Cho nó màu rực rỡ thì cả map lúc nào cũng " +
             "loè loẹt, và ba loại nguy hiểm kia mất hết sức cảnh báo.")]
    public Color normalColor = new Color(0.85f, 0.85f, 0.9f);

    [Tooltip("Vật nặng - TÍM. Không đụng màu nào đang có nghĩa khác.")]
    public Color heavyColor = new Color(0.68f, 0.45f, 1f);

    [Tooltip("Bóng gai - HỒNG. Chói và gắt, hợp với thứ trừng phạt phản xạ hút sai.")]
    public Color spikeColor = new Color(1f, 0.35f, 0.78f);

    [Tooltip("Thùng nổ - CAM. Màu quy ước quốc tế cho chất nổ, ai cũng đọc được mà " +
             "không phải học.")]
    public Color tntColor = new Color(1f, 0.58f, 0.08f);

    /// <summary>Vật đang nằm dưới tâm ngắm, null nếu không nhìn vào vật nào.</summary>
    public MagneticObject Current { get; private set; }

    // Vật đang được làm sáng. Giữ tham chiếu riêng thay vì dùng lại Current, vì còn phải
    // tắt sáng cho nó ngay cả khi nó vừa bị huỷ hoặc vừa bị cất vào túi.
    private MagneticAura _highlighted;

    private Transform _camera;
    private FPSMovement _movement;
    private float _nextScanTime;

    // Phần thân (mesh) của vật đang soi, để đo đỉnh. Tìm một lần khi đổi mục tiêu.
    private Renderer _targetBody;

    // Đỉnh vật cao hơn tâm (pivot) bao nhiêu mét, đã làm mượt.
    private float _labelHeight;
    private ScanOverlay _scan;
    private Renderer[] _bodyParts;
    private float _groundY = float.MinValue;
    private PlayerHealth _health;
    private Camera _cameraComp;
    private Vector3 _hitPoint;
    private string _typeLabel, _chargeLabel;
    private Color _typeColor, _chargeColor;
    private int _statsDist = -1;
    private float _hitDist;
    private MagneticObject.Polarity _shownPolarity;
    private MagneticObject.ObjectType _shownType;
    private string _stats;
    private float _labelHeightVelocity;
    private bool _snapLabel;

    private void Awake()
    {
        _movement = GetComponent<FPSMovement>();

        // Ẩn ngay từ đầu. Không ẩn thì bảng chữ rỗng lơ lửng giữa map lúc vừa vào trận.
        if (labelRoot != null) labelRoot.gameObject.SetActive(false);
    }

    private void Update()
    {
        // CHỈ chạy trên nhân vật của người ngồi trước máy này.
        //
        // Không có dòng này thì trong phòng 4 người, máy nào cũng bắn 4 tia mỗi lần quét,
        // và nhân vật của người khác sẽ làm sáng vật thể trên màn hình của bạn theo hướng
        // họ đang nhìn - vừa tốn vừa loạn.
        if (FPSMovement.Local != _movement) return;

        if (Time.time < _nextScanTime) return;
        _nextScanTime = Time.time + Mathf.Max(0f, scanInterval);

        Scan();
    }

    private void Scan()
    {
        if (_camera == null)
        {
            if (_movement == null || _movement.cameraTransform == null) return;
            _camera = _movement.cameraTransform;
        }

        MagneticObject found = null;

        if (Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hit, inspectRange))
        {
            MagneticObject obj = hit.collider.GetComponent<MagneticObject>();

            // Bỏ qua vật đang nằm trong túi hoặc đã nổ - chúng vẫn còn tồn tại trong thế
            // giới (xem ghi chú InventorySystem ở CLAUDE.md) nhưng người chơi không nhìn
            // thấy, nên soi chúng là vô nghĩa.
            if (obj != null && !obj.IsStored && !obj.IsDestroyed) { found = obj; _hitPoint = hit.point; _hitDist = hit.distance; }
        }

        if (found == Current)
        {
            // Vẫn nhìn vật cũ nhưng nó vừa được nạp điện / đổi thành TNT → ghi lại chữ (24/10).
            // So sánh rẻ (hai số enum), chỉ ghi chữ khi thật sự đổi.
            if (Current != null && (Current.currentPolarity != _shownPolarity || Current.CurrentType != _shownType))
                RefreshLabelText();
            return;
        }

        // Đổi mục tiêu -> tắt sáng vật cũ trước, rồi mới bật vật mới.
        // Làm ngược thứ tự thì khi hai vật là một sẽ tắt mất cái vừa bật.
        if (_highlighted != null) _highlighted.SetHighlighted(false);
        _highlighted = null;

        Current = found;

        // Vật mới -> bảng chữ NHẢY thẳng tới đỉnh vật mới, không trượt từ vật cũ sang.
        _targetBody = Current != null ? FindBody(Current) : null;
        _bodyParts = Current != null ? FindBodyParts(Current) : null;
        _groundY = Current != null ? GroundUnder(Current) : float.MinValue;
        _snapLabel = true;

        if (Current != null)
        {
            _highlighted = Current.GetComponent<MagneticAura>();
            if (_highlighted != null) _highlighted.SetHighlighted(true);
        }

        RefreshLabelText();
    }

    /// <summary>
    /// Ghi lại chữ. Chỉ gọi khi ĐỔI mục tiêu, không gọi mỗi khung hình.
    ///
    /// Gán TMP_Text.text là thao tác đắt: nó dựng lại toàn bộ lưới chữ. Gán đúng chuỗi cũ
    /// vẫn tốn y như vậy vì TextMeshPro không so sánh trước. Nhìn chằm chằm vào một vật
    /// suốt 5 giây mà dựng lại chữ 300 lần thì phí hoàn toàn.
    /// </summary>
    private void RefreshLabelText()
    {
        if (Current == null) return;
        _shownPolarity = Current.currentPolarity;
        _shownType = Current.CurrentType;

        // Đọc một lần khi đổi mục tiêu cho bảng phân tích (thuộc tính mạng - không đọc mỗi khung hình)
        _typeLabel = TypeLabel;
        _chargeLabel = Current.currentPolarity == MagneticObject.Polarity.Positive ? "+  POSITIVE"
                     : Current.currentPolarity == MagneticObject.Polarity.Negative ? "-  NEGATIVE" : "NEUTRAL  ·  NO CHARGE";
        _typeColor = TypeColor;
        // Pha trắng 30%: màu cực nguyên chất (nhất là xanh âm) chìm trên nền trời sáng
        _chargeColor = Current.currentPolarity == MagneticObject.Polarity.None ? new Color(0.75f, 0.8f, 0.88f)
                     : Color.Lerp(ChargeColor, Color.white, 0.3f);
        _statsDist = -1;

        Color typeColor = TypeColor;

        if (labelTypeText != null)
        {
            labelTypeText.text = TypeLabel;
            labelTypeText.color = typeColor;
        }

        if (labelChargeText != null)
        {
            labelChargeText.text = ChargeLabel;
            labelChargeText.color = ChargeColor;
        }

        // Nền mang màu LOẠI VẬT, không mang màu điện tích.
        //
        // Vì sao chọn loại chứ không chọn điện tích: điện tích ĐÃ được báo bằng màu viền
        // bao quanh cả vật thể - một kênh to và rõ hơn hẳn. Cho nền mang màu điện tích nữa
        // là lặp lại thông tin cũ, trong khi loại vật thì chưa có kênh nào cả.
        if (labelBackground != null)
        {
            labelBackground.color = new Color(typeColor.r, typeColor.g, typeColor.b, labelBackgroundAlpha);
        }
    }

    /// <summary>
    /// Đặt bảng chữ lên trên đầu vật, mỗi khung hình.
    ///
    /// Phải ở LateUpdate chứ không phải Update: vật thể được vật lý và Fusion dịch chuyển
    /// trong khung hình, nếu đặt ở Update thì bảng bám theo vị trí CŨ và trôi lệch phía sau
    /// mỗi khi vật đang bay.
    ///
    /// Chạy mỗi khung hình dù tia chỉ bắn 20 lần/giây - hai việc khác nhau: tìm mục tiêu
    /// thì thưa được, còn bám theo mục tiêu đang bay thì không.
    /// </summary>
    private void LateUpdate()
    {
        if (useScanOverlay)
        {
            if (labelRoot != null && labelRoot.gameObject.activeSelf) labelRoot.gameObject.SetActive(false);
            TickScanOverlay();
            return;
        }
        if (labelRoot == null) return;

        bool show = Current != null && FPSMovement.Local == _movement;

        if (labelRoot.gameObject.activeSelf != show) labelRoot.gameObject.SetActive(show);
        if (!show) return;

        // ĐẶT THEO ĐỈNH THẬT CỦA VẬT, rồi làm mượt riêng phần độ cao.
        //
        // ⚠️ Trước đây dùng GetBoundingRadius() - nhưng đó là nửa ĐƯỜNG CHÉO hộp bao, không
        // phải nửa chiều cao. Cái bàn 3m x 3m x 0.1m có nửa đường chéo 2.1m trong khi chỉ
        // cao 5cm, nên bảng chữ bay lơ lửng hơn 2 mét trên mặt bàn. Vật tròn thì đường
        // chéo xấp xỉ chiều cao nên lại đúng - đó là lý do lúc đúng lúc sai.
        //
        // Ở đây dùng renderer.bounds là ĐÚNG, khác với bẫy số 9 ở CLAUDE.md: bẫy đó nói về
        // ĐO KÍCH THƯỚC (bounds phình khi vật xoay nên cỡ đo được sai). Còn ở đây ta cần
        // ĐỈNH theo trục thế giới - chính xác là thứ bounds cho ra. Phần phình co khi vật
        // lộn vòng thì do SmoothDamp bên dưới nuốt đi.
        float top = TopAbovePivot();

        if (_snapLabel || labelHeightSmoothTime <= 0f)
        {
            _labelHeight = top;
            _labelHeightVelocity = 0f;
            _snapLabel = false;
        }
        else
        {
            _labelHeight = Mathf.SmoothDamp(_labelHeight, top, ref _labelHeightVelocity,
                                            labelHeightSmoothTime);
        }

        // Chỉ làm mượt ĐỘ CAO, còn vị trí ngang vẫn bám cứng theo vật - không thì vật bay
        // nhanh sẽ bỏ bảng chữ lại phía sau.
        labelRoot.position = Current.transform.position + Vector3.up * (_labelHeight + labelHeightOffset);
    }

    /// <summary>Đỉnh vật cao hơn tâm (pivot) của nó bao nhiêu mét, theo trục thế giới.</summary>
    private float TopAbovePivot()
    {
        if (_targetBody == null) return Current.GetBoundingRadius();

        return _targetBody.bounds.max.y - Current.transform.position.y;
    }

    /// <summary>
    /// Tìm phần THÂN của vật - mesh thật, không phải hiệu ứng.
    ///
    /// Không dùng GetComponentInChildren&lt;Renderer&gt;() thẳng: MagneticObject tự gắn thêm
    /// TrailRenderer (vệt đạn, vệt gió) lúc vật bay, và hàm đó có thể trả về vệt đuôi thay
    /// vì thân vật. Hộp bao của vệt đuôi kéo dài theo cả đường bay, đỉnh của nó vô nghĩa.
    /// </summary>
    private static Renderer FindBody(MagneticObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer) return r;
        }
        return null;
    }

    /// <summary>Đẩy thông tin vật đang soi sang bảng phân tích (ScanOverlay), mỗi khung hình.</summary>
    private void TickScanOverlay()
    {
        if (FPSMovement.Local != _movement) return;

        if (_cameraComp == null && _camera != null) _cameraComp = _camera.GetComponent<Camera>();
        if (_cameraComp == null) _cameraComp = Camera.main;

        // Ẩn khi đang mở bảng (Shop, Settings...) - lúc đó chuột dùng để bấm nút, tâm ngắm cũng ẩn
        if (_health == null) _health = GetComponent<PlayerHealth>();
        bool alive = _health == null || !_health.NetworkReady || _health.IsAlive;
        bool show = alive && Current != null && !Current.IsStored && !Current.IsDestroyed && !NetworkRunnerHandler.IsCursorFree();

        if (_scan == null)
        {
            if (!show) return;
            _scan = ScanOverlay.Create(null);
        }

        if (!show) { _scan.Tick(null, _cameraComp, Time.unscaledDeltaTime); return; }

        Bounds b = _targetBody != null ? _targetBody.bounds : new Bounds(Current.transform.position, Vector3.one * Current.GetBoundingRadius() * 2f);

        // Dòng số liệu chỉ ghi lại khi khoảng cách đổi qua mốc 0,5m - gán chữ cho TextMeshPro là thao tác đắt
        int dist = _cameraComp != null ? Mathf.RoundToInt(Vector3.Distance(_cameraComp.transform.position, _hitPoint) * 2f) : 0;
        if (dist != _statsDist)
        {
            _statsDist = dist;
            float dmg = Current.CurrentType == MagneticObject.ObjectType.TNT ? Current.tntDamage : Current.baseDamage;
            _stats = $"CHARGE +{Mathf.RoundToInt(dmg)}   ·   {dist * 0.5f:0.0} m";
        }

        _scan.Tick(new ScanOverlay.Target
        {
            Id = Current.GetInstanceID(),
            Bounds = b,
            Parts = _bodyParts,
            GroundY = _groundY,
            // Điểm nhìn tính lại MỖI KHUNG HÌNH từ hướng camera - điểm trúng tia chỉ cập nhật 20 lần/giây,
            // dùng nó thì khung nhảy bậc khi xoay đầu
            HitPoint = _cameraComp != null ? _cameraComp.transform.position + _cameraComp.transform.forward * _hitDist : _hitPoint,
            Type = _typeLabel, TypeColor = _typeColor,
            Charge = _chargeLabel, ChargeColor = _chargeColor,
            Stats = _stats
        }, _cameraComp, Time.unscaledDeltaTime);
    }

    private void OnDestroy()
    {
        if (_scan != null) _scan.Destroy();
        _scan = null;
    }

    /// <summary>Mọi mảnh thân (mesh) của vật - bỏ qua vệt đuôi, hạt, viền. Đá có thể gồm nhiều mảnh.</summary>
    private static Renderer[] FindBodyParts(MagneticObject obj)
    {
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            if ((r is MeshRenderer || r is SkinnedMeshRenderer) && r.enabled) list.Add(r);
        return list.ToArray();
    }

    /// <summary>Độ cao mặt đất ngay dưới vật (bỏ qua chính vật). Vật đang bay cao thì trả về rất thấp = không cắt gì.</summary>
    private static float GroundUnder(MagneticObject obj)
    {
        Vector3 p = obj.transform.position;
        float best = float.MinValue;
        foreach (RaycastHit h in Physics.RaycastAll(p + Vector3.up * 6f, Vector3.down, 14f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(obj.transform) || h.collider.attachedRigidbody != null) continue;
            if (h.point.y > best) best = h.point.y;
        }
        return best;
    }

    private void OnDisable()
    {
        // Chết, sang round mới, hay thoát trận đều đi qua đây.
        // Không tắt thì vật cuối cùng bạn nhìn sẽ sáng rực mãi mãi, kể cả khi bạn đã
        // là cái xác nằm dưới vực.
        if (_highlighted != null) _highlighted.SetHighlighted(false);

        _highlighted = null;
        Current = null;

        if (labelRoot != null) labelRoot.gameObject.SetActive(false);

        if (_scan != null) _scan.Destroy();   // dựng lại khi cần - để không treo trên màn hình
        _scan = null;
    }

    // --- CHỮ HIỂN THỊ CHO HUD ---

    /// <summary>Tên loại vật thể bằng tiếng Việt, rỗng nếu không nhìn vào gì.</summary>
    public string TypeLabel
    {
        get
        {
            if (Current == null) return string.Empty;

            switch (Current.CurrentType)
            {
                case MagneticObject.ObjectType.Heavy: return "HEAVY";
                case MagneticObject.ObjectType.Spike: return "SPIKE";
                case MagneticObject.ObjectType.TNT: return "EXPLOSIVE";
                default: return "NORMAL";
            }
        }
    }

    /// <summary>Điện tích bằng tiếng Việt, rỗng nếu không nhìn vào gì.</summary>
    public string ChargeLabel
    {
        get
        {
            if (Current == null) return string.Empty;

            switch (Current.currentPolarity)
            {
                case MagneticObject.Polarity.Positive: return "POSITIVE (+)";
                case MagneticObject.Polarity.Negative: return "NEGATIVE (-)";
                default: return "UNCHARGED";
            }
        }
    }

    /// <summary>Màu tương ứng với LOẠI vật. Bốn màu tách bạch để nhận ra không cần đọc chữ.</summary>
    public Color TypeColor
    {
        get
        {
            if (Current == null) return Color.white;

            switch (Current.CurrentType)
            {
                case MagneticObject.ObjectType.Heavy: return heavyColor;
                case MagneticObject.ObjectType.Spike: return spikeColor;
                case MagneticObject.ObjectType.TNT: return tntColor;
                default: return normalColor;
            }
        }
    }

    /// <summary>Màu tương ứng với điện tích, tô chữ cho khớp với màu viền đang thấy trên vật.</summary>
    public Color ChargeColor
    {
        get
        {
            if (Current == null) return Color.white;

            switch (Current.currentPolarity)
            {
                case MagneticObject.Polarity.Positive: return new Color(1f, 0.25f, 0.25f);
                case MagneticObject.Polarity.Negative: return new Color(0.3f, 0.5f, 1f);
                default: return Color.white;
            }
        }
    }
}

/// <summary>
/// Bảng phân tích kiểu "kính công nghệ" (24/10): bốn góc khung ngắm ôm lấy vật + thẻ thông tin khung nhạt.
///
/// Lớp C# thường (không phải MonoBehaviour), tự dựng giao diện bằng code. MagneticObjectInspector gọi Tick()
/// mỗi khung hình với thông tin đã đọc sẵn - lớp này KHÔNG tự đọc thuộc tính mạng của vật nên dùng được cả
/// ngoài trận (để chụp thử).
///
/// ⚠️ VẬT TO / QUÁ GẦN: khung ngắm không ôm cả vật (sẽ phủ kín màn hình) mà co về một ô cố định quanh điểm
/// đang nhìn. Thẻ luôn bị kẹp trong vùng an toàn giữa màn hình, không đè lên HUD trên và dưới.
/// </summary>
public class ScanOverlay
{
    public struct Target
    {
        public int Id;
        public Bounds Bounds;              // hộp bao theo trục thế giới (dự phòng)
        public Renderer[] Parts;           // các mảnh thân vật: dùng hộp bao XOAY THEO từng mảnh - bám sát vật hơn
        public float GroundY;              // phần vật nằm dưới độ cao này (chìm trong đất) không tính vào khung
        public Vector3 HitPoint;
        public string Type, Charge, Stats;
        public Color TypeColor, ChargeColor;
    }

    private static readonly Color Cyan = new Color(0.36f, 0.85f, 1f);
    private static readonly Color Soft = new Color(0.86f, 0.9f, 0.96f);

    private const float CardW = 300f, CardH = 124f;
    private const float CardScale = 1.2f;   // phóng cả thẻ (chữ, viền) - kích thước trên màn hình = CardW/H × CardScale
    private const float SafeTop = 175f, SafeBottom = 235f, SafeSide = 40f;   // chừa chỗ HUD trên/dưới
    // Vật chiếm quá 60% bề ngang hoặc gần kín chiều cao vùng an toàn → co khung về ô cố định quanh tâm ngắm

    private RectTransform _canvasRect, _root, _card;
    private CanvasGroup _group;
    private Camera _uiCam;
    private readonly Image[] _bars = new Image[8];
    private readonly Image[] _barShadows = new Image[8];
    private Image _edge, _scanline, _dot, _tick;
    private TMP_Text _head, _type, _charge, _stats;
    private Rect _cur;
    private int _lastId = -1;
    private float _reveal, _lockT, _scanT, _flash;
    private Vector2 _cardPos;
    private bool _cardSnap = true;
    private string _lastStats;

    /// <param name="renderCam">null = phủ lên màn hình (trong game); có camera = vẽ vào camera đó (chụp thử).</param>
    public static ScanOverlay Create(Camera renderCam)
    {
        var s = new ScanOverlay();
        s.Build(renderCam);
        return s;
    }

    public GameObject Root => _canvasRect != null ? _canvasRect.gameObject : null;

    private void Build(Camera renderCam)
    {
        GameObject go = new GameObject("ScanOverlay", typeof(RectTransform));
        Canvas c = go.AddComponent<Canvas>();
        if (renderCam == null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = -6; }
        else { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = renderCam; c.planeDistance = 1f; }
        _uiCam = renderCam;
        var sc = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        _canvasRect = (RectTransform)go.transform;

        _root = Rt("Root", _canvasRect, Vector2.zero, Vector2.zero);
        _group = _root.gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;

        // Viền tối đặt TRƯỚC (vẽ dưới) góc khung màu: góc khung nổi rõ trên cả nền sáng lẫn nền tối
        for (int i = 0; i < 8; i++) _barShadows[i] = Img(_root, "BracketShadow", HUDSkin.WhiteSprite, new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one);
        for (int i = 0; i < 8; i++) _bars[i] = Img(_root, "Bracket", HUDSkin.WhiteSprite, Color.white, Vector2.zero, Vector2.one);

        // Thẻ thông tin: kính tối rất nhạt + viền mảnh + vạch góc
        _card = Rt("Card", _root, Vector2.zero, new Vector2(CardW, CardH));
        _card.pivot = new Vector2(0f, 1f);
        _card.localScale = Vector3.one * CardScale;
        Image fill = Img(_card, "Fill", HUDSkin.FillSprite, new Color(0.02f, 0.05f, 0.1f, 0.6f), new Vector2(CardW / 2f, -CardH / 2f), new Vector2(CardW, CardH));
        fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 2f;
        _edge = Img(_card, "Edge", HUDSkin.EdgeSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f), new Vector2(CardW / 2f, -CardH / 2f), new Vector2(CardW, CardH));
        _edge.type = Image.Type.Sliced; _edge.pixelsPerUnitMultiplier = 2f;
        _tick = Img(_card, "Tick", HUDSkin.WhiteSprite, Cyan, new Vector2(4f, -CardH / 2f), new Vector2(3f, CardH - 30f));

        TMP_FontAsset bold = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/Poppins-Bold SDF");
        TMP_FontAsset black = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/Poppins-Black SDF");
        TMP_FontAsset medium = Resources.Load<TMP_FontAsset>("UI/UI kit/Font/Poppins-Medium SDF");

        _head = Txt(_card, "Head", "SCAN // OBJECT", bold, 13f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.85f), new Vector2(18f, -10f), 4f);
        _dot = Img(_card, "Dot", HUDSkin.WhiteSprite, Cyan, new Vector2(CardW - 20f, -19f), new Vector2(7f, 7f));
        Img(_card, "Divider", HUDSkin.WhiteSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.22f), new Vector2(CardW / 2f + 6f, -34f), new Vector2(CardW - 38f, 1f));
        _type = Txt(_card, "Type", "", black, 26f, Color.white, new Vector2(18f, -38f), 2.5f);
        _charge = Txt(_card, "Charge", "", bold, 16f, Color.white, new Vector2(18f, -73f), 2f);
        _stats = Txt(_card, "Stats", "", medium, 14f, new Color(Soft.r, Soft.g, Soft.b, 0.78f), new Vector2(18f, -97f), 1f);

        _scanline = Img(_card, "Scan", HUDSkin.WhiteSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0f), new Vector2(CardW / 2f, 0f), new Vector2(CardW - 8f, 2f));
    }

    public void Destroy()
    {
        if (_canvasRect != null) Object.Destroy(_canvasRect.gameObject);
    }

    /// <summary>Gọi mỗi khung hình. target = null → mờ dần rồi ẩn.</summary>
    public void Tick(Target? target, Camera cam, float dt)
    {
        if (target == null || cam == null)
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, dt * 9f);
            if (_group.alpha <= 0f) _lastId = -1;
            return;
        }
        Target t = target.Value;
        Vector2 size = _canvasRect.rect.size;

        Rect box = ScreenBox(t, cam, size);
        bool fresh = t.Id != _lastId;
        if (fresh)
        {
            // Khoá mục tiêu mới: khung bắt đầu to hơn 35% rồi thu vào - như ống kính lấy nét
            _cur = new Rect(box.center - box.size * 0.675f, box.size * 1.35f);
            _lastId = t.Id;
            _reveal = 0f; _lockT = 0f; _scanT = 0f; _cardSnap = true;
            _type.text = t.Type; _type.color = t.TypeColor;
            _charge.text = t.Charge; _charge.color = t.ChargeColor;
            _stats.text = t.Stats; _lastStats = t.Stats;
            Color e = Color.Lerp(Cyan, t.TypeColor, 0.35f);
            _edge.color = new Color(e.r, e.g, e.b, 0.5f);
            _tick.color = t.TypeColor;
        }
        else
        {
            if (t.Stats != _lastStats) { _stats.text = t.Stats; _lastStats = t.Stats; }
            // Vật đổi điện tích / đổi loại ngay lúc đang nhìn: ghi lại dòng đó + loé thẻ một nhịp
            if (t.Charge != _charge.text || t.Type != _type.text)
            {
                _type.text = t.Type; _type.color = t.TypeColor;
                _charge.text = t.Charge; _charge.color = t.ChargeColor;
                _tick.color = t.TypeColor;
                _flash = 1f;
            }
        }

        // Làm mượt: vị trí bám nhanh (khung không tuột khỏi vật khi xoay đầu), cỡ đổi êm hơn
        float kp = 1f - Mathf.Exp(-dt * 24f), ks = 1f - Mathf.Exp(-dt * 13f);
        Vector2 c = Vector2.Lerp(_cur.center, box.center, kp);
        Vector2 sz = Vector2.Lerp(_cur.size, box.size, ks);
        _cur = new Rect(c - sz / 2f, sz);

        _flash = Mathf.MoveTowards(_flash, 0f, dt * 3f);
        float fl = _flash * _flash;
        Color ec = Color.Lerp(Cyan, t.TypeColor, 0.35f);
        _edge.color = Color.Lerp(new Color(ec.r, ec.g, ec.b, 0.5f), new Color(1f, 1f, 1f, 0.95f), fl);
        _charge.rectTransform.localScale = Vector3.one * (1f + 0.12f * fl);
        _lockT += dt;
        _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, dt * 10f);

        PlaceBrackets(_cur, t.TypeColor);
        PlaceCard(_cur, size);

        // Chữ hiện dần như máy đang gõ
        _reveal += dt * 70f;
        int n = Mathf.FloorToInt(_reveal);
        _head.maxVisibleCharacters = n;
        _type.maxVisibleCharacters = Mathf.Max(0, n - 6);
        _charge.maxVisibleCharacters = Mathf.Max(0, n - 12);
        _stats.maxVisibleCharacters = Mathf.Max(0, n - 18);

        // Chấm nháy + vạch quét chạy dọc thẻ lúc khoá mục tiêu, sau đó cứ 2,6 giây quét nhẹ một lần
        _dot.color = new Color(Cyan.r, Cyan.g, Cyan.b, Mathf.Repeat(_lockT, 0.8f) < 0.45f ? 1f : 0.2f);
        _scanT += dt;
        float period = _lockT < 0.5f ? 0.45f : 2.6f;
        float p = Mathf.Repeat(_scanT, period) / 0.45f;
        float a = p <= 1f ? (_lockT < 0.5f ? 0.85f : 0.3f) * (1f - p * p) : 0f;
        _scanline.color = new Color(Cyan.r, Cyan.g, Cyan.b, a);
        _scanline.rectTransform.anchoredPosition = new Vector2(CardW / 2f, -Mathf.Clamp01(p) * (CardH - 4f) - 2f);
    }

    /// <summary>Khung bao của vật trên màn hình (toạ độ canvas, gốc ở giữa). Vật to/gần → ô cố định quanh điểm nhìn.</summary>
    private Rect ScreenBox(Target t, Camera cam, Vector2 size)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        bool behind = false;
        int partCount = t.Parts != null ? t.Parts.Length : 0;
        for (int p = 0; p < Mathf.Max(1, partCount) && !behind; p++)
        {
            Renderer part = partCount > 0 ? t.Parts[p] : null;
            if (partCount > 0 && part == null) continue;
            Vector3 c = part != null ? part.localBounds.center : t.Bounds.center;
            Vector3 e = part != null ? part.localBounds.extents : t.Bounds.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 w = c + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                if (part != null) w = part.localToWorldMatrix.MultiplyPoint3x4(w);
                if (w.y < t.GroundY) w.y = t.GroundY;
                Vector3 sp = cam.WorldToScreenPoint(w);
                if (sp.z < 0.05f) { behind = true; break; }
                Vector2 l = ToLocal(cam, sp, size);
                min = Vector2.Min(min, l); max = Vector2.Max(max, l);
            }
        }

        // Khung NỞ THEO VẬT kể cả khi đứng sát (24/10 - trước đây vật to/gần thì co về ô nhỏ, không bao nổi vật).
        // Chỉ giới hạn trong một cửa sổ lớn quanh điểm nhìn: chỉ là 4 góc khung nên to cũng không che gì, còn
        // phần vật chìm dưới đất / chìa ra xa thì không kéo khung tới tận mép màn hình.
        float safeH = size.y - SafeTop - SafeBottom;
        Vector3 hp = cam.WorldToScreenPoint(t.HitPoint);
        Vector2 at = hp.z > 0f ? ToLocal(cam, hp, size) : Vector2.zero;
        Vector2 lim = new Vector2(Mathf.Min(size.x * 0.36f, 700f), safeH * 0.5f);
        if (behind || min.x > max.x) { min = at - lim; max = at + lim; }   // có góc nằm sau camera = vật bao trùm tầm nhìn
        min = Vector2.Max(min - new Vector2(10f, 10f), at - lim);
        max = Vector2.Min(max + new Vector2(10f, 10f), at + lim);
        Vector2 bsz = Vector2.Max(max - min, new Vector2(76f, 76f));
        Rect r = new Rect((min + max) / 2f - bsz / 2f, bsz);

        // Kẹp trong vùng an toàn
        float xMin = -size.x / 2f + SafeSide, xMax = size.x / 2f - SafeSide;
        float yMin = -size.y / 2f + SafeBottom, yMax = size.y / 2f - SafeTop;
        r.x = Mathf.Clamp(r.x, xMin, Mathf.Max(xMin, xMax - r.width));
        r.y = Mathf.Clamp(r.y, yMin, Mathf.Max(yMin, yMax - r.height));
        return r;
    }

    /// <summary>Điểm trên màn hình của camera → toạ độ canvas (gốc ở giữa). Tự tính theo cỡ camera, không phụ thuộc chế độ canvas.</summary>
    private static Vector2 ToLocal(Camera cam, Vector3 screen, Vector2 canvasSize)
    {
        return new Vector2(screen.x / cam.pixelWidth - 0.5f, screen.y / cam.pixelHeight - 0.5f) * canvasSize;
    }

    private void PlaceBrackets(Rect r, Color col)
    {
        // Góc khung dày + dài hơn, pha trắng 25% cho sáng, có viền tối 1px quanh (sửa 24/10 - bản cũ quá nhạt)
        const float arm = 30f, th = 4f, pad = 2f;
        Color bright = Color.Lerp(col, Color.white, 0.25f);
        Color cc = new Color(bright.r, bright.g, bright.b, 1f);
        Vector2[] corners = { new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin) };
        Vector2[] dirs = { new Vector2(1, -1), new Vector2(-1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
        for (int i = 0; i < 4; i++)
        {
            Image h = _bars[i * 2], v = _bars[i * 2 + 1];
            h.color = cc; v.color = cc;
            h.rectTransform.anchoredPosition = corners[i] + new Vector2(dirs[i].x * arm / 2f, 0f);
            h.rectTransform.sizeDelta = new Vector2(arm, th);
            v.rectTransform.anchoredPosition = corners[i] + new Vector2(0f, dirs[i].y * arm / 2f);
            v.rectTransform.sizeDelta = new Vector2(th, arm);

            Image hs = _barShadows[i * 2], vs = _barShadows[i * 2 + 1];
            hs.rectTransform.anchoredPosition = h.rectTransform.anchoredPosition;
            hs.rectTransform.sizeDelta = h.rectTransform.sizeDelta + new Vector2(pad, pad);
            vs.rectTransform.anchoredPosition = v.rectTransform.anchoredPosition;
            vs.rectTransform.sizeDelta = v.rectTransform.sizeDelta + new Vector2(pad, pad);
        }
    }

    private void PlaceCard(Rect r, Vector2 size)
    {
        // Mặc định bám góc trên-phải khung ngắm; sát mép phải thì chuyển sang bên trái
        float w = CardW * CardScale, h = CardH * CardScale;
        float x = r.xMax + 16f;
        if (x + w > size.x / 2f - SafeSide) x = r.xMin - 16f - w;
        float y = r.yMax;
        y = Mathf.Clamp(y, -size.y / 2f + SafeBottom + h, size.y / 2f - SafeTop);
        x = Mathf.Clamp(x, -size.x / 2f + SafeSide, size.x / 2f - SafeSide - w);
        // Trượt mượt tới chỗ mới (đổi bên trái/phải không nhảy cái rụp); vật mới thì đặt thẳng
        Vector2 want = new Vector2(x, y);
        _cardPos = _cardSnap ? want : Vector2.Lerp(_cardPos, want, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f));
        _cardSnap = false;
        _card.anchoredPosition = _cardPos;
    }

    private static RectTransform Rt(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static Image Img(RectTransform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rt(name, parent, pos, size);
        if (parent.name == "Card") { rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); }
        Image i = rt.gameObject.AddComponent<Image>();
        i.sprite = sprite; i.color = color; i.raycastTarget = false;
        return i;
    }

    private static TMP_Text Txt(RectTransform card, string name, string text, TMP_FontAsset font, float sizePt, Color color, Vector2 topLeft, float spacing)
    {
        RectTransform rt = Rt(name, card, topLeft, new Vector2(CardW - 28f, sizePt * 1.5f));
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text; t.fontSize = sizePt; t.color = color; t.characterSpacing = spacing;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }
}
