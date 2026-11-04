namespace SugarDash.Domain.Director
{
    /// <summary>혈당 재기 습관 순간의 결과.</summary>
    public enum HabitCheckOutcome
    {
        /// <summary>말풍선이 떠 있는 동안 측정기를 눌렀다.</summary>
        Checked,
        /// <summary>누르지 않고 시간이 지났다. 불이익은 없다.</summary>
        Missed,
        /// <summary>대처 중·유예 중이라 띄우지 않았거나, 떠 있는 동안 대처가 시작돼 거뒀다. 습관 확인률에서 뺀다.</summary>
        Skipped,
    }
}
