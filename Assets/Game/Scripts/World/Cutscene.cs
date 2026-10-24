using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    // ---------------- กล้อง ----------------
    [Serializable]
    public class CamShotBehaviour : PlayableBehaviour
    {
        public string fromAnchor = "cam0", fromLook = "cam0", toAnchor = "cam0", toLook = "cam0";
        public Vector3 fromOff, fromLookOff, toOff, toLookOff;
        public float fromFov = 1, toFov = 1, fromRoll, toRoll;
        public bool smooth = true;   // ค่อย ๆ เร่ง-ผ่อน (ไม่งั้นเคลื่อนคงที่)
    }
    public class CamShotMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var rig = playerData as CutsceneRig; if (rig == null) return;
            Vector3 pos = Vector3.zero, look = Vector3.zero; float fov = 0, roll = 0, w = 0;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float wi = playable.GetInputWeight(i); if (wi <= 0) continue;
                var inp = (ScriptPlayable<CamShotBehaviour>)playable.GetInput(i); var b = inp.GetBehaviour();
                float k = (float)(inp.GetTime() / Math.Max(0.0001, inp.GetDuration())); k = Mathf.Clamp01(k); if (b.smooth) k = Mathf.SmoothStep(0, 1, k);
                pos += Vector3.Lerp(rig.Point(b.fromAnchor, b.fromOff), rig.Point(b.toAnchor, b.toOff), k) * wi;
                look += Vector3.Lerp(rig.LookPoint(b.fromLook, b.fromLookOff), rig.LookPoint(b.toLook, b.toLookOff), k) * wi;
                fov += Mathf.Lerp(b.fromFov, b.toFov, k) * wi; roll += Mathf.Lerp(b.fromRoll, b.toRoll, k) * wi; w += wi;
            }
            if (w <= 0.0001f) { CutsceneCamera.Weight = 0; return; }
            CutsceneCamera.Pos = pos / w; CutsceneCamera.Look = look / w; CutsceneCamera.Fov = fov / w; CutsceneCamera.Roll = roll / w;
            CutsceneCamera.Weight = Mathf.Clamp01(w);
        }
    }

    // ---------------- บทพูด / ชื่อเรื่อง ----------------
    [Serializable]
    public class CaptionBehaviour : PlayableBehaviour { public string who = "", text = ""; public bool title; }
    public class CaptionMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var rig = playerData as CutsceneRig; if (rig == null || rig.hud == null) return;
            CaptionBehaviour best = null; float bw = 0;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float wi = playable.GetInputWeight(i);
                if (wi > bw) { bw = wi; best = ((ScriptPlayable<CaptionBehaviour>)playable.GetInput(i)).GetBehaviour(); }
            }
            if (best == null) rig.hud.CutsceneCaption(null, null, 0, false);
            else rig.hud.CutsceneCaption(Loc.T(best.who), Loc.T(best.text), bw, best.title);
        }
    }

    // ---------------- เสียง / เหตุการณ์ (ยิงครั้งเดียวตอนคลิปเริ่ม) ----------------
    [Serializable]
    public class CueBehaviour : PlayableBehaviour
    {
        public string cue = ""; public bool isEvent;
        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            var rig = Cutscene.Rig; if (rig == null || info.evaluationType != FrameData.EvaluationType.Playback) return;
            if (isEvent) rig.onEvent?.Invoke(cue); else rig.onCue?.Invoke(cue);
        }
    }

    // ---------------- ค่าตัวเลขตามเวลา: ตะเกียงวูบ / สโลโมชั่น ----------------
    [Serializable]
    public class ValueBehaviour : PlayableBehaviour { public float value = 1; }
    public class ValueMixer : PlayableBehaviour
    {
        public bool timeScale;
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            float v = 0, w = 0;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float wi = playable.GetInputWeight(i); if (wi <= 0) continue;
                v += ((ScriptPlayable<ValueBehaviour>)playable.GetInput(i)).GetBehaviour().value * wi; w += wi;
            }
            if (timeScale) Time.timeScale = Mathf.Lerp(1f, w > 0 ? v / w : 1f, Mathf.Clamp01(w));
            else Cutscene.Flicker = w > 0 ? v / w * Mathf.Clamp01(w) : 0;
        }
    }

    // ---------------- ตัวเล่นคัตซีน ----------------
    public static class Cutscene
    {
        public static CutsceneRig Rig { get; private set; }
        public static bool Playing => Rig != null;
        public static float Flicker;   // 0..1 ตะเกียงวูบ (GhostSystem อ่านไปใช้)
        static readonly Dictionary<string, TimelineAsset> cache = new Dictionary<string, TimelineAsset>();

        public static bool Has(string name) => Load(name) != null;
        static TimelineAsset Load(string name)
        {
            if (!cache.TryGetValue(name, out var a)) { a = Resources.Load<TimelineAsset>("Cutscenes/" + name); cache[name] = a; }
            return a;
        }

        /// เล่นคัตซีนจนจบ · skippable = แตะจอ/คลิก/กดปุ่มเพื่อข้าม (หลังเริ่มไปแล้ว 1 วิ)
        public static IEnumerator Play(string name, CutsceneRig rig, bool skippable)
        {
            var asset = Load(name);
            if (asset == null || rig == null) yield break;
            if (rig.cam != null && rig.cam.GetComponent<CutsceneCamera>() == null) rig.cam.gameObject.AddComponent<CutsceneCamera>();
            rig.SnapCamera();
            Rig = rig; Flicker = 0; CutsceneCamera.Weight = 0;
            var dir = rig.gameObject.AddComponent<PlayableDirector>();
            dir.playOnAwake = false; dir.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime; dir.extrapolationMode = DirectorWrapMode.None;
            dir.playableAsset = asset;
            foreach (var tr in asset.GetOutputTracks()) dir.SetGenericBinding(tr, rig);
            dir.Play();
            float t0 = Time.unscaledTime;
            try
            {
                while (dir != null && dir.state == PlayState.Playing && dir.time < asset.duration - 0.001)
                {
                    if (skippable && Time.unscaledTime - t0 > 1f && SkipPressed()) break;
                    yield return null;
                }
            }
            finally   // จบเอง ข้าม หรือผู้เรียกเลิกกลางคัน (Dispose) → คืนทุกอย่าง
            {
                if (dir != null) { dir.Stop(); UnityEngine.Object.Destroy(dir); }
                Rig = null; Flicker = 0; CutsceneCamera.Weight = 0; Time.timeScale = 1;
                if (rig != null && rig.hud != null) rig.hud.CutsceneCaption(null, null, 0, false);
            }
        }

        static bool SkipPressed()
        {
            var p = UnityEngine.InputSystem.Pointer.current; var k = UnityEngine.InputSystem.Keyboard.current;
            return (p != null && p.press.wasPressedThisFrame) || (k != null && (k.spaceKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame));
        }
    }
}
