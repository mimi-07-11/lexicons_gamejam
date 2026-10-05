using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Temporary stand-in for the 3D arena so you can test the whole flow today.
// Put it on any object in a new scene called "Arena1" and add that scene to Build Profiles > Scene List.
public class ArenaStub : MonoBehaviour
{
    public string returnScene = "Level1";

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.fKey.wasPressedThisFrame)
        {
            LightEnergy.Refill();                     // winning the fight gives a full bar
            SceneManager.LoadScene(returnScene);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 700, 40), "PLACEHOLDER ARENA - press F to win the fight and return with a full Light bar.");
    }
}
