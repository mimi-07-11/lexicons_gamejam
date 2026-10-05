using UnityEngine;

// A SpriteRenderer object (use guide_arrow.png) that floats around the player and points at a target.
public class GuideArrow : MonoBehaviour
{
    public Transform player;
    public float distance = 1.4f;       // how far from the player the arrow floats
    public float hideWithin = 2.5f;     // hide it when the target is this close

    Transform target;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
    }

    public void SetTarget(Transform t) { target = t; }

    void LateUpdate()
    {
        if (target == null || player == null) { sr.enabled = false; return; }
        Vector3 d = target.position - player.position; d.z = 0f;
        if (d.magnitude < hideWithin) { sr.enabled = false; return; }

        sr.enabled = true;
        Vector3 dir = d.normalized;
        float bob = Mathf.Sin(Time.time * 5f) * 0.08f;
        transform.position = player.position + dir * (distance + bob);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }
}
