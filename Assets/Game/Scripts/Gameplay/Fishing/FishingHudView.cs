using UnityEngine;

namespace CozyFishing
{
    // ส่งสถานะตกปลาขึ้น HUD ทุกเฟรม: คำแนะนำ ปุ่มหลัก เกจแรงง้าง/แรงดึง/ตีเบ็ด เครื่องหมายปลากิน
    sealed class FishingHudView
    {
        readonly FishingController c;

        public FishingHudView(FishingController c) { this.c = c; }

        public void Update()
        {
            var hud = c.hud; var player = c.player; var State = c.State; var fight = c.fight; var strike = c.strike; var bob = c.bob; var fish = c.fish;
            bool touch = hud.TouchUi;
            switch (State)
            {
                case FishState.Free when CastNet.Using: hud.Hint(touch ? $"ถือแหอยู่ · รอฝูงปลาผุดน้ำ แล้วกดค้างแกว่งแห ปล่อยตอนเข็มเข้าช่องเขียว (เหลือ {CastNet.ThrowsLeft} ครั้ง)" : $"ถือแหอยู่ · รอฝูงปลาผุดน้ำ แล้วกด E ค้างแกว่งแห ปล่อยตอนเข็มเข้าช่องเขียว (เหลือ {CastNet.ThrowsLeft} ครั้ง)"); hud.ActionLabel("", false, "net"); break;
                case FishState.Netting:
                    hud.Hint(touch ? "" : c.net.Swinging ? "ปล่อยตอนเข็มเข้าช่องเขียว · วงทองต้องครอบฝูงปลา" : c.net.Hauling ? "สาวแห! แตะตอนวงสว่าง" : "");   // มือถือ: เกจทอดแหมีคำบอกในตัวแล้ว ไม่ซ้อนป้ายคำแนะนำ
                    hud.ActionLabel(c.net.Hauling ? "สาว!" : "", false, c.net.Hauling ? "reelin" : "net"); break;
                case FishState.Free: hud.Hint(touch ? "กดค้างปุ่มเหวี่ยงเพื่อเล็ง · ลากจอเพื่อหมุนกล้อง" : "คลิกค้าง / กด E ค้าง เพื่อเล็ง · ปล่อยเพื่อเหวี่ยง\nWASD เดิน · Space กระโดด · คลิกขวาลาก หมุนกล้อง · ล้อเมาส์ ซูม"); hud.ActionLabel("", false, "cast"); break;
                case FishState.Charging: hud.Hint(""); hud.ActionLabel("", true, "cast"); break;
                case FishState.Casting: case FishState.Landed: case FishState.Retrieve: hud.Hint(""); hud.ActionLabel("", false, "wait"); break;
                case FishState.Waiting:
                    if (c.Jigging) { hud.Hint("แตะตอนไฟเขียวสว่าง ล่อหมึกเข้าวงแสง · กดค้างเก็บสาย"); hud.ActionLabel("", FishingController.JigPulse > 0.5f, "reelin"); }   // วงปุ่มสว่างตามจังหวะไฟ
                    else if (c.lure.Run != null) { hud.Hint(c.lure.Run.def.hint); hud.ActionLabel("", false, "reel"); }   // ไม่โชว์ป้าย วัดเบ็ด! ระหว่างลาก (จังหวะดูที่จุดในแถบลากเหยื่อ)
                    else { hud.Hint("รอให้ทุ่นจม… (กดเพื่อเก็บสาย)"); hud.ActionLabel("", false, "reelin"); }
                    break;
                case FishState.Bite: hud.Hint("ทุ่นจม! กดเลย!"); hud.ActionLabel("ดึง!", true, "hook"); break;
                case FishState.Strike: hud.Hint("กดตอนเข็มอยู่ในโซนทอง!"); hud.ActionLabel("ตีเบ็ด!", true, "strike"); break;
                case FishState.Fight: hud.Hint(touch ? "กดค้างกรอสาย · ปล่อยเมื่อใกล้แดง" : "กดค้างเพื่อกรอสาย · ปล่อยเมื่อเข็มใกล้สีแดง"); hud.ActionLabel("", false, "reel"); break;
                default: hud.Hint(""); break;
            }
            hud.Power(State == FishState.Charging ? c.cast.Charge : -1);
            hud.Net(State == FishState.Netting, c.net);
            hud.NetButton(CastNet.Owned && CastNet.StageOk(c.world.StageId) && (State == FishState.Free || State == FishState.Netting), CastNet.Using, CastNet.ThrowsLeft, State == FishState.Free);
            hud.Lure(State == FishState.Waiting && c.lure.Run != null, c.lure.Run);
            hud.LineDistance(c.LineOut || State == FishState.Bite ? Vector2.Distance(new Vector2(player.Pos.x, player.Pos.z), new Vector2(bob.x, bob.z)) : -1);
            hud.Tension(State == FishState.Fight, Mathf.Clamp01((fight?.Tension ?? 0) + c.GaugeWobble * 0.18f * Mathf.Sin(Time.time * 11) * Mathf.PerlinNoise(Time.time * 4, 2)), fight?.Progress ?? 0, fight?.ZoneCenter ?? 0.5f, fight?.ZoneWidth ?? 0.6f, State == FishState.Fight && fight.Tension > 0.85f);
            hud.Strike(State == FishState.Strike, strike?.Pos ?? 0, strike?.Center ?? 0, strike?.Width ?? 0, strike?.Hits ?? 0, strike?.Need ?? 0, strike?.Misses ?? 0);
            hud.Alert(State == FishState.Bite, player.Pos + Vector3.up * 2.3f, !fish.IsJunk && fish.species != null && fish.species.stars >= 3);
        }
    }
}
