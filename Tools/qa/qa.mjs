// ทดสอบอัตโนมัติก่อนขึ้นเว็บ: เปิดทุกด่านด้วยบอท บนจอคอม/มือถือ/iPad → เก็บภาพ วัดจอขาว/จอดำ นับ error ใน console วัด fps คร่าว ๆ
// รัน (ต้องเปิดเซิร์ฟเวอร์ Builds/WebGL ที่พอร์ต 5190 ก่อน):
//   node <browser-automation skill>/browser.mjs about:blank --script tools/qa/qa.mjs --timeout 1800000
// ตัวเลือกผ่านตัวแปรแวดล้อม: QA_BASE (URL เกม) · QA_STAGES (เช่น paddy,cave) · QA_DEVICES (desktop,phone,pad) · QA_PAR (เปิดพร้อมกันกี่แท็บ)
// ผลลัพธ์: tools/qa/out/report.md + report.json + ภาพทุกจังหวะ (tools/qa/out/shots) · จบด้วยสรุป ผ่าน/ไม่ผ่าน
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(HERE, 'out'), SHOTS = path.join(OUT, 'shots');
const BASE = process.env.QA_BASE || 'http://localhost:5190/index.html';
const ALL_STAGES = ['paddy', 'rapids', 'temple', 'marsh', 'houseboat', 'bend', 'cave', 'mangrove', 'headman', 'reservoir'];
const STAGES = (process.env.QA_STAGES || ALL_STAGES.join(',')).split(',');
const DEVICES = {
  desktop: { viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 },
  phone: { viewport: { width: 844, height: 390 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
  pad: { viewport: { width: 820, height: 1180 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
  laptop: { viewport: { width: 1366, height: 768 }, deviceScaleFactor: 1, hasTouch: true },   // โน้ตบุ๊กจอสัมผัส: ปุ่มขนาดจอสัมผัสบนจอเตี้ย (เคยทับกันตรงนี้)
  padL: { viewport: { width: 1180, height: 820 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
};
const DEV = (process.env.QA_DEVICES || 'desktop,phone').split(',');
const PAR = +(process.env.QA_PAR || 2);
const LOAD_MS = 70000, SHOTS_N = 8, SHOT_GAP = 4000;
// เกณฑ์: จอขาวโพลน (บั๊ก NaN/bloom) · จอดำทั้งหมด (ฉากไม่ขึ้น) · error จริงใน console
const WHITE_FAIL = 0.6, BLACK_FAIL = 0.97;
const IGNORE = [/navigator\.vibrate/i, /Blocked call to navigator/i, /AudioContext was not allowed/i, /favicon/i];

export default async function run(page) {
  fs.mkdirSync(SHOTS, { recursive: true });
  const browser = page.context().browser();
  // ใช้แท็บแรกถอดรหัสภาพ (PNG → พิกเซล) ด้วย canvas ของเบราว์เซอร์เอง ไม่ต้องลงไลบรารีเพิ่ม
  await page.setContent('<canvas id=c></canvas>');
  async function stats(buf) {
    return page.evaluate(async (b64) => {
      const img = await createImageBitmap(await (await fetch('data:image/png;base64,' + b64)).blob());
      const c = document.getElementById('c'); const w = 160, h = Math.round(img.height * 160 / img.width);
      c.width = w; c.height = h; const g = c.getContext('2d'); g.drawImage(img, 0, 0, w, h);
      const d = g.getImageData(0, 0, w, h).data; let white = 0, black = 0, sum = 0;
      for (let i = 0; i < d.length; i += 4) { const l = 0.299 * d[i] + 0.587 * d[i + 1] + 0.114 * d[i + 2]; sum += l; if (l > 225) white++; if (l < 8) black++; }
      const n = d.length / 4; return { white: white / n, black: black / n, mean: sum / n };
    }, buf.toString('base64'));
  }

  const jobs = [];
  for (const s of STAGES) for (const d of DEV) jobs.push({ stage: s, device: d });
  const results = [];
  async function one(job) {
    const ctx = await browser.newContext(DEVICES[job.device]);
    const p = await ctx.newPage();
    const errors = [];
    p.on('console', m => { if ((m.type() === 'error' || /Exception/.test(m.text())) && !IGNORE.some(r => r.test(m.text()))) errors.push(m.text().slice(0, 200)); });
    p.on('pageerror', e => errors.push('pageerror: ' + String(e).slice(0, 200)));
    p.on('crash', () => errors.push('PAGE CRASHED'));
    const t0 = Date.now();
    await p.goto(`${BASE}?bot&stage=${job.stage}&lang=th`, { timeout: 120000 }).catch(e => errors.push('goto: ' + e.message));
    await p.waitForTimeout(LOAD_MS);
    const frames = [];
    for (let i = 0; i < SHOTS_N; i++) {
      try {
        const buf = await p.screenshot({ timeout: 20000 });
        const file = `${job.stage}_${job.device}_${i}.png`; fs.writeFileSync(path.join(SHOTS, file), buf);
        frames.push({ file, ...(await stats(buf)) });
      } catch (e) { errors.push('screenshot: ' + e.message.slice(0, 80)); }
      await p.waitForTimeout(SHOT_GAP);
    }
    // fps คร่าว ๆ (เบราว์เซอร์ทดสอบไม่มี GPU จริง ใช้เทียบกันระหว่างด่าน/ก่อน-หลัง ไม่ใช่ค่าบนมือถือจริง)
    const fps = await p.evaluate(() => new Promise(res => { let n = 0; const t = performance.now(); (function f() { n++; if (performance.now() - t < 3000) requestAnimationFrame(f); else res(n / 3); })(); })).catch(() => 0);
    await ctx.close();
    const maxWhite = Math.max(0, ...frames.map(f => f.white)), allBlack = frames.length > 0 && frames.every(f => f.black > BLACK_FAIL);
    const problems = [];
    if (frames.length === 0) problems.push('ไม่ได้ภาพเลย');
    // แฟลชกล้องตอนได้ปลาขาวทั้งจอได้ 1 เฟรม → ไม่ผ่านต่อเมื่อขาวติดกัน 2 ภาพขึ้นไป (จอขาวค้าง = บั๊กจริง เช่น NaN/bloom)
    const whiteRun = frames.some((f, i) => i > 0 && f.white > WHITE_FAIL && frames[i - 1].white > WHITE_FAIL);
    if (whiteRun) problems.push(`จอขาวค้าง ${(maxWhite * 100).toFixed(0)}%`);
    const warns = maxWhite > WHITE_FAIL && !whiteRun ? ['ขาว 1 ภาพ (น่าจะแฟลชกล้อง)'] : [];
    if (allBlack) problems.push('จอดำทุกภาพ');
    // จอค้าง (เช่นหน้าโหลดค้างเพราะหน่วยความจำเต็ม): ภาพครึ่งหลังเหมือนกันทุกภาพ
    const tail = frames.slice(Math.floor(frames.length / 2));
    if (tail.length >= 3 && tail.every(f => Math.abs(f.mean - tail[0].mean) < 0.3 && Math.abs(f.black - tail[0].black) < 0.003)) problems.push('จอค้าง (ภาพไม่ขยับ)');
    if (errors.length) problems.push(`error ${errors.length}`);
    const r = { ...job, sec: Math.round((Date.now() - t0) / 1000), fps: Math.round(fps), maxWhite, meanLum: frames.length ? frames.reduce((a, f) => a + f.mean, 0) / frames.length : 0, errors, frames, pass: problems.length === 0, problems, warns };
    console.log(`${r.pass ? 'PASS' : 'FAIL'} ${job.stage}/${job.device} fps≈${r.fps} ${problems.concat(warns).join(', ')}`);
    results.push(r);
  }
  // เปิดพร้อมกันทีละ PAR แท็บ (WebGL หนัก เปิดมากไปเครื่องค้าง)
  for (let i = 0; i < jobs.length; i += PAR) await Promise.all(jobs.slice(i, i + PAR).map(one));

  results.sort((a, b) => STAGES.indexOf(a.stage) - STAGES.indexOf(b.stage) || a.device.localeCompare(b.device));
  fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify(results, null, 1));
  const md = [`# QA ${new Date().toISOString()}`, '', `เกม: ${BASE}`, '', '| ด่าน | จอ | ผล | fps≈ | จอขาวสูงสุด | ความสว่างเฉลี่ย | ปัญหา |', '|---|---|---|---|---|---|---|',
    ...results.map(r => `| ${r.stage} | ${r.device} | ${r.pass ? '✅' : '❌'} | ${r.fps} | ${(r.maxWhite * 100).toFixed(0)}% | ${r.meanLum.toFixed(0)} | ${r.problems.concat(r.warns || []).join(', ')} |`),
    '', ...results.filter(r => r.errors.length).map(r => `### ${r.stage}/${r.device}\n` + r.errors.slice(0, 5).map(e => '- ' + e).join('\n'))].join('\n');
  fs.writeFileSync(path.join(OUT, 'report.md'), md);
  const failed = results.filter(r => !r.pass).length;
  return { total: results.length, failed, report: path.join(OUT, 'report.md') };
}
