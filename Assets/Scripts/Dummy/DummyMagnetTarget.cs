using UnityEngine;

public class DummyMagnetTarget : MonoBehaviour
{
    [Header("Dummy Polarity")]
    public MagneticObject.Polarity currentGlovePolarity = MagneticObject.Polarity.Positive;

    private Renderer dummyRenderer;

    void Start()
    {
        dummyRenderer = GetComponent<Renderer>();
        UpdateDummyColor();
    }

    public void SetPolarity(MagneticObject.Polarity newPolarity)
    {
        currentGlovePolarity = newPolarity;
        UpdateDummyColor();
    }

    void UpdateDummyColor()
    {
        if (dummyRenderer == null) return;

        if (currentGlovePolarity == MagneticObject.Polarity.Positive)
            dummyRenderer.material.color = Color.red; 
        else if (currentGlovePolarity == MagneticObject.Polarity.Negative)
            dummyRenderer.material.color = Color.blue; 
        else
            dummyRenderer.material.color = Color.gray;
    }
}