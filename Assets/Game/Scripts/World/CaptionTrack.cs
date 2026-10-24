using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [TrackColor(0.95f, 0.85f, 0.45f), TrackClipType(typeof(CaptionClip)), TrackBindingType(typeof(CutsceneRig))]
    public class CaptionTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) => ScriptPlayable<CaptionMixer>.Create(graph, inputCount);
    }
}
