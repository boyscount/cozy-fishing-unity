using System.Collections.Generic;

namespace CozyFishing
{
    // ได้ปลาแล้ว: บันทึกสมุดปลา/กระดาน/ภารกิจ/เบาะแส → การ์ดให้เลือก เก็บ/ปล่อย/แลก (ข้องเต็ม) · บอสปลาบึก: เก็บหรือปล่อยรับพร
    sealed class CatchFlow
    {
        readonly FishingController c;
        BasketFish pending;   // ยังไม่ใส่ข้อง รอผู้เล่นกด "เก็บ" หรือ "ปล่อย" ในการ์ด
        Clue caughtClue;
        FishSecret caughtSecret;
        List<string> dailyMsgs;
        public bool IsNew, CardShown;

        public CatchFlow(FishingController c) { this.c = c; }

        public void Clear() { pending = null; CardShown = false; }

        public void OnCaught()
        {
            var fish = c.fish; var world = c.world; var hud = c.hud;
            WaterFx.AddRipple(c.bob.x, c.bob.z, 1.6f);
            c.caughtGo = c.MakeFish(fish.species, UnityEngine.Mathf.Min(2.4f, 0.55f + fish.cm / 80f));
            c.caughtGo.transform.position = c.bob;
            c.from = c.bob;
            if (c.BossHooked) { Boss.MarkCaught(); c.boss.Reset(); }
            if (Duel.Active && !Duel.Done) { var dm = Duel.Resolve(true, fish.kg, fish.species.name); if (dm.Length > 0) hud.Toast(dm); }
            IsNew = FishDex.Record(fish.species.id, fish.kg);
            Cloud.SubmitFish(world.StageId, fish.species.id, fish.kg);   // กระดานปลาใหญ่ประจำสัปดาห์ของด่านนี้ (ออนไลน์เท่านั้น)
            if (Shop.ActiveBait != "worm" && Shop.UseBait()) hud.Toast("เหยื่อหมดแล้ว กลับไปใช้ไส้เดือน");
            caughtClue = Mystery.OnCatch(world.StageId, fish.species.id, Story.S.time, c.NearSpecial, c.NearDock, c.rng);
            caughtSecret = Mystery.CheckCatchSecret(fish.species.id, fish.kg, Story.S.time, Story.S.luck);
            dailyMsgs = Daily.OnCatch(world.StageId, fish.species, fish.kg);
            dailyMsgs.AddRange(Weekly.OnCatch(world.StageId, fish.species, fish.kg));   // เทศกาลปลาใหญ่ประจำสัปดาห์   // ภารกิจรายวัน/ปลาเด่น: รางวัลเข้ากระเป๋าทันที แจ้งในการ์ด
            // ข้องเต็ม → ยังไม่ใส่ รอให้ผู้เล่นเลือกแลกหรือปล่อยในการ์ด
            pending = new BasketFish { id = fish.species.id, kg = fish.kg, cm = fish.cm };
            c.Set(FishState.Caught); Sfx.Splash();
        }

        public void ShowCard()
        {
            var fish = c.fish; var hud = c.hud;
            var img = c.preview != null ? c.preview.Live(fish.species) : null;
            string clueNote = caughtClue != null ? $"พบเบาะแส! {caughtClue.name} — {caughtClue.found}" : null;
            string dailyNote = dailyMsgs != null && dailyMsgs.Count > 0 ? string.Join("\n", dailyMsgs) : null;
            if (dailyNote != null) { Sfx.Coin(); clueNote = clueNote != null ? clueNote + "\n" + dailyNote : dailyNote; }
            if (caughtClue != null) hud.Toast($"เบาะแส {Mystery.CluesFound}/{Mystery.Current.clues.Length} · บันทึกลงสมุดคดีแล้ว");
            if (caughtSecret != null) hud.Toast($"ค้นพบความลับของ{fish.species.name}! ดูในสมุดปลา");
            if (c.BossHooked && pending != null)
            {
                hud.ShowCatch(fish, img, IsNew, "เก็บ (ขายได้ราคาสูง)", () => { Boss.OnKeep(); KeepPending(true); },
                    $"ปล่อยคืนแม่น้ำ (รับพร {Story.Baht(Boss.Blessing)})", () => { Boss.OnRelease(); pending = null; hud.Toast("ปลาบึกยักษ์ว่ายกลับสู่แม่น้ำโขง… ลูกไฟพญานาคลอยขึ้นเป็นสาย"); End(false); },
                    "ปลาบึกยักษ์แห่งแม่น้ำโขง! ปล่อยคืนแม่น้ำ = พญานาคให้พร ครอบครัวมีความสุข · เก็บ = ขายได้เงินก้อนใหญ่");
                return;
            }
            if (Story.BasketFull)
            {
                int ci = Story.CheapestIndex(); var cheap = Story.S.basket[ci];
                bool better = Story.Value(pending) > Story.Value(cheap);
                hud.ShowCatch(fish, img, IsNew, $"เก็บ (แลกกับ{FishDatabase.Get(cheap.id)?.name} {Story.Baht(Story.Value(cheap))})", () => KeepPending(true),
                    "ปล่อย", () => KeepPending(false), clueNote ?? (better ? "ตัวนี้ราคาดีกว่าตัวที่ถูกสุดในข้อง ควรแลก!" : "ตัวนี้ราคาน้อยกว่าทุกตัวในข้อง ปล่อยคืนน้ำดีกว่า"));
            }
            else hud.ShowCatch(fish, img, IsNew, $"เก็บใส่ข้อง ({Story.S.basket.Count + 1}/{Story.Capacity})",
                () => { Story.AddFish(pending); pending = null; End(true); c.director?.OnFishKept(); },
                "ปล่อย", () => { pending = null; hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ นะ"); End(false); }, clueNote);
        }

        void KeepPending(bool keep)
        {
            var hud = c.hud;
            if (keep && pending != null && !Story.BasketFull) { Story.AddFish(pending); hud.Toast($"เก็บ{FishDatabase.Get(pending.id)?.name}ใส่ข้องแล้ว"); }
            else if (keep && pending != null) { var old = Story.SwapCheapest(pending); hud.Toast($"เก็บ{FishDatabase.Get(pending.id)?.name} · ปล่อย{FishDatabase.Get(old.id)?.name}คืนน้ำ"); }
            else hud.Toast("ปล่อยคืนน้ำ ขอให้โตไว ๆ นะ");
            pending = null;
            End(keep);
        }

        void End(bool kept)
        {
            CardShown = false;
            if (Duel.Done && Duel.Active) c.hud.ShowDuelResult();
            if (c.preview != null) c.preview.StopLive();
            var go = c.caughtGo;
            bool taken = go && c.ExternalShowcase && !c.fish.IsJunk && FishingController.CatchOutro != null && FishingController.CatchOutro(go, kept);
            if (go && !taken) UnityEngine.Object.Destroy(go);
            c.caughtGo = null;
            c.Set(FishState.Free);
        }
    }
}
