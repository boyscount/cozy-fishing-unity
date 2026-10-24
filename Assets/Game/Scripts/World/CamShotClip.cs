using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [Serializable]
    public class CamShotClip : PlayableAsset, ITimelineClipAsset
    {
        public CamShotBehaviour template = new CamShotBehaviour();
        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<CamShotBehaviour>.Create(graph, template);
    }
}
