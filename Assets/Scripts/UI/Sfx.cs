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
        private static AudioClip _cHit, _cPickup, _cDeath, _cBuild, _cClick, _cNight, _cBoss;

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

        public static void Hit()       { _cHit ??= MakeSine(820f, 0.06f, 0.04f);    Source().PlayOneShot(_cHit, 0.7f * Volume); }
        public static void Pickup()    { _cPickup ??= MakeSine(1320f, 0.08f, 0.06f); Source().PlayOneShot(_cPickup, 0.5f * Volume); }
        public static void Death()     { _cDeath ??= MakeSweep(440f, 110f, 0.18f);  Source().PlayOneShot(_cDeath, 0.6f * Volume); }
        public static void Build()     { _cBuild ??= MakeSine(560f, 0.10f, 0.08f);  Source().PlayOneShot(_cBuild, 0.55f * Volume); }
        public static void Click()     { _cClick ??= MakeSine(660f, 0.04f, 0.03f);  Source().PlayOneShot(_cClick, 0.4f * Volume); }
        public static void NightHowl() { _cNight ??= MakeSweep(220f, 95f, 0.55f);   Source().PlayOneShot(_cNight, 0.45f * Volume); }
        public static void Boss()      { _cBoss ??= MakeSweep(140f, 60f, 0.8f);     Source().PlayOneShot(_cBoss, 0.7f * Volume); }

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
