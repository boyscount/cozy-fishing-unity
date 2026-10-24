using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [TrackColor(0.6f, 0.4f, 0.9f), TrackClipType(typeof(ValueClip)), TrackBindingType(typeof(CutsceneRig))]
    public class TimeScaleTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var p = ScriptPlayable<ValueMixer>.Create(graph, inputCount); p.GetBehaviour().timeScale = true; return p;
        }
    }
}
