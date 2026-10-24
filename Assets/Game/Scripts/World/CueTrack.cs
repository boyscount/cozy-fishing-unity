using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CozyFishing
{
    [TrackColor(0.9f, 0.5f, 0.35f), TrackClipType(typeof(CueClip)), TrackBindingType(typeof(CutsceneRig))]
    public class CueTrack : TrackAsset { }
}
