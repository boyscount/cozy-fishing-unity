using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [Serializable]
    public class CueClip : PlayableAsset, ITimelineClipAsset
    {
        public CueBehaviour template = new CueBehaviour();
        public ClipCaps clipCaps => ClipCaps.None;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<CueBehaviour>.Create(graph, template);
    }
}
