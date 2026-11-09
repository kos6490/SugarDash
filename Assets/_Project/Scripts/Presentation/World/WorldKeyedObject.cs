using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// Inspector 목록 한 줄: 문자열 키(장면 id, 음식 id 등)와 오브젝트(프리팹 또는 씬 오브젝트).
    /// TrackItemPrefabSet(음식 id → 음식 프리팹), SceneTrackItemSet(장면 id → 장애물 묶음),
    /// SceneBackdropSwitcher(장면 id → 맵 배경)가 같이 쓴다.
    /// </summary>
    // [영역 파일] 담당: 월드
    [Serializable]
    public struct WorldKeyedObject
    {
        [Tooltip("설정에 적힌 id 그대로. 예: 장면 schoolyard, 음식 juice")]
        public string key;
        public GameObject target;

        /// <summary>키가 같은 첫 줄의 오브젝트. 없거나 키가 비었으면 null. 대소문자를 구분한다.</summary>
        public static GameObject Find(IReadOnlyList<WorldKeyedObject> entries, string key)
        {
            if (entries == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key) return entries[i].target;
            }
            return null;
        }
    }
}
