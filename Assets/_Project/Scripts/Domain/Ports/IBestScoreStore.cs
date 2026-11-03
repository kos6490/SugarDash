namespace SugarDash.Domain.Ports
{
    /// <summary>최고 점수 저장 포트. 설정 버전이 다르면 점수 기준이 달라지므로 서로 비교하지 않는다.</summary>
    public interface IBestScoreStore
    {
        /// <summary>저장된 최고 점수. 기록이 없거나 읽을 수 없거나 다른 설정 버전이면 null.</summary>
        double? Load(string configVersion);

        /// <summary>최고 점수를 저장한다. 디스크 오류는 호출자에게 올리지 않는다.</summary>
        void Save(string configVersion, double score);
    }
}
