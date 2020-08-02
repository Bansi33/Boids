using System.Collections.Generic;
using UnityEngine;

namespace Bansi.Boids.Cpu
{
    public class Boid : MonoBehaviour
    {
        private const int NO_NEIGHBOURS = 0;
        private const float HALF_FACTOR = 0.5f;

        // Accessable for other Boids, but shouldn't be changed from inspector
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public Vector3 Velocity;
        [HideInInspector] public Vector3 Acceleration;

        private BoidSimulationController _boidSimulationController = null;
        private BoidSimulationParametersCpu _boidSimulationParameters = null;
        private List<Boid> _neighbors = new List<Boid>();
        private List<Target> _targets = new List<Target>();
        private List<Obstacle> _obstacles = new List<Obstacle>();

        public void Initialize(BoidSimulationController boidSimulationController, BoidSimulationParametersCpu boidSimulationParameters)
        {
            // Caching references and initializing start position and velocity
            _boidSimulationController = boidSimulationController;
            _boidSimulationParameters = boidSimulationParameters;
            Position = transform.position;
            Velocity = transform.forward * _boidSimulationParameters.InitialSpeed;
        }

        private void Update()
        {
            // Whole boid logic is contained in these methods, boids behave based on parameters that determine their bahaviour
            // in terms of how close they will try to get to other boids, how fast will they avoid obstacles etc.
            UpdateNeighbors();
            UpdateTargets();
            UpdateObstacles();

            UpdateSimulationSpace();
            UpdateSeparation();
            UpdateAllignment();
            UpdateCohesion();

            UpdateTargetsAttraction();
            UpdateObstaclesRejection();

            UpdateMove();
        }

        private void UpdateNeighbors()
        {
            // Only other boids that are in ceratain radius from this boid will affect it's behaviour.
            // Also each boid has angle of percipation. So boids out of percipation angle won't affect it's behaviour either.
            _neighbors.Clear();
            float neighborViewThreshold = Mathf.Cos(_boidSimulationParameters.NeighborFov * Mathf.Deg2Rad);
            float neighborMinDistance = _boidSimulationParameters.MinNeighborDistance;

            foreach(Boid boid in _boidSimulationController.Boids)
            {
                if(boid == this)
                {
                    return;
                }

                Vector3 directionToBoid = boid.Position - Position;
                float distance = directionToBoid.magnitude;

                if(distance < neighborMinDistance)
                {
                    Vector3 directionNormalized = directionToBoid.normalized;
                    Vector3 forward = Velocity.normalized;
                    float angleBetween = Vector3.Dot(forward, directionNormalized);

                    if(angleBetween > neighborViewThreshold)
                    {
                        _neighbors.Add(boid);
                    }
                }
            }
        }

        private void UpdateTargets()
        {
            // Each boid is affected by series of targets that attract it.
            _targets.Clear();
            foreach (Target target in _boidSimulationController.Targets)
            {
                float sqrdDistanceFromTarget = (target.Position - Position).sqrMagnitude;
                if (sqrdDistanceFromTarget < target.TargetAttractionRadiusSqrd && 
                    sqrdDistanceFromTarget > target.TargetNotAffectingRadiusSqrd)
                {
                    _targets.Add(target);
                }
            }
        }

        private void UpdateObstacles()
        {
            // Each boid is affected by series of obstacles it runs away from.
            _obstacles.Clear();
            foreach (Obstacle obstacle in _boidSimulationController.Obstacles)
            {
                float sqrdDistanceFromObstacle = (obstacle.Position - Position).sqrMagnitude;
                if (sqrdDistanceFromObstacle < obstacle.ObstacleRadiusSqrd)
                {
                    _obstacles.Add(obstacle);
                }
            }
        }

