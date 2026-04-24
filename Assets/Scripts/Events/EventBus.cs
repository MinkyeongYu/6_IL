using System;
using System.Collections.Generic;
using UnityEngine;
using IL6.Events;

namespace IL6
{
    /// <summary>
    /// 타입 안전 이벤트 버스. 씬 전환 시에도 살아있는 싱글톤.
    /// 각 이벤트 타입(struct)당 별도의 리스너 집합.
    /// </summary>
    public sealed class EventBus
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance ??= new EventBus();

        // 타입별 리스너 보관 (Action&lt;T&gt;)
        private readonly Dictionary<Type, Delegate> _listeners = new();

        public Action Subscribe<T>(Action<T> handler) where T : struct
        {
            var t = typeof(T);
            if (_listeners.TryGetValue(t, out var existing))
                _listeners[t] = Delegate.Combine(existing, handler);
            else
                _listeners[t] = handler;

            return () =>
            {
                if (_listeners.TryGetValue(t, out var cur))
                {
                    var next = Delegate.Remove(cur, handler);
                    if (next == null) _listeners.Remove(t);
                    else _listeners[t] = next;
                }
            };
        }

        public void Emit<T>(T payload) where T : struct
        {
            var t = typeof(T);
            if (!_listeners.TryGetValue(t, out var d)) return;
            if (d is Action<T> handler)
            {
                foreach (var inv in handler.GetInvocationList())
                {
                    try
                    {
                        ((Action<T>)inv)(payload);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[EventBus] listener for {t.Name} threw: {ex}");
                    }
                }
            }
        }

        public void Clear() => _listeners.Clear();

        // 정적 헬퍼 (Reset on scene reload not needed — singleton)
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void OnEditorReload() => _instance = null;
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnRuntimeReset() => _instance = null;
    }
}
