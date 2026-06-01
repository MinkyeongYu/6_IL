using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 페이즈별 절차적 음악 루프. 외부 오디오 파일 없이 사인파 chord progression 으로 생성.
    /// 페이즈 변경 시 오디오소스의 clip 만 교체 + crossfade.
    /// </summary>
    public static class Music
    {
        private static AudioSource _src;
        private static AudioClip _day, _night, _evening, _dawn;
        private static Phase? _currentPhase;

        private static AudioSource Source()
        {
            if (_src != null) return _src;
            var go = new GameObject("__MusicRunner");
            Object.DontDestroyOnLoad(go);
            _src = go.AddComponent<AudioSource>();
            _src.loop = true;
            _src.spatialBlend = 0f;
            _src.volume = Volume * 0.5f; // BGM 은 SFX 의 절반
            return _src;
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("il6_bgm_vol", 0.5f);
            set { PlayerPrefs.SetFloat("il6_bgm_vol", Mathf.Clamp01(value)); PlayerPrefs.Save();
                if (_src != null) _src.volume = Mathf.Clamp01(value) * 0.5f; }
        }

        public static void PlayForPhase(Phase phase)
        {
            if (_currentPhase == phase) return;
            _currentPhase = phase;
            var src = Source();
            AudioClip target = phase switch
            {
                Phase.Day => _day ??= MakeLoop(new[] { 196f, 247f, 294f }, peaceful: true, seed: 11),  // G major
                Phase.Night => _night ??= MakeLoop(new[] { 110f, 131f, 156f }, peaceful: false, seed: 23), // A minor 저음
                Phase.Evening => _evening ??= MakeLoop(new[] { 165f, 196f, 247f }, peaceful: false, seed: 31),
                Phase.Dawn => _dawn ??= MakeLoop(new[] { 220f, 277f, 330f }, peaceful: true, seed: 41), // 가벼운 메이저
                _ => null,
            };
            if (target == null) return;
            src.clip = target;
            src.Play();
        }

        public static void Stop()
        {
            if (_src != null) _src.Stop();
            _currentPhase = null;
        }

        /// <summary>
        /// 사인파 합성으로 8초 분량 loop AudioClip 생성. chord(허츠 배열) 위에 가벼운 멜로디.
        /// peaceful=true 면 더 밝고 느린 envelope, false 면 어둡고 무거운 톤.
        /// </summary>
        private static AudioClip MakeLoop(float[] chord, bool peaceful, int seed)
        {
            const int sr = 22050;
            float dur = 8f;
            int n = (int)(sr * dur);
            var data = new float[n];
            float twoPi = Mathf.PI * 2f;
            var rng = new System.Random(seed);

            // chord 톤 — 깔리는 사운드 (드론)
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float v = 0f;
                foreach (var hz in chord)
                {
                    v += Mathf.Sin(twoPi * hz * t) * 0.18f;
                }
                // 부드러운 풀 페이드 인/아웃 + 진폭 변조 (호흡감)
                float breathe = 0.85f + 0.15f * Mathf.Sin(twoPi * (peaceful ? 0.13f : 0.08f) * t);
                float fade = Mathf.Min(t / 0.6f, (dur - t) / 0.6f);
                fade = Mathf.Clamp01(fade);
                data[i] = v * breathe * fade * 0.45f;
            }

            // 가벼운 멜로디 — peaceful 이면 밝은 ping, 아니면 묵직한 hit
            int notes = peaceful ? 6 : 4;
            for (int k = 0; k < notes; k++)
            {
                float startSec = (k + 0.5f) * (dur / notes);
                float noteDur = peaceful ? 0.6f : 1.0f;
                float pitch = chord[rng.Next(chord.Length)] * (peaceful ? 2f : 1f);
                float decay = peaceful ? 0.3f : 0.6f;
                float volume = peaceful ? 0.22f : 0.30f;

                int startI = (int)(startSec * sr);
                int noteN = (int)(noteDur * sr);
                for (int i = 0; i < noteN && startI + i < n; i++)
                {
                    float lt = i / (float)sr;
                    float env = Mathf.Exp(-lt / decay);
                    data[startI + i] += Mathf.Sin(twoPi * pitch * lt) * env * volume;
                }
            }

            // 부드러운 normalize — clip 방지
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak > 0.001f) { float scale = 0.7f / peak; for (int i = 0; i < n; i++) data[i] *= scale; }

            var clip = AudioClip.Create("music", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
