using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 트랙 물체(장애물, 음식, 쉬는 곳)의 모습을 만든다. TrackSpawner의 look 필드에 연결하면
    /// 이 클래스로 만들고, 비어 있으면 TrackSpawner가 PrimitiveViews로 만든다.
    /// </summary>
    // [빈 틀] 담당: 월드
    public sealed class TrackItemLook : MonoBehaviour
    {
        /// <summary>물체의 보이는 모습을 만들어 parent 아래에 둔다.</summary>
        public Transform CreateView(TrackItem item, Transform parent)
        {
            // TODO(월드): 물체 종류(item.Kind)와 음식 id(item.SnackId)별 프리팹으로 만든다.
            // 지금은 기본 도형(PrimitiveViews)을 쓴다.
            return PrimitiveViews.CreateItem(item, parent);
        }
    }
}
