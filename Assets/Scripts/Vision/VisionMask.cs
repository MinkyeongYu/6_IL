using UnityEngine;
using IL6.Events;
using System.Collections.Generic;

namespace IL6
{
    /// <summary>
    /// 시야 마스크 — 밤에 그라데이션 어둠 + 플레이어 시야 원 + 모닥불 시야 원.
    /// IMGUI OnGUI 에서 전체화면 어둠 레이어를 직접 그림으로써
    /// SpriteMask 한계(경계 하드엣지) 없이 소프트 그라데이션 구현.
    /// </summary>
    public sealed class VisionMask : MonoBehaviour
    {
        public Transform Target;
        public float RadiusUnits = 3.2f; // = 10 tiles * 32px / 100 PPU
        public Camera Cam;

        // ── 그라데이션 파라미터 ────────────────────────────────────────────
        /// <summary>어둠 최대 알파 (밤). 0.92 = 거의 완전 어둠.</summary>
        public float NightOuterAlpha = 0.92f;
        /// <summary>시야 반경의 몇 배 지점에서 완전 어둠이 되는지 (예: 1.3 = 130%).</summary>
        public float GradientEdgeMul = 1.3f;
        /// <summary>마스크 텍스처 가로 해상도. 낮을수록 가볍고, 높을수록 경계가 부드러움.</summary>
        public int MaskWidth = 256;

        private bool _isNight;
        private System.Action _unsubE, _unsubN, _unsubD, _unsubA;

        private Texture2D _maskTex;
        private Color[] _maskPixels;
        private int _maskW;
        private int _maskH;
        private readonly List<VisionHole> _holes = new();

        private struct VisionHole
        {
            public Vector2 ScreenCenter;
            public float InnerRadius;
            public float OuterRadius;
        }

        private void Awake()
        {
            if (Cam == null) Cam = Camera.main;
        }

        private void Start()
        {
            _unsubE = EventBus.Instance.Subscribe<EveningStartedPayload>(_ => _isNight = false);
            _unsubN = EventBus.Instance.Subscribe<NightStartedPayload>(_ => _isNight = true);
            _unsubD = EventBus.Instance.Subscribe<DawnStartedPayload>(_ => _isNight = false);
            _unsubA = EventBus.Instance.Subscribe<DayStartedPayload>(_ => _isNight = false);
            var s = GameSession.Instance;
            _isNight = s != null && s.Cycle != null && s.Cycle.Phase == Phase.Night;
        }

        private void OnDestroy()
        {
            _unsubE?.Invoke(); _unsubN?.Invoke(); _unsubD?.Invoke(); _unsubA?.Invoke();
        }

        private void OnGUI()
        {
            if (!_isNight) return;
            if (Cam == null || Target == null) return;
            if (Event.current.type != EventType.Repaint) return;

            DrawDarknessWithHoles();
        }

        // ── SetRadius 공개 API (기존 호환) ───────────────────────────────
        public void SetRadius(float radiusUnits)
        {
            RadiusUnits = radiusUnits;
        }

        // ── 핵심: 시야 안은 투명, 시야 밖은 검정인 알파 마스크를 직접 생성 ──
        private void DrawDarknessWithHoles()
        {
            _holes.Clear();
            AddVisionHole(Target.position, RadiusUnits);

            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            foreach (var a in auras)
            {
                if (a == null || !a.IsActive) continue;
                AddVisionHole(a.transform.position, a.VisionRadius);
            }

            float lookoutRadius = BuildingUpgradeRules.LookoutVisionRadius();
            if (lookoutRadius > 0f)
            {
                var buildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
                foreach (var b in buildings)
                {
                    if (b == null || b.CurrentHp <= 0 || b.Kind != BuildingKind.LookoutPost) continue;
                    AddVisionHole(b.transform.position, lookoutRadius);
                }
            }

            RebuildMaskTexture();
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _maskTex, ScaleMode.StretchToFill);
        }

        private void AddVisionHole(Vector3 worldPos, float innerRadius)
        {
            if (innerRadius <= 0f) return;

            Vector3 sp = Cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0) return;

            float pixelRadius = WorldToScreenRadius(worldPos, innerRadius);
            _holes.Add(new VisionHole
            {
                ScreenCenter = new Vector2(sp.x, Screen.height - sp.y),
                InnerRadius = pixelRadius,
                OuterRadius = pixelRadius * GradientEdgeMul,
            });
        }

        private void RebuildMaskTexture()
        {
            int targetW = Mathf.Max(64, MaskWidth);
            int targetH = Mathf.Max(36, Mathf.RoundToInt(targetW * (Screen.height / Mathf.Max(1f, (float)Screen.width))));

            if (_maskTex == null || _maskW != targetW || _maskH != targetH)
            {
                _maskW = targetW;
                _maskH = targetH;
                _maskTex = new Texture2D(_maskW, _maskH, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _maskPixels = new Color[_maskW * _maskH];
            }

            float sx = Screen.width / (float)_maskW;
            float sy = Screen.height / (float)_maskH;
            for (int y = 0; y < _maskH; y++)
            {
                float screenY = (y + 0.5f) * sy;
                for (int x = 0; x < _maskW; x++)
                {
                    float screenX = (x + 0.5f) * sx;
                    float alpha = AlphaAtScreenPoint(screenX, screenY);
                    _maskPixels[y * _maskW + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            _maskTex.SetPixels(_maskPixels);
            _maskTex.Apply(false);
        }

        private float AlphaAtScreenPoint(float x, float y)
        {
            float alpha = NightOuterAlpha;
            for (int i = 0; i < _holes.Count; i++)
            {
                var h = _holes[i];
                float dist = Vector2.Distance(new Vector2(x, y), h.ScreenCenter);
                float holeAlpha = dist <= h.InnerRadius
                    ? 0f
                    : NightOuterAlpha * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(h.InnerRadius, h.OuterRadius, dist));
                if (holeAlpha < alpha) alpha = holeAlpha;
            }
            return alpha;
        }

        /// <summary>월드 유닛 반경을 현재 카메라 스크린 픽셀 반경으로 변환.</summary>
        private float WorldToScreenRadius(Vector3 worldPos, float worldRadius)
        {
            Vector3 center = Cam.WorldToScreenPoint(worldPos);
            Vector3 edge = Cam.WorldToScreenPoint(worldPos + Vector3.right * worldRadius);
            return Mathf.Abs(edge.x - center.x);
        }

        private void LateUpdate()
        {
            // 카메라 위치 추적 (이전 방식 호환 — SpriteMask 없어도 유지)
            if (Cam != null)
                transform.position = new Vector3(Cam.transform.position.x, Cam.transform.position.y, 0);
        }
    }
}
