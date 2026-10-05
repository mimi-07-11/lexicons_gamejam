using UnityEngine;

// Fixed angled top-down camera with screen shake. Put on the Arena camera.
public class CameraFollow : MonoBehaviour
{
    public Transform target;                       // leave empty: finds the Player tag
    public Vector3 offset = new Vector3(0f, 12f, -8f);
    public float smooth = 8f;

    static CameraFollow instance;
    float shakeAmount, shakeUntil;

    void Awake() { instance = this; }

    void Start() { transform.rotation = Quaternion.LookRotation(-offset); }

    public static void Shake(float amount, float duration)
    {
        if (instance == null) return;
        instance.shakeAmount = amount;
        instance.shakeUntil = Time.unscaledTime + duration;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            target = p.transform;
        }
        Vector3 want = target.position + offset;
        Vector3 pos = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
        if (Time.unscaledTime < shakeUntil) pos += Random.insideUnitSphere * shakeAmount;
        transform.position = pos;
    }
}
