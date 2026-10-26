using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace CozyFishing
{
    // บริการออนไลน์ (Unity Gaming Services) — ไม่ต้อง login:
    //  · เข้าแบบไม่ระบุตัวตนอัตโนมัติ ได้ ID ผู้เล่นติดเครื่อง/เบราว์เซอร์
    //  · เซฟคลาวด์: ความคืบหน้าเนื้อเรื่อง · โหมดอิสระ · สมุดปลา · สมุดคดี (เก็บเป็นก้อนเดียว พร้อมเวลาเซฟ)
    //    เปิดเกม: ถ้าเซฟบนคลาวด์ใหม่กว่าในเครื่อง (เช่นเปลี่ยนเครื่อง/ล้างเบราว์เซอร์) ดึงลงมาก่อนเริ่มเล่น
    //  · กระดาน "ปลาใหญ่ประจำสัปดาห์" แยกตามด่าน (bigfish_<ด่าน>) ส่งน้ำหนักทุกครั้งที่ได้ปลา ระบบเก็บตัวที่หนักที่สุดเอง
    //  ออฟไลน์/เน็ตหลุด/โหมดทดสอบ (bot): เล่นได้ปกติ แค่ไม่มีส่วนออนไลน์
    public static class Cloud
    {
        static readonly string[] SyncKeys = { "cozy.story.v1", "cozy.free.v1", "cozy.dex.v1", "abot.mystery.v1", "cozy.daily.v1", "cozy.weekly.v1", "cozy.boss.v1" };
        const string SavedAtKey = "cozy.cloud.savedAt", BlobKey = "save";
        public static bool Ready { get; private set; }
        public static string PlayerName { get; private set; } = "";
        static Task init; static float nextPush; static bool pushQueued;

        [Serializable] class Blob { public long savedAt; public List<string> keys = new List<string>(); public List<string> values = new List<string>(); }

        public static Task Init() => init ??= InitAsync();
        // บอท/ทดสอบ/เอดิเตอร์: ออฟไลน์ (ไม่ปนกระดานจริง) · ใส่ ?online เพื่อทดสอบส่วนออนไลน์โดยตั้งใจ
        static bool Offline => (GameUrl.Value.Contains("bot") || GameUrl.Value.Contains("offline") || Application.isEditor) && !GameUrl.Value.Contains("online");

        static async Task InitAsync()
        {
            if (Offline) return;
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                PlayerName = await AuthenticationService.Instance.GetPlayerNameAsync(true) ?? "";
                Ready = true;
                await Pull();
                Debug.Log($"[Cloud] signed in {AuthenticationService.Instance.PlayerId} as {PlayerName}");
            }
            catch (Exception e) { Debug.LogWarning("[Cloud] offline: " + e.Message); }
        }

        static long LocalSavedAt { get { try { return long.Parse(PlayerPrefs.GetString(SavedAtKey, "0")); } catch (Exception) { return 0; } } }

        // คลาวด์ใหม่กว่า → เขียนทับในเครื่อง แล้วให้ระบบที่โหลดไว้แล้วอ่านใหม่ (ทำเฉพาะตอนยังไม่เริ่มเล่น)
        static async Task Pull()
        {
            var got = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { BlobKey });
            if (!got.TryGetValue(BlobKey, out var item)) { Push(true); return; }
            var blob = JsonUtility.FromJson<Blob>(item.Value.GetAsString());
            if (blob == null || blob.savedAt <= LocalSavedAt) { if (blob == null || blob.savedAt < LocalSavedAt) Push(true); return; }
            if (Story.HasStorySave && Story.Phase != StoryPhase.New && Time.realtimeSinceStartup > 20) return;   // กำลังเล่นอยู่แล้ว ไม่ดึงทับกลางเกม
            for (int i = 0; i < blob.keys.Count && i < blob.values.Count; i++) PlayerPrefs.SetString(blob.keys[i], blob.values[i]);
            PlayerPrefs.SetString(SavedAtKey, blob.savedAt.ToString()); PlayerPrefs.Save();
            Story.ReloadFromPrefs(); FishDex.Reload(); Mystery.Reload(); Daily.Reload(); Weekly.Reload(); Boss.Reload();
            Debug.Log("[Cloud] restored save from cloud");
        }

        /// เรียกหลังเซฟในเครื่องทุกครั้ง · ส่งขึ้นคลาวด์ไม่ถี่กว่า 20 วินาทีครั้ง (รวมหลายการเซฟเป็นครั้งเดียว)
        public static void MarkDirty()
        {
            try { PlayerPrefs.SetString(SavedAtKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()); } catch (Exception) { }
            if (!Ready) return;
            if (Time.realtimeSinceStartup >= nextPush) Push(false); else pushQueued = true;
        }
        /// เรียกทุกเฟรมจากระบบที่อยู่ตลอด (ส่งก้อนที่ค้างไว้)
        public static void Tick() { if (pushQueued && Ready && Time.realtimeSinceStartup >= nextPush) Push(false); }

        static async void Push(bool force)
        {
            pushQueued = false; nextPush = Time.realtimeSinceStartup + 20;
            try
            {
                var b = new Blob { savedAt = LocalSavedAt };
                if (b.savedAt == 0) { b.savedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); PlayerPrefs.SetString(SavedAtKey, b.savedAt.ToString()); }
                foreach (var k in SyncKeys) { var v = PlayerPrefs.GetString(k, ""); if (v.Length > 0) { b.keys.Add(k); b.values.Add(v); } }
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { BlobKey, JsonUtility.ToJson(b) } });
            }
            catch (Exception e) { Debug.LogWarning("[Cloud] save failed: " + e.Message); }
        }

        // ---------- กระดานปลาใหญ่ ----------
        public static string BoardId(string stage) => "bigfish_" + stage;

        public static async void SubmitFish(string stage, string fishId, float kg)
        {
            if (!Ready || kg <= 0) { Debug.Log($"[Cloud] score skipped (ready={Ready})"); return; }
            try { var r = await LeaderboardsService.Instance.AddPlayerScoreAsync(BoardId(stage), Math.Round(kg, 2), new AddPlayerScoreOptions { Metadata = new Dictionary<string, string> { { "fish", fishId } } }); Debug.Log($"[Cloud] score {BoardId(stage)} {kg:0.00}kg → rank {r.Rank + 1}"); }
            catch (Exception e) { Debug.LogWarning("[Cloud] score failed: " + e.Message); }
        }

        public class Row { public int rank; public string name; public double kg; public string fish; public bool me; }

        /// อันดับ 1–10 ของด่าน + อันดับของเราเอง (ถ้าอยู่นอก 10 อันดับแรก) · null = ออฟไลน์/ยังไม่มีกระดาน
        public static async Task<List<Row>> Top(string stage)
        {
            if (!Ready) return null;
            try
            {
                var res = await LeaderboardsService.Instance.GetScoresAsync(BoardId(stage), new GetScoresOptions { Limit = 10, IncludeMetadata = true });
                var me = AuthenticationService.Instance.PlayerId;
                var rows = new List<Row>();
                foreach (var e in res.Results) rows.Add(ToRow(e, me));
                if (!rows.Exists(r => r.me))
                {
                    try { var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(BoardId(stage), new GetPlayerScoreOptions { IncludeMetadata = true }); rows.Add(ToRow(mine, me)); }
                    catch (Exception) { /* ยังไม่เคยส่งคะแนนด่านนี้ */ }
                }
                return rows;
            }
            catch (Exception e) { Debug.LogWarning("[Cloud] board failed: " + e.Message); return null; }
        }

        static Row ToRow(Unity.Services.Leaderboards.Models.LeaderboardEntry e, string me)
        {
            string fish = "";
            if (!string.IsNullOrEmpty(e.Metadata)) { var i = e.Metadata.IndexOf("\"fish\":\""); if (i >= 0) { int s = i + 8, t = e.Metadata.IndexOf('"', s); if (t > s) fish = e.Metadata.Substring(s, t - s); } }
            var name = e.PlayerName ?? ""; var hash = name.IndexOf('#'); if (hash > 0) name = name.Substring(0, hash);
            return new Row { rank = e.Rank + 1, name = name, kg = e.Score, fish = fish, me = e.PlayerId == me };
        }
    }
}
