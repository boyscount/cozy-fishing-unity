using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [TrackColor(1f, 0.7f, 0.3f), TrackClipType(typeof(ValueClip)), TrackBindingType(typeof(CutsceneRig))]
    public class FlickerTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) => ScriptPlayable<ValueMixer>.Create(graph, inputCount);
    }
}
