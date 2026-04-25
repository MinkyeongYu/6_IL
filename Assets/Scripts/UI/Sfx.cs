using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 외부 오디오 파일 없이 런타임에 사인파 + ADSR 엔벨로프로 짧은 효과음을 만들어 재생.
    /// 모든 호출은 정적 — 처음 호출 시 DontDestroyOnLoad AudioSource 를 1개 생성.
    /// 볼륨 마스터는 PlayerPrefs("il6_sfx_vol", 0.6f) 에서 읽음.
    /// </summary>
    public static class Sfx
    {
        private static AudioSource _src;

        private static AudioSource Source()
        {
            if (_src != null) return _src;
            var go = new GameObject("__SfxRunner");
            Object.DontDestroyOnLoad(go);
            _src = go.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 0f; // 2D
            return _src;
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("il6_sfx_vol", 0.6f);
            set { PlayerPrefs.SetFloat("il6_sfx_vol", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static void Hit()       => PlayBlip(820f, 0.06f, 0.7f, decay: 0.04f);
        public static void Pickup()    => PlayBlip(1320f, 0.08f, 0.5f, decay: 0.06f);
        public static void Death()     => PlaySweep(440f, 110f, 0.18f, 0.6f);
        public static void Build()     => PlayBlip(560f, 0.10f, 0.55f, decay: 0.08f);
        public static void Click()     => PlayBlip(660f, 0.04f, 0.4f, decay: 0.03f);
        public static void NightHowl() => PlaySweep(220f, 95f, 0.55f, 0.45f);
        public static void Boss()      => PlaySweep(140f, 60f, 0.8f, 0.7f);

        private static void PlayBlip(float freq, float dur, float vol, float decay)
        {
            var src = Source();
            var clip = MakeSine(freq, dur, decay);
            src.PlayOneShot(clip, vol * Volume);
        }

        private static void PlaySweep(float fromHz, float toHz, float dur, float vol)
        {
            var src = Source();
            var clip = MakeSweep(fromHz, toHz, dur);
            src.PlayOneShot(clip, vol * Volume);
        }

        private static AudioClip MakeSine(float freq, float dur, float decay)
        {
            int sr = 22050;
            int n = Mathf.Max(8, Mathf.CeilToInt(sr * dur));
            var data = new float[n];
            float twoPi = Mathf.PI * 2f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float env = Mathf.Exp(-t / Mathf.Max(0.001f, decay));
                data[i] = Mathf.Sin(twoPi * freq * t) * env;
            }
            var c = AudioClip.Create("blip", n, 1, sr, false);
            c.SetData(data, 0);
            return c;
        }

        private static AudioClip MakeSweep(float fromHz, float toHz, float dur)
        {
            int sr = 22050;
            int n = Mathf.Max(8, Mathf.CeilToInt(sr * dur));
            var data = new float[n];
            float twoPi = Mathf.PI * 2f;
            float phase = 0f;
            float invSr = 1f / sr;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float freq = Mathf.Lerp(fromHz, toHz, k);
                phase += twoPi * freq * invSr;
                float env = (1f - k) * (1f - k);
                data[i] = Mathf.Sin(phase) * env;
            }
            var c = AudioClip.Create("sweep", n, 1, sr, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
