using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    public string weaponName = "GoldSquare";
    
    // The TutorialManager listens for this exact event
    public static event Action<string> Acquired;

    void OnTriggerEnter2D(Collider2D c)
    {
        // Make sure it only triggers when the Player touches it
        if (c.CompareTag("Player"))
        {
            // Shout out to the Tutorial Manager that we got it
            if (Acquired != null) Acquired(weaponName);
            
            // Remove the weapon from the map
            Destroy(gameObject);
        }
    }
}