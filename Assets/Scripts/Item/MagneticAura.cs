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

    private Outline outline;

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
    }

    public void UpdateAura(MagneticObject.Polarity polarity)
    {
        if (polarity == MagneticObject.Polarity.None)
        {
            DisableAura();
            return;
        }

        Color targetColor = (polarity == MagneticObject.Polarity.Positive) ? positiveColor : negativeColor;

        // 1. BẬT VIỀN TOON TỰ ĐỘNG (BAO GỒM CẢ CÂY CỐI/LÁ CÂY)
        if (outline != null)
        {
            outline.OutlineColor = targetColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true; // Kích hoạt render viền
        }

        // 2. BẬT POINT LIGHT HẮT SÁNG (NẾU CÓ)
        if (auraPointLight != null)
        {
            auraPointLight.color = targetColor;
            auraPointLight.enabled = true;
        }
    }

    public void DisableAura()
    {
        // Tắt viền -> Ngừng tính toán hoàn toàn (0% Lag)
        if (outline != null)
        {
            outline.enabled = false;
        }

        // Tắt Point Light
        if (auraPointLight != null)
        {
            auraPointLight.enabled = false;
        }
    }
}