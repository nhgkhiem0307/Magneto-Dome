# -*- coding: utf-8 -*-
"""
Tổng hợp tiếng hiệu ứng kiểu trailer bằng code (không dùng file tải về -> không vướng bản quyền).
Chạy:  py -3 TrailerBuilder/edit/sfx.py        -> ghi các file .wav vào TrailerBuilder/edit/sfx/

  whoosh.wav      tiếng vụt khi chuyển cảnh (0.55s)
  whoosh_rev.wav  vụt ngược - hút vào trước cú cắt (0.5s)
  boom.wav        cú đập trầm khi chữ đập vào màn hình (1.6s)
  hit.wav         cú va ngắn, gắt (0.5s)
  riser.wav       tiếng dâng dần trước đoạn bùng nổ (3.75s, cắt cụt ở cuối)
  zap.wav         rít điện / glitch từ tính (0.35s)
  subdrop.wav     tiếng trầm tụt xuống - kết thúc, màn logo (2.2s)
  temp_beat.wav   nhịp trống TẠM (128 BPM) để dựng thử khi chưa có nhạc thật
"""
import os, sys, wave
import numpy as np

SR = 48000
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'sfx')
rng = np.random.default_rng(7)
sys.stdout.reconfigure(encoding='utf-8')   # in được chữ tiếng Việt trên cửa sổ lệnh Windows


def t_axis(sec):
    return np.arange(int(SR * sec)) / SR


