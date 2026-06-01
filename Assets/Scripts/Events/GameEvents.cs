using UnityEngine;

namespace IL6.Events
{
    public readonly struct DayPayload { public readonly int Day; public DayPayload(int d) { Day = d; } }
    public readonly struct PlayerDamagedPayload { public readonly int Amount, Remaining; public PlayerDamagedPayload(int a, int r) { Amount = a; Remaining = r; } }
    public readonly struct PlayerDiedPayload { public readonly int Day; public PlayerDiedPayload(int d) { Day = d; } }
    public readonly struct ZombieDiedPayload { public readonly int Id; public readonly Vector2 Position; public ZombieDiedPayload(int id, Vector2 p) { Id = id; Position = p; } }
    public readonly struct ResourceChangedPayload { public readonly string Kind; public readonly int Delta, Total; public ResourceChangedPayload(string k, int d, int t) { Kind = k; Delta = d; Total = t; } }
    public readonly struct BuildingDestroyedPayload { public readonly int Eid; public readonly string Kind; public BuildingDestroyedPayload(int e, string k) { Eid = e; Kind = k; } }
    public readonly struct WaveStartedPayload { public readonly int Day, WaveIndex, EnemyCount; public WaveStartedPayload(int d, int w, int e) { Day = d; WaveIndex = w; EnemyCount = e; } }
    public readonly struct WaveClearedPayload { public readonly int Day, WaveIndex; public WaveClearedPayload(int d, int w) { Day = d; WaveIndex = w; } }
    public readonly struct BuildRequestPayload { public readonly string Kind; public BuildRequestPayload(string k) { Kind = k; } }
}
