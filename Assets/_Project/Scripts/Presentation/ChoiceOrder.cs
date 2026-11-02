using System;
using System.Collections.Generic;

namespace SugarDash.Presentation
{
    /// <summary>선택지 순서를 섞는다. 정답이 늘 같은 자리에 있지 않게 하려는 것이다.</summary>
    public static class ChoiceOrder
    {
        /// <summary>Fisher–Yates로 섞은 새 목록을 돌려준다. 입력은 바꾸지 않는다.</summary>
        public static List<T> Shuffle<T>(IReadOnlyList<T> items, Random random)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var result = new List<T>(items);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            return result;
        }
    }
}
