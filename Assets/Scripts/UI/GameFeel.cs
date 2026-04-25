using System.Collections;
using UnityEngine;

namespace IL6
{
    /// <summary>
    /// 게임 필 헬퍼: 피격 시 짧은 흰색 플래시.
    /// </summary>
    public static class GameFeel
    {
        public static void HitFlash(MonoBehaviour owner, SpriteRenderer sr)
        {
            if (sr == null || owner == null || !owner.gameObject.activeInHierarchy) return;
            owner.StartCoroutine(FlashRoutine(sr));
        }

        private static IEnumerator FlashRoutine(SpriteRenderer sr)
        {
            if (sr == null) yield break;
            Color original = sr.color;
            sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (sr != null) sr.color = original;
        }
    }
}
