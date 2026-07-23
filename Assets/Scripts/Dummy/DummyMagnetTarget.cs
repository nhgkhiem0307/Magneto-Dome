using UnityEngine;

public class DummyMagnetTarget : MonoBehaviour
{
    [Header("Dummy Polarity")]
    public MagneticObject.Polarity currentGlovePolarity = MagneticObject.Polarity.Positive;

    void Start()
    {
        UpdateDummyColor();
    }

    public void SetPolarity(MagneticObject.Polarity newPolarity)
    {
        currentGlovePolarity = newPolarity;
        UpdateDummyColor();
    }

    public void UpdateDummyColor()
    {
        // SỬA CS0411: Phải có <MagneticAura> trong ngoặc nhọn
        MagneticAura aura = GetComponent<MagneticAura>();
        if (aura != null)
        {
            aura.UpdateAura(currentGlovePolarity);
        }
    }
}