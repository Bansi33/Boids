using System.Collections.Generic;
using UnityEngine;

namespace Bansi.Boids.Cpu
{
    /// <summary>
    /// Main purpose of this controller is to spawn and manage boids count.
    /// </summary>
    public class BoidSimulationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _boidPrefab = null;
        [SerializeField] private List<Target> _targets = null;
        [SerializeField] private List<Obstacle> _obstacles = null;
        [SerializeField] private BoidSimulationParametersCpu _boidSimulationParameters = null;

        [Header("Options")]
        [SerializeField] private int _wantedNumberOfBoids = 100;

        private List<Boid> _boids = new List<Boid>();
        public List<Boid> Boids { get { return _boids; } }
        public List<Target> Targets { get { return _targets; } }
        public List<Obstacle> Obstacles { get { return _obstacles; } }

        private void Update()
        {
            while(_boids.Count < _wantedNumberOfBoids)
            {
                AddBoid();
            }

            while(_boids.Count > _wantedNumberOfBoids)
            {
                RemoveBoid();
            }
        }

        private void AddBoid()
        {
            GameObject boidObject = Instantiate(_boidPrefab, Random.insideUnitSphere, Random.rotation);
            boidObject.transform.SetParent(transform);

            Boid boid = boidObject.GetComponent<Boid>();
            boid.Initialize(this, _boidSimulationParameters);

            _boids.Add(boid);
        }

        private void RemoveBoid()
        {
            if (_boids.Count.Equals(0))
            {
                return;
            }

            Boid boid = _boids[_boids.Count - 1];
            _boids.RemoveAt(_boids.Count - 1);
            Destroy(boid.gameObject);
        }

        void OnDrawGizmos()
        {
            if(_boidSimulationParameters == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * _boidSimulationParameters.SimulationBoxSize);
        }
    }
}