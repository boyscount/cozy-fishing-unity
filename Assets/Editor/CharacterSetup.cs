// ตัวละครชาวประมงจาก Mixamo (Remy + ท่ายืน/เดิน/ถือคันเบ็ด/เหวี่ยงเบ็ด) — ตั้งค่า import แบบ Humanoid + สร้าง Animator Controller
// ไฟล์ FBX ดาวน์โหลดจาก mixamo.com (ใช้ในเกมได้ฟรี) ไว้ที่ Assets/Game/Models/Character
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterSetup
{
    public const string Dir = "Assets/Game/Models/Character";
    public const string ModelPath = Dir + "/remy_idle.fbx";
    const string ControllerPath = Dir + "/Fisher.controller";
    static readonly string[] Loops = { "remy_idle", "remy_walk", "remy_fishing_idle" };

    // Pose (ตั้งจาก FishingController ผ่าน PlayerController): 0 เดิน/ยืน, 1 ท่าเหวี่ยง/กรอสาย (ตำแหน่งในคลิปจาก CastT), 3 ถือคันรอปลา
    // ท่ากระโดดสั่งตรงจากโค้ด (CrossFade "Jump") แล้วกลับท่าเดินเองเมื่อจบ
    public static RuntimeAnimatorController Configure()
    {
        Import("remy_idle", null);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        foreach (var n in new[] { "remy_walk", "remy_fishing_idle", "remy_fishing_cast", "remy_jump" }) Import(n, avatar);
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; ti.textureCompression = TextureImporterCompression.Compressed; ti.SaveAndReimport(); }
        }
        return BuildController();
    }

    static void Import(string name, Avatar source)
    {
        var path = $"{Dir}/{name}.fbx";
        var mi = (ModelImporter)AssetImporter.GetAtPath(path);
        mi.animationType = ModelImporterAnimationType.Human;
        if (source == null) mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        else { mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; mi.sourceAvatar = source; }
        mi.importCameras = false; mi.importLights = false;
        mi.SaveAndReimport();
        var clips = mi.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.name = name;
            c.loopTime = Loops.Contains(name);
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootHeightY = true; c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true; c.keepOriginalPositionXZ = true;
        }
        mi.clipAnimations = clips;
        if (source == null)
        {
            // พื้นผิวฝังใน FBX → แยกออกมาเป็นไฟล์ให้วัสดุอ้างอิงได้
            var texDir = $"{Dir}/Textures";
            if (!Directory.Exists(texDir)) { Directory.CreateDirectory(texDir); mi.ExtractTextures(texDir); AssetDatabase.Refresh(); }
        }
        mi.SaveAndReimport();
    }

    static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath($"{Dir}/{name}.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

    // เปิด IK Pass ให้ controller ที่มีอยู่ (ไม่ต้องสร้างฉากใหม่ทั้งหมด)
    public static void EnableIK()
    {
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var layers = ac.layers; layers[0].iKPass = true; ac.layers = layers;
        EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets();
        Debug.Log("[CharacterSetup] IK pass on");
    }

    static RuntimeAnimatorController BuildController()
    {
        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Pose", AnimatorControllerParameterType.Int);
        var layers = ac.layers; layers[0].iKPass = true; ac.layers = layers;   // มุมบุคคลที่หนึ่ง: มือซ้ายจับปลาโชว์ (HandIK)
        var sm = ac.layers[0].stateMachine;

        var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
        tree.AddChild(Clip("remy_idle"), 0f);
        tree.AddChild(Clip("remy_walk"), 2.2f);
        sm.defaultState = loco;

        ac.AddParameter("CastT", AnimatorControllerParameterType.Float);
        var fish = sm.AddState("FishIdle"); fish.motion = Clip("remy_fishing_idle");
        var cast = sm.AddState("Cast"); cast.motion = Clip("remy_fishing_cast"); cast.timeParameterActive = true; cast.timeParameter = "CastT";
        var jump = sm.AddState("Jump"); jump.motion = Clip("remy_jump");

        AnimatorStateTransition T(AnimatorState from, AnimatorState to, int pose, float dur = 0.25f)
        {
            var t = from.AddTransition(to); t.hasExitTime = false; t.duration = dur;
            t.AddCondition(AnimatorConditionMode.Equals, pose, "Pose");
            return t;
        }
        T(loco, cast, 1, 0.15f); T(loco, fish, 3, 0.3f);
        T(fish, cast, 1, 0.15f); T(fish, loco, 0, 0.3f);
        T(cast, fish, 3, 0.4f); T(cast, loco, 0, 0.3f);
        var land = jump.AddTransition(loco); land.hasExitTime = true; land.exitTime = 0.6f; land.duration = 0.2f;
        T(jump, cast, 1, 0.15f);
        AssetDatabase.SaveAssets();
        return ac;
    }

    // ใส่ Remy แทนตัวละครเดิม: ซ่อนชิ้นส่วน low-poly, ย้ายคันเบ็ดไปมือขวา และหมวกงอบไปที่หัว
    public static Animator Attach(GameObject fisher)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var remy = Object.Instantiate(prefab, fisher.transform);
        remy.name = "Remy";
        remy.transform.localPosition = Vector3.zero; remy.transform.localRotation = Quaternion.identity; remy.transform.localScale = Vector3.one * 1.08f;
        var anim = remy.GetComponent<Animator>() ?? remy.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var r in remy.GetComponentsInChildren<SkinnedMeshRenderer>()) { r.updateWhenOffscreen = true; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }

        foreach (var n in new[] { "Body", "ArmL", "ArmR", "Head", "LegL", "LegR" })
        {
            var t = Find(fisher.transform, n);
            if (t != null) { var mr = t.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false; }
        }
        var hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        var head = anim.GetBoneTransform(HumanBodyBones.Head);
        var rod = Find(fisher.transform, "Rod");
        if (rod != null && hand != null)
        {
            rod.SetParent(hand, false);
            // คันเบ็ดยาวตามแกน +Y ของตัวเอง จุดจับอยู่ที่ origin → หันไปทางหน้ามือ (ระหว่างนิ้วกำ)
            rod.localRotation = Quaternion.Euler(0, 0, -90) * Quaternion.Euler(-80, 0, 0);
            rod.localPosition = Vector3.zero;
            SetWorldScale(rod, 0.85f);
            rod.position = hand.position + hand.up * 0.08f;
        }
        var hat = Find(fisher.transform, "Hat");
        if (hat != null && head != null)
        {
            hat.SetParent(head, true);
            hat.rotation = fisher.transform.rotation;
            SetWorldScale(hat, 0.78f);
            hat.position = head.position + Vector3.up * 0.2f;
        }
        return anim;
    }

    static void SetWorldScale(Transform t, float s)
    {
        var p = t.parent ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(s / p.x, s / p.y, s / p.z);
    }

    static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var r = Find(c, name); if (r != null) return r; }
        return null;
    }
}
