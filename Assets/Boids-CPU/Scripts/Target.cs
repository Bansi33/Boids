using UnityEngine;

namespace Bansi.Boids.Cpu
{
    /// <summary>
    /// Stationary target boids are trying to get close to. After they are inside certain radius
    /// target stops affecting them so they wouldn't all circle around it's center all the time.
    /// </summary>
    public class Target : MonoBehaviour
    {
        [SerializeField] private float _targetNotAffectingRadius = 0.5f;
        [SerializeField] private float _targetAttractionRadius = 1f;

        [HideInInspector] public Vector3 Position;

        private float _targetNotAffectingRadiusSqrd = 0f;
        private float _targetAttractionRadiusSqrd = 0f;

        public float TargetNotAffectingRadiusSqrd { get { return _targetNotAffectingRadiusSqrd; } }
        public float TargetAttractionRadiusSqrd { get { return _targetAttractionRadiusSqrd; } }

        private void Awake()
        {
            Position = transform.position;

            _targetNotAffectingRadiusSqrd = _targetNotAffectingRadius * _targetNotAffectingRadius;
            _targetAttractionRadiusSqrd = _targetAttractionRadius * _targetAttractionRadius;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _targetAttractionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _targetNotAffectingRadius);
        }
    }
}