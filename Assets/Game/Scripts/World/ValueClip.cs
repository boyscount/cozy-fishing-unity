using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [Serializable]
    public class ValueClip : PlayableAsset, ITimelineClipAsset
    {
        public ValueBehaviour template = new ValueBehaviour();
        public ClipCaps clipCaps => ClipCaps.Blending;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<ValueBehaviour>.Create(graph, template);
    }
}
