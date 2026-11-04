using System;
using System.Collections.Generic;
using SugarDash.App;
using SugarDash.Presentation.Runner;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 트랙, 장애물, 간식 배치. 앞쪽에 미리 놓고 캐릭터 뒤로 지나간 것은 지운다.
    /// 세계 시간(worldDt)으로만 움직인다. 장애물은 줄마다 한 레인에만 놓아 항상 피할 길이 있다.
    /// 간식은 설정의 간격(worldDt 기준 초)마다 놓는다.
    /// </summary>
    public sealed class TrackSpawner : MonoBehaviour
    {
        private const int LaneCount = 3;

        [Tooltip("물체를 놓는 앞쪽 거리")]
        [SerializeField] private float spawnAheadDistance = 60f;
        [Tooltip("캐릭터 뒤로 이만큼 지나가면 지운다")]
        [SerializeField] private float despawnBehindDistance = 10f;
        [Tooltip("장애물 줄 사이 거리")]
        [SerializeField] private float obstacleSpacing = 18f;
        [Tooltip("주행 시작 후 장애물 없이 달리는 거리")]
        [SerializeField] private float startClearDistance = 30f;
        [Tooltip("간식이 장애물 줄과 같은 레인에 놓일 때 띄울 최소 거리")]
        [SerializeField] private float snackObstacleGap = 4f;
        [Tooltip("2형 쉬는 곳 앞뒤로 장애물·음식을 놓지 않는 거리")]
        [SerializeField] private float restClearDistance = 8f;
        [SerializeField] private float markerSpacing = 5f;
        [SerializeField] private float laneWidth = 2.5f;
        [Tooltip("물체 모습을 만드는 월드 담당 도우미. 비워 두면 기본 도형(PrimitiveViews)을 쓴다.")]
        [SerializeField] private TrackItemLook look;

        private readonly List<TrackItem> _items = new List<TrackItem>();
        private readonly List<TrackItem> _crossed = new List<TrackItem>();
        private TrackLayout _layout;
        private TrackLayout _pendingLayout;
        private double _nextSnackIntervalSec;
        private double _nextRestIntervalSec;
        private double _restTimer;
        private double _lastRestAt = double.NegativeInfinity;
        private int _seed;
        private bool _createViews;
        private System.Random _random;
        private double _distance;
        private double _nextObstacleAt;
        private double _nextMarkerAt;
        private double _snackTimer;
        private int _lastObstacleLane;
        private double _lastObstacleZ = double.NegativeInfinity;
        private bool _tutorialGuided;

        public float SpawnAheadDistance => spawnAheadDistance;
        public float StartClearDistance => startClearDistance;
        public float ObstacleSpacing => obstacleSpacing;
        public IReadOnlyList<TrackItem> Items => _items;
        /// <summary>주행 시작부터 달린 거리.</summary>
        public double Distance => _distance;

        /// <summary>가장 가까운 튜토리얼 대상의 z(z &gt; 0인 것만). 없으면 null.</summary>
        public double? NearestTutorialTargetZ
        {
            get
            {
                double? nearest = null;
                foreach (var item in _items)
                {
                    if (!item.IsTutorialTarget || !(item.Z > 0)) continue;
                    if (nearest == null || item.Z < nearest.Value) nearest = item.Z;
                }
                return nearest;
            }
        }

        /// <summary>
        /// true인 동안 무작위 장애물·간식·음식 줄·쉬는 곳을 놓지 않고 간식·쉬는 곳 타이머를 멈춘다.
        /// 바닥 표시는 놓는다. 튜토리얼 안내 구간 동안 켠다.
        /// </summary>
        public void SetTutorialGuided(bool guided) => _tutorialGuided = guided;

        /// <summary>튜토리얼 안내 대상을 spawnAheadDistance 앞에 놓고 <see cref="TrackItem.IsTutorialTarget"/>을 켠다.</summary>
        public void PlaceTutorial(TutorialPlacement p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            double z = spawnAheadDistance;
            switch (p.Kind)
            {
                case TutorialObject.LaneBlock:
                    PlaceTutorialItems(TrackItemKind.LaneBlock, p.Lanes, z);
                    break;
                case TutorialObject.LowObstacle:
                    PlaceTutorialItems(TrackItemKind.LowObstacle, p.Lanes, z);
                    break;
                case TutorialObject.HighObstacle:
                    PlaceTutorialItems(TrackItemKind.HighObstacle, p.Lanes, z);
                    break;
                case TutorialObject.Snack:
                    Add(new TrackItem(TrackItemKind.Snack, p.Lanes.Count > 0 ? p.Lanes[0] : 0, z, _layout.SnackIds[0])
                        { IsTutorialTarget = true });
                    break;
                case TutorialObject.FoodRow:
                    int before = _items.Count;
                    PlaceFoodRow(z);
                    for (int i = before; i < _items.Count; i++) _items[i].IsTutorialTarget = true;
                    break;
                case TutorialObject.RestSpot:
                    Add(new TrackItem(TrackItemKind.RestSpot, p.Lanes.Count > 0 ? p.Lanes[0] : 0, z)
                        { IsTutorialTarget = true });
                    break;
            }
        }

        private void PlaceTutorialItems(TrackItemKind kind, IReadOnlyList<int> lanes, double z)
        {
            foreach (int lane in lanes) Add(new TrackItem(kind, lane, z) { IsTutorialTarget = true });
        }

        /// <param name="layout">간식·음식·쉬는 곳 배치 값. 1형은 음식 줄·쉬는 곳이 없다.</param>
        /// <param name="seed">배치 난수 시드. 같으면 같은 트랙이 나온다.</param>
        /// <param name="createViews">false면 화면 없이 배치만 계산한다(테스트용).</param>
        public void Init(TrackLayout layout, int seed, bool createViews)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            layout.Validate();

            _layout = layout;
            _pendingLayout = null;
            _seed = seed;
            _createViews = createViews;
            if (createViews) PrimitiveViews.CreateGround(transform, LaneCount * laneWidth + 1f, spawnAheadDistance + 20f);
            ResetRun(seed);
        }

        /// <summary>배치 값을 바꾼다. 지금 주행은 그대로이고 다음 <see cref="ResetRun"/>부터 적용한다.</summary>
        public void SetLayout(TrackLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            layout.Validate();
            _pendingLayout = layout;
        }

        /// <summary>
        /// 배치 값을 지금 주행에 바로 바꾼다(장면이 바뀔 때). 이미 놓인 물체는 그대로 두고 앞으로 놓을 물체부터 새 값을 쓴다.
        /// 간식·쉬는 곳 타이머는 처음부터 다시 잰다. 물체는 계속 spawnAheadDistance 앞에만 놓는다.
        /// 물체를 놓지 않는 배치(집 장면)면 앞에 놓인 장애물·간식·쉬는 곳도 치운다.
        /// </summary>
        public void ChangeLayout(TrackLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            layout.Validate();
            _layout = layout;
            if (!layout.SpawnsItems) RemoveItemsAhead();
            _snackTimer = 0;
            _nextSnackIntervalSec = NextSnackInterval();
            _restTimer = 0;
            _nextRestIntervalSec = _layout.HasRestSpots ? NextRestInterval() : 0;
        }

        /// <summary>지금 장면 id를 모습 도우미에 넘긴다. 장면 없는 구간이면 null.</summary>
        public void SetScene(string sceneId)
        {
            if (look != null) look.SetScene(sceneId);
        }

        /// <summary>새 주행 시작 시 트랙을 비우고 처음부터 놓는다.</summary>
        public void ResetRun(int seed)
        {
            foreach (var item in _items) DestroyView(item);
            _items.Clear();
            if (_pendingLayout != null)
            {
                _layout = _pendingLayout;
                _pendingLayout = null;
            }
            _random = new System.Random(seed);
            _distance = 0;
            _nextObstacleAt = startClearDistance;
            _nextMarkerAt = 0;
            _snackTimer = 0;
            _nextSnackIntervalSec = NextSnackInterval();
            _restTimer = 0;
            _lastRestAt = double.NegativeInfinity;
            _nextRestIntervalSec = _layout.HasRestSpots ? NextRestInterval() : 0;
            _lastObstacleZ = double.NegativeInfinity;
            FillAhead(true);
        }

        /// <summary>
        /// GameBootstrapper가 PlayerController.Advance 뒤에 부른다. 물체를 speed × worldDt만큼 다가오게 하고,
        /// 이번에 캐릭터 위치(z = 0)를 지나간 물체를 돌려준다. 돌려준 목록은 다음 호출 때 바뀐다.
        /// </summary>
        /// <param name="obstaclesAllowed">false면 장애물을 모두 지우고 새로 놓지 않는다(재개 카운트다운·유예 시간).</param>
        public IReadOnlyList<TrackItem> Advance(double worldDt, double speed, bool obstaclesAllowed)
        {
            _crossed.Clear();
            if (!obstaclesAllowed) RemoveObstacles();
            if (!(worldDt > 0)) return _crossed;

            double move = speed * worldDt;
            _distance += move;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                double before = item.Z;
                item.Z -= move;
                if (before > 0 && item.Z <= 0) _crossed.Add(item);
                if (item.Z < -despawnBehindDistance)
                {
                    DestroyView(item);
                    _items.RemoveAt(i);
                }
                else
                {
                    UpdateView(item);
                }
            }
            _lastObstacleZ -= move;
            // 같은 프레임에 여러 개가 지나가면 먼저 지나간 것(더 뒤로 간 것)부터 판정한다.
            _crossed.Sort((a, b) => a.Z.CompareTo(b.Z));

            // 멈춘 동안(쉬는 중)과 물체를 놓지 않는 장면에서는 간식·쉬는 곳 타이머도 멈춘다.
            if (speed > 0 && _layout.SpawnsItems && !_tutorialGuided)
            {
                _snackTimer += worldDt;
                _restTimer += worldDt;
            }
            FillAhead(obstaclesAllowed);
            return _crossed;
        }

        /// <summary>먹었거나 부딪힌 물체를 트랙에서 뺀다.</summary>
        public void Remove(TrackItem item)
        {
            if (!_items.Remove(item)) return;
            DestroyView(item);
        }

        private void FillAhead(bool obstaclesAllowed)
        {
            double horizon = _distance + spawnAheadDistance;

            while (_nextMarkerAt <= horizon)
            {
                for (int lane = -1; lane <= 1; lane++) Add(new TrackItem(TrackItemKind.Marker, lane, Ahead(_nextMarkerAt)));
                _nextMarkerAt += markerSpacing;
            }

            while (_nextObstacleAt <= horizon)
            {
                // 장애물을 못 놓는 동안(유예, 쉬는 곳 근처, 물체를 놓지 않는 장면) 지나간 줄은 건너뛴다.
                if (obstaclesAllowed && _layout.SpawnsItems && !_tutorialGuided && !InRestClearZone(_nextObstacleAt))
                {
                    var kind = (TrackItemKind)_random.Next(0, 3);
                    int lane = _random.Next(-1, 2);
                    double z = Ahead(_nextObstacleAt);
                    Add(new TrackItem(kind, lane, z));
                    _lastObstacleLane = lane;
                    _lastObstacleZ = z;
                }
                _nextObstacleAt += obstacleSpacing;
            }

            while (!_tutorialGuided && _layout.HasRestSpots && _restTimer >= _nextRestIntervalSec)
            {
                _restTimer -= _nextRestIntervalSec;
                _nextRestIntervalSec = NextRestInterval();
                PlaceRestSpot();
            }

            while (!_tutorialGuided && _snackTimer >= _nextSnackIntervalSec)
            {
                _snackTimer -= _nextSnackIntervalSec;
                _nextSnackIntervalSec = NextSnackInterval();
                double z = spawnAheadDistance;
                // 쉬는 곳 근처에는 음식을 놓지 않는다(시간만 소비한다).
                if (InRestClearZone(_distance + z)) continue;
                if (_layout.FoodRows) PlaceFoodRow(z);
                else PlaceSnack(z);
            }
        }

        private void PlaceSnack(double z)
        {
            int lane = _random.Next(-1, 2);
            if (lane == _lastObstacleLane && Math.Abs(z - _lastObstacleZ) < snackObstacleGap)
                lane = lane == 1 ? -1 : lane + 1;
            string id = _layout.SnackIds[_random.Next(_layout.SnackIds.Count)];
            Add(new TrackItem(TrackItemKind.Snack, lane, z, id));
        }

        /// <summary>서로 다른 음식 2개를 3레인 중 2레인에 놓는다. 빈 레인 하나는 장애물 줄과 겹치면 그 레인으로 한다.</summary>
        private void PlaceFoodRow(double z)
        {
            int count = _layout.SnackIds.Count;
            int first = _random.Next(count);
            int second = _random.Next(count - 1);
            if (second >= first) second++;

            int empty = _random.Next(-1, 2);
            if (Math.Abs(z - _lastObstacleZ) < snackObstacleGap) empty = _lastObstacleLane;

            var ids = new[] { _layout.SnackIds[first], _layout.SnackIds[second] };
            int n = 0;
            for (int lane = -1; lane <= 1; lane++)
            {
                if (lane == empty) continue;
                Add(new TrackItem(TrackItemKind.Snack, lane, z, ids[n++]));
            }
        }

        /// <summary>쉬는 곳을 무작위 레인 하나에 놓고, 앞뒤 restClearDistance 안의 장애물·음식은 치운다.</summary>
        private void PlaceRestSpot()
        {
            double z = spawnAheadDistance;
            double at = _distance + z;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (!(item.IsObstacle || item.Kind == TrackItemKind.Snack)) continue;
                if (Math.Abs(item.Z - z) >= restClearDistance) continue;
                DestroyView(item);
                _items.RemoveAt(i);
            }
            _lastRestAt = at;
            Add(new TrackItem(TrackItemKind.RestSpot, _random.Next(-1, 2), z));
        }

        private bool InRestClearZone(double atDistance) => Math.Abs(atDistance - _lastRestAt) < restClearDistance;

        private double NextRestInterval() =>
            _layout.RestIntervalMinSec + (_layout.RestIntervalMaxSec - _layout.RestIntervalMinSec) * _random.NextDouble();

        private double NextSnackInterval() =>
            _layout.SnackIntervalMinSec + (_layout.SnackIntervalMaxSec - _layout.SnackIntervalMinSec) * _random.NextDouble();

        /// <summary>달린 거리 기준 위치를 캐릭터 기준 z로 바꾼다.</summary>
        private double Ahead(double atDistance) => atDistance - _distance;

        private void RemoveItemsAhead()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.Kind == TrackItemKind.Marker || !(item.Z > 0)) continue;
                DestroyView(item);
                _items.RemoveAt(i);
            }
        }

        private void RemoveObstacles()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (!_items[i].IsObstacle) continue;
                DestroyView(_items[i]);
                _items.RemoveAt(i);
            }
        }

        private void Add(TrackItem item)
        {
            if (_createViews)
                item.View = look != null ? look.CreateView(item, transform) : PrimitiveViews.CreateItem(item, transform);
            UpdateView(item);
            _items.Add(item);
        }

        private void UpdateView(TrackItem item)
        {
            if (item.View == null) return;
            item.View.localPosition = new Vector3(item.Lane * laneWidth, PrimitiveViews.BaseHeight(item.Kind),
                (float)item.Z);
        }

        private static void DestroyView(TrackItem item)
        {
            if (item.View == null) return;
            Destroy(item.View.gameObject);
            item.View = null;
        }
    }
}
