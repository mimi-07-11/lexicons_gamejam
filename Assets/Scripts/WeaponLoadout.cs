using System;
using System.Collections.Generic;
using UnityEngine;

// Remembers which weapon the player picked up in 2D and tracks its HP in 3D.
// Add new weapons to the table below. Keys must match WeaponPickup.weaponName.
public static class WeaponLoadout
{
    public struct Stats
    {
        public string name; public float damage; public float maxHp;
        public Stats(string n, float d, float h) { name = n; damage = d; maxHp = h; }
    }

    static readonly Dictionary<string, Stats> table = new Dictionary<string, Stats>
    {
        { "Starter",    new Stats("Starter",    10f, 100f) },
        { "GoldSquare", new Stats("GoldSquare", 20f, 160f) },
        { "Super Comic Weapon", new Stats("Super Comic Weapon", 20f, 160f) },
        // { "NextWeapon", new Stats("NextWeapon", 30f, 200f) },
    };

    public static Stats Current { get; private set; }
    public static float Hp { get; private set; }
    public static event Action<float, float> Changed;   // (hp, maxHp) - the weapon bar listens

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Current = table["Starter"]; Hp = Current.maxHp; Changed = null;
    }

    // Subscribes once to the 2D pickup event; survives scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() { WeaponPickup.Acquired += Equip; }

    public static void ResetToStarter()
    {
        Current = table["Starter"]; Hp = Current.maxHp;
        if (Changed != null) Changed(Hp, Current.maxHp);
    }

    public static void Equip(string weaponName)
    {
        Stats s;
        if (!table.TryGetValue(weaponName, out s))
        {
            Debug.LogWarning("WeaponLoadout: '" + weaponName + "' not in table, using GoldSquare stats.");
            s = table["GoldSquare"];
        }
        Current = s; Hp = s.maxHp;
        AudioManager.Play("pickup");
        if (Changed != null) Changed(Hp, Current.maxHp);
    }

    public static void Damage(float amount)
    {
        Hp = Mathf.Max(0f, Hp - amount);
        if (Changed != null) Changed(Hp, Current.maxHp);
    }

    public static void Heal(float fractionOfMax)
    {
        Hp = Mathf.Min(Current.maxHp, Hp + Current.maxHp * fractionOfMax);
        if (Changed != null) Changed(Hp, Current.maxHp);
    }

    public static void RestoreFull()
    {
        Hp = Current.maxHp;
        if (Changed != null) Changed(Hp, Current.maxHp);
    }
}
