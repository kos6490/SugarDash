using SugarDash.Domain.Config;

namespace SugarDash.Domain.Ports
{
    /// <summary>설정 저장 포트. 마지막에 고른 당뇨 유형과 튜토리얼 값(다음 판이 튜토리얼인지, 유형별로 끝낸 적이 있는지)을 다룬다.</summary>
    public interface ISettingsStore
    {
        /// <summary>저장된 유형. 기록이 없거나 읽을 수 없거나 알 수 없는 값이면 null.</summary>
        TrackType? LoadTrackType();

        /// <summary>유형을 저장한다. 디스크 오류는 호출자에게 올리지 않는다.</summary>
        void SaveTrackType(TrackType type);

        /// <summary>튜토리얼을 지금 해야 하는지. 값이 없거나 읽을 수 없으면 true.</summary>
        bool LoadTutorialPending();

        /// <summary>튜토리얼 대기 값을 저장한다. 디스크 오류는 호출자에게 올리지 않는다.</summary>
        void SaveTutorialPending(bool pending);

        /// <summary>그 유형의 튜토리얼을 끝낸 적이 있는지. 값이 없거나 읽을 수 없으면 false.</summary>
        bool LoadTutorialDone(TrackType type);

        /// <summary>그 유형의 튜토리얼을 끝냈다고 저장한다. 디스크 오류는 호출자에게 올리지 않는다.</summary>
        void SaveTutorialDone(TrackType type);
    }
}
