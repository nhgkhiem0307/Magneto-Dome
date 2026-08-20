using UnityEngine;

/// <summary>
/// Nhãn quan nhân vật: giúp người chơi đọc được BỐN thông tin chỉ bằng mắt, từ xa,
/// trên màn hình nhỏ, giữa map nhiều cây cối.
///
/// GẮN LÊN Player.prefab (cùng chỗ với FPSMovement).
///
/// NGUYÊN TẮC THIẾT KẾ: mỗi loại tin một kênh riêng, không kênh nào mang hai nghĩa.
///
/// | Thông tin        | Kênh                        | Màu                          |
/// |------------------|-----------------------------|------------------------------|
/// | Cực găng của địch| Outline toàn thân + cầu tay | Đỏ / Xanh dương              |
/// | Địch hay bạn     | Đồng đội có marker + outline| Xanh lá (địch KHÔNG có màu)  |
/// | Mức nhiễm điện   | Aura sét quanh người        | Vàng                         |
///
/// VÌ SAO ĐỊCH KHÔNG ĐƯỢC GÁN MÀU RIÊNG: nếu địch có viền cam hay đỏ thì nó cạnh tranh
/// thị giác với chính màu cực găng - thứ quan trọng nhất cần đọc. Để địch "sạch màu"
/// thì thứ duy nhất rực rỡ trên người họ là cực găng, mắt bị hút thẳng vào đó.
/// Địch được nhận ra bằng LOẠI TRỪ: không thấy xanh lá nghĩa là không phải phe mình.
///
/// KHÁC BIỆT QUAN TRỌNG VỀ CHE KHUẤT:
///   - Đồng đội: outline XUYÊN TƯỜNG (OutlineAll) - luôn biết họ ở đâu, khỏi bắn nhầm.
///   - Địch: outline CÓ CHE KHUẤT (OutlineVisible) - núp sau tường là mất, thò nửa người
///     ra thì chỉ nửa đó hiện viền. Nếu cho xuyên tường thì thành gian lận nhìn xuyên vách.
///
/// MonoBehaviour thuần, KHÔNG networked. Mọi thứ ở đây suy ra từ ba giá trị [Networked]
/// đã có sẵn: PlayerHealth.Team, PlayerHealth.ChargeRatio, và
/// PlayerMagnetController.currentGlovePolarity. Đúng nguyên tắc "chỉ đồng bộ thứ BẮT BUỘC".
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    private enum Relation { Unknown, Self, Ally, Enemy }

    [Header("Viền quanh người")]
    [Tooltip("Bề dày viền. To quá thì hai người đứng cạnh nhau viền dính vào nhau.")]
    public float outlineWidth = 4f;

    [Tooltip("Màu viền ĐỒNG ĐỘI. Cố ý là xanh LÁ chứ không phải xanh dương, " +
             "để không lẫn với cực Âm của găng tay.")]
    public Color allyOutlineColor = new Color(0.35f, 1f, 0.25f);

    [Tooltip("Màu viền của địch khi họ mang cực DƯƠNG.")]
    public Color positiveColor = new Color(1f, 0.2f, 0.15f);

    [Tooltip("Màu viền của địch khi họ mang cực ÂM.")]
    public Color negativeColor = new Color(0.2f, 0.5f, 1f);

    [Tooltip("Màu viền của địch khi chưa chọn cực nào.")]
    public Color neutralColor = new Color(0.75f, 0.75f, 0.8f);

    [Header("Quả cầu năng lượng ở hai tay")]
    [Tooltip("Tự tạo hai quả cầu phát sáng gắn vào xương bàn tay. " +
             "KHÔNG cần model găng tay - rig nhân vật là Humanoid nên lấy được xương bằng code.")]
    public bool spawnHandOrbs = true;

    [Tooltip("Đường kính quả cầu, tính bằng mét.")]
    public float orbSize = 0.13f;

    [Tooltip("Độ chói. Trên 1 thì Bloom kéo hào quang ra xung quanh - đó mới là thứ " +
             "làm quả cầu đọc được từ xa, nên đừng để thấp.")]
    public float orbEmission = 4f;

    [Tooltip("Để trống thì tự tạo material. Gán vào nếu muốn dùng shader riêng.")]
    public Material orbMaterialTemplate;

    [Header("Aura sét theo mức nhiễm điện")]
    [Tooltip("Kéo một ParticleSystem (hiệu ứng sét vàng) đặt sẵn trong Player.prefab vào đây. " +
             "Để trống thì bỏ qua phần này, script vẫn chạy bình thường.")]
    public ParticleSystem lightningAura;

    [Tooltip("Nhiễm dưới mức này thì chưa hiện sét. Để 0 thì sét hiện ngay từ giọt điện đầu tiên.")]
    [Range(0f, 1f)]
    public float lightningStartAt = 0.15f;

    [Tooltip("Lượng hạt khi nhiễm đầy 100%. Tỉ lệ thuận với mức nhiễm.")]
    public float lightningMaxRate = 40f;

    [Header("Marker trên đầu đồng đội")]
    [Tooltip("Kéo một object (icon/mũi tên) đặt phía trên đầu trong Player.prefab vào đây. " +
             "Chỉ bật cho ĐỒNG ĐỘI. Để trống thì bỏ qua.")]
    public GameObject allyMarker;

    // Ai cũng phải chờ FPSMovement.Local được gán mới biết được mình là phe nào,
    // mà nó chỉ có trong Spawned() của Fusion - có thể xảy ra sau Start() của script này.
    private Relation _relation = Relation.Unknown;

    private PlayerHealth _health;
    private PlayerMagnetController _magnet;
    private Outline _outline;

    private Transform _leftOrb, _rightOrb;
    private Material _orbMaterial;

    // Nhớ giá trị đã áp lần trước, chỉ ghi lại khi thật sự đổi.
    // Đặt màu vào material mỗi khung hình cho 8 nhân vật là lãng phí vô ích.
    private MagneticObject.Polarity _lastPolarity = (MagneticObject.Polarity)(-1);
    private float _lastChargeRatio = -1f;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _magnet = GetComponent<PlayerMagnetController>();
    }

    private void Update()
    {
        // Chưa xác định được phe thì thử lại mỗi khung hình cho tới khi biết
        if (_relation == Relation.Unknown)
        {
            if (!TryResolveRelation()) return;

            ApplyRelationVisuals();
        }

        // Cực găng của địch đổi liên tục trong trận, phải theo dõi mỗi khung hình
        if (_relation == Relation.Enemy) UpdateEnemyPolarityColor();

        UpdateLightningAura();
    }

    /// <summary>
    /// Xác định nhân vật này là mình, đồng đội, hay địch.
    /// Trả về false nếu chưa đủ dữ liệu (Fusion chưa spawn xong).
    /// </summary>
    private bool TryResolveRelation()
    {
        if (FPSMovement.Local == null) return false;
        if (_health == null) return false;

        PlayerHealth myHealth = FPSMovement.Local.GetComponent<PlayerHealth>();
        if (myHealth == null) return false;

        if (FPSMovement.Local.gameObject == gameObject)
        {
            _relation = Relation.Self;
        }
        else
        {
            _relation = (_health.Team == myHealth.Team) ? Relation.Ally : Relation.Enemy;
        }

        return true;
    }

    /// <summary>
    /// Dựng phần hình ảnh cố định theo phe. Chỉ chạy một lần.
    ///
    /// Đội của một người KHÔNG đổi giữa trận, nên không cần kiểm lại mỗi khung hình.
    /// (Nếu sau này cho đổi đội giữa trận thì phải gọi lại hàm này.)
    /// </summary>
    private void ApplyRelationVisuals()
    {
        // Marker chỉ dành cho đồng đội. Địch mà có marker thì thành máy dò người.
        if (allyMarker != null) allyMarker.SetActive(_relation == Relation.Ally);

        // NHÂN VẬT CỦA CHÍNH MÌNH: không viền, không quả cầu.
        //
        // Thân mình đã bị PlayerAnimatorDriver chuyển sang ShadowsOnly (chỉ đổ bóng,
        // không hiện hình). Thêm viền vào đây thì sẽ thấy một đường viền rỗng ruột
        // lơ lửng quanh chỗ mình đứng - rất kỳ.
        if (_relation == Relation.Self) return;

        SetupOutline();

        // Quả cầu tay chỉ gắn cho ĐỊCH.
        //
        // Đồng đội không cần: bạn không đấm đồng đội nên cực găng của họ vô nghĩa với bạn,
        // mà thêm hai đốm đỏ/xanh vào người đã có viền xanh lá chỉ làm rối mắt.
        if (_relation == Relation.Enemy && spawnHandOrbs) SetupHandOrbs();
    }

    private void SetupOutline()
    {
        _outline = GetComponent<Outline>();
        if (_outline == null) _outline = gameObject.AddComponent<Outline>();

        _outline.OutlineWidth = outlineWidth;

        if (_relation == Relation.Ally)
        {
            // XUYÊN TƯỜNG. Vị trí đồng đội là thông tin cho không - bạn cần biết họ ở đâu
            // mọi lúc để không bắn nhầm và để phối hợp.
            _outline.OutlineMode = Outline.Mode.OutlineAll;
            _outline.OutlineColor = allyOutlineColor;
        }
        else
        {
            // CÓ CHE KHUẤT. Núp sau tường là mất viền, thò nửa người ra thì chỉ nửa đó
            // hiện viền. Vị trí địch là thông tin phải TỰ KIẾM - cho xuyên tường thì
            // thành gian lận nhìn xuyên vách và mất hết giá trị của việc ẩn nấp.
            _outline.OutlineMode = Outline.Mode.OutlineVisible;
            _outline.OutlineColor = neutralColor; // màu thật đặt ngay sau, theo cực găng
        }
    }

    /// <summary>
    /// Tạo hai quả cầu phát sáng gắn vào xương bàn tay.
    ///
    /// VÌ SAO KHÔNG CẦN MODEL GĂNG TAY: rig của character.fbx là Humanoid, nên Unity
    /// cho tra thẳng ra xương bàn tay bằng GetBoneTransform(). Một quả cầu phát sáng
    /// còn ĐỌC TỐT HƠN model găng chi tiết: găng chỉ vài pixel ở khoảng cách 30m và hay
    /// bị thân che, còn quả cầu là nguồn sáng nên Bloom kéo hào quang của nó loang ra.
    /// </summary>
    private void SetupHandOrbs()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;

        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);

        _orbMaterial = CreateOrbMaterial();

        _leftOrb = CreateOrb(left);
        _rightOrb = CreateOrb(right);
    }

    private Material CreateOrbMaterial()
    {
        Material mat;

        if (orbMaterialTemplate != null)
        {
            mat = new Material(orbMaterialTemplate);
        }
        else
        {
            // Unlit: quả cầu năng lượng không nên bị bóng đổ làm tối đi.
            // Nó là nguồn sáng, không phải vật được chiếu sáng.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // phòng hờ
            mat = new Material(shader);
        }

        mat.EnableKeyword("_EMISSION");
        return mat;
    }

    private Transform CreateOrb(Transform parentBone)
    {
        if (parentBone == null) return null;

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "EnergyOrb";

        // XOÁ COLLIDER NGAY. CreatePrimitive luôn kèm collider, để lại thì nó va vào
        // người chơi và vật thể - đủ để phá cả hệ vật lý của game.
        Collider col = orb.GetComponent<Collider>();
        if (col != null) Destroy(col);

        orb.transform.SetParent(parentBone, false);
        orb.transform.localPosition = Vector3.zero;
        orb.transform.localScale = Vector3.one * orbSize;

        Renderer rend = orb.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = _orbMaterial;

            // Không đổ bóng: quả cầu bé xíu mà đổ bóng thì chỉ tạo vệt nhiễu dưới đất,
            // lại tốn thêm một lượt vẽ vào shadow map.
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        return orb.transform;
    }

    /// <summary>
    /// Đổi màu viền và quả cầu của địch theo cực găng họ đang mang.
    /// Chỉ ghi lại khi cực THẬT SỰ đổi, không ghi mỗi khung hình.
    /// </summary>
    private void UpdateEnemyPolarityColor()
    {
        if (_magnet == null) return;

        MagneticObject.Polarity polarity = _magnet.currentGlovePolarity;
        if (polarity == _lastPolarity) return;

        _lastPolarity = polarity;

        Color color = GetColorFor(polarity);

        if (_outline != null) _outline.OutlineColor = color;

        if (_orbMaterial != null)
        {
            _orbMaterial.color = color;
            _orbMaterial.SetColor("_BaseColor", color);
            _orbMaterial.SetColor("_EmissionColor", color * orbEmission);
        }
    }

    private Color GetColorFor(MagneticObject.Polarity polarity)
    {
        switch (polarity)
        {
            case MagneticObject.Polarity.Positive: return positiveColor;
            case MagneticObject.Polarity.Negative: return negativeColor;
            default: return neutralColor;
        }
    }

    /// <summary>
    /// Sét quanh người mạnh dần theo mức nhiễm điện.
    ///
    /// Đây là thông tin quan trọng nhất của chế độ Quá Tải mà hiện KHÔNG có cách nào đọc
    /// từ xa: nhìn một đối thủ, bạn không biết họ đang 20% hay 90% điện - trong khi đó
    /// chính là thứ quyết định có nên lao vào đấm hay không.
    ///
    /// Nó cũng tạo một vòng phản hồi đẹp: ai sắp thua thì tự phát sáng, tức là bị lộ.
    /// Nhiễm nặng vừa dễ bị hất bay vừa dễ bị nhìn thấy - căng thẳng tự leo thang mà
    /// không cần thêm luật nào.
    /// </summary>
    private void UpdateLightningAura()
    {
        if (lightningAura == null || _health == null) return;

        float ratio = _health.ChargeRatio;

        // Chỉ đụng vào ParticleSystem khi mức nhiễm đổi đủ đáng kể.
        // Ghi vào emission module mỗi khung hình là một trong những cách phí CPU
        // dễ mắc nhất với particle.
        if (Mathf.Abs(ratio - _lastChargeRatio) < 0.02f) return;
        _lastChargeRatio = ratio;

        bool shouldShow = ratio >= lightningStartAt && _health.IsAlive;

        var emission = lightningAura.emission;
        emission.rateOverTime = shouldShow ? lightningMaxRate * ratio : 0f;

        if (shouldShow && !lightningAura.isPlaying) lightningAura.Play();
        else if (!shouldShow && lightningAura.isPlaying) lightningAura.Stop();
    }
}
