using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>구간 하나의 설정.</summary>
    public sealed class SectionConfig
    {
        public SectionConfig(string id, double? lengthM, IReadOnlyList<SceneConfig> scenes = null)
        {
            Id = id;
            LengthM = lengthM;
            Scenes = scenes;
        }

        /// <summary>signal, condition, complex 중 하나.</summary>
        public string Id { get; }
        /// <summary>구간 길이(m). 마지막 구간만 null(끝없이 이어진다).</summary>
        public double? LengthM { get; }
        /// <summary>장면 목록. 장면 없는 구간이면 null. 장면이 있는 구간은 마지막이고 장면이 끝없이 되풀이된다.</summary>
        public IReadOnlyList<SceneConfig> Scenes { get; }
        public bool HasScenes => Scenes != null;
    }
}
