using SugarDash.Domain.Config;

namespace SugarDash.Domain.Ports
{
    /// <summary>설정 저장 포트. 지금은 마지막에 고른 당뇨 유형만 다룬다.</summary>
    public interface ISettingsStore
    {
        /// <summary>저장된 유형. 기록이 없거나 읽을 수 없거나 알 수 없는 값이면 null.</summary>
        TrackType? LoadTrackType();

        /// <summary>유형을 저장한다. 디스크 오류는 호출자에게 올리지 않는다.</summary>
        void SaveTrackType(TrackType type);
    }
}
