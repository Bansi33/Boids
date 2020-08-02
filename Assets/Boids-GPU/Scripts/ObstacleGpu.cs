using UnityEngine;

namespace Bansi.Boids.Gpu
{
    public class ObstacleGpu : MonoBehaviour
    {
        [SerializeField] private float _obstacleRadius = 0.5f;
        [SerializeField] private Vector3[] _movementCheckpoints = null;
        [SerializeField] private float _movementSpeed = 2f;
        [SerializeField] private float _checkpointReachedDistance = 0.5f;

        [HideInInspector] public Vector3 Position;

        private Vector3 _movementDirection = Vector3.zero;
        private int _currentCheckpointIndex = 0;
        private float _checkpointDistanceSqrd = 0f;

        public float ObstacleRadiusSqrd { get { return _obstacleRadius * _obstacleRadius; } }

        private void Awake()
        {
            _checkpointDistanceSqrd = _checkpointReachedDistance * _checkpointReachedDistance;
        }

        private void Update()
        {
            _movementDirection = (_movementCheckpoints[_currentCheckpointIndex] - Position).normalized;
            transform.forward = _movementDirection;
            transform.position += _movementDirection * _movementSpeed * Time.deltaTime;
            Position = transform.position;

            if ((_movementCheckpoints[_currentCheckpointIndex] - Position).sqrMagnitude < _checkpointDistanceSqrd)
            {
                ChangeCheckpoint();
            }
        }

        private void ChangeCheckpoint()
        {
            if (_movementCheckpoints == null || _movementCheckpoints.Length < 2)
            {
                return;
            }

            int nextCheckpointId = _currentCheckpointIndex;
            while (nextCheckpointId == _currentCheckpointIndex)
            {
                nextCheckpointId = Random.Range(0, _movementCheckpoints.Length);
            }
            _currentCheckpointIndex = nextCheckpointId;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _obstacleRadius);
        }
    }
}