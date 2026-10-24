using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    // ---------------- กล้อง: ทับมุมของเกมหลังทุกอย่างวางกล้องเสร็จ แล้วคืนก่อนเฟรมหน้า (ระบบกล้องเดิมทำงานต่อได้ไม่สะดุด) ----------------
    [DefaultExecutionOrder(10000)]
    public class CutsceneCamera : MonoBehaviour
    {
        public static Vector3 Pos, Look; public static float Fov = 1, Roll, Weight;
        internal Vector3 savedPos; internal Quaternion savedRot; internal float savedFov; internal bool applied;
        Camera cam;
        void Awake() { cam = GetComponent<Camera>(); if (GetComponent<CutsceneCameraRestore>() == null) gameObject.AddComponent<CutsceneCameraRestore>(); }
        void LateUpdate()
        {
            if (Weight <= 0.0001f || cam == null) return;
            var t = cam.transform;
            savedPos = t.position; savedRot = t.rotation; savedFov = cam.fieldOfView; applied = true;
            var dir = Look - Pos; if (dir.sqrMagnitude < 1e-6f) dir = t.forward;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0, 0, Roll);
            t.SetPositionAndRotation(Vector3.Lerp(savedPos, Pos, Weight), Quaternion.Slerp(savedRot, rot, Weight));
            cam.fieldOfView = Mathf.Lerp(savedFov, savedFov * Fov, Weight);
        }
    }
}
