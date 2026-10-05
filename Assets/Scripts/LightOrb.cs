using UnityEngine;

// Pops out of a dead enemy, then flies to the player by itself and fills the light bar.
public class LightOrb : MonoBehaviour
{
    public float value = 0.1f;
    float t;
    Vector3 popVel;

    public static void Spawn(GameObject prefab, Vector3 pos, float value)
    {
        GameObject go;
        if (prefab != null) go = Instantiate(prefab, pos, Quaternion.identity);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.35f;
            MatColor.Set(go.GetComponent<Renderer>(), new Color(1f, 0.9f, 0.2f));
        }
        var orb = go.GetComponent<LightOrb>();
        if (orb == null) orb = go.AddComponent<LightOrb>();
        orb.value = value;
    }

    void Start()
    {
        popVel = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(2f, 3.5f), Random.Range(-1.5f, 1.5f));
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t < 0.5f)
        {
            transform.position += popVel * Time.deltaTime;
            popVel += Vector3.down * 8f * Time.deltaTime;
            return;
        }
        var p = FighterController.Instance;
        if (p == null) return;
        Vector3 target = p.transform.position + Vector3.up * 0.8f;
        float speed = Mathf.Lerp(5f, 25f, Mathf.Clamp01((t - 0.5f) / 1f));
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude < 0.16f)
        {
            LightEnergy.Add(value);
            AudioManager.Play("orb", 0.7f, 0.15f);
            Destroy(gameObject);
        }
    }
}
