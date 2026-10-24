# ดนตรีประกอบคลิปโปรโมท 30 วิ (สังเคราะห์เองทั้งหมด) — python tools/make-promo-music.py <out.wav>
#   0–15 วิ ระนาดเพนทาโทนิกสดใส + กลองทน · 15–21.5 วิ ช่วงผี: เสียงทุ้มหลอน หัวใจเต้น เสียงกระชาก · 21.5–27 ทำนองกลับมา · 27–30 คอร์ดจบ
import sys, wave
import numpy as np

SR = 44100; DUR = 30.0
t_all = np.arange(int(SR * DUR)) / SR
out = np.zeros_like(t_all)
rng = np.random.default_rng(5)

def add(start, sig, gain=1.0):
    i = int(start * SR); n = min(len(sig), len(out) - i)
    if n > 0: out[i:i + n] += sig[:n] * gain

def ranat(f, dur=0.6):
    t = np.arange(int(SR * dur)) / SR
    env = np.exp(-t * 7.5)
    return (np.sin(2 * np.pi * f * t) + 0.45 * np.sin(2 * np.pi * f * 3.95 * t) * np.exp(-t * 14) + 0.2 * np.sin(2 * np.pi * f * 2 * t)) * env

def thon(dur=0.35):
    t = np.arange(int(SR * dur)) / SR
    return np.sin(2 * np.pi * (110 - 40 * t) * t) * np.exp(-t * 10) + rng.normal(0, 1, len(t)) * np.exp(-t * 50) * 0.15

def ching(dur=0.5):
    t = np.arange(int(SR * dur)) / SR
    return (np.sin(2 * np.pi * 2700 * t) + np.sin(2 * np.pi * 3410 * t)) * np.exp(-t * 9) * 0.25

# สเกลเพนทาโทนิกไทยประมาณ (C D E G A)
scale = [261.6, 293.7, 329.6, 392.0, 440.0, 523.3, 587.3, 659.3, 784.0, 880.0]
motif = [5, 7, 8, 7, 5, 4, 5, 2, 4, 5, 7, 9, 8, 7, 5, 4]
beat = 60 / 104

def melody(t0, t1, bright=1.0):
    k = 0; t = t0
    while t < t1 - 0.3:
        n = motif[k % len(motif)]
        add(t, ranat(scale[n] * bright), 0.32)
        if k % 2 == 0: add(t, ranat(scale[max(0, n - 5)] * bright * 0.5, 0.9), 0.14)   # เสียงต่ำคู่
        if k % 4 == 0: add(t, thon(), 0.45)
        if k % 4 == 2: add(t, ching(), 0.35)
        t += beat / 2; k += 1

melody(0.0, 15.2)
# ---------- ช่วงผี ----------
t = np.arange(int(SR * 6.3)) / SR
drone = (np.sin(2 * np.pi * 55 * t) + 0.6 * np.sin(2 * np.pi * 58.3 * t) + 0.3 * np.sin(2 * np.pi * 110.5 * t)) * np.minimum(1, t / 0.8) * 0.35
add(15.2, drone)
for hb in np.arange(15.6, 21.4, 0.85):   # หัวใจเต้น ตุบ-ตุบ
    for dt, g in ((0, 1), (0.22, 0.7)):
        tt = np.arange(int(SR * 0.18)) / SR
        add(hb + dt, np.sin(2 * np.pi * 50 * tt) * np.exp(-tt * 26) * g, 0.8)
for st in (15.5, 18.7):   # เสียงกระชาก: ตูมต่ำ + แหลมไถลลง + ซ่า
    tt = np.arange(int(SR * 1.6)) / SR
    sting = np.sin(2 * np.pi * (48 - tt * 10) * tt) * np.exp(-tt * 2.5) + 0.5 * np.sin(2 * np.pi * (1500 - tt * 900) * tt) * np.exp(-tt * 3) + rng.normal(0, 1, len(tt)) * np.exp(-tt * 6) * 0.3
    add(st, sting, 0.55)
# ---------- ทำนองกลับมา + คอร์ดจบ ----------
melody(21.5, 27.0, 1.0)
for i, n in enumerate([5, 7, 9]):
    add(27.0 + i * 0.06, ranat(scale[n], 3.0) * 1.0, 0.3)
add(27.0, thon(0.6), 0.6); add(27.0, ching(1.5), 0.5)
# fade in/out + normalize
fade = np.ones_like(out); fi = int(0.3 * SR); fo = int(1.5 * SR)
fade[:fi] = np.linspace(0, 1, fi); fade[-fo:] = np.linspace(1, 0, fo)
out *= fade; out /= np.max(np.abs(out)) * 1.15
pcm = (out * 32767).astype(np.int16)
with wave.open(sys.argv[1] if len(sys.argv) > 1 else 'promo_music.wav', 'wb') as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
print('ok')
