"""ภาพรวมกระดานปลาใหญ่ประจำสัปดาห์ สำหรับหน้าแรกเว็บ (GitHub Actions รันทุกชั่วโมง)

ดึง 5 อันดับแรกของทุกกระดาน bigfish_<ด่าน> จาก Unity Gaming Services ด้วย service account (สิทธิ์ Leaderboards Viewer)
รวมกันแล้วเรียงตามน้ำหนัก เก็บ 5 ตัวที่หนักที่สุด → leaderboard.json (หน้าแรกอ่านไฟล์นี้ ไม่ต้องล็อกอินผู้เข้าชม)

ตัวแปรแวดล้อม: UGS_KEY_ID, UGS_SECRET_KEY (GitHub Secrets — ห้ามใส่ในโค้ด) · OUT (ที่เก็บไฟล์ผลลัพธ์)
"""
import base64, json, os, sys, urllib.error, urllib.request
from datetime import datetime, timezone

PROJECT = "dc8b4b3f-26a0-4744-a764-267157818139"   # cloudProjectId ใน ProjectSettings (ไม่ใช่ความลับ)
STAGES = {   # ชื่อด่านตาม Assets/Game/Scripts/Core/Stages.cs
    "paddy": "บ่อปลาทุ่งนา", "rapids": "แก่งหินตาปลา", "temple": "วังปลาโบสถ์ร้าง", "marsh": "หนองบัวซ่อนศาล", "houseboat": "หลืบแพสุสาน",
    "bend": "คุ้งน้ำมืดป่าสนเถา", "cave": "ถ้ำลอดรอยจารึก", "mangrove": "ป่าชายเลนรากมรณะ", "headman": "ท่าน้ำบ้านผู้ใหญ่",
    "reservoir": "เขื่อนม่านหมอกยอดดอย", "squid": "เรือไดหมึกคลื่นมรณะ", "mekong": "แก่งอาถรรพ์พญานาค",
}
API = "https://services.api.unity.com"


def basic():
    kid, sec = os.environ.get("UGS_KEY_ID", ""), os.environ.get("UGS_SECRET_KEY", "")
    if not kid or not sec:
        sys.exit("UGS_KEY_ID / UGS_SECRET_KEY not set")
    return "Basic " + base64.b64encode(f"{kid}:{sec}".encode()).decode()


def get(url, auth):
    req = urllib.request.Request(url, headers={"Authorization": auth, "Accept": "application/json"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode())


def fish_names():
    p = os.path.join(os.path.dirname(__file__), "..", "Assets", "Game", "Resources", "Data", "species.json")
    try:
        d = json.load(open(p, encoding="utf-8"))
        items = d.get("species", d) if isinstance(d, dict) else d
        return {s["id"]: s.get("name", s["id"]) for s in items}
    except Exception:
        return {}


def main():
    auth = basic()
    envs = get(f"{API}/unity/v1/projects/{PROJECT}/environments", auth).get("results", [])
    env = next((e for e in envs if e.get("name") == "production"), envs[0] if envs else None)
    if env is None:
        sys.exit("no environment")
    names = fish_names()
    rows = []
    for stage, stage_name in STAGES.items():
        url = f"{API}/leaderboards/v1/projects/{PROJECT}/environments/{env['id']}/leaderboards/bigfish_{stage}/scores?limit=5&includeMetadata=true"
        try:
            res = get(url, auth).get("results", [])
        except urllib.error.HTTPError as e:
            print(f"{stage}: HTTP {e.code}", file=sys.stderr)
            continue
        for r in res:
            meta = r.get("metadata") or {}
            if isinstance(meta, str):
                try:
                    meta = json.loads(meta)
                except Exception:
                    meta = {}
            fid = meta.get("fish", "")
            name = (r.get("playerName") or "ชาวประมงนิรนาม").split("#")[0]   # ตัดเลขท้ายชื่อที่ระบบสุ่มให้
            rows.append({"name": name, "kg": round(float(r.get("score", 0)), 2), "fish": names.get(fid, ""), "stage": stage_name})
    rows.sort(key=lambda x: -x["kg"])
    out = {"updated": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"), "entries": rows[:5]}
    path = os.environ.get("OUT", "leaderboard.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    print(f"wrote {path}: {len(out['entries'])} entries from {len(rows)} scores")


if __name__ == "__main__":
    main()
