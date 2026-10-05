using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// WASD move, mouse aim, LMB (or J) attack, Space dodge roll.
// No player HP: enemy hits damage the WEAPON (WeaponLoadout).
[RequireComponent(typeof(CharacterController))]
public class FighterController : MonoBehaviour
{
    public static FighterController Instance { get; private set; }
    public static event Action WeaponBroke;

    [Header("Move")]
    public float moveSpeed = 6f;
    public float turnSpeed = 18f;

    [Header("Dodge")]
    public float dodgeSpeed = 15f;
    public float dodgeTime = 0.22f;
    public float dodgeCooldown = 0.7f;

    [Header("Attack")]
    public float attackReach = 1.6f;
    public float attackRadius = 1.3f;
    public float attackCooldown = 0.4f;
    public float attackWindup = 0.08f;

    [Header("Weapon break")]
    public bool autoRestart = true;
    public float restartDelay = 1.5f;
    public float hitInvulnTime = 0.6f;

    [Header("Look")]
    public Renderer bodyRenderer;      // the capsule's renderer (for flashes)
    public Transform visual;           // child to squash; leave empty to squash self

    [Header("Aim / Animation")]
    public bool useMouseAim = false;   // false = attack where you face (keyboard only)
    public Animator animator;          // optional: real character model

    CharacterController cc;
    bool dodging, attacking, broken;
    float dodgeReadyAt, attackReadyAt, invulnUntil, vy;
    Color baseColor = Color.white;
    FlashTint tint;
    ModelAnimator anim;
    Vector3 baseScale;
    public bool Invulnerable { get { return dodging || Time.time < invulnUntil; } }

    void Awake()
    {
        Instance = this;
        cc = GetComponent<CharacterController>();
        tint = GetComponentInChildren<FlashTint>(true);
        anim = GetComponentInChildren<ModelAnimator>(true);
        if (visual == null) visual = tint != null ? tint.transform : transform;
        baseScale = visual.localScale;
        if (bodyRenderer != null) baseColor = MatColor.Get(bodyRenderer);
        CacheAnimParams();
    }

    void Start() { WeaponLoadout.RestoreFull(); }   // every arena entry starts with a full weapon
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (broken) return;

        Vector2 m = GameInput.Move;
        Vector3 move = new Vector3(m.x, 0f, m.y);
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (GameInput.DodgePressed && !dodging && Time.time >= dodgeReadyAt)
            StartCoroutine(Dodge(move.sqrMagnitude > 0.01f ? move : transform.forward));
        if (GameInput.AttackPressed && !dodging && !attacking && Time.time >= attackReadyAt)
            StartCoroutine(Attack());

        if (dodging) return;   // dodge coroutine drives movement

        if (move.sqrMagnitude > 0.01f && !attacking)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);

        vy = cc.isGrounded ? -1f : vy - 20f * Time.deltaTime;
        cc.Move((move * moveSpeed + Vector3.up * vy) * Time.deltaTime);
        SetSpeed(move.magnitude * moveSpeed);
    }

    IEnumerator Dodge(Vector3 dir)
    {
        dodging = true;
        Trig("Dodge");
        if (anim != null) anim.PlayDodge(dodgeTime);
        AudioManager.Play("dodge");
        dodgeReadyAt = Time.time + dodgeCooldown;
        dir.y = 0f; dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        StartCoroutine(Squash(new Vector3(0.8f, 0.6f, 1.3f), dodgeTime));
        float t = 0f;
        while (t < dodgeTime)
        {
            cc.Move((dir * dodgeSpeed + Vector3.down) * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
        dodging = false;
    }

    Vector3 AimDirection()
    {
        Camera cam = Camera.main;
        if (useMouseAim && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(GameInput.MousePosition);
            Plane plane = new Plane(Vector3.up, transform.position);
            float d;
            if (plane.Raycast(ray, out d))
            {
                Vector3 v = ray.GetPoint(d) - transform.position; v.y = 0f;
                if (v.sqrMagnitude > 0.05f) return v.normalized;
            }
        }
        return transform.forward;
    }

    IEnumerator Attack()
    {
        attacking = true;
        Trig("Attack");
        if (anim != null) anim.PlayAttack();
        AudioManager.Play("attack");
        attackReadyAt = Time.time + attackCooldown;
        transform.rotation = Quaternion.LookRotation(AimDirection());
        StartCoroutine(Squash(new Vector3(1.25f, 0.8f, 1.25f), 0.15f));
        yield return new WaitForSeconds(attackWindup);

        Vector3 center = transform.position + transform.forward * attackReach * 0.6f;
        var hit = Physics.OverlapSphere(center, attackRadius);
        var done = new System.Collections.Generic.HashSet<EnemyAI>();
        foreach (var c in hit)
        {
            var e = c.GetComponentInParent<EnemyAI>();
            if (e != null && done.Add(e)) e.TakeHit(WeaponLoadout.Current.damage, transform.position);
        }
        if (done.Count > 0) CameraFollow.Shake(0.08f, 0.1f);

        yield return new WaitForSeconds(0.15f);
        attacking = false;
    }

    // Called by enemies when their wind-up strike lands.
    public void ReceiveHit(float weaponDamage)
    {
        if (broken || Invulnerable) return;
        invulnUntil = Time.time + hitInvulnTime;
        Trig("Hit");
        AudioManager.Play("player_hurt");
        WeaponLoadout.Damage(weaponDamage);
        CameraFollow.Shake(0.25f, 0.2f);
        StartCoroutine(Flash(Color.red, 0.12f));
        if (WeaponLoadout.Hp <= 0f)
        {
            broken = true;
            AudioManager.Play("weapon_break");
            if (WeaponBroke != null) WeaponBroke();
            if (autoRestart) StartCoroutine(RestartArena());
        }
    }

    // ---- optional Animator hooks: parameters Speed (float), Attack/Dodge/Hit (triggers). Missing ones are ignored.
    System.Collections.Generic.HashSet<string> animParams = new System.Collections.Generic.HashSet<string>();

    void CacheAnimParams()
    {
        if (animator == null) return;
        foreach (var p in animator.parameters) animParams.Add(p.name);
        if (animParams.Contains("MotionSpeed")) animator.SetFloat("MotionSpeed", 1f);   // Starter Assets character
        if (animParams.Contains("Grounded")) animator.SetBool("Grounded", true);
    }

    void Trig(string n) { if (animator != null && animParams.Contains(n)) animator.SetTrigger(n); }

    void SetSpeed(float v)
    {
        if (animator != null && animParams.Contains("Speed")) animator.SetFloat("Speed", v, 0.1f, Time.deltaTime);
    }

    IEnumerator RestartArena()
    {
        yield return new WaitForSecondsRealtime(restartDelay);
        WeaponLoadout.RestoreFull();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator Flash(Color c, float time)
    {
        if (tint != null)
        {
            tint.Set(c);
            yield return new WaitForSeconds(time);
            tint.Restore();
            yield break;
        }
        if (bodyRenderer == null) yield break;
        MatColor.Set(bodyRenderer, c);
        yield return new WaitForSeconds(time);
        MatColor.Set(bodyRenderer, baseColor);
    }

    IEnumerator Squash(Vector3 mult, float time)
    {
        float t = 0f;
        Vector3 peak = Vector3.Scale(baseScale, mult);
        while (t < time)
        {
            t += Time.deltaTime;
            visual.localScale = Vector3.Lerp(peak, baseScale, t / time);
            yield return null;
        }
        visual.localScale = baseScale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackReach * 0.6f, attackRadius);
    }
}
