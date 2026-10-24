# ดนตรีคลิปโปรโมทแบบช้า อบอุ่น 30 วิ (สังเคราะห์เองทั้งหมด) — python tools/make-promo-music-slow.py <out.wav>
#   เสียงพื้นอบอุ่น (pad) + ขิม/ระนาดเบา ๆ ก้องกังวาน + เสียงน้ำ/นกไกล ๆ · 72 bpm · ช่วง 20–26 วิ ไต่ขึ้นนิด ๆ ตอนพลุ · คอร์ดจบยาว
import sys, wave
import numpy as np

SR = 44100; DUR = 30.0
n_all = int(SR * DUR)
out = np.zeros(n_all)
rng = np.random.default_rng(9)

def add(start, sig, gain=1.0):
    i = int(start * SR); n = min(len(sig), n_all - i)
    if n > 0: out[i:i + n] += sig[:n] * gain

def khim(f, dur=2.4):
    t = np.arange(int(SR * dur)) / SR
    env = np.exp(-t * 2.2) * np.minimum(1, t * 400)
    return (np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * f * 2.01 * t) * np.exp(-t * 3) + 0.15 * np.sin(2 * np.pi * f * 3.02 * t) * np.exp(-t * 5)) * env

def pad(freqs, dur, attack=2.5, release=3.0):
    t = np.arange(int(SR * dur)) / SR
    env = np.minimum(1, t / attack) * np.minimum(1, (dur - t) / release)
    s = sum(np.sin(2 * np.pi * f * t + np.sin(2 * np.pi * 0.17 * t) * 0.6) + 0.5 * np.sin(2 * np.pi * f * 1.003 * t) for f in freqs)
    return s * env / len(freqs)

def reverb(x, wet=0.35):
    y = x.copy()
    for d, g in ((0.031, 0.5), (0.047, 0.45), (0.071, 0.4), (0.113, 0.35), (0.167, 0.3), (0.241, 0.25)):
        k = int(d * SR); z = np.zeros_like(x)
        z[k:] = x[:-k]
        for _ in range(3): z[k:] += z[:-k] * g * 0.55
        y += z * wet * g
    return y

# คอร์ดเพนทาโทนิก (C D E G A): C → Am → F(ไม่มี F ใช้ G/D) → C
chords = [(0, [130.8, 196.0, 261.6, 329.6]), (7.5, [110.0, 164.8, 220.0, 261.6]), (15, [98.0, 146.8, 196.0, 293.7]), (22.5, [130.8, 196.0, 261.6, 392.0])]
for st, fr in chords:
    add(st, pad(fr, 8.5 if st < 22 else 7.5), 0.16)

scale = [261.6, 293.7, 329.6, 392.0, 440.0, 523.3, 587.3, 659.3, 784.0, 880.0]
melody = [(1.0, 5), (2.7, 7), (4.3, 6), (5.6, 4),   (8.4, 4), (10.1, 5), (11.6, 7), (13.0, 5),
          (15.8, 3), (17.4, 5), (18.9, 6), (20.2, 7),   (21.4, 8), (22.3, 9), (23.4, 8), (24.6, 7), (25.8, 5)]
for t0, k in melody:
    add(t0, khim(scale[k]), 0.26)
    add(t0 + 0.012, khim(scale[k] * 0.5, 3.0), 0.1)
# ระฆังเบา ๆ ทุกต้นวลี
for t0 in (0.2, 7.6, 15.1, 22.6):
    t = np.arange(int(SR * 4)) / SR
    add(t0, (np.sin(2 * np.pi * 1046 * t) + 0.6 * np.sin(2 * np.pi * 2793 * t) * np.exp(-t * 2)) * np.exp(-t * 1.1) * 0.12)
# คอร์ดจบ
for i, k in enumerate([0, 2, 5, 7]):
    add(26.6 + i * 0.09, khim(scale[k], 3.4), 0.22)
# บรรยากาศ: น้ำไหลเบา ๆ (noise กรองต่ำ) + นกร้องไกล ๆ
w = rng.normal(0, 1, n_all); lo = np.zeros(n_all); a = 0.0
for i in range(n_all): a += (w[i] - a) * 0.02; lo[i] = a
out += lo * 0.25 * (0.6 + 0.4 * np.sin(np.arange(n_all) / SR * 0.5))
for t0 in (3.2, 3.5, 9.8, 12.4, 12.65, 18.1):
    t = np.arange(int(SR * 0.22)) / SR
    add(t0, np.sin(2 * np.pi * (2900 + 900 * np.sin(t * 60)) * t) * np.exp(-t * 14) * 0.05)

out = reverb(out)
fade = np.ones(n_all); fi = int(1.2 * SR); fo = int(2.5 * SR)
fade[:fi] = np.linspace(0, 1, fi); fade[-fo:] = np.linspace(1, 0, fo)
out *= fade; out /= np.max(np.abs(out)) * 1.12
with wave.open(sys.argv[1] if len(sys.argv) > 1 else 'promo_music_slow.wav', 'wb') as wv:
    wv.setnchannels(1); wv.setsampwidth(2); wv.setframerate(SR); wv.writeframes((out * 32767).astype(np.int16).tobytes())
print('ok')
