// สร้างฉากทดสอบแรก (บ่อปลาทุ่งนาแบบย่อ) จากโมเดล Blender ที่ส่งออกเป็น assets.glb
//   Unity.exe -batchmode -projectPath . -executeMethod SceneBuilder.CreatePaddyTest -quit
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SceneBuilder
{
    const string ModelPath = "Assets/Game/Models/assets.glb";
    const string ScenePath = "Assets/Game/Scenes/PaddyTest.unity";

    public static void CreatePaddyTest()
    {
        Directory.CreateDirectory("Assets/Game/Scenes");
        Directory.CreateDirectory("Assets/Game/Materials");
        AssetDatabase.Refresh();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) { Debug.LogError("[SceneBuilder] model not imported: " + ModelPath); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Material Mat(string name, Color c, float smooth = 0.1f)
        {
            var path = $"Assets/Game/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }

        // พื้นหญ้า + ผิวน้ำบ่อ (ชั่วคราว จะเปลี่ยนเป็น terrain + shader น้ำแบบเว็บในขั้นต่อไป)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(8, 1, 8);
        ground.GetComponent<Renderer>().sharedMaterial = Mat("Grass", new Color(0.53f, 0.74f, 0.30f));
        var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = "Pond";
        water.transform.position = new Vector3(0, -0.02f, -6);
        water.transform.localScale = new Vector3(20, 0.04f, 16);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.GetComponent<Renderer>().sharedMaterial = Mat("Water", new Color(0.24f, 0.55f, 0.48f), 0.9f);

        // โมเดลจาก Blender: ดึงทีละชิ้นตามชื่อ node
        void Place(string node, Vector3 pos, float yaw = 0, float scale = 1)
        {
            var src = FindDeep(model.transform, node);
            if (src == null) { Debug.LogWarning("[SceneBuilder] node not found: " + node); return; }
            var go = Object.Instantiate(src.gameObject);
            go.name = node;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
        }
        Place("Fisher", new Vector3(1.2f, 0, 4.5f), 180);
        Place("Dock", new Vector3(-3.2f, 0.27f, 5.5f), 180);
        Place("Hut", new Vector3(-9f, 0, 6f), 30, 1.1f);
        Place("Tree1", new Vector3(-7, 0, -1), 0, 1.3f);
        Place("Tree2", new Vector3(9, 0, 2), 40, 1.1f);
        Place("Pine1", new Vector3(12, 0, -8), 0, 1.2f);
        Place("Palm", new Vector3(7, 0, 8), 90);
        Place("Boulder1", new Vector3(-5.5f, 0, -1.5f), 20, 0.8f);
        Place("Boulder3", new Vector3(5.8f, 0, -2.2f), 70, 0.7f);
        Place("Bush", new Vector3(4, 0, 7), 0, 0.9f);

        // แสงแดด + กล้องมุมสูงแบบ diorama
        var sun = GameObject.Find("Directional Light");
        sun.transform.rotation = Quaternion.Euler(48, -35, 0);
        sun.GetComponent<Light>().color = new Color(1f, 0.95f, 0.84f);
        sun.GetComponent<Light>().intensity = 1.6f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.86f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.72f, 0.58f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.42f, 0.28f);
        var cam = Camera.main;
        cam.transform.SetPositionAndRotation(new Vector3(0, 13, 17), Quaternion.Euler(38, 180, 0));
        cam.transform.LookAt(new Vector3(0, 0, -1));
        cam.fieldOfView = 40;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.78f, 0.93f);
        cam.gameObject.AddComponent<CameraOrbit>();

        // ป้ายภาษาไทยทดสอบ (UI Toolkit)
        new GameObject("ThaiTextTest").AddComponent<ThaiTextTest>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[SceneBuilder] saved " + ScenePath);
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; }
        return null;
    }
}
