using UnityEngine;
using IL6.Events;

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
        /// <summary>그라데이션 스텝 수 — 많을수록 부드럽고 GPU 비용 증가.</summary>
        public int GradientSteps = 24;

        private bool _isNight;
        private System.Action _unsubE, _unsubN, _unsubD, _unsubA;

        // 그라데이션용 1x1 텍스처 캐시
        private static Texture2D _whiteTex;

        private void Awake()
        {
            if (Cam == null) Cam = Camera.main;
            EnsureWhiteTex();
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

        private static void EnsureWhiteTex()
        {
            if (_whiteTex != null) return;
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        private void OnGUI()
        {
            if (!_isNight) return;
            if (Cam == null || Target == null) return;

            DrawDarknessWithHoles();
        }

        // ── SetRadius 공개 API (기존 호환) ───────────────────────────────
        public void SetRadius(float radiusUnits)
        {
            RadiusUnits = radiusUnits;
        }

        // ── 핵심: 전체화면 어둠 + 밝은 원(플레이어 + 모닥불) 그라데이션 ──
        private void DrawDarknessWithHoles()
        {
            // IMGUI 페인터 알고리즘 활용:
            // (a) 전체 어둠 베이스를 먼저 그린다
            // (b) 각 시야 원에 대해 "바깥 → 안쪽" 방향으로 동심원을 쌓는다.
            //     가장 바깥 링 = NightOuterAlpha(어둠), 안쪽으로 갈수록 알파 0(투명) 으로 덮어쓰며
            //     앞서 그린 어둠을 지운다. 이 방식이 IMGUI AddAlpha 모드 없이도 작동함.

            // 1) 전체 어둠 베이스
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, NightOuterAlpha));

            // 2) 플레이어 시야 원 그라데이션 (구멍 뚫기)
            DrawVisionHole(Target.position, RadiusUnits);

            // 3) 모닥불 시야 원 그라데이션
            var auras = Object.FindObjectsByType<CampfireAura>(FindObjectsSortMode.None);
            foreach (var a in auras)
            {
                if (a == null || !a.IsActive) continue;
                DrawVisionHole(a.transform.position, a.VisionRadius);
            }
        }

        /// <summary>
        /// 월드 좌표 center 주변을 밝게 뚫는다.
        /// IMGUI 페인터 방식: 큰 원(바깥=어둠 alpha)부터 작은 원(안쪽=alpha 0) 순으로 그려
        /// 앞서 그린 어둠 베이스를 점점 투명하게 덮어 그라데이션 효과 생성.
        /// </summary>
        private void DrawVisionHole(Vector3 worldPos, float innerRadius)
        {
            if (innerRadius <= 0f) return;

            float pixelRadius = WorldToScreenRadius(innerRadius);
            float outerPixelRadius = pixelRadius * GradientEdgeMul;

            Vector3 sp = Cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0) return;
            float cx = sp.x;
            float cy = Screen.height - sp.y;

            // 바깥 → 안쪽 순서로 그림 (IMGUI 페인터 알고리즘: 나중에 그린 것이 앞)
            // step i=0: 가장 바깥 원 (반경=outerPixelRadius, alpha=NightOuterAlpha — 베이스와 동일, 효과 없음)
            // step i=GradientSteps-1: 가장 안쪽 원 (반경=pixelRadius, alpha=0 — 완전히 투명으로 덮어씀)
            for (int i = 0; i < GradientSteps; i++)
            {
                float t = i / (float)(GradientSteps - 1); // 0 = 바깥, 1 = 안쪽
                float r = Mathf.Lerp(outerPixelRadius, pixelRadius, t);
                // SmoothStep: 바깥=NightOuterAlpha → 안쪽=0
                float alpha = NightOuterAlpha * (1f - Mathf.SmoothStep(0f, 1f, t));
                DrawCircleQuad(cx, cy, r, new Color(0f, 0f, 0f, alpha));
            }

            // 가장 안쪽 완전 투명 원 (시야 중심 = 완전히 밝음)
            DrawCircleQuad(cx, cy, pixelRadius, new Color(0f, 0f, 0f, 0f));
        }

        /// <summary>
        /// IMGUI 에서 원형 마스크를 근사. 여러 겹의 작은 사각형을 원 경계에 배치해 원 모양을 만든다.
        /// 정밀도는 GradientSteps 와 별개로 원의 분할 수(circleSegs)에 비례.
        /// </summary>
        private void DrawCircleQuad(float cx, float cy, float r, Color col)
        {
            if (r <= 0f) return;
            // 원형 근사: 원 내부 전체를 덮는 사각 격자 방식
            // 반지름 r 의 원 내부를 rows×cols 격자로 채움
            int segs = Mathf.Clamp(Mathf.RoundToInt(r / 4f), 6, 40);
            float step = r * 2f / segs;
            for (int row = 0; row < segs; row++)
            {
                float py = cy - r + row * step;
                float relY = (py + step * 0.5f) - cy;
                float halfW = Mathf.Sqrt(Mathf.Max(0f, r * r - relY * relY));
                if (halfW <= 0f) continue;
                DrawRect(new Rect(cx - halfW, py, halfW * 2f, step + 1f), col);
            }
        }

        /// <summary>월드 유닛 반경을 현재 카메라 스크린 픽셀 반경으로 변환.</summary>
        private float WorldToScreenRadius(float worldRadius)
        {
            // 카메라 중심 기준 두 점의 스크린 거리로 반경 계산
            Vector3 center = Cam.WorldToScreenPoint(Target.position);
            Vector3 edge = Cam.WorldToScreenPoint(Target.position + Vector3.right * worldRadius);
            return Mathf.Abs(edge.x - center.x);
        }

        private static void DrawRect(Rect r, Color c)
        {
            if (_whiteTex == null) EnsureWhiteTex();
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _whiteTex);
            GUI.color = prev;
        }

        private void LateUpdate()
        {
            // 카메라 위치 추적 (이전 방식 호환 — SpriteMask 없어도 유지)
            if (Cam != null)
                transform.position = new Vector3(Cam.transform.position.x, Cam.transform.position.y, 0);
        }
    }
}
