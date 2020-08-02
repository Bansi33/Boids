using UnityEngine;

namespace Bansi.Boids.Cpu
{
    [CreateAssetMenu(fileName = "BoidSimulationParameters")]
    public class BoidSimulationParametersCpu : ScriptableObject
    {
        public float InitialSpeed = 2f;
        public float MinSpeed = 2f;
        public float MaxSpeed = 5f;

        public float MinNeighborDistance = 1f; // How close another boid needs to be to be considered a neighbor
        public float NeighborFov = 90f; // Field of view for recognizing neighbors

        // Simulation bounds 
        public float SimulationBoxSize = 5f;
        public float SimulationSpaceEdgeEffectDistance = 3f; // How close to walls can boid be before oppositional force starts to effect it

        // Factors that determine how strongly effects of cohesion, allignment, separation will affect boid
        public float WallWeight = 1f;
        public float AllignmentWeight = 2f;
        public float CohesionWeight = 3f;
        public float SeparationWeight = 5f;

        // Targets that attract boids
        public float TargetAttractionWeight = 3f;

        // Obstacles that reject fish
        public float ObstacleRejectionWeight = 10f;
    }
}