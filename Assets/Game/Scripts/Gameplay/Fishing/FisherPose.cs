using UnityEngine;

namespace CozyFishing
{
    // ท่าคนตกปลาตามสถานะ: แขนขวาถือคัน · แขนซ้ายกรอสาย · ท่า Mixamo เหวี่ยง/กรอ · เดิน/หมุนได้ตอนไหน
    sealed class FisherPose
    {
        readonly FishingController c;
        float reelT;

        public FisherPose(FishingController c) { this.c = c; }

        public void Update()
        {
            var player = c.player; var State = c.State; float t = c.t, charge = c.cast.Charge, armSign = c.armSign;
            player.CanWalk = State == FishState.Free;
            player.CanTurn = State == FishState.Free || State == FishState.Charging || (State == FishState.Netting && c.net.CanTurn);
            float now = WaterFx.Time;
            float a = State switch
            {
                FishState.Charging => -80 * charge,
                FishState.Casting => Mathf.Min(31, -80 + t * 800),
                FishState.Waiting or FishState.Bite or FishState.Landed => 20 + Mathf.Sin(now * 1.5f),
                FishState.Strike => -34 + Mathf.Sin(now * 25) * 6,
                FishState.Fight => -20 + Mathf.Sin(now * 30) * 2.3f * c.fight.Tension,
                FishState.Caught => -150,
                _ => 0,
            };
            player.ArmRTarget = a * armSign;
            // ท่าตัวละคร Mixamo: คลิป "เหวี่ยงเบ็ด+กรอสาย" 8.8 วิ — ง้าง 0.06→0.165 · สะบัดไปหน้า 0.165→0.30 (ปล่อยสาย ~0.215) · กรอสาย 0.52→0.92
            switch (State)
            {
                case FishState.Charging: player.Pose = 1; player.CastT = Mathf.Lerp(0.06f, 0.165f, Mathf.SmoothStep(0, 1, charge)); break;
                case FishState.Casting: player.Pose = 1; player.CastT = Mathf.Lerp(0.165f, 0.3f, Mathf.Clamp01(t / (FishingCast.ThrowDelay * 2.6f))); break;
                case FishState.Fight: case FishState.Retrieve: case FishState.Snapped:
                    player.Pose = 1; reelT += Time.deltaTime * (State == FishState.Fight && !GameInput.ActionHeld ? 0.03f : 0.11f);
                    player.CastT = 0.52f + Mathf.Repeat(reelT, 0.4f); break;
                case FishState.Netting: player.Pose = 1; player.CastT = c.net.PoseT; break;   // ทอดแห: แกว่ง → สะบัด → สาวแห
                case FishState.Free: player.Pose = 0; break;
                default: player.Pose = 3; break;
            }
            player.ArmLTarget = State == FishState.Caught ? -150 * armSign : State == FishState.Fight ? (GameInput.ActionHeld ? -52 + Mathf.Sin(now * 18) * 20 : -34) * armSign : float.NaN;
        }
    }
}
