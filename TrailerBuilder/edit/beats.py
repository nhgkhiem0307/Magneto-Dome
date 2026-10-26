# -*- coding: utf-8 -*-
"""
Dò nhịp (BPM) và chỗ "drop" của một bài nhạc, rồi in ra 3 dòng để chép vào timeline.py.
Chạy:  py -3 TrailerBuilder/edit/beats.py "D:/Bt vtc/Capstone Project/Trailer_Footage/music/bai_nhac.mp3"

Cách làm (chỉ cần numpy, không cần thư viện âm nhạc):
  1. Giải mã bài nhạc thành mẫu âm, tính "độ bùng năng lượng" (onset) theo từng khung 10ms.
  2. Tự tương quan đường onset -> khoảng lặp mạnh nhất trong dải 90-160 BPM = nhịp bài.
  3. Tìm pha (vị trí phách đầu) khớp onset nhất.
  4. "Drop" = chỗ năng lượng âm trầm tăng vọt mạnh nhất (so trung bình 4s trước và 4s sau), làm tròn về đầu ô nhịp.
⚠ Kết quả là ƯỚC LƯỢNG. Nghe lại: nếu cảnh cắt lệch nhịp đều đặn thì BPM sai (thử x2 / ÷2), nếu lệch
một khoảng cố định thì chỉnh MUSIC_OFFSET thêm/bớt vài phần trăm giây.
"""
import sys, subprocess
import numpy as np
import imageio_ffmpeg

sys.stdout.reconfigure(encoding='utf-8')
SR = 22050
HOP = 220   # ~10ms


def load(path):
    raw = subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-i', path,
                          '-f', 'f32le', '-ac', '1', '-ar', str(SR), '-'], capture_output=True).stdout
    return np.frombuffer(raw, '<f4').astype(np.float64)


def main(path, drop_beat=16):
    x = load(path)
    n = len(x) // HOP
    frames = x[:n * HOP].reshape(n, HOP)
    spec = np.abs(np.fft.rfft(frames * np.hanning(HOP), axis=1))
    flux = np.maximum(0, np.diff(np.log1p(spec), axis=0)).sum(1)
    flux = np.concatenate([[0], flux]); flux -= flux.mean()
    fps = SR / HOP

    # 2. BPM
    ac = np.correlate(flux, flux, mode='full')[len(flux) - 1:]
    lags = np.arange(len(ac))
    lo, hi = int(fps * 60 / 160), int(fps * 60 / 90)
    lag = lo + np.argmax(ac[lo:hi])
    bpm = 60 * fps / lag
    beat = 60 / bpm

    # 3. pha phách đầu - dò theo TIẾNG TRỐNG TRẦM (dải < 150 Hz): hi-hat hay rơi vào nửa phách, dò theo
    #    toàn dải thì dễ nhận nhầm phách lệch
    nb = max(2, int(150 / (SR / HOP)))
    bflux = np.maximum(0, np.diff(np.log1p(spec[:, :nb]), axis=0)).sum(1)
    bflux = np.concatenate([[0], bflux])
    best, phase = -1, 0.0
    for ph in np.linspace(0, beat, 48, endpoint=False):
        idx = (np.arange(ph, len(flux) / fps, beat) * fps).astype(int)
        idx = idx[idx < len(flux)]
        sc = bflux[idx].sum()
        if sc > best: best, phase = sc, ph

    # 4. drop: năng lượng dải trầm (< 150 Hz) tăng vọt mạnh nhất
    bass = spec[:, :max(2, int(150 / (SR / HOP)))].sum(1)
    w = int(4 * fps)
    score = np.zeros(len(bass))
    for i in range(w, len(bass) - w):
        score[i] = bass[i:i + w].mean() - bass[i - w:i].mean()
    t_drop = np.argmax(score) / fps
    phase = phase % beat
    # Làm tròn về PHÁCH gần nhất (không phải ô nhịp - không biết chắc phách nào là phách 1 của ô)
    t_drop = phase + round((t_drop - phase) / beat) * beat

    offset = t_drop - drop_beat * beat
    print(f'File      : {path}')
    print(f'Độ dài    : {len(x)/SR:.1f}s')
    print(f'BPM ước   : {bpm:.2f}   (phách đầu ở {phase:.3f}s)')
    print(f'Drop ước  : {t_drop:.2f}s trong bài')
    print('\nChép vào timeline.py:')
    print(f'BPM = {bpm:.2f}')
    print(f"MUSIC_FILE = r'{path}'")
    print(f'MUSIC_OFFSET = {offset:.3f}')
    total = 102.4 * beat
    print(f'\n-> Trailer sẽ dài {total:.1f}s (102.4 phách). Trường yêu cầu 30-50s.')
    if total > 50: print('   ⚠ Dài quá 50s: bài nhạc chậm. Chọn bài nhanh hơn (>=124 BPM) hoặc bớt cảnh dồn dập.')
    if offset < 0: print('   ⚠ MUSIC_OFFSET âm: drop của bài đến quá sớm, phần mở màn sẽ im lặng một lúc trước khi vào nhạc.')


if __name__ == '__main__':
    if len(sys.argv) < 2:
        raise SystemExit('Cách dùng: py -3 beats.py <đường dẫn bài nhạc>')
    main(sys.argv[1])
