using System;
using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>설정 로드 실패. 비어 있는 값과 불변식 위반을 한 번에 모두 담는다.</summary>
    public sealed class ConfigValidationException : Exception
    {
        public ConfigValidationException(IReadOnlyList<string> missingPaths, IReadOnlyList<string> violations)
            : base(BuildMessage(missingPaths, violations))
        {
            MissingPaths = new List<string>(missingPaths);
            Violations = new List<string>(violations);
        }

        /// <summary>값이 null이거나 없는 설정 경로. 예: <c>glucose.initial</c>, <c>snacks[1].onsetSec</c></summary>
        public IReadOnlyList<string> MissingPaths { get; }
        /// <summary>형식 오류와 불변식 위반.</summary>
        public IReadOnlyList<string> Violations { get; }

        private static string BuildMessage(IReadOnlyList<string> missingPaths, IReadOnlyList<string> violations)
        {
            var lines = new List<string> { "설정 로드 실패" };
            if (missingPaths.Count > 0) lines.Add("값 없음: " + string.Join(", ", missingPaths));
            foreach (var violation in violations) lines.Add("- " + violation);
            return string.Join(Environment.NewLine, lines);
        }
    }
}
