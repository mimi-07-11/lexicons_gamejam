using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class ClueNpc : MonoBehaviour
{
    public Tilemap darkOverlay;
    public GameObject bubble; // Drag your Canvas or Bubble object here

    // Added for TutorialManager compatibility
    public bool IsLit { get; private set; }
    public bool PlayerInRange { get; private set; }
    public static event Action<ClueNpc> TalkFinished;

    private SpriteRenderer sr;
    private bool tutorialEventFired = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (darkOverlay != null)
        {
            Vector3Int cellPosition = darkOverlay.WorldToCell(transform.position);
            IsLit = !darkOverlay.HasTile(cellPosition);

            if (sr != null) sr.enabled = IsLit;
            if (bubble != null) bubble.SetActive(IsLit);

            // Auto-advance the tutorial when the player is near the lit NPC
            if (IsLit && PlayerInRange && !tutorialEventFired)
            {
                tutorialEventFired = true;
                if (TalkFinished != null) TalkFinished(this);
            }
        }
    }

    // Detects when the player gets close
    void OnTriggerEnter2D(Collider2D c) { if (c.CompareTag("Player")) PlayerInRange = true; }
    void OnTriggerExit2D(Collider2D c)  { if (c.CompareTag("Player")) PlayerInRange = false; }
}