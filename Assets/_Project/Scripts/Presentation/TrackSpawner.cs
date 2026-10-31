using System;
using System.Collections.Generic;
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
        [SerializeField] private float markerSpacing = 5f;
        [SerializeField] private float laneWidth = 2.5f;

        private readonly List<TrackItem> _items = new List<TrackItem>();
        private readonly List<TrackItem> _crossed = new List<TrackItem>();
        private IReadOnlyList<string> _snackIds;
        private double _snackIntervalSec;
        private int _seed;
        private bool _createViews;
        private System.Random _random;
        private double _distance;
        private double _nextObstacleAt;
        private double _nextMarkerAt;
        private double _snackTimer;
        private int _lastObstacleLane;
        private double _lastObstacleZ = double.NegativeInfinity;

        public float SpawnAheadDistance => spawnAheadDistance;
        public float StartClearDistance => startClearDistance;
        public float ObstacleSpacing => obstacleSpacing;
        public IReadOnlyList<TrackItem> Items => _items;
        /// <summary>주행 시작부터 달린 거리.</summary>
        public double Distance => _distance;

        /// <param name="snackIntervalSec">설정 runner.trackSnackIntervalSec.</param>
        /// <param name="snackIds">트랙에 놓을 간식 id. 무작위로 고른다.</param>
        /// <param name="seed">배치 난수 시드. 같으면 같은 트랙이 나온다.</param>
        /// <param name="createViews">false면 화면 없이 배치만 계산한다(테스트용).</param>
        public void Init(double snackIntervalSec, IReadOnlyList<string> snackIds, int seed, bool createViews)
        {
            if (!(snackIntervalSec > 0)) throw new ArgumentOutOfRangeException(nameof(snackIntervalSec));
            if (snackIds == null || snackIds.Count == 0) throw new ArgumentException("간식이 없다", nameof(snackIds));

            _snackIntervalSec = snackIntervalSec;
            _snackIds = snackIds;
            _seed = seed;
            _createViews = createViews;
            if (createViews) PrimitiveViews.CreateGround(transform, LaneCount * laneWidth + 1f, spawnAheadDistance + 20f);
            ResetRun(seed);
        }

        /// <summary>새 주행 시작 시 트랙을 비우고 처음부터 놓는다.</summary>
        public void ResetRun(int seed)
        {
            foreach (var item in _items) DestroyView(item);
            _items.Clear();
            _random = new System.Random(seed);
            _distance = 0;
            _nextObstacleAt = startClearDistance;
            _nextMarkerAt = 0;
            _snackTimer = 0;
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

            _snackTimer += worldDt;
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
                // 장애물을 못 놓는 동안 지나간 줄은 건너뛴다.
                if (obstaclesAllowed)
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

            while (_snackTimer >= _snackIntervalSec)
            {
                _snackTimer -= _snackIntervalSec;
                double z = spawnAheadDistance;
                int lane = _random.Next(-1, 2);
                if (lane == _lastObstacleLane && Math.Abs(z - _lastObstacleZ) < snackObstacleGap)
                    lane = lane == 1 ? -1 : lane + 1;
                string id = _snackIds[_random.Next(_snackIds.Count)];
                Add(new TrackItem(TrackItemKind.Snack, lane, z, id));
            }
        }

        /// <summary>달린 거리 기준 위치를 캐릭터 기준 z로 바꾼다.</summary>
        private double Ahead(double atDistance) => atDistance - _distance;

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
            if (_createViews) item.View = PrimitiveViews.CreateItem(item, transform);
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
