using UnityEngine.Scripting;

namespace SugarDash.Infrastructure.Progress.Dto
{
    // settings.json에 대응하는 DTO. Newtonsoft 역직렬화 대상이라 IL2CPP 스트리핑으로 지워지지 않게 [Preserve]를 붙인다.
    [Preserve]
    public sealed class SettingsDto
    {
        public int SchemaVersion { get; set; }
        public string TrackType { get; set; }
        public bool? TutorialPending { get; set; }
        public System.Collections.Generic.List<string> TutorialDone { get; set; }
    }
}
