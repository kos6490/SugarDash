using SugarDash.Domain.Config;

namespace SugarDash.Domain.Ports
{
    /// <summary>설정 읽기 포트.</summary>
    public interface IConfigSource
    {
        /// <summary>
        /// 불변식 검증까지 마친 설정을 반환한다.
        /// 값이 비었거나 불변식을 어기면 <see cref="ConfigValidationException"/>을 던진다.
        /// </summary>
        GameConfig Load();
    }
}
