using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class TwistPortal : MonoBehaviour
{
    public string arenaScene = "Arena";         // First trip (refill)
    public string finalArenaScene = "Arena2";   // Second trip (exit level)
    public bool unlocked = true;
    public float spinSpeed = -40f;

    public static event Action Entered;    
    public static event Action Blocked;    

    // Listen for the weapon being collected
    void OnEnable() { WeaponPickup.Acquired += OnWeaponCollected; }
    void OnDisable() { WeaponPickup.Acquired -= OnWeaponCollected; }

    // When the weapon is grabbed, swap the destination!
    void OnWeaponCollected(string weaponName) 
    { 
        arenaScene = finalArenaScene; 
    }

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!c.CompareTag("Player")) return;
        if (!unlocked) { if (Blocked != null) Blocked(); return; }
        if (Entered != null) Entered();
        SceneManager.LoadScene(arenaScene);
    }
}