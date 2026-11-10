using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;

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

        /// <summary>
        /// 2단계에 보여 줄 간식: 빠른 당 하나와 느린 당 하나를 무작위로 고른다(버튼 수를 줄이고 매번 다른 짝에서 빠른 당을 알아보게).
        /// 한쪽 종류가 없으면 있는 종류에서 하나만 고른다. 빈 간식 가방이면 느린 당 하나만이다. 순서는 섞지 않는다(호출하는 쪽이 다른 선택지와 함께 섞는다).
        /// </summary>
        public static List<SnackConfig> RespondSnacks(IReadOnlyList<SnackConfig> snacks, Random random, bool emptyBag = false)
        {
            if (snacks == null) throw new ArgumentNullException(nameof(snacks));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var result = new List<SnackConfig>();
            // 빈 간식 가방이면 빠른 당이 없다. 느린 당 하나만 고른다.
            foreach (var kind in emptyBag ? new[] { SnackKind.Slow } : new[] { SnackKind.Fast, SnackKind.Slow })
            {
                var ofKind = snacks.Where(s => s.Kind == kind).ToList();
                if (ofKind.Count > 0) result.Add(ofKind[random.Next(ofKind.Count)]);
            }
            return result;
        }
    }
}
