using UnityEngine;

public class MagneticAura : MonoBehaviour
{
    [Header("Aura HDR Colors")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color positiveColor = new Color(3f, 0.3f, 0.3f); // Đỏ rực
    
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color negativeColor = new Color(0.3f, 1f, 3f);   // Xanh rực

    [Header("Outline Settings")]
    [Range(0f, 10f)]
    public float outlineWidth = 8f; // Độ dày viền (Chỉnh từ 2 đến 5 là đẹp)

    [Header("Optional Light")]
    public Light auraPointLight;

    [Header("Khi người chơi NHÌN VÀO (chỉ máy của mình)")]
    [Tooltip("Màu viền khi nhìn vào vật CHƯA có điện. Trắng để phân biệt rõ với " +
             "đỏ/xanh của điện tích - trắng không mang nghĩa cực nào cả.")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color inspectNeutralColor = new Color(2.2f, 2.2f, 2.2f);

    [Tooltip("Vật ĐÃ có điện thì giữ nguyên màu cực, chỉ nhân sáng lên bấy nhiêu lần.\n\n" +
             "Cố ý KHÔNG đổi sang trắng: màu cực là thông tin quan trọng nhất, đổi màu " +
             "lúc nhìn vào thì đúng lúc cần đọc nhất lại không đọc được.")]
    public float inspectChargedBoost = 2f;

    [Tooltip("Viền dày thêm bao nhiêu khi đang nhìn vào.")]
    public float inspectWidthBonus = 4f;

    [Tooltip("Thân vật sáng lên bấy nhiêu lần khi nhìn vào. 1 = không đổi.")]
    public float inspectBrightness = 1.6f;

    private Outline outline;

    // Cực điện hiện tại, nhớ lại để lúc bật/tắt highlight còn dựng lại đúng vẻ ngoài.
    private MagneticObject.Polarity _polarity = MagneticObject.Polarity.None;

    // Người chơi ở máy này có đang nhìn vào vật này không.
    private bool _highlighted;

    // Dùng MaterialPropertyBlock chứ KHÔNG đụng vào renderer.material.
    //
    // Đọc renderer.material sẽ khiến Unity nhân bản material ra một bản riêng cho vật đó -
    // vĩnh viễn, kể cả sau khi thôi nhìn. Với 4000 vật trang trí thì chỉ cần lướt mắt qua
    // vài chục cái là sinh ra vài chục material mới, phá vỡ gộp lô vẽ và rò rỉ bộ nhớ.
    // PropertyBlock ghi đè giá trị mà không tạo material nào.
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Color[] _originalColors;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        // Tự động kiểm tra và thêm component Quick Outline nếu vật thể chưa có
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        // Cấu hình chế độ viền: OutlineAll (Viền bao bọc cả thân lẫn lá cây)
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineWidth = outlineWidth;
        
        // Mặc định tắt viền khi chưa tích điện
        outline.enabled = false;

        // Lấy sẵn renderer và màu gốc, để phần làm sáng khi nhìn vào khỏi đi tìm mỗi lần.
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Material shared = _renderers[i] != null ? _renderers[i].sharedMaterial : null;

            // sharedMaterial chứ không phải material: đọc .material là Unity nhân bản ngay.
            _originalColors[i] = (shared != null && shared.HasProperty(BaseColorId))
                ? shared.GetColor(BaseColorId)
                : Color.white;
        }
    }

    public void UpdateAura(MagneticObject.Polarity polarity)
    {
        _polarity = polarity;
        Apply();
    }

    /// <summary>
    /// Người chơi ở MÁY NÀY có đang chĩa tâm ngắm vào vật này không.
    ///
    /// Thuần cục bộ, cố ý không đồng bộ: đây là thông tin trợ giúp cho riêng người đang
    /// nhìn, đối thủ không cần và không nên biết bạn đang ngắm vào cái gì.
    /// </summary>
    public void SetHighlighted(bool on)
    {
        if (_highlighted == on) return;

        _highlighted = on;
        Apply();
    }

    /// <summary>
    /// Dựng lại toàn bộ vẻ ngoài từ hai dữ kiện: cực điện, và có đang bị nhìn hay không.
    ///
    /// Gộp về MỘT hàm duy nhất thay vì để mỗi nơi tự bật/tắt viền. Nếu tách ra thì thôi
    /// nhìn vào một vật đang nhiễm điện sẽ tắt luôn viền điện tích của nó - đúng kiểu lỗi
    /// "hai chỗ cùng ghi, ai chạy sau xoá công người trước".
    /// </summary>
    private void Apply()
    {
        bool charged = _polarity != MagneticObject.Polarity.None;

        // --- VIỀN ---
        if (outline != null)
        {
            if (!charged && !_highlighted)
            {
                outline.enabled = false;
            }
            else
            {
                Color color;

                if (!charged)
                {
                    // Chưa có điện mà đang nhìn -> trắng. Trắng không mang nghĩa cực nào,
                    // nên không lẫn với đỏ/xanh của điện tích.
                    color = inspectNeutralColor;
                }
                else
                {
                    color = (_polarity == MagneticObject.Polarity.Positive) ? positiveColor : negativeColor;

                    // Giữ nguyên MÀU cực, chỉ nhân độ sáng. Đổi màu lúc nhìn vào thì đúng
                    // lúc cần đọc cực nhất lại không đọc được.
                    if (_highlighted) color *= inspectChargedBoost;
                }

                outline.OutlineColor = color;
                outline.OutlineWidth = outlineWidth + (_highlighted ? inspectWidthBonus : 0f);
                outline.enabled = true;
            }
        }

        // --- ĐÈN HẮT SÁNG (nếu prefab có gắn) ---
        if (auraPointLight != null)
        {
            if (charged)
            {
                auraPointLight.color = (_polarity == MagneticObject.Polarity.Positive) ? positiveColor : negativeColor;
                auraPointLight.enabled = true;
            }
            else
            {
                auraPointLight.enabled = false;
            }
        }

        // --- THÂN VẬT SÁNG LÊN ---
        ApplyBrightness(_highlighted ? inspectBrightness : 1f);
    }

    private void ApplyBrightness(float multiplier)
    {
        if (_renderers == null || _mpb == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);

            // Nhân đúng phần màu, GIỮ NGUYÊN alpha. Nhân cả alpha thì vật trong suốt
            // (lá cây, kính) sẽ đục hẳn lại mỗi lần nhìn vào.
            Color c = _originalColors[i];
            _mpb.SetColor(BaseColorId, new Color(c.r * multiplier, c.g * multiplier, c.b * multiplier, c.a));

            r.SetPropertyBlock(_mpb);
        }
    }

    public void DisableAura()
    {
        _polarity = MagneticObject.Polarity.None;
        Apply();
    }
}