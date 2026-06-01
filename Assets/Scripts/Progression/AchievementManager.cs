using System.Collections.Generic;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 한 세션 내 마일스톤(첫 사냥, 첫 동료, 보스 격퇴, N일 생존 등) 달성 추적.
    /// 매 프레임 GameSession 상태를 폴링해 신규 달성을 LastUnlocked 큐에 기록 — HUD가 토스트로 표시.
    /// 외부 의존 없음. 세션 단위로만 존재 (영구 저장 아님).
    /// </summary>
    public sealed class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        public struct Entry
        {
            public string Id;
            public string Title;
            public string Detail;
        }

        private static readonly List<Definition> _defs = new()
        {
            new Definition("first_kill",  "첫 사냥",      "최초의 좀비를 처치했습니다.",   s => s.TotalKills >= 1),
            new Definition("kills_25",    "사냥꾼",       "좀비 25 처치.",               s => s.TotalKills >= 25),
            new Definition("kills_100",   "노련한 학살자", "좀비 100 처치.",              s => s.TotalKills >= 100),
            new Definition("first_comp",  "첫 동료",      "방랑자를 영입했습니다.",        s => s.MaxCompanionsAtOnce >= 1),
            new Definition("five_comp",   "작은 마을",    "동시에 5명의 동료와 함께.",     s => s.MaxCompanionsAtOnce >= 5),
            new Definition("survive_3",   "한 주 절반",   "3일째 새벽을 맞이했습니다.",   s => s.Cycle != null && s.Cycle.Day >= 3),
            new Definition("survive_7",   "일주일",       "7일을 살아남았습니다.",        s => s.Cycle != null && s.Cycle.Day >= 7),
            new Definition("survive_15",  "보름",         "15일을 살아남았습니다.",       s => s.Cycle != null && s.Cycle.Day >= 15),
            new Definition("score_300",   "전설",         "점수 300 돌파.",              s => s.Score >= 300),
        };

        private readonly HashSet<string> _unlocked = new();
        public Queue<Entry> NewlyUnlocked { get; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private float _pollTimer;

        private void Update()
        {
            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer < 0.4f) return;
            _pollTimer = 0f;

            var s = GameSession.Instance;
            if (s == null) return;

            foreach (var d in _defs)
            {
                if (_unlocked.Contains(d.Id)) continue;
                if (d.Predicate(s))
                {
                    _unlocked.Add(d.Id);
                    NewlyUnlocked.Enqueue(new Entry { Id = d.Id, Title = d.Title, Detail = d.Detail });
                }
            }
        }

        private sealed class Definition
        {
            public readonly string Id;
            public readonly string Title;
            public readonly string Detail;
            public readonly System.Func<GameSession, bool> Predicate;
            public Definition(string id, string title, string detail, System.Func<GameSession, bool> p)
            { Id = id; Title = title; Detail = detail; Predicate = p; }
        }
    }
}
