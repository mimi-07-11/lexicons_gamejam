using UnityEngine;

// Procedural animation for the block models (no animation clips needed):
// walk swing from movement speed, windup / attack swing, dodge lean, idle bob.
// Finds children named ArmL, ArmR, LegL, LegR, Body automatically.
public class ModelAnimator : MonoBehaviour
{
    public Transform armL, armR, legL, legR, body;
    public float stride = 38f;
    public float restArmR = -25f;

    Vector3 lastPos, bodyBase;
    float speed, phase;
    float windupLeft, windupTotal = 1f, attackLeft, dodgeLeft, dodgeTotal = 1f;
    const float AttackDur = 0.22f;

    void Awake()
    {
        armL = Pick(armL, "ArmL"); armR = Pick(armR, "ArmR");
        legL = Pick(legL, "LegL"); legR = Pick(legR, "LegR");
        body = Pick(body, "Body");
        lastPos = transform.position;
        if (body != null) bodyBase = body.localPosition;
    }

    Transform Pick(Transform t, string n)
    {
        if (t != null) return t;
        foreach (var c in GetComponentsInChildren<Transform>(true)) if (c.name == n) return c;
        return null;
    }

    public void PlayAttack() { attackLeft = AttackDur; windupLeft = 0f; }
    public void PlayWindup(float seconds) { windupTotal = Mathf.Max(0.05f, seconds); windupLeft = windupTotal; }
    public void PlayDodge(float seconds) { dodgeTotal = Mathf.Max(0.05f, seconds); dodgeLeft = dodgeTotal; }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 d = transform.position - lastPos; d.y = 0f; lastPos = transform.position;
        speed = Mathf.Lerp(speed, d.magnitude / dt, 10f * dt);
        phase += Mathf.Min(speed, 12f) * 1.7f * dt;
        float move01 = Mathf.Clamp01(speed / 3f);
        float amp = move01 * stride;
        float s = Mathf.Sin(phase);

        float rx = restArmR - s * amp * 0.7f;
        float lx = s * amp * 0.7f;
        if (windupLeft > 0f)
        {
            windupLeft -= dt;
            rx = Mathf.Lerp(restArmR, -125f, 1f - Mathf.Clamp01(windupLeft / windupTotal));
        }
        if (attackLeft > 0f)
        {
            attackLeft -= dt;
            float t = 1f - Mathf.Clamp01(attackLeft / AttackDur);
            rx = Mathf.Lerp(-125f, 45f, t * t);
        }

        Rot(legL, s * amp); Rot(legR, -s * amp);
        Rot(armL, lx); Rot(armR, rx);

        if (body != null)
        {
            float lean = 0f;
            if (dodgeLeft > 0f)
            {
                dodgeLeft -= dt;
                lean = Mathf.Sin(Mathf.PI * (1f - Mathf.Clamp01(dodgeLeft / dodgeTotal))) * 40f;
            }
            body.localRotation = Quaternion.Euler(lean, 0f, 0f);
            body.localPosition = bodyBase + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.015f + Mathf.Abs(s) * 0.04f * move01);
        }
    }

    static void Rot(Transform t, float x) { if (t != null) t.localRotation = Quaternion.Euler(x, 0f, 0f); }
}
