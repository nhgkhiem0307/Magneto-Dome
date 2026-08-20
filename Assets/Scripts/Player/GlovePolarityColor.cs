using UnityEngine;

/// <summary>
/// Đổi màu găng tay theo cực điện đang mang, và giấu găng của người chơi khác đi.
///
/// GẮN LÊN object găng tay (cái nằm làm con của Camera trong Player.prefab).
///
/// Đây là "view model" - phần chỉ mình bạn nhìn thấy, khác với thân nhân vật mà người
/// khác nhìn thấy. Mọi game FPS đều tách hai thứ này ra.
///
/// VÌ SAO ĐÁNG LÀM: cực găng hiện chỉ hiện ở góc HUD. Có găng đổi màu ngay giữa tầm nhìn
/// thì người chơi biết mình đang mang cực gì mà KHÔNG phải liếc xuống góc màn hình -
/// trong lúc đang bị hất văng thì đó là khác biệt lớn.
///
/// MonoBehaviour thuần, không networked: currentGlovePolarity vốn đã là [Networked] rồi,
/// script này chỉ đọc và tô màu. Đúng nguyên tắc "chỉ đồng bộ thứ BẮT BUỘC".
/// </summary>
public class GlovePolarityColor : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("Để trống thì tự tìm ngược lên các object cha.")]
    public PlayerMagnetController magnetController;

    [Tooltip("Những Renderer sẽ bị tô màu. Để trống thì lấy hết trên object này và các con.")]
    public Renderer[] targetRenderers;

    [Header("Màu theo cực")]
    [Tooltip("Cực Dương. Nên trùng với màu vật thể mang điện dương để người chơi liên hệ được.")]
    public Color positiveColor = new Color(1f, 0.2f, 0.15f);

    [Tooltip("Cực Âm.")]
    public Color negativeColor = new Color(0.2f, 0.5f, 1f);

    [Tooltip("Lúc chưa chọn cực nào.")]
    public Color neutralColor = new Color(0.6f, 0.6f, 0.65f);

    [Header("Phát sáng")]
    [Tooltip("Bật thì găng tự phát sáng, nhìn rõ cả trong bóng tối. " +
             "Cần bật Bloom trong post-processing thì mới thật sự toả sáng.")]
    public bool useEmission = true;

    [Tooltip("Độ sáng của phần phát sáng. Trên 1 thì cháy sáng mạnh khi có Bloom.")]
    public float emissionIntensity = 2f;

    [Header("Nháy khi đổi cực")]
    [Tooltip("Loé sáng một cái mỗi khi đổi cực, để người chơi thấy rõ thao tác đã ăn.")]
    public bool flashOnSwitch = true;

    [Tooltip("Loé sáng gấp mấy lần bình thường.")]
    public float flashMultiplier = 3f;

    [Tooltip("Loé trong bao nhiêu giây rồi trở lại bình thường.")]
    public float flashDuration = 0.15f;

    // Tên thuộc tính màu trong shader. URP Lit và URP Unlit đều dùng _BaseColor.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Material[] _materials;
    private MagneticObject.Polarity _lastPolarity = (MagneticObject.Polarity)(-1);
    private float _flashTimer;
    private bool _initialised;

    private void Start()
    {
        if (magnetController == null) magnetController = GetComponentInParent<PlayerMagnetController>();
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void Update()
    {
        // GIẤU GĂNG CỦA NGƯỜI KHÁC.
        //
        // Bắt buộc phải có. Găng là con của Camera, mà camera của người chơi khác tuy
        // đã bị tắt trong FPSMovement.Spawned() thì RENDERER VẪN VẼ BÌNH THƯỜNG -
        // tắt camera không tắt renderer. Không xử lý thì bạn sẽ thấy găng tay của
        // 3 người kia lơ lửng giữa không trung ngay chỗ đầu họ.
        //
        // Kiểm mỗi khung hình cho tới khi biết được, vì FPSMovement.Local chỉ được gán
        // trong Spawned() của Fusion - có thể xảy ra sau Start() của script này.
        if (!_initialised)
        {
            if (FPSMovement.Local == null) return;

            _initialised = true;

            bool isMine = magnetController != null
                       && FPSMovement.Local.gameObject == magnetController.gameObject;

            if (!isMine)
            {
                gameObject.SetActive(false);
                return;
            }

            CacheMaterials();
        }

        if (magnetController == null || _materials == null) return;

        MagneticObject.Polarity polarity = magnetController.currentGlovePolarity;

        if (polarity != _lastPolarity)
        {
            _lastPolarity = polarity;
            if (flashOnSwitch) _flashTimer = flashDuration;
        }

        // Đếm ngược cú loé sáng
        float flash = 1f;
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;

            // Loé mạnh nhất lúc vừa đổi rồi tắt dần, chứ không sáng đều rồi tắt phụt
            float t = Mathf.Clamp01(_flashTimer / Mathf.Max(flashDuration, 0.0001f));
            flash = Mathf.Lerp(1f, flashMultiplier, t);
        }

        ApplyColor(GetColorFor(polarity), flash);
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
    /// Lấy bản sao vật liệu của riêng găng này.
    ///
    /// Đọc renderer.materials là Unity tự tạo BẢN SAO riêng, nên tô màu ở đây không
    /// ảnh hưởng tới file material gốc trong project - nếu không thì sửa màu găng
    /// một lần là hỏng luôn asset, và lỗi đó còn giữ nguyên sau khi thoát Play.
    /// </summary>
    private void CacheMaterials()
    {
        var list = new System.Collections.Generic.List<Material>();

        foreach (Renderer r in targetRenderers)
        {
            if (r == null) continue;

            foreach (Material m in r.materials)
            {
                if (m == null) continue;

                // Bật cờ phát sáng. Không bật thì đặt _EmissionColor cũng không hiện gì,
                // vì shader đã loại bỏ hẳn nhánh tính emission lúc biên dịch.
                if (useEmission) m.EnableKeyword("_EMISSION");

                list.Add(m);
            }
        }

        _materials = list.ToArray();
    }

    private void ApplyColor(Color color, float flashMul)
    {
        foreach (Material m in _materials)
        {
            if (m == null) continue;

            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);

            if (useEmission && m.HasProperty(EmissionColorId))
            {
                m.SetColor(EmissionColorId, color * emissionIntensity * flashMul);
            }
        }
    }
}
