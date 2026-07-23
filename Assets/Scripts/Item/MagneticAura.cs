using UnityEngine;

public class MagneticAura : MonoBehaviour
{
    [Header("Aura HDR Colors")]
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color positiveColor = new Color(2f, 0.2f, 0.2f);
    
    [ColorUsage(showAlpha: false, hdr: true)]
    public Color negativeColor = new Color(0.2f, 0.8f, 2f);

    [Header("Aura FX (Tùy chọn)")]
    public Light auraPointLight;
    public ParticleSystem auraParticles; // <--- KHAI BÁO BỔ SUNG Ô KÉO THẢ Ở ĐÂY

    private Renderer rend;
    private Material mat;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
        }
    }

    public void UpdateAura(MagneticObject.Polarity polarity)
    {
        if (polarity == MagneticObject.Polarity.None)
        {
            DisableAura();
            return;
        }

        Color targetColor = (polarity == MagneticObject.Polarity.Positive) ? positiveColor : negativeColor;

        // 1. Đổi màu Material Emission (An toàn cho mọi Shader)
        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", targetColor);
        }

        // 2. Bật Light hắt sáng ra sàn/môi trường
        if (auraPointLight != null)
        {
            auraPointLight.color = targetColor;
            auraPointLight.enabled = true;
        }

        // 3. Đổi màu và BẬT Particle System bao quanh
        if (auraParticles != null)
        {
            var main = auraParticles.main;
            main.startColor = targetColor;
            if (!auraParticles.isPlaying) auraParticles.Play();
        }
    }

    public void DisableAura()
    {
        if (mat != null && mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", Color.black);
        }

        if (auraPointLight != null) auraPointLight.enabled = false;
        
        // Tắt hạt khi không có điện tích
        if (auraParticles != null) auraParticles.Stop();
    }
}