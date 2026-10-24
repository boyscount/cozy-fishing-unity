using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [TrackColor(0.35f, 0.6f, 0.95f), TrackClipType(typeof(CamShotClip)), TrackBindingType(typeof(CutsceneRig))]
    public class CamShotTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) => ScriptPlayable<CamShotMixer>.Create(graph, inputCount);
    }
}
