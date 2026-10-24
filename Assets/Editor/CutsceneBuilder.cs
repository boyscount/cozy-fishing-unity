using CozyFishing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

// สร้างไฟล์คัตซีน Timeline (.playable) ใน Resources/Cutscenes — เปิดแก้ต่อในหน้าต่าง Window ▸ Sequencing ▸ Timeline ได้
//   batch: Unity -batchmode -quit -executeMethod CutsceneBuilder.BuildAll
public static class CutsceneBuilder
{
    const string Dir = "Assets/Game/Resources/Cutscenes";

    [MenuItem("Tools/Cozy/Build Cutscenes")]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Game/Resources", "Cutscenes");
        GhostReveal();
        FairEnding();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("[CutsceneBuilder] built GhostReveal + FairEnding");
    }

    static TimelineAsset New(string name)
    {
        var path = $"{Dir}/{name}.playable";
        AssetDatabase.DeleteAsset(path);
        var t = ScriptableObject.CreateInstance<TimelineAsset>(); t.name = name;
        AssetDatabase.CreateAsset(t, path);
        return t;
    }

    static void Shot(CamShotTrack tr, string label, double start, double dur, double easeIn, double easeOut,
        string fa, Vector3 fo, string fl, Vector3 flo, string ta, Vector3 to, string tl, Vector3 tlo, float fov0 = 1, float fov1 = 1, float roll0 = 0, float roll1 = 0)
    {
        var c = tr.CreateClip<CamShotClip>(); c.displayName = label; c.start = start; c.duration = dur; c.easeInDuration = easeIn; c.easeOutDuration = easeOut;
        var a = (CamShotClip)c.asset;
        a.template = new CamShotBehaviour { fromAnchor = fa, fromOff = fo, fromLook = fl, fromLookOff = flo, toAnchor = ta, toOff = to, toLook = tl, toLookOff = tlo, fromFov = fov0, toFov = fov1, fromRoll = roll0, toRoll = roll1 };
        EditorUtility.SetDirty(a);
    }
    static void Line(CaptionTrack tr, double start, double dur, string who, string text, bool title = false)
    {
        var c = tr.CreateClip<CaptionClip>(); c.displayName = string.IsNullOrEmpty(who) ? text : who; c.start = start; c.duration = dur;
        c.easeInDuration = 0.35; c.easeOutDuration = 0.35;
        var a = (CaptionClip)c.asset; a.template = new CaptionBehaviour { who = who, text = text, title = title }; EditorUtility.SetDirty(a);
    }
    static void Cue(CueTrack tr, double at, string cue, bool isEvent)
    {
        var c = tr.CreateClip<CueClip>(); c.displayName = cue; c.start = at; c.duration = 0.2;
        var a = (CueClip)c.asset; a.template = new CueBehaviour { cue = cue, isEvent = isEvent }; EditorUtility.SetDirty(a);
    }
    static void Value(TrackAsset tr, double start, double dur, double easeIn, double easeOut, float v, string label)
    {
        var c = tr.CreateClip<ValueClip>(); c.displayName = label; c.start = start; c.duration = dur; c.easeInDuration = easeIn; c.easeOutDuration = easeOut;
        var a = (ValueClip)c.asset; a.template = new ValueBehaviour { value = v }; EditorUtility.SetDirty(a);
    }

    // ผีปรากฏ 3.6 วิ (เวลาจริง ขณะเกมสโลว์เหลือ 18%) · frame: z = ทิศจากกล้องไปหาผี
    static void GhostReveal()
    {
        var t = New("GhostReveal");
        var cam = t.CreateTrack<CamShotTrack>(null, "Camera");
        Shot(cam, "Push in", 0.0, 0.8, 0.25, 0.15, "cam0", Vector3.zero, "cam0", Vector3.zero, "ghostHead", new Vector3(0.35f, 0.15f, -3.2f), "ghostHead", Vector3.zero, 1f, 0.8f);
        Shot(cam, "Face close-up", 0.65, 1.3, 0.15, 0.15, "ghostHead", new Vector3(0.3f, 0.05f, -2.2f), "ghostHead", Vector3.zero, "ghostHead", new Vector3(0.15f, 0f, -1.6f), "ghostHead", Vector3.zero, 0.7f, 0.62f, 6f, 9f);
        Shot(cam, "Over shoulder", 1.8, 1.8, 0.15, 0.7, "player", new Vector3(0.9f, 1.9f, -1.6f), "ghost", new Vector3(0, 1f, 0), "player", new Vector3(0.7f, 1.7f, -1.1f), "ghost", new Vector3(0, 1f, 0), 0.9f, 0.9f);
        var ev = t.CreateTrack<CueTrack>(null, "Events");
        Cue(ev, 0.68, "shake", true); Cue(ev, 1.82, "shake", true);
        var fl = t.CreateTrack<FlickerTrack>(null, "Lantern flicker");
        Value(fl, 0.2, 1.9, 0.1, 0.4, 1f, "flicker");
        var ts = t.CreateTrack<TimeScaleTrack>(null, "Slow motion");
        Value(ts, 0.0, 3.6, 0.0, 0.6, 0.18f, "slow-mo 18%");
        EditorUtility.SetDirty(t);
    }

    // ฉากจบงานวัด ~26 วิ · frame: z = ทิศจากบ้านไปงานวัด (ห่าง 68 ม.) · wheel = ดุมชิงช้าสวรรค์
    static void FairEnding()
    {
        var t = New("FairEnding");
        var cam = t.CreateTrack<CamShotTrack>(null, "Camera");
        Shot(cam, "Crane over home", 0.0, 6.4, 1.2, 0.6, "home", new Vector3(-4, 9, -10), "home", new Vector3(0, 3, 30), "home", new Vector3(-2, 15, 4), "fair", new Vector3(0, 9, 0), 1f, 0.9f);
        Shot(cam, "Glide to the fair", 5.8, 6.6, 0.6, 0.6, "fair", new Vector3(-20, 7, -34), "wheel", Vector3.zero, "fair", new Vector3(-9, 3.5f, -17), "wheel", new Vector3(0, -2, 0), 0.95f, 0.9f);
        Shot(cam, "Under the wheel", 11.8, 6.6, 0.6, 0.6, "wheel", new Vector3(7, -10.5f, -13), "wheel", new Vector3(0, 1, 0), "wheel", new Vector3(-6, -10.5f, -11), "wheel", new Vector3(0, 3, 0), 0.92f, 0.85f, -3f, 3f);
        Shot(cam, "Lanterns to the sky", 17.8, 8.4, 0.6, 1.2, "fair", new Vector3(2, 2.2f, -24), "fair", new Vector3(0, 8, 0), "fair", new Vector3(0, 5, -21), "fair", new Vector3(0, 34, 12), 0.95f, 1f);
        var cap = t.CreateTrack<CaptionTrack>(null, "Subtitles");
        Line(cap, 0.6, 4.8, "", "คืนนั้น ทั้งบ้านไปเที่ยวงานวัดด้วยกัน");
        Line(cap, 6.0, 3.2, "แม่", "ทั้งบ้านได้ชิงช้าสวรรค์ ขนมสายไหม และรอยยิ้มเต็มหน้า");
        Line(cap, 9.2, 3.0, "ยายอิ่ม", "ชิงช้าสวรรค์สูงขนาดนี้ ยายมองเห็นทุ่งนาบ้านเราด้วยแหละหลาน");
        Line(cap, 12.2, 3.6, "ผู้ใหญ่ทอง", "ขอบคุณที่พาแม่ผมกลับมานะครับ ถ้าไม่มีพวกคุณ ลายสานพวกนั้นคงจมอยู่ใต้น้ำไปตลอดกาล");
        Line(cap, 15.8, 2.6, "ลูก", "พ่อ… ตะกร้าเก้าลายใส่ขนมสายไหมได้เต็มเลย!");
        Line(cap, 18.4, 4.6, "พ่อ", "ตะกร้าไม่ได้วิเศษหรอกลูก แต่ใครที่ตั้งใจหาปลาเลี้ยงคนที่รัก ตะกร้าของเขาจะไม่ว่างเปล่าในวันพรุ่งนี้");
        Line(cap, 23.2, 3.0, "", "ตะกร้าแห่งวันพรุ่งนี้\n— จบ —", true);
        var cues = t.CreateTrack<CueTrack>(null, "Music");
        Cue(cues, 0.0, "music", false);
        var ev = t.CreateTrack<CueTrack>(null, "Fireworks & lanterns");
        foreach (var at in new[] { 7.6, 10.4, 12.2, 13.3, 14.0, 15.2, 16.6, 19.0, 20.3, 21.4 }) Cue(ev, at, "fw", true);
        foreach (var at in new[] { 22.6, 23.0, 23.5, 24.1 }) Cue(ev, at, "fwbig", true);
        Cue(ev, 16.9, "lanterns", true);
        EditorUtility.SetDirty(t);
    }
}
