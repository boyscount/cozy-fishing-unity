using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [Serializable]
    public class CaptionClip : PlayableAsset, ITimelineClipAsset
    {
        public CaptionBehaviour template = new CaptionBehaviour();
        public ClipCaps clipCaps => ClipCaps.Blending;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<CaptionBehaviour>.Create(graph, template);
    }
}
