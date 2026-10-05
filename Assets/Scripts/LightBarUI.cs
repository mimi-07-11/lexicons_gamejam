using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Put this on a UI Slider (min 0, max 1, not interactable). Assign the Slider and its Fill image.
public class LightBarUI : MonoBehaviour
{
    public Slider bar;
    public Image fill;
    public Color full = new Color(1f, 0.85f, 0.3f);
    public Color low = new Color(0.9f, 0.25f, 0.2f);

    void OnEnable()
    {
        if (bar != null)
        {
            bar.minValue = 0f; 
            bar.maxValue = 1f;
        }
        LightEnergy.Changed += Set;
        LightEnergy.Empty += Flash;
        Set(LightEnergy.Current);
    }

    void OnDisable()
    {
        LightEnergy.Changed -= Set;
        LightEnergy.Empty -= Flash;
    }

    void Set(float v)
    {
        // Null checks ensure we don't throw errors if the UI is unloading
        if (bar != null) bar.value = v;
        if (fill != null) fill.color = Color.Lerp(low, full, Mathf.Clamp01(v * 3f));
    }

    void Flash()
    {
        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(Pulse());
        }
    }

    IEnumerator Pulse()
    {
        for (int i = 0; i < 3; i++)
        {
            transform.localScale = Vector3.one * 1.12f;
            yield return new WaitForSeconds(0.07f);
            transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.07f);
        }
    }
}