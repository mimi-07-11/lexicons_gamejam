using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Menu: Twist > Polish > ...  (put this file in Assets/Editor)
// Builds the robot, Fun Police grunt/captain and Light models from primitives (no art files needed),
// applies them to your Player / enemy prefabs / Light, and gives the arena a bland ground.
public static class PolishBuilder
{
    const string MatDir = "Assets/Material/Generated";
    const string ModelDir = "Assets/Prefabs/Models";
    const string ArtDir = "Assets/Art/Generated";

    // ---------------------------------------------------------------- helpers
    static Color Col(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f, 1f); }
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    static void Ensure(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static Material Mat(string name, Color c, float emission = 0f, float smooth = 0.3f)
    {
        Ensure(MatDir);
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default(Vector3))
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = name;
        Collider col = g.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = scale;
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return g;
    }

    static Transform Pivot(string name, Transform parent, Vector3 pos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    static void Save(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, ModelDir + "/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }

    // ---------------------------------------------------------------- 1. build models
    [MenuItem("Twist/Polish/1 Build Model Prefabs")]
    public static void BuildAll()
    {
        Ensure(MatDir);
        Ensure(ModelDir);
        Save(BuildRobot(), "RobotModel");
        Save(BuildHumanoid(false), "GruntModel");
        Save(BuildHumanoid(true), "CaptainModel");
        Save(BuildLight(), "LightModel");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Twist Polish: built RobotModel, GruntModel, CaptainModel, LightModel in " + ModelDir);
    }

    static GameObject BuildRobot()
    {
        Material white = Mat("Poly_RobotWhite", Col(236, 241, 247));
        Material blue = Mat("Poly_RobotBlue", Col(48, 128, 232));
        Material orange = Mat("Poly_RobotOrange", Col(255, 140, 40));
        Material dark = Mat("Poly_RobotDark", Col(46, 52, 64));
        Material eye = Mat("Poly_RobotEye", Col(80, 240, 255), 2f);
        Material bulb = Mat("Poly_RobotBulb", Col(255, 225, 80), 2.5f);
        Material paper = Mat("Poly_Paper", Col(240, 236, 220));
        Material ink = Mat("Poly_Ink", Col(30, 30, 36));
        Material wood = Mat("Poly_Wood", Col(150, 95, 45));
        Material gold = Mat("Poly_HammerGold", Col(255, 196, 40), 0.4f, 0.5f);

        GameObject root = new GameObject("RobotModel");
        root.AddComponent<FlashTint>();
        root.AddComponent<ModelAnimator>();
        WeaponVisual wv = root.AddComponent<WeaponVisual>();

        float hip = 0.7f;
        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? -1f : 1f;
            Transform leg = Pivot(i == 0 ? "LegL" : "LegR", root.transform, V(s * 0.22f, hip, 0f));
            Prim(PrimitiveType.Cube, "Leg", leg, V(0f, -0.32f, 0f), V(0.24f, 0.62f, 0.28f), blue);
            Prim(PrimitiveType.Cube, "Foot", leg, V(0f, -0.65f, 0.06f), V(0.3f, 0.14f, 0.44f), dark);
        }

        Transform body = Pivot("Body", root.transform, V(0f, hip, 0f));
        Prim(PrimitiveType.Cube, "Torso", body, V(0f, 0.42f, 0f), V(0.86f, 0.76f, 0.5f), white);
        Prim(PrimitiveType.Cube, "Belt", body, V(0f, 0.06f, 0f), V(0.9f, 0.1f, 0.54f), orange);
        Prim(PrimitiveType.Cube, "ChestPanel", body, V(0f, 0.5f, 0.255f), V(0.38f, 0.38f, 0.04f), dark);
        Prim(PrimitiveType.Sphere, "ChestLight", body, V(0f, 0.5f, 0.26f), V(0.24f, 0.24f, 0.1f), bulb);
        Prim(PrimitiveType.Cube, "Neck", body, V(0f, 0.82f, 0f), V(0.2f, 0.12f, 0.2f), dark);

        Transform head = Pivot("Head", body, V(0f, 1.12f, 0f));
        Prim(PrimitiveType.Cube, "HeadBox", head, V(0f, 0f, 0f), V(0.62f, 0.5f, 0.56f), white);
        Prim(PrimitiveType.Cube, "Visor", head, V(0f, 0f, 0.29f), V(0.5f, 0.22f, 0.06f), dark);
        Prim(PrimitiveType.Cube, "EyeL", head, V(-0.12f, 0f, 0.325f), V(0.12f, 0.09f, 0.03f), eye);
        Prim(PrimitiveType.Cube, "EyeR", head, V(0.12f, 0f, 0.325f), V(0.12f, 0.09f, 0.03f), eye);
        Prim(PrimitiveType.Cylinder, "EarL", head, V(-0.34f, 0f, 0f), V(0.22f, 0.05f, 0.22f), orange, V(0f, 0f, 90f));
        Prim(PrimitiveType.Cylinder, "EarR", head, V(0.34f, 0f, 0f), V(0.22f, 0.05f, 0.22f), orange, V(0f, 0f, 90f));
        Prim(PrimitiveType.Cylinder, "Antenna", head, V(0f, 0.34f, 0f), V(0.05f, 0.1f, 0.05f), dark);
        Prim(PrimitiveType.Sphere, "AntennaBulb", head, V(0f, 0.5f, 0f), V(0.16f, 0.16f, 0.16f), bulb);

        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? -1f : 1f;
            Transform arm = Pivot(i == 0 ? "ArmL" : "ArmR", body, V(s * 0.56f, 0.7f, 0f));
            Prim(PrimitiveType.Sphere, "Shoulder", arm, V(0f, 0f, 0f), V(0.3f, 0.3f, 0.3f), orange);
            Prim(PrimitiveType.Cube, "Arm", arm, V(0f, -0.3f, 0f), V(0.2f, 0.52f, 0.22f), white);
            Transform hand = Pivot(i == 0 ? "HandL" : "HandR", arm, V(0f, -0.62f, 0f));
            Prim(PrimitiveType.Sphere, "Hand", hand, V(0f, 0f, 0f), V(0.24f, 0.24f, 0.24f), dark);

            if (i == 1)
            {
                // Starter: rolled newspaper (handle runs along local Z)
                Transform w1 = Pivot("Weapon_Starter", hand, V(0f, 0f, 0f));
                Prim(PrimitiveType.Cylinder, "Paper", w1, V(0f, 0f, 0.38f), V(0.15f, 0.4f, 0.15f), paper, V(90f, 0f, 0f));
                Prim(PrimitiveType.Cylinder, "BandA", w1, V(0f, 0f, 0.15f), V(0.16f, 0.015f, 0.16f), ink, V(90f, 0f, 0f));
                Prim(PrimitiveType.Cylinder, "BandB", w1, V(0f, 0f, 0.38f), V(0.16f, 0.015f, 0.16f), ink, V(90f, 0f, 0f));
                Prim(PrimitiveType.Cylinder, "BandC", w1, V(0f, 0f, 0.61f), V(0.16f, 0.015f, 0.16f), ink, V(90f, 0f, 0f));

                // Super Comic Weapon: golden hammer
                Transform w2 = Pivot("Weapon_SuperComic", hand, V(0f, 0f, 0f));
                Prim(PrimitiveType.Cylinder, "Handle", w2, V(0f, 0f, 0.4f), V(0.09f, 0.45f, 0.09f), wood, V(90f, 0f, 0f));
                Prim(PrimitiveType.Cube, "Head", w2, V(0f, 0f, 0.92f), V(0.34f, 0.78f, 0.34f), gold);
                Prim(PrimitiveType.Cube, "CapTop", w2, V(0f, 0.4f, 0.92f), V(0.38f, 0.1f, 0.38f), orange);
                Prim(PrimitiveType.Cube, "CapBottom", w2, V(0f, -0.4f, 0.92f), V(0.38f, 0.1f, 0.38f), orange);
                Prim(PrimitiveType.Cube, "StarAR", w2, V(0.175f, 0f, 0.92f), V(0.02f, 0.3f, 0.07f), white);
                Prim(PrimitiveType.Cube, "StarBR", w2, V(0.175f, 0f, 0.92f), V(0.02f, 0.07f, 0.3f), white);
                Prim(PrimitiveType.Cube, "StarAL", w2, V(-0.175f, 0f, 0.92f), V(0.02f, 0.3f, 0.07f), white);
                Prim(PrimitiveType.Cube, "StarBL", w2, V(-0.175f, 0f, 0.92f), V(0.02f, 0.07f, 0.3f), white);
                w2.gameObject.SetActive(false);

                wv.starter = w1.gameObject;
                wv.superComic = w2.gameObject;
            }
        }
        return root;
    }

    static GameObject BuildHumanoid(bool captain)
    {
        string p = captain ? "Poly_Cap" : "Poly_Grunt";
        Material uniform = Mat(p + "Uniform", captain ? Col(52, 54, 68) : Col(66, 88, 142));
        Material dark = Mat(p + "Dark", captain ? Col(30, 30, 38) : Col(36, 46, 82));
        Material skin = Mat("Poly_Skin", Col(236, 201, 171));
        Material black = Mat("Poly_Black", Col(24, 24, 30), 0f, 0.5f);
        Material gold = Mat("Poly_Gold", Col(240, 200, 60), 0f, 0.5f);
        Material glove = Mat("Poly_Glove", Col(236, 236, 242));
        Material red = Mat("Poly_Red", Col(190, 40, 50));
        Material white = Mat("Poly_White", Col(245, 245, 248));
        Material mouth = Mat("Poly_Mouth", Col(110, 50, 50));

        GameObject root = new GameObject(captain ? "CaptainModel" : "GruntModel");
        root.AddComponent<FlashTint>();
        root.AddComponent<ModelAnimator>();

        float legLen = 0.7f;
        float tw = captain ? 1.05f : 0.9f;
        float th = captain ? 0.85f : 0.75f;
        float td = captain ? 0.62f : 0.52f;

        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? -1f : 1f;
            Transform leg = Pivot(i == 0 ? "LegL" : "LegR", root.transform, V(s * tw * 0.27f, legLen, 0f));
            Prim(PrimitiveType.Cube, "Leg", leg, V(0f, -0.32f, 0f), V(0.28f, 0.62f, 0.3f), dark);
            Prim(PrimitiveType.Cube, "Boot", leg, V(0f, -0.65f, 0.05f), V(0.32f, 0.14f, 0.44f), black);
        }

        Transform body = Pivot("Body", root.transform, V(0f, legLen, 0f));
        Prim(PrimitiveType.Cube, "Torso", body, V(0f, th * 0.5f + 0.02f, 0f), V(tw, th, td), uniform);
        Prim(PrimitiveType.Cube, "Belt", body, V(0f, 0.06f, 0f), V(tw + 0.04f, 0.1f, td + 0.04f), black);
        Prim(PrimitiveType.Cube, "Tie", body, V(0f, th * 0.55f, td * 0.5f + 0.01f), V(0.1f, th * 0.6f, 0.03f), black);
        Prim(PrimitiveType.Cube, "Badge", body, V(tw * 0.25f, th * 0.72f, td * 0.5f + 0.01f), V(0.13f, 0.13f, 0.04f), gold);

        if (captain)
        {
            Prim(PrimitiveType.Cube, "Sash", body, V(0f, th * 0.5f, td * 0.5f + 0.02f), V(0.14f, th * 1.15f, 0.03f), red, V(0f, 0f, 35f));
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Sphere, "Button" + i, body, V(-tw * 0.22f, 0.25f + i * 0.2f, td * 0.5f + 0.02f), V(0.08f, 0.08f, 0.08f), gold);
            Prim(PrimitiveType.Cube, "EpauletteL", body, V(-(tw * 0.5f + 0.13f), th + 0.02f, 0f), V(0.3f, 0.08f, 0.34f), gold);
            Prim(PrimitiveType.Cube, "EpauletteR", body, V(tw * 0.5f + 0.13f, th + 0.02f, 0f), V(0.3f, 0.08f, 0.34f), gold);
        }

        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? -1f : 1f;
            Transform arm = Pivot(i == 0 ? "ArmL" : "ArmR", body, V(s * (tw * 0.5f + 0.13f), th - 0.05f, 0f));
            Prim(PrimitiveType.Cube, "Sleeve", arm, V(0f, -0.3f, 0f), V(0.24f, 0.62f, 0.26f), uniform);
            Transform hand = Pivot(i == 0 ? "HandL" : "HandR", arm, V(0f, -0.65f, 0f));
            Prim(PrimitiveType.Sphere, "Glove", hand, V(0f, 0f, 0f), V(0.24f, 0.24f, 0.24f), glove);

            if (i == 1)
            {
                Transform w = Pivot("Weapon", hand, V(0f, 0f, 0f));
                if (captain)
                {
                    // big "NO FUN" sign on a pole
                    Prim(PrimitiveType.Cylinder, "Pole", w, V(0f, 0f, 0.5f), V(0.07f, 0.5f, 0.07f), black, V(90f, 0f, 0f));
                    Prim(PrimitiveType.Cylinder, "SignBorder", w, V(0f, 0f, 1.05f), V(0.8f, 0.025f, 0.8f), white, V(90f, 0f, 0f));
                    Prim(PrimitiveType.Cylinder, "SignRed", w, V(0f, 0f, 1.07f), V(0.68f, 0.04f, 0.68f), red, V(90f, 0f, 0f));
                    Prim(PrimitiveType.Cube, "SignBar", w, V(0f, 0f, 1.1f), V(0.46f, 0.1f, 0.02f), white);
                }
                else
                {
                    // baton
                    Prim(PrimitiveType.Cylinder, "Baton", w, V(0f, 0f, 0.36f), V(0.1f, 0.38f, 0.1f), black, V(90f, 0f, 0f));
                    Prim(PrimitiveType.Cylinder, "BatonBand", w, V(0f, 0f, 0.55f), V(0.11f, 0.03f, 0.11f), white, V(90f, 0f, 0f));
                }
            }
        }

        // head: skin cube, sunglasses, frown, cap
        Prim(PrimitiveType.Cube, "Head", body, V(0f, th + 0.33f, 0f), V(0.56f, 0.5f, 0.52f), skin);
        Prim(PrimitiveType.Cube, "Sunglasses", body, V(0f, th + 0.38f, 0.27f), V(0.6f, 0.16f, 0.1f), black);
        Prim(PrimitiveType.Cube, "Frown", body, V(0f, th + 0.2f, 0.27f), V(0.22f, 0.04f, 0.04f), mouth);
        Prim(PrimitiveType.Cube, "CapTop", body, V(0f, th + (captain ? 0.64f : 0.6f), 0f), captain ? V(0.7f, 0.22f, 0.68f) : V(0.62f, 0.16f, 0.6f), dark);
        Prim(PrimitiveType.Cube, "CapVisor", body, V(0f, th + 0.55f, 0.36f), V(0.5f, 0.04f, 0.3f), black);
        Prim(PrimitiveType.Cube, "CapBadge", body, V(0f, th + 0.62f, captain ? 0.35f : 0.31f), V(0.14f, 0.14f, 0.04f), gold);
        if (captain)
        {
            Prim(PrimitiveType.Cube, "CapBand", body, V(0f, th + 0.53f, 0f), V(0.72f, 0.06f, 0.7f), gold);
            Prim(PrimitiveType.Cube, "Mustache", body, V(0f, th + 0.27f, 0.275f), V(0.3f, 0.07f, 0.05f), black);
        }
        return root;
    }

    static GameObject BuildLight()
    {
        Material gold = Mat("Poly_LightBody", Col(255, 214, 70), 0.8f, 0.5f);
        Material headMat = Mat("Poly_LightHead", Col(255, 247, 200), 1.6f, 0.5f);
        Material gray = Mat("Poly_LightCollar", Col(150, 150, 160));
        Material black = Mat("Poly_Black", Col(24, 24, 30), 0f, 0.5f);
        Material orange = Mat("Poly_LightFilament", Col(255, 150, 30), 1.5f);
        Material rayMat = Mat("Poly_LightRay", Col(255, 235, 120), 2f);
        Material red = Mat("Poly_Red", Col(190, 40, 50));

        GameObject root = new GameObject("LightModel");
        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? -1f : 1f;
            Prim(PrimitiveType.Cube, "Leg", root.transform, V(s * 0.14f, 0.25f, 0f), V(0.18f, 0.5f, 0.2f), gold);
            Prim(PrimitiveType.Cube, "Shoe", root.transform, V(s * 0.14f, 0.04f, 0.05f), V(0.22f, 0.08f, 0.3f), black);
            Prim(PrimitiveType.Cube, "Arm", root.transform, V(s * 0.42f, 0.85f, 0f), V(0.14f, 0.55f, 0.16f), gold, V(0f, 0f, -s * 20f));
        }
        Prim(PrimitiveType.Cube, "Body", root.transform, V(0f, 0.85f, 0f), V(0.55f, 0.75f, 0.38f), gold);
        Prim(PrimitiveType.Cube, "Tie", root.transform, V(0f, 0.95f, 0.2f), V(0.08f, 0.3f, 0.03f), red);
        Prim(PrimitiveType.Cylinder, "Collar", root.transform, V(0f, 1.26f, 0f), V(0.32f, 0.07f, 0.32f), gray);
        Prim(PrimitiveType.Sphere, "Head", root.transform, V(0f, 1.66f, 0f), V(0.75f, 0.75f, 0.75f), headMat);
        Prim(PrimitiveType.Sphere, "EyeL", root.transform, V(-0.15f, 1.7f, 0.34f), V(0.1f, 0.1f, 0.06f), black);
        Prim(PrimitiveType.Sphere, "EyeR", root.transform, V(0.15f, 1.7f, 0.34f), V(0.1f, 0.1f, 0.06f), black);
        Prim(PrimitiveType.Cube, "Smile", root.transform, V(0f, 1.52f, 0.34f), V(0.2f, 0.03f, 0.03f), black);
        Prim(PrimitiveType.Cube, "FilamentA", root.transform, V(-0.1f, 1.95f, 0.05f), V(0.04f, 0.12f, 0.03f), orange, V(0f, 0f, 25f));
        Prim(PrimitiveType.Cube, "FilamentB", root.transform, V(0f, 1.95f, 0.05f), V(0.04f, 0.12f, 0.03f), orange, V(0f, 0f, -25f));
        Prim(PrimitiveType.Cube, "FilamentC", root.transform, V(0.1f, 1.95f, 0.05f), V(0.04f, 0.12f, 0.03f), orange, V(0f, 0f, 25f));

        Transform rays = Pivot("Rays", root.transform, V(0f, 1.66f, -0.15f));
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Cube, "Ray" + i, rays, V(Mathf.Cos(a) * 0.65f, Mathf.Sin(a) * 0.65f, 0f), V(0.05f, 0.34f, 0.04f), rayMat, V(0f, 0f, i * 36f - 90f));
        }

        GameObject lg = new GameObject("LightGlow");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = V(0f, 1.7f, 0.4f);
        Light l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.92f, 0.55f);
        l.range = 7f;
        l.intensity = 2.5f;
        l.shadows = LightShadows.None;
        return root;
    }

    // ---------------------------------------------------------------- 2. apply to selection
    [MenuItem("Twist/Polish/2 Apply Models To Selection")]
    public static void ApplyToSelection()
    {
        GameObject[] sel = Selection.gameObjects;
        if (sel.Length == 0)
        {
            EditorUtility.DisplayDialog("Twist Polish", "Select first: the 3D Player, the enemy PREFABS (Project window), or the Light character.", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/RobotModel.prefab") == null) BuildAll();

        int n = 0;
        foreach (GameObject go in sel)
        {
            string path = AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                if (Apply(contents)) { PrefabUtility.SaveAsPrefabAsset(contents, path); n++; }
                PrefabUtility.UnloadPrefabContents(contents);
            }
            else if (Apply(go))
            {
                n++;
                EditorUtility.SetDirty(go);
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Twist Polish: model applied to " + n + " object(s).");
    }

    static GameObject Load(string name) { return AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/" + name + ".prefab"); }

    static bool Apply(GameObject go)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(go))
        {
            Debug.LogWarning("Twist Polish: '" + go.name + "' is a prefab instance. Select the prefab ASSET in the Project window instead.");
            return false;
        }

        FighterController fighter = go.GetComponent<FighterController>();
        EnemyAI enemy = go.GetComponent<EnemyAI>();
        GameObject src = null;
        float scale = 1f;

        if (fighter != null) src = Load("RobotModel");
        else if (enemy != null)
        {
            bool captain = enemy.maxHp >= 100f;
            src = Load(captain ? "CaptainModel" : "GruntModel");
            if (captain && go.transform.lossyScale.y < 1.3f) scale = 1.5f;
        }
        else if (go.GetComponent<Light>() == null && go.name.ToLower().Contains("light")) src = Load("LightModel");

        if (src == null)
        {
            Debug.LogWarning("Twist Polish: skipped '" + go.name + "' (not a Player, enemy or Light character).");
            return false;
        }

        Transform t = go.transform;
        Transform old = t.Find("Model");
        float feet = FeetLocal(go);            // measure before hiding the old mesh
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src, t);
        inst.name = "Model";
        inst.transform.localPosition = V(0f, feet, 0f);
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one * scale;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;     // hide the old placeholder capsule

        ClearVisual(fighter);
        ClearVisual(enemy);
        return true;
    }

    // local Y of the model's feet inside the object
    static float FeetLocal(GameObject go)
    {
        float ls = Mathf.Max(0.01f, go.transform.lossyScale.y);
        CharacterController cc = go.GetComponent<CharacterController>();
        if (cc != null) return cc.center.y - cc.height * 0.5f;
        NavMeshAgent ag = go.GetComponent<NavMeshAgent>();
        if (ag != null) return -ag.baseOffset / ls;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mr.enabled) return (mr.bounds.min.y - go.transform.position.y) / ls;
        return 0f;
    }

    static void ClearVisual(Component c)
    {
        if (c == null) return;
        SerializedObject so = new SerializedObject(c);
        SerializedProperty p = so.FindProperty("visual");
        if (p != null)
        {
            p.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ---------------------------------------------------------------- 3. ground + walls
    [MenuItem("Twist/Polish/3 Apply Bland Ground And Walls")]
    public static void ApplyGround()
    {
        string texPath = ArtDir + "/ground_bland.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Twist Polish", "Put ground_bland.png into " + ArtDir + " first.", "OK");
            return;
        }
        TextureImporter imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && (imp.wrapMode != TextureWrapMode.Repeat || imp.anisoLevel < 4))
        {
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        GameObject floor = GameObject.Find("Floor");
        if (floor == null) Debug.LogWarning("Twist Polish: no object named 'Floor' in this scene.");
        else
        {
            Material m = Mat("Poly_GroundBland", Color.white, 0f, 0.15f);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); else m.mainTexture = tex;
            Vector3 fs = floor.transform.localScale;
            Vector2 rep = new Vector2(Mathf.Max(1f, fs.x / 8f), Mathf.Max(1f, fs.z / 8f));
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", rep); else m.mainTextureScale = rep;
            floor.GetComponent<Renderer>().sharedMaterial = m;
            EditorUtility.SetDirty(m);
        }

        Material wallMat = Mat("Poly_WallBland", Col(204, 207, 214), 0f, 0.2f);
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform tr in all)
        {
            if (!tr.name.StartsWith("Wall")) continue;
            Renderer r = tr.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = wallMat;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Col(228, 231, 236);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Twist Polish: bland ground and walls applied.");
    }

    // ---------------------------------------------------------------- 4. sprite import settings
    [MenuItem("Twist/Polish/4 Set 2D Sprite Import Settings")]
    public static void SpriteSettings()
    {
        string[] names = { "player_2d", "fun_2d", "light_2d", "weapon_supercomic_2d" };
        int n = 0;
        foreach (string nm in names)
        {
            string path = ArtDir + "/" + nm + ".png";
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("Twist Polish: missing " + path); continue; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 512f;     // 512 px = 1 unit tall
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            n++;
        }
        Debug.Log("Twist Polish: " + n + " sprite(s) configured (1 unit tall each; scale the object to resize).");
    }
}
