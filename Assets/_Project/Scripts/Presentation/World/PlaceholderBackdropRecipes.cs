using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>개발용 배경 한 장면분: 장면 id와 도형 목록. PlaceholderBackdrops가 Inspector 목록으로 든다.</summary>
    // [영역 파일] 담당: 월드
    [Serializable]
    public sealed class PlaceholderScene
    {
        [Tooltip("설정의 장면 id: schoolyard, playground, outing, home. 장면 없는 구간은 street")]
        public string sceneId;
        public List<PlaceholderShape> shapes = new List<PlaceholderShape>();
    }

    /// <summary>
    /// 장면 4종(운동장, 놀이터, 나들이, 집)의 개발용 배경 기본값. 모델이 나오기 전에 장면 차이가 눈에 보이게 한다.
    /// 여기 값은 PlaceholderBackdrops의 Inspector 목록 처음 값이며, Inspector에서 고칠 수 있다.
    /// 길(레인 3개, 폭 약 8)을 막지 않도록 모두 |x| ≥ 7 바깥에 둔다. 도형만 있고 글자·숫자·음식은 없다.
    /// </summary>
    // [영역 파일] 담당: 월드
    public static class PlaceholderBackdropRecipes
    {
        /// <summary>도형이 길에서 떨어져야 하는 최소 거리(길 가운데에서 x 방향).</summary>
        public const float MinSideDistance = 7f;

        /// <summary>배경 한 칸의 길이(m). 도형은 z 0 ~ TileLength 안에 두고, 칸을 이어 붙여 흘려보낸다.</summary>
        public const float TileLength = 120f;

        /// <summary>장면이 없는 구간(튜토리얼, 1구간)에 보여 줄 배경 id. 설정의 장면 id와 겹치지 않는다.</summary>
        public const string NoSceneId = "street";

        public static List<PlaceholderScene> CreateDefaults() => new List<PlaceholderScene>
        {
            Scene(NoSceneId, Street()),
            Scene("schoolyard", Schoolyard()),
            Scene("playground", Playground()),
            Scene("outing", Outing()),
            Scene("home", Home()),
        };

        // 색 (모두 밝고 부드러운 색. 어떤 물건도 무섭거나 나쁘게 보이지 않게 한다)
        private static readonly Color Trunk = new Color(0.55f, 0.38f, 0.24f);
        private static readonly Color Leaf = new Color(0.36f, 0.68f, 0.34f);
        private static readonly Color LeafDark = new Color(0.27f, 0.55f, 0.30f);
        private static readonly Color White = new Color(0.95f, 0.95f, 0.93f);
        private static readonly Color Fence = new Color(0.82f, 0.82f, 0.86f);
        private static readonly Color Cream = new Color(0.96f, 0.90f, 0.76f);
        private static readonly Color Roof = new Color(0.78f, 0.36f, 0.30f);
        private static readonly Color Sky = new Color(0.55f, 0.78f, 0.95f);
        private static readonly Color Yellow = new Color(1.00f, 0.84f, 0.30f);
        private static readonly Color Red = new Color(0.92f, 0.40f, 0.36f);
        private static readonly Color Blue = new Color(0.38f, 0.62f, 0.92f);
        private static readonly Color Sand = new Color(0.93f, 0.84f, 0.62f);
        private static readonly Color Water = new Color(0.45f, 0.72f, 0.92f);
        private static readonly Color Hill = new Color(0.50f, 0.74f, 0.42f);
        private static readonly Color Gray = new Color(0.62f, 0.62f, 0.66f);

        // 장면 없는 구간: 동네 길. 가로수, 낮은 생울타리, 멀리 낮은 건물.
        private static List<PlaceholderShape> Street()
        {
            var s = new List<PlaceholderShape>();
            // 길 양쪽 보도(밝은 회색 띠)
            s.AddRange(Both(Box(new Vector3(MinSideDistance + 1.5f, 0.03f, TileLength / 2f), new Vector3(3f, 0.06f, TileLength), Fence)));
            // 가로수(양쪽, 엇갈려 심음)
            for (int i = 0; i < 8; i++)
            {
                s.AddRange(Tree(new Vector3(MinSideDistance + 3.8f, 0f, 7.5f + i * 15f), 1f));
                s.AddRange(Tree(new Vector3(-(MinSideDistance + 3.8f), 0f, 15f + i * 15f), 1.05f));
            }
            // 생울타리(짧은 초록 상자)
            for (int i = 0; i < 4; i++)
                s.AddRange(Both(Box(new Vector3(MinSideDistance + 6f, 0.5f, 15f + i * 30f), new Vector3(1.2f, 1f, 12f), LeafDark)));
            // 멀리 낮은 건물
            Color[] walls = { Cream, new Color(0.80f, 0.88f, 0.96f), new Color(0.98f, 0.84f, 0.80f) };
            for (int i = 0; i < 4; i++)
            {
                float h = 4f + (i % 2) * 2f;
                s.Add(Box(new Vector3(-24f, h / 2f, 15f + i * 30f), new Vector3(10f, h, 14f), walls[i % 3]));
                s.Add(Box(new Vector3(24f, h / 2f, 30f + i * 30f), new Vector3(10f, h, 14f), walls[(i + 1) % 3]));
            }
            return s;
        }

        private static List<PlaceholderShape> Schoolyard()
        {
            var s = new List<PlaceholderShape>();
            // 학교 건물(왼쪽 멀리)과 지붕 띠, 창문 줄
            s.Add(Box(new Vector3(-24f, 4f, 60f), new Vector3(14f, 8f, 40f), Cream));
            s.Add(Box(new Vector3(-24f, 8.3f, 60f), new Vector3(14.4f, 0.6f, 40.4f), Roof));
            for (int i = 0; i < 6; i++)
                s.Add(Box(new Vector3(-16.9f, 5f, 44f + i * 6.4f), new Vector3(0.1f, 1.6f, 3f), Sky));
            // 축구 골대(오른쪽)
            s.AddRange(Goal(new Vector3(15f, 0f, 30f)));
            s.AddRange(Goal(new Vector3(15f, 0f, 75f)));
            // 운동장 둘레 낮은 울타리(길 양쪽)
            s.AddRange(Both(Box(new Vector3(MinSideDistance + 0.5f, 0.4f, TileLength / 2f), new Vector3(0.15f, 0.8f, TileLength), Fence)));
            // 국기 게양대 대신 빈 깃대와 노란 깃발(글자·그림 없음)
            s.Add(Cyl(new Vector3(11f, 3f, 55f), new Vector3(0.15f, 3f, 0.15f), White));
            s.Add(Box(new Vector3(11.6f, 5.4f, 55f), new Vector3(1.1f, 0.7f, 0.05f), Yellow));
            // 나무 몇 그루
            s.AddRange(Tree(new Vector3(-10f, 0f, 15f), 1f));
            s.AddRange(Tree(new Vector3(10f, 0f, 92f), 1.2f));
            s.AddRange(Tree(new Vector3(-10f, 0f, 100f), 1f));
            return s;
        }

        private static List<PlaceholderShape> Playground()
        {
            var s = new List<PlaceholderShape>();
            // 미끄럼틀(왼쪽): 계단 기둥, 꼭대기 판, 기울어진 미끄럼판
            s.Add(Box(new Vector3(-11f, 1.2f, 25f), new Vector3(1.2f, 2.4f, 1.2f), Blue));
            s.Add(Box(new Vector3(-11f, 2.5f, 25f), new Vector3(1.6f, 0.2f, 1.6f), Yellow));
            s.Add(Box(new Vector3(-11f, 1.3f, 27.6f), new Vector3(1f, 0.15f, 4f), Red, new Vector3(35f, 0f, 0f)));
            // 그네(오른쪽): 기둥 2개, 가로대, 의자 2개
            s.Add(Cyl(new Vector3(9.5f, 1.5f, 40f), new Vector3(0.2f, 1.5f, 0.2f), Red));
            s.Add(Cyl(new Vector3(14.5f, 1.5f, 40f), new Vector3(0.2f, 1.5f, 0.2f), Red));
            s.Add(Cyl(new Vector3(12f, 3f, 40f), new Vector3(0.15f, 2.6f, 0.15f), Yellow, new Vector3(0f, 0f, 90f)));
            s.Add(Box(new Vector3(11f, 0.7f, 40f), new Vector3(0.8f, 0.1f, 0.4f), Blue));
            s.Add(Box(new Vector3(13f, 0.7f, 40f), new Vector3(0.8f, 0.1f, 0.4f), Blue));
            // 모래밭(왼쪽 앞)
            s.Add(Box(new Vector3(-12f, 0.05f, 55f), new Vector3(6f, 0.1f, 6f), Sand));
            s.Add(Box(new Vector3(-12f, 0.2f, 52f), new Vector3(6.2f, 0.4f, 0.2f), Gray));
            s.Add(Box(new Vector3(-12f, 0.2f, 58f), new Vector3(6.2f, 0.4f, 0.2f), Gray));
            // 정글짐 대신 공 모양 놀이 기구(오른쪽 멀리)
            s.Add(Sphere(new Vector3(13f, 1.2f, 70f), new Vector3(2.4f, 2.4f, 2.4f), Yellow));
            // 나무 숲
            for (int i = 0; i < 5; i++)
            {
                s.AddRange(Tree(new Vector3(-18f, 0f, 12f + i * 18f), 1.1f));
                s.AddRange(Tree(new Vector3(19f, 0f, 20f + i * 18f), 1f));
            }
            return s;
        }

        private static List<PlaceholderShape> Outing()
        {
            var s = new List<PlaceholderShape>();
            // 먼 언덕(납작한 공)
            s.Add(Sphere(new Vector3(-34f, -2f, 70f), new Vector3(36f, 14f, 30f), Hill));
            s.Add(Sphere(new Vector3(36f, -3f, 95f), new Vector3(40f, 16f, 34f), Hill));
            s.Add(Sphere(new Vector3(-30f, -3f, 115f), new Vector3(30f, 12f, 26f), LeafDark));
            // 시냇물(오른쪽)
            s.Add(Box(new Vector3(15f, 0.02f, TileLength / 2f), new Vector3(5f, 0.04f, TileLength), Water));
            // 돗자리(왼쪽): 두 색 띠
            s.Add(Box(new Vector3(-11f, 0.03f, 28f), new Vector3(3f, 0.04f, 2.2f), White));
            s.Add(Box(new Vector3(-11f, 0.04f, 28f), new Vector3(1f, 0.04f, 2.2f), Red));
            // 나무 길(양쪽)
            for (int i = 0; i < 6; i++)
            {
                s.AddRange(Tree(new Vector3(-9.5f, 0f, 10f + i * 16f), 1f + (i % 2) * 0.2f));
                s.AddRange(Tree(new Vector3(10f, 0f, 18f + i * 16f), 1.1f - (i % 2) * 0.15f));
            }
            return s;
        }

        private static List<PlaceholderShape> Home()
        {
            var s = new List<PlaceholderShape>();
            // 집(양쪽): 몸통 + 45도 돌린 지붕
            Color[] walls = { Cream, new Color(0.80f, 0.88f, 0.96f), new Color(0.98f, 0.84f, 0.80f) };
            for (int i = 0; i < 3; i++)
            {
                s.AddRange(House(new Vector3(-15f, 0f, 20f + i * 25f), walls[i]));
                s.AddRange(House(new Vector3(15f, 0f, 32f + i * 25f), walls[(i + 1) % 3]));
            }
            // 가로등(양쪽, 밤에 노란 등)
            for (int i = 0; i < 6; i++)
            {
                s.AddRange(Both(Cyl(new Vector3(MinSideDistance + 0.8f, 1.6f, 10f + i * 20f), new Vector3(0.12f, 1.6f, 0.12f), Gray)));
                s.AddRange(Both(Sphere(new Vector3(MinSideDistance + 0.8f, 3.3f, 10f + i * 20f), new Vector3(0.45f, 0.45f, 0.45f), Yellow)));
            }
            // 낮은 담장
            s.AddRange(Both(Box(new Vector3(MinSideDistance + 2.5f, 0.35f, TileLength / 2f), new Vector3(0.2f, 0.7f, TileLength), Fence)));
            return s;
        }

        // ───── 조립 도우미 ─────

        private static PlaceholderScene Scene(string id, List<PlaceholderShape> shapes) =>
            new PlaceholderScene { sceneId = id, shapes = shapes };

        private static PlaceholderShape Box(Vector3 p, Vector3 size, Color c, Vector3 euler = default) =>
            new PlaceholderShape(PrimitiveType.Cube, p, size, c, euler);

        private static PlaceholderShape Sphere(Vector3 p, Vector3 size, Color c) =>
            new PlaceholderShape(PrimitiveType.Sphere, p, size, c);

        // 원기둥은 기본 높이가 2라서 scale.y가 반높이다.
        private static PlaceholderShape Cyl(Vector3 p, Vector3 scale, Color c, Vector3 euler = default) =>
            new PlaceholderShape(PrimitiveType.Cylinder, p, scale, c, euler);

        private static IEnumerable<PlaceholderShape> Both(PlaceholderShape right)
        {
            yield return right;
            yield return right.Mirrored();
        }

        private static IEnumerable<PlaceholderShape> Tree(Vector3 at, float size)
        {
            yield return Cyl(at + new Vector3(0f, 1f * size, 0f), new Vector3(0.3f, 1f, 0.3f) * size, Trunk);
            yield return Sphere(at + new Vector3(0f, 2.6f * size, 0f), new Vector3(2.2f, 2.2f, 2.2f) * size, Leaf);
        }

        private static IEnumerable<PlaceholderShape> Goal(Vector3 at)
        {
            yield return Cyl(at + new Vector3(0f, 1.1f, -1.8f), new Vector3(0.12f, 1.1f, 0.12f), White);
            yield return Cyl(at + new Vector3(0f, 1.1f, 1.8f), new Vector3(0.12f, 1.1f, 0.12f), White);
            yield return Cyl(at + new Vector3(0f, 2.2f, 0f), new Vector3(0.12f, 1.8f, 0.12f), White, new Vector3(90f, 0f, 0f));
        }

        private static IEnumerable<PlaceholderShape> House(Vector3 at, Color wall)
        {
            float side = at.x < 0f ? -1f : 1f;
            yield return Box(at + new Vector3(0f, 1.75f, 0f), new Vector3(6f, 3.5f, 6f), wall);
            yield return Box(at + new Vector3(0f, 3.6f, 0f), new Vector3(4.3f, 4.3f, 6.2f), Roof, new Vector3(0f, 0f, 45f));
            // 문(길 쪽 벽)
            yield return Box(at + new Vector3(-side * 3.02f, 0.9f, 0f), new Vector3(0.05f, 1.8f, 1f), Trunk);
        }
    }
}
