using TMPro;
using UnityEngine;

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

    [Header("Bảng chữ nổi trên vật")]
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
            if (obj != null && !obj.IsStored && !obj.IsDestroyed) found = obj;
        }

        if (found == Current) return;

        // Đổi mục tiêu -> tắt sáng vật cũ trước, rồi mới bật vật mới.
        // Làm ngược thứ tự thì khi hai vật là một sẽ tắt mất cái vừa bật.
        if (_highlighted != null) _highlighted.SetHighlighted(false);
        _highlighted = null;

        Current = found;

        // Vật mới -> bảng chữ NHẢY thẳng tới đỉnh vật mới, không trượt từ vật cũ sang.
        _targetBody = Current != null ? FindBody(Current) : null;
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

    private void OnDisable()
    {
        // Chết, sang round mới, hay thoát trận đều đi qua đây.
        // Không tắt thì vật cuối cùng bạn nhìn sẽ sáng rực mãi mãi, kể cả khi bạn đã
        // là cái xác nằm dưới vực.
        if (_highlighted != null) _highlighted.SetHighlighted(false);

        _highlighted = null;
        Current = null;

        if (labelRoot != null) labelRoot.gameObject.SetActive(false);
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
