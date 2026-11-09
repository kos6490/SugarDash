namespace SugarDash.Presentation
{
    /// <summary>
    /// 음식 id별 화면 이름(docs/food-evidence.md 1절). 선택 패널 버튼, 양 묻는 제목, 고른 뒤 문구가 쓴다.
    /// 음식을 "나쁜" 것으로 부르는 말이나 의학 설명은 넣지 않는다.
    /// </summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class FoodText
    {
        /// <summary>음식 이름. 모르는 id는 id를 그대로 돌려준다.</summary>
        public static string Name(string id)
        {
            switch (id)
            {
                case "water": return "물";
                case "juice": return "주스";
                case "soda": return "탄산음료";
                case "apple": return "사과";
                case "cracker": return "크래커";
                case "chicken": return "치킨";
                case "pizza": return "피자";
                case "burger": return "햄버거";
                default: return id ?? "";
            }
        }

        /// <summary>이름에 "을/를"을 붙인다(받침이 있으면 을). 한글로 끝나지 않으면 "을(를)".</summary>
        public static string WithObjectParticle(string id)
        {
            string name = Name(id);
            if (name.Length == 0) return name;
            char last = name[name.Length - 1];
            if (last < '가' || last > '힣') return name + "을(를)";
            return name + ((last - '가') % 28 == 0 ? "를" : "을");
        }
    }
}
