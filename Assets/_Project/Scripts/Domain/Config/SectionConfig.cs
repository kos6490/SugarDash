using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>구간 하나의 설정.</summary>
    public sealed class SectionConfig
    {
        public SectionConfig(string id, double? lengthM, IReadOnlyList<SceneConfig> scenes = null, int? days = null)
        {
            Id = id;
            LengthM = lengthM;
            Scenes = scenes;
            Days = days;
        }

        /// <summary>signal, condition, complex 중 하나.</summary>
        public string Id { get; }
        /// <summary>구간 길이(m). 마지막 구간만 null(끝없이 이어진다).</summary>
        public double? LengthM { get; }
        /// <summary>
        /// 장면 목록. 장면 없는 구간이면 null. 마지막 구간이면 장면(하루)이 끝없이 되풀이되고,
        /// 마지막이 아니면 하루를 <see cref="Days"/>번 돌고 다음 구간으로 간다(docs/section3.md 2절).
        /// </summary>
        public IReadOnlyList<SceneConfig> Scenes { get; }
        /// <summary>장면 구간이 하루를 도는 횟수. 마지막 구간이면 null(끝없이).</summary>
        public int? Days { get; }
        public bool HasScenes => Scenes != null;
    }
}