def onepole_lp(x, cutoff):
    """Lọc thông thấp một cực; cutoff có thể là mảng (thay đổi theo thời gian)."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=np.float64), x.shape)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a[i]) * x[i] + a[i] * acc
        y[i] = acc
    return y


def bandpass(x, center):
    lo = onepole_lp(x, np.asarray(center) * 1.6)
    return lo - onepole_lp(lo, np.asarray(center) * 0.6)


def norm(x, peak=0.9):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


def save(name, x, stereo_width=0.0):
    os.makedirs(OUT, exist_ok=True)
    x = np.clip(x, -1, 1)
    if stereo_width > 0:
        d = int(SR * 0.012)
        r = np.concatenate([np.zeros(d), x[:-d]])
        l, r = x, (1 - stereo_width) * x + stereo_width * r
    else:
        l = r = x
    data = (np.stack([l, r], 1) * 32767).astype('<i2')
    with wave.open(os.path.join(OUT, name), 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())
    print('  ', name, f'{len(x)/SR:.2f}s')


def whoosh(sec=0.55, reverse=False):
    t = t_axis(sec)
    p = t / sec
    env = np.sin(np.pi * p) ** 1.6 * (1 - 0.6 * p)
    center = 300 + 4200 * np.sin(np.pi * p) ** 2       # quét lên rồi xuống
    x = bandpass(rng.standard_normal(len(t)), center) * env
    x = norm(x, 0.85)
    if reverse:
        x = x[::-1] * np.linspace(0.3, 1, len(x)) ** 2
    return x


def boom(sec=1.6):
    t = t_axis(sec)
    f = 32 + 48 * np.exp(-t * 9)                       # cao độ tụt nhanh
    sub = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 2.6)
    click = rng.standard_normal(len(t)) * np.exp(-t * 60) * 0.5
    body = onepole_lp(rng.standard_normal(len(t)), 900) * np.exp(-t * 7) * 2.2
    return norm(np.tanh((sub * 1.4 + click + body) * 1.6), 0.95)


def hit(sec=0.5):
    t = t_axis(sec)
    tone = np.sin(2 * np.pi * (60 + 140 * np.exp(-t * 30)) * t) * np.exp(-t * 11)
    crack = bandpass(rng.standard_normal(len(t)), 2500) * np.exp(-t * 35) * 3
    return norm(np.tanh((tone + crack) * 2), 0.9)


def riser(sec=3.75):
    t = t_axis(sec)
    p = t / sec
    f = 180 * (1 + 7 * p ** 2)
    saw = 2 * ((np.cumsum(f) / SR) % 1) - 1
    saw2 = 2 * ((np.cumsum(f * 1.01) / SR) % 1) - 1
    noise = bandpass(rng.standard_normal(len(t)), 500 + 7000 * p ** 1.5)
    trem = 0.75 + 0.25 * np.sin(2 * np.pi * (4 + 18 * p ** 2) * t)
    x = (onepole_lp(saw + saw2, 600 + 6000 * p) * 0.5 + noise * 1.2) * (p ** 2.2) * trem
    return norm(x, 0.85)


def zap(sec=0.35):
    t = t_axis(sec)
    x = np.zeros(len(t))
    i = 0
    while i < len(t):                                   # những đoạn sóng vuông tần số ngẫu nhiên
        n = int(SR * rng.uniform(0.012, 0.04))
        f = rng.uniform(300, 2600)
        seg = np.sign(np.sin(2 * np.pi * f * np.arange(n) / SR))
        x[i:i + n] = seg[:len(x[i:i + n])] * rng.uniform(0.4, 1)
        i += n + int(SR * rng.uniform(0, 0.015))
    x = onepole_lp(x, 5000) * np.exp(-t * 7)
    x += bandpass(rng.standard_normal(len(t)), 6000) * np.exp(-t * 14) * 1.5
    return norm(x, 0.7)


def subdrop(sec=2.2):
    t = t_axis(sec)
    f = 90 * np.exp(-t * 1.3) + 28
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.minimum(1, t * 30) * np.exp(-t * 1.1)
    return norm(np.tanh(x * 1.5), 0.95)


def temp_beat(sec=52.0, bpm=128.0, drop=7.5):
    """Nhịp trống tạm: trước drop chỉ có tiếng tích nhẹ, sau drop có trống đầy đủ."""
    beat = 60.0 / bpm
    out = np.zeros(int(SR * sec))
    kick_t = t_axis(0.35)
    kick = np.sin(2 * np.pi * np.cumsum(50 + 110 * np.exp(-kick_t * 35)) / SR) * np.exp(-kick_t * 9)
    hat_t = t_axis(0.06)
    hat = bandpass(rng.standard_normal(len(hat_t)), 8000) * np.exp(-hat_t * 70) * 2
    clap_t = t_axis(0.25)
    clap = bandpass(rng.standard_normal(len(clap_t)), 1600) * np.exp(-clap_t * 18) * 2.5
    bass_t = t_axis(beat * 0.9)

    def put(sig, at, g=1.0):
        i = int(at * SR)
        if i >= len(out): return
        n = min(len(sig), len(out) - i)
        out[i:i + n] += sig[:n] * g

    n_beats = int(sec / beat)
    for b in range(n_beats):
        at = b * beat
        if at < drop:
            if b % 2 == 0: put(hat, at, 0.25)
            continue
        if at > 41.25 and at < 41.25 + beat * 2:   # chừa khoảng lặng ở cú đập cuối
            continue
        if at > 43.0:
            break
        put(kick, at, 0.9)
        put(hat, at + beat / 2, 0.35)
        if b % 2 == 1: put(clap, at, 0.5)
        root = 55.0 if (b // 8) % 2 == 0 else 49.0
        put(np.sin(2 * np.pi * root * bass_t) * np.exp(-bass_t * 3), at, 0.35)
    return norm(np.tanh(out * 1.2), 0.8)


if __name__ == '__main__':
    print('Ghi tiếng hiệu ứng vào', OUT)
    save('whoosh.wav', whoosh(), 0.5)
    save('whoosh_rev.wav', whoosh(0.5, True), 0.5)
    save('boom.wav', boom())
    save('hit.wav', hit())
    save('riser.wav', riser(), 0.6)
    save('zap.wav', zap(), 0.4)
    save('subdrop.wav', subdrop())
    save('temp_beat.wav', temp_beat(), 0.2)
