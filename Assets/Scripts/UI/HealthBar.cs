using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image  fillImage;
    public Image  bgImage;
    public Vector3 worldOffset = new Vector3(0f, 0.85f, 0f);
    private float maxHP;

    public void Setup(float max, Color col, string className)
    {
        maxHP = max;
        if (bgImage   != null) bgImage.color   = col * 0.3f;
        if (fillImage != null) fillImage.color = col;
        UpdateBar(max, Vector3.zero);
    }

    public void UpdateBar(float hp, Vector3 ballPos)
    {
        transform.position = ballPos + worldOffset;
        if (fillImage == null) return;
        fillImage.fillAmount = Mathf.Clamp01(hp / maxHP);
        fillImage.color = Color.Lerp(
            new Color(0.9f, 0.2f, 0.1f),
            new Color(0.2f, 0.9f, 0.2f),
            fillImage.fillAmount);
    }
}
