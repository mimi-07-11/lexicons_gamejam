using TMPro;
using UnityEngine;

public enum TutStage { Move, NeedLight, BackWithLight, GoToNpc, FindWeapon, TwistBack, Done }

// Remembers where we are in the tutorial while the 3D scene is loaded.
public static class TutorialState
{
    public static TutStage Stage = TutStage.Move;
    public static bool WentToArena;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Stage = TutStage.Move; WentToArena = false; }
}

// Level 1 teacher. Put it on an empty object (Tools > Maze > Build Level 1 creates and wires most of it).
public class TutorialManager : MonoBehaviour
{
    [Header("Scene objects")]
    public Transform player;
    public TwistPortal portal;
    public ClueNpc npc;
    public WeaponPickup weapon;
    public GuideArrow arrow;
    public Transform returnPoint;          // where the player reappears after the 3D fight

    [Header("UI")]
    public GameObject panel;               // background panel (can hold the text)
    public TMP_Text text;

    [Header("Tuning")]
    public float moveDistance = 3f;        // how far the player walks before we move on
    public float hintDelay = 30f;          // seconds before an arrow helps find the weapon
    public int sceneMessageSeconds = 3;

    TutStage stage;
    Vector3 startPos;
    float stageTime, overrideUntil;
    string stageMessage = "";
    bool shownTalkPrompt, shownHint;

    void OnEnable()
    {
        TwistPortal.Entered += OnPortalEntered;
        TwistPortal.Blocked += OnPortalBlocked;
        ClueNpc.TalkFinished += OnTalkFinished;
        WeaponPickup.Acquired += OnWeapon;
    }

    void OnDisable()
    {
        TwistPortal.Entered -= OnPortalEntered;
        TwistPortal.Blocked -= OnPortalBlocked;
        ClueNpc.TalkFinished -= OnTalkFinished;
        WeaponPickup.Acquired -= OnWeapon;
    }

    void Start()
    {
        startPos = player.position;
        if (TutorialState.WentToArena && TutorialState.Stage == TutStage.NeedLight)
        {
            if (returnPoint != null) player.position = returnPoint.position;   // back from the first fight
            TutorialState.WentToArena = false;
            Go(TutStage.BackWithLight);
        }
        else Go(TutorialState.Stage);
    }

    void Update()
    {
        // temporary messages (like "the portal is asleep") expire and the stage message returns
        if (overrideUntil > 0f && Time.time > overrideUntil) { overrideUntil = 0f; Show(stageMessage); }

        switch (stage)
        {
            case TutStage.Move:
                if (Vector3.Distance(player.position, startPos) >= moveDistance) Go(TutStage.NeedLight);
                break;

            case TutStage.BackWithLight:
                if (LightEnergy.Current < 0.999f) Go(TutStage.GoToNpc);       // the first light has been spent
                break;

            case TutStage.GoToNpc:
                if (!shownTalkPrompt && npc != null && npc.IsLit && npc.PlayerInRange)
                {
                    shownTalkPrompt = true;
                    SetStageMessage("Read the character's clue!");
                }
                break;

            case TutStage.FindWeapon:
                if (!shownHint && Time.time - stageTime > hintDelay)
                {
                    shownHint = true;
                    SetStageMessage("Stuck? Follow the arrow. Light the dark with SPACE as you go.");
                    if (weapon != null) Point(weapon.transform);
                }
                break;
        }
    }

    // ------------------------------------------------------------ stages
    void Go(TutStage s)
    {
        stage = s;
        TutorialState.Stage = s;
        stageTime = Time.time;
        bool portalOpen = false;

        switch (s)
        {
            case TutStage.Move:
                SetStageMessage("Use W A S D to move.");
                Point(null); portalOpen = true;
                break;
            case TutStage.NeedLight:
                SetStageMessage("It's pitch black ahead, and you can't walk into the dark.\nYou need Light! Step into the Twist portal, fight in 3D, and bring some back.");
                Point(portal != null ? portal.transform : null); portalOpen = true;
                break;
            case TutStage.BackWithLight:
                SetStageMessage("Your Light bar is full!\nPress SPACE to light up the dark around you. Light costs energy, so choose where to spend it.");
                Point(null);
                break;
            case TutStage.GoToNpc:
                SetStageMessage("Nice! Follow the arrow to the first dark area and light your way.");
                Point(npc != null ? npc.transform : null);
                break;
            case TutStage.FindWeapon:
                SetStageMessage("Clues are how you find things here.\nThe weapon is hiding in the other dark area. Light your way and find it!");
                Point(null); shownHint = false;
                break;
            case TutStage.TwistBack:
                SetStageMessage("You got the weapon! Step into the Twist portal and take it to 3D.");
                Point(portal != null ? portal.transform : null); portalOpen = true;
                break;
            case TutStage.Done:
                if (panel != null) panel.SetActive(false);
                Point(null);
                break;
        }
        if (portal != null) portal.unlocked = portalOpen;
    }

    void OnPortalEntered()
    {
        if (stage == TutStage.Move || stage == TutStage.NeedLight)
        {
            TutorialState.Stage = TutStage.NeedLight;
            TutorialState.WentToArena = true;
        }
        else if (stage == TutStage.TwistBack) TutorialState.Stage = TutStage.Done;
    }

    void OnPortalBlocked()
    {
        Temporary("The portal is asleep. Light your way and find the weapon first!");
    }

    void OnTalkFinished(ClueNpc who)
    {
        if (stage == TutStage.GoToNpc) Go(TutStage.FindWeapon);
    }

    void OnWeapon(string weaponName)
    {
        if (stage == TutStage.FindWeapon || stage == TutStage.GoToNpc) Go(TutStage.TwistBack);
    }

    // ------------------------------------------------------------ helpers
    void SetStageMessage(string msg) { stageMessage = msg; if (overrideUntil <= 0f) Show(msg); }

    void Temporary(string msg)
    {
        overrideUntil = Time.time + sceneMessageSeconds;
        Show(msg);
    }

    void Show(string msg)
    {
        if (panel != null) panel.SetActive(true);
        if (text != null) text.text = msg;
    }

    void Point(Transform t) { if (arrow != null) arrow.SetTarget(t); }
}
