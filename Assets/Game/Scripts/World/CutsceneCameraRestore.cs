using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [DefaultExecutionOrder(-10000)]
    public class CutsceneCameraRestore : MonoBehaviour
    {
        CutsceneCamera cc; Camera cam;
        void Awake() { cc = GetComponent<CutsceneCamera>(); cam = GetComponent<Camera>(); }
        void Update()
        {
            if (cc == null || !cc.applied) return;
            cc.applied = false;
            transform.SetPositionAndRotation(cc.savedPos, cc.savedRot); if (cam != null) cam.fieldOfView = cc.savedFov;
        }
    }
}
