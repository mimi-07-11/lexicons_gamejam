using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[System.Serializable] public class SpawnGroup { public GameObject prefab; public int count = 1; }
[System.Serializable] public class Wave { public string label = "Wave"; public SpawnGroup[] groups; }

// Runs the whole arena: waves -> win -> light refill -> exit portal. Also draws a quick HUD (no Canvas needed).
// Put ONE of these in each arena scene (empty GameObject "ArenaManager").
public class ArenaManager : MonoBehaviour
{
    [Header("Waves")]
    public Wave[] waves;
    public int maxAlive = 3;                    // never more than this many enemies at once
    public float timeBetweenWaves = 2.5f;
    [Range(0f, 1f)] public float healBetweenWaves = 0.25f;   // gives back this fraction of weapon HP

    [Header("Spawning")]
    public Transform[] spawnPoints;             // empty? uses a ring around this object
    public float ringRadius = 10f;
    public float minDistanceFromPlayer = 5f;

    [Header("Win")]
    public bool isFinalArena = false;           // Arena2 = true
    public GameObject exitPortal;               // disabled in the scene; enabled on win (has ScenePortal)
    public UnityEvent onWin;                    // hook the "Light" dialogue here (step 6)
    public string winMessage = "Light collected! Step into the portal.";

    string msg = ""; float msgUntil;
    int waveIndex; bool won;

    void Start()
    {
        if (exitPortal != null) exitPortal.SetActive(false);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(1f);
        for (waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            Show(waves[waveIndex].label + " " + (waveIndex + 1) + "/" + waves.Length, 2f);
            AudioManager.Play("wave_start");
            yield return new WaitForSeconds(1.2f);

            var queue = new Queue<GameObject>();
            foreach (var g in waves[waveIndex].groups)
                for (int i = 0; i < g.count; i++) queue.Enqueue(g.prefab);

            while (queue.Count > 0)
            {
                if (EnemyAI.Alive < maxAlive) { Spawn(queue.Dequeue()); yield return new WaitForSeconds(0.6f); }
                else yield return null;
            }
            while (EnemyAI.Alive > 0) yield return null;

            if (waveIndex < waves.Length - 1)
            {
                WeaponLoadout.Heal(healBetweenWaves);
                Show("Wave cleared!", 2f);
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
        Win();
    }

    void Spawn(GameObject prefab)
    {
        Vector3 pos = transform.position;
        var player = FighterController.Instance;
        for (int tries = 0; tries < 10; tries++)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
                pos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            else
            {
                Vector2 c = Random.insideUnitCircle.normalized * ringRadius;
                pos = transform.position + new Vector3(c.x, 0f, c.y);
            }
            if (player == null || Vector3.Distance(pos, player.transform.position) >= minDistanceFromPlayer) break;
        }
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pos, out hit, 4f, NavMesh.AllAreas)) pos = hit.position;
        Instantiate(prefab, pos, Quaternion.identity);
    }

    void Win()
    {
        won = true;
        LightEnergy.Refill();
        AudioManager.Play("win");
        if (isFinalArena) { GameProgress.Arena2Won = true; GameProgress.FinalLightCarried = true; }
        else GameProgress.Arena1Won = true;
        Show(winMessage, 6f);
        if (exitPortal != null) exitPortal.SetActive(true);
        onWin.Invoke();
    }

    void Show(string m, float t) { msg = m; msgUntil = Time.time + t; }

    // ---------- quick HUD ----------
    void Bar(Rect r, float fill, Color c, string label)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = c; GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(fill), r.height - 4), Texture2D.whiteTexture);
        GUI.color = Color.white; GUI.Label(new Rect(r.x + 6, r.y - 1, r.width, r.height), label);
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        float maxHp = WeaponLoadout.Current.maxHp;
        Bar(new Rect(20, 20, 260, 24), maxHp > 0 ? WeaponLoadout.Hp / maxHp : 0f,
            new Color(0.95f, 0.45f, 0.2f), "Weapon: " + WeaponLoadout.Current.name);
        Bar(new Rect(20, 50, 260, 24), LightEnergy.Current, new Color(1f, 0.9f, 0.25f), "Light");

        if (waves != null && !won && waveIndex < waves.Length)
            GUI.Label(new Rect(20, 80, 300, 24), waves[waveIndex].label + " " + (waveIndex + 1) + "/" + waves.Length + "   Enemies: " + EnemyAI.Alive);

        if (Time.time < msgUntil)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 60), msg, style);
        }
        if (FighterController.Instance != null && WeaponLoadout.Hp <= 0f)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60), "Your weapon broke! Restarting...", style);
        }
    }
}
