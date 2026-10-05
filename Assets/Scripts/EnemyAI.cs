using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// One reusable "Fun Police" enemy. Grunt and Captain are the SAME script with different numbers.
// chase -> wind-up (turns red, telegraph) -> strike -> recover -> chase
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public static int Alive { get; private set; }
    public static event Action<EnemyAI> Died;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Alive = 0; Died = null; }

    [Header("Stats")]
    public float maxHp = 30f;
    public float moveSpeed = 3.5f;
    public float attackRange = 1.6f;     // starts the wind-up inside this distance
    public float hitRadius = 2.0f;       // strike lands if player is within this at the end of the wind-up
    public float windupTime = 0.6f;
    public float recoverTime = 1.0f;
    public float hitDamage = 10f;        // damage dealt to the WEAPON
    public bool staggerOnHit = true;     // captain: set false so he can't be interrupted
    public float knockbackForce = 6f;

    [Header("Drops")]
    public int orbCount = 3;
    public float orbValue = 0.1f;        // fraction of the light bar per orb
    public GameObject orbPrefab;         // optional; a yellow sphere is made if empty

    [Header("Look")]
    public Renderer bodyRenderer;
    public Transform visual;
    public Color telegraphColor = new Color(1f, 0.2f, 0.2f);

    enum State { Chase, Windup, Recover, Dead }
    State state = State.Chase;
    NavMeshAgent agent;
    float hp, timer, repathTimer;
    Vector3 knock, baseScale;
    Color baseColor = Color.white;
    FlashTint tint;
    ModelAnimator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        hp = maxHp;
        tint = GetComponentInChildren<FlashTint>(true);
        anim = GetComponentInChildren<ModelAnimator>(true);
        if (visual == null) visual = tint != null ? tint.transform : transform;
        baseScale = visual.localScale;
        if (bodyRenderer != null) baseColor = MatColor.Get(bodyRenderer);
        if (tint != null) baseColor = new Color(0.011f, 0.022f, 0.033f, 0.5f);   // sentinel: "no tint"
        Alive++;
    }

    void OnDestroy() { if (state != State.Dead) Alive--; }

    Vector3 FlatToPlayer(out float dist)
    {
        var p = FighterController.Instance;
        if (p == null) { dist = 999f; return transform.forward; }
        Vector3 v = p.transform.position - transform.position; v.y = 0f;
        dist = v.magnitude;
        return dist > 0.001f ? v / dist : transform.forward;
    }

    void Update()
    {
        if (state == State.Dead || !agent.enabled) return;

        if (knock.sqrMagnitude > 0.01f)
        {
            agent.Move(knock * Time.deltaTime);
            knock = Vector3.Lerp(knock, Vector3.zero, 10f * Time.deltaTime);
        }

        float dist; Vector3 dir = FlatToPlayer(out dist);

        switch (state)
        {
            case State.Chase:
                agent.isStopped = false;
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f && FighterController.Instance != null)
                {
                    agent.SetDestination(FighterController.Instance.transform.position);
                    repathTimer = 0.2f;
                }
                if (dist <= attackRange) BeginWindup();
                break;

            case State.Windup:
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                timer -= Time.deltaTime;
                if (timer <= 0f) Strike(dist);
                break;

            case State.Recover:
                timer -= Time.deltaTime;
                if (timer <= 0f) state = State.Chase;
                break;
        }
    }

    void BeginWindup()
    {
        state = State.Windup;
        timer = windupTime;
        agent.isStopped = true;
        SetColor(telegraphColor);
        if (anim != null) anim.PlayWindup(windupTime);
        AudioManager.Play("windup", 0.6f);
        StartCoroutine(Pulse(new Vector3(1.2f, 0.85f, 1.2f), windupTime));
    }

    void Strike(float dist)
    {
        var p = FighterController.Instance;
        if (p != null && dist <= hitRadius) p.ReceiveHit(hitDamage);
        state = State.Recover;
        timer = recoverTime;
        if (anim != null) anim.PlayAttack();
        SetColor(baseColor);
        StartCoroutine(Pulse(new Vector3(0.8f, 1.25f, 0.8f), 0.15f));
    }

    public void TakeHit(float damage, Vector3 from)
    {
        if (state == State.Dead) return;
        hp -= damage;
        AudioManager.Play("hit");
        Vector3 away = transform.position - from; away.y = 0f;
        knock = away.normalized * knockbackForce;
        StartCoroutine(Flash());
        if (hp <= 0f) { Die(); return; }
        if (staggerOnHit)
        {
            state = State.Recover;
            timer = 0.35f;
            agent.isStopped = true;
        }
    }

    void Die()
    {
        state = State.Dead;
        Alive--;
        AudioManager.Play("enemy_die");
        agent.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (Died != null) Died(this);
        for (int i = 0; i < orbCount; i++)
            LightOrb.Spawn(orbPrefab, transform.position + Vector3.up * 0.5f, orbValue);
        StartCoroutine(Shrink());
    }

    void SetColor(Color c)
    {
        if (tint != null) { if (c == baseColor) tint.Restore(); else tint.Set(c); return; }
        if (bodyRenderer != null) MatColor.Set(bodyRenderer, c);
    }

    IEnumerator Flash()
    {
        SetColor(Color.white);
        yield return new WaitForSeconds(0.08f);
        if (state != State.Dead) SetColor(state == State.Windup ? telegraphColor : baseColor);
    }

    IEnumerator Pulse(Vector3 mult, float time)
    {
        float t = 0f;
        Vector3 peak = Vector3.Scale(baseScale, mult);
        while (t < time)
        {
            t += Time.deltaTime;
            visual.localScale = Vector3.Lerp(baseScale, peak, t / time);
            yield return null;
        }
        visual.localScale = baseScale;
    }

    IEnumerator Shrink()
    {
        float t = 0f;
        Vector3 start = transform.localScale;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / 0.25f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
