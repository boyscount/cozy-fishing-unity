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

    // Pose (ตั้งจาก FishingController ผ่าน PlayerController): 0 เดิน/ยืน, 1 ง้างเบ็ด, 2 เหวี่ยง, 3 รอปลา, 4 สู้ปลา, 5 ได้ปลา
    public static RuntimeAnimatorController Configure()
    {
        Import("remy_idle", null);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        foreach (var n in new[] { "remy_walk", "remy_fishing_idle", "remy_fishing_cast" }) Import(n, avatar);
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

    static RuntimeAnimatorController BuildController()
    {
        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Pose", AnimatorControllerParameterType.Int);
        var sm = ac.layers[0].stateMachine;

        var loco = ac.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
        tree.AddChild(Clip("remy_idle"), 0f);
        tree.AddChild(Clip("remy_walk"), 2.2f);
        sm.defaultState = loco;

        var fish = sm.AddState("FishIdle"); fish.motion = Clip("remy_fishing_idle");
        var fight = sm.AddState("Fight"); fight.motion = Clip("remy_fishing_idle"); fight.speed = 1.8f;
        var cast = sm.AddState("Cast"); cast.motion = Clip("remy_fishing_cast");

        void Any(AnimatorState to, int pose, float dur = 0.25f)
        {
            var t = sm.AddAnyStateTransition(to);
            t.canTransitionToSelf = false; t.hasExitTime = false; t.duration = dur;
            t.AddCondition(AnimatorConditionMode.Equals, pose, "Pose");
        }
        Any(loco, 0, 0.3f); Any(fish, 1); Any(cast, 2, 0.1f); Any(fight, 4); Any(fish, 5);
        // หลังเหวี่ยงจบช่วงขว้าง ค่อยเข้าท่าถือคันรอปลา (Pose 3 ไม่ตัดท่าเหวี่ยงกลางคัน)
        var after = cast.AddTransition(fish); after.hasExitTime = true; after.exitTime = 0.55f; after.duration = 0.35f;
        var direct = loco.AddTransition(fish); direct.hasExitTime = false; direct.duration = 0.3f; direct.AddCondition(AnimatorConditionMode.Equals, 3, "Pose");
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
