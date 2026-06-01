using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 런타임에 눈 ParticleSystem 생성. 에셋 없이 작동.
    /// Camera 자식에 부착하면 카메라 따라 움직이며 상시 내리는 눈.
    /// </summary>
    public sealed class SnowEmitter : MonoBehaviour
    {
        public int Rate = 40;
        public float AreaWidth = 32f;
        public float TopOffset = 10f;
        public float FallSpeed = 1.2f;
        public float Lifetime = 10f;
        public float ParticleSize = 0.12f;

        private void Awake()
        {
            var go = new GameObject("SnowParticles");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, TopOffset, 0);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = Lifetime;
            main.startSpeed = 0f;
            main.startSize = ParticleSize;
            main.startColor = new Color(1f, 1f, 1f, 0.85f);
            main.gravityModifier = 0f;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;

            var emission = ps.emission;
            emission.rateOverTime = Rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(AreaWidth, 0.1f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            // Unity 요구사항: x/y/z 가 같은 모드여야 함 — 둘 다 TwoConstants 로 통일
            vel.y = new ParticleSystem.MinMaxCurve(-FallSpeed * 1.1f, -FallSpeed * 0.9f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.1f, 1f),
                new Keyframe(1f, 0.8f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 15;

            // 런타임 머티리얼: Built-in Sprites/Default → URP 대체 → 최후 Unlit
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            var mat = new Material(shader);
            var tex = MakeSnowflakeTexture(16);
            mat.mainTexture = tex;
            renderer.sharedMaterial = mat;
        }

        private static Texture2D MakeSnowflakeTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
            };
            float r = size / 2f;
            var transparent = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d / r);
                    alpha = alpha * alpha; // soften edges
                    tex.SetPixel(x, y, alpha > 0.01f ? new Color(1f, 1f, 1f, alpha) : transparent);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