        private void UpdateSimulationSpace()
        {
            // Boid simulation is calculated inside predefined space so each boid must stay inside that space.
            float simulationBoxRadius = _boidSimulationParameters.SimulationBoxSize * HALF_FACTOR;
            Acceleration +=
                CalculateAccelerationAgainstWall(-simulationBoxRadius - Position.x, Vector3.right) +
                CalculateAccelerationAgainstWall(-simulationBoxRadius - Position.y, Vector3.up) +
                CalculateAccelerationAgainstWall(-simulationBoxRadius - Position.z, Vector3.forward) +
                CalculateAccelerationAgainstWall(+simulationBoxRadius - Position.x, Vector3.left) +
                CalculateAccelerationAgainstWall(+simulationBoxRadius - Position.y, Vector3.down) +
                CalculateAccelerationAgainstWall(+simulationBoxRadius - Position.z, Vector3.back);
        }

        private Vector3 CalculateAccelerationAgainstWall(float distance, Vector3 directionToAvoidWall)
        {
            // Acceleration amount is determined how close to the simulation border boid is. 
            // Closer the distance -> higher the acceleration.
            if(distance < _boidSimulationParameters.SimulationSpaceEdgeEffectDistance)
            {
                return directionToAvoidWall * (_boidSimulationParameters.WallWeight / 
                    Mathf.Abs(distance / _boidSimulationParameters.SimulationSpaceEdgeEffectDistance));
            }
            return Vector3.zero;
        }

        private void UpdateSeparation()
        {
            // Boids try to separate themselves from other boids to avoid collision.
            if (_neighbors.Count.Equals(NO_NEIGHBOURS))
            {
                return;
            }

            Vector3 separationForce = Vector3.zero;
            foreach (Boid neighbor in _neighbors)
            {
                separationForce += (Position - neighbor.Position).normalized;
            }
            separationForce /= _neighbors.Count;

            Acceleration += separationForce * _boidSimulationParameters.SeparationWeight;
        }

        private void UpdateAllignment()
        {
            // Boids try to allign themselves with other boids so they would reduce the chances
            // of being chased by predator by confusing them by their numbers.
            if (_neighbors.Count == NO_NEIGHBOURS) 
            { 
                return; 
            }

            Vector3 averageVelocity = Vector3.zero;
            foreach (Boid neighbor in _neighbors)
            {
                averageVelocity += neighbor.Velocity;
            }
            averageVelocity /= _neighbors.Count;

            Acceleration += (averageVelocity - Velocity) * _boidSimulationParameters.AllignmentWeight;
        }

        private void UpdateCohesion()
        {
            // Each boid tries to move along with others and get as close to them as possible.
            if (_neighbors.Count == NO_NEIGHBOURS)
            {
                return;
            }

            Vector3 averagePosition = Vector3.zero;
            foreach (Boid neighbor in _neighbors)
            {
                averagePosition += neighbor.Position;
            }
            averagePosition /= _neighbors.Count;

            Acceleration += (averagePosition - Position) * _boidSimulationParameters.CohesionWeight;
        }

        private void UpdateTargetsAttraction()
        {
            foreach(Target target in _targets)
            {
                Vector3 directionTowardsTarget = target.Position - Position;
                float distance = directionTowardsTarget.magnitude;
                Acceleration += directionTowardsTarget * (_boidSimulationParameters.TargetAttractionWeight * Mathf.Abs(distance / target.TargetAttractionRadiusSqrd));
            }
        }

        private void UpdateObstaclesRejection()
        {
            if(_obstacles.Count > 0)
            {
                Acceleration = Vector3.zero;
            }

            foreach (Obstacle obstacle in _obstacles)
            {
                Vector3 directionAwayFromObstacle = Position - obstacle.Position;
                float distance = directionAwayFromObstacle.magnitude;
                Acceleration += directionAwayFromObstacle * (_boidSimulationParameters.ObstacleRejectionWeight / Mathf.Abs(distance));
            }
        }

        private void UpdateMove()
        {
            // Update the boid movement direction and speed based on the final acceleration calculated by all effects.
            Velocity += Acceleration * Time.deltaTime;

            Vector3 direction = Velocity.normalized;
            float speed = Velocity.magnitude;

            Velocity = Mathf.Clamp(speed, _boidSimulationParameters.MinSpeed, _boidSimulationParameters.MaxSpeed) * direction;
            Position += Velocity * Time.deltaTime;

            Quaternion rotation = Quaternion.LookRotation(Velocity);
            transform.SetPositionAndRotation(Position, rotation);

            Acceleration = Vector3.zero;
        }
    }
}

