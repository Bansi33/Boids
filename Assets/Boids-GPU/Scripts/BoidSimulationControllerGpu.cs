using System.Collections.Generic;
using UnityEngine;

namespace Bansi.Boids.Gpu
{
    public class BoidSimulationControllerGpu : MonoBehaviour
    {
        #region STRUCTURES
        public struct BoidInfo 
        {
            public Vector3 Position;
            public Vector3 Acceleration;
            public Vector3 Velocity;
        }

        public struct TargetInfo
        {
            public Vector3 Position;
            public float AttractionRadiusSqrd; // Larger radius which attracts boids
            public float CoreRadiusSqrd; // Smaller radius in which attraction is not working
        }

        public struct ObstacleInfo
        {
            public Vector3 Position;
            public float RadiusSqrd;
        }

        public struct SimulationSpaceBounds
        {
            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;
            public float MinZ;
            public float MaxZ;
        }
        #endregion

        private const int THREAD_GROUP_SIZE = 8;

        [Header("References")]
        [SerializeField] private FishInstantiatorIndirect _fishInstantiator = null;
        [SerializeField] private List<TargetGpu> _targets = null;
        [SerializeField] private List<ObstacleGpu> _obstacles = null;
        [Space]
        [SerializeField] private ComputeShader _boidSimulationShaderPrefab = null;
        [SerializeField] private BoidSimulationParametersGpu _boidSimulationParameters = null;

        [Header("Options")]
        [SerializeField, Range(1, 3)] private int _numberOfFishChunksX = 1;
        [SerializeField, Range(1, 3)] private int _numberOfFishChunksY = 1;
        [SerializeField, Range(1, 3)] private int _numberOfFishChunksZ = 1;

        private int _totalNumberOfBoids = 0;
        private int _numberOfBoidsX = 0;
        private int _numberOfBoidsY = 0;
        private int _numberOfBoidsZ = 0;

        private ComputeShader _boidSimulationShader = null;

        private BoidInfo[] _boidsData = null;
        private ComputeBuffer _boidsBuffer;

        private TargetInfo[] _targetsData = null;
        private Dictionary<TargetGpu, TargetInfo> _targetsByInfo = new Dictionary<TargetGpu, TargetInfo>();
        private ComputeBuffer _targetsBuffer;

        private ObstacleInfo[] _obstaclesData = null;
        private Dictionary<ObstacleGpu, ObstacleInfo> _obstaclesByInfo = new Dictionary<ObstacleGpu, ObstacleInfo>();
        private ComputeBuffer _obstaclesBuffer;

        private ComputeBuffer _neighborsBuffer;
        private int[] _neighbors;

        private ComputeBuffer _simulationSpaceBuffer;
        private SimulationSpaceBounds[] _simulationSpaceBounds;

        private void Start()
        {
            _boidSimulationShader = (ComputeShader)Instantiate(Resources.Load(_boidSimulationShaderPrefab.name));

            _numberOfBoidsX = THREAD_GROUP_SIZE * _numberOfFishChunksX;
            _numberOfBoidsY = THREAD_GROUP_SIZE * _numberOfFishChunksY;
            _numberOfBoidsZ = THREAD_GROUP_SIZE * _numberOfFishChunksZ;

            _totalNumberOfBoids = _numberOfBoidsX * _numberOfBoidsY * _numberOfBoidsZ;

            InitializeBoids();
            InitializeDataStructures();
            InitializeBuffers();

            SetSimulationParameters();
        }

        private void Update()
        {
            // Updating data arrays for shader based on current obstacle and targets positions
            UpdateTargetsPositions();
            UpdateObstaclesPositions();

            // Resending changable data to the GPU
            _boidSimulationShader.SetBuffer(0, "targets", _targetsBuffer);
            _boidSimulationShader.SetBuffer(0, "obstacles", _obstaclesBuffer);
            _boidSimulationShader.SetFloat("deltaTime", Time.deltaTime);

            // Calculating boids movement on GPU
            _boidSimulationShader.Dispatch(0, _numberOfFishChunksX, _numberOfFishChunksY, _numberOfFishChunksZ);

            // Draw fishes instanced on GPU with providing position, rotation, scale and acceleration data to the material
            _fishInstantiator.DrawFishInstanced(_boidsBuffer);
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        void OnDrawGizmos()
        {
            if(_boidSimulationParameters == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * _boidSimulationParameters.SimulationBoxSize);
        }

        private void SetSimulationParameters()
        {
            _boidSimulationShader.SetInt("totalNumberOfBoids", _totalNumberOfBoids);
            _boidSimulationShader.SetInt("numberOfBoidsX", _numberOfBoidsX);
            _boidSimulationShader.SetInt("numberOfBoidsY", _numberOfBoidsY);
            _boidSimulationShader.SetInt("numberOfBoidsZ", _numberOfBoidsZ);

            _boidSimulationShader.SetFloat("minSpeed", _boidSimulationParameters.MinSpeed);
            _boidSimulationShader.SetFloat("maxSpeed", _boidSimulationParameters.MaxSpeed);
            _boidSimulationShader.SetFloat("minNeighborDistance", _boidSimulationParameters.MinNeighborDistance);

            float neighborViewThreshold = Mathf.Cos(_boidSimulationParameters.NeighborFov * Mathf.Deg2Rad);
            _boidSimulationShader.SetFloat("neighborViewThreshold", neighborViewThreshold);

            _boidSimulationShader.SetFloat("simulationSpaceEdgeEffectDistance", _boidSimulationParameters.SimulationSpaceEdgeEffectDistance);

            _boidSimulationShader.SetFloat("wallWeight", _boidSimulationParameters.WallWeight);
            _boidSimulationShader.SetFloat("allignmentWeight", _boidSimulationParameters.AllignmentWeight);
            _boidSimulationShader.SetFloat("cohesionWeight", _boidSimulationParameters.CohesionWeight);
            _boidSimulationShader.SetFloat("separationWeight", _boidSimulationParameters.SeparationWeight);

            _boidSimulationShader.SetFloat("targetAttractionWeight", _boidSimulationParameters.TargetAttractionWeight);
            _boidSimulationShader.SetFloat("obstacleRejectionWeight", _boidSimulationParameters.ObstacleRejectionWeight);

            _boidSimulationShader.SetInt("targetsCount", _targets.Count);
            _boidSimulationShader.SetInt("obstaclesCount", _obstacles.Count);
        }

        private void InitializeBoids()
        {
            _fishInstantiator.InitializeData(_totalNumberOfBoids, _boidSimulationParameters, transform.position);

            _boidsData = new BoidInfo[_totalNumberOfBoids];

            for (int i = 0; i < _totalNumberOfBoids; i++)
            {
                CreateBoid(i);
            }
        }

        private void CreateBoid(int index)
        {
            BoidInfo boidInfo = new BoidInfo
            {
                Position = Random.insideUnitSphere + transform.position,
                Velocity = Random.insideUnitSphere.normalized * _boidSimulationParameters.InitialSpeed,
                Acceleration = Vector3.zero
            };

            _boidsData[index] = boidInfo;
        }

        private void InitializeDataStructures()
        {
            _targetsData = new TargetInfo[_targets.Count];
            for (int i = 0; i < _targets.Count; i++)
            {
                CreateTargetDataStructure(i);
            }

            _obstaclesData = new ObstacleInfo[_obstacles.Count];
            for (int i = 0; i < _obstacles.Count; i++)
            {
                CreateObstacleDataStructure(i);
            }

            _neighbors = new int[_totalNumberOfBoids * _totalNumberOfBoids];

            float radius = _boidSimulationParameters.SimulationBoxSize * 0.5f;
            SimulationSpaceBounds simulationSpaceBounds = new SimulationSpaceBounds
            {
                MaxX = transform.position.x + radius,
                MinX = transform.position.x - radius,
                MaxY = transform.position.y + radius,
                MinY = transform.position.y - radius,
                MaxZ = transform.position.z + radius,
                MinZ = transform.position.z - radius
            };
            _simulationSpaceBounds = new SimulationSpaceBounds[1];
            _simulationSpaceBounds[0] = simulationSpaceBounds;
        }

        private void CreateTargetDataStructure(int index)
        {
            TargetGpu target = _targets[index];
            TargetInfo targetInfo = new TargetInfo
            {
                Position = target.Position,
                AttractionRadiusSqrd = target.TargetAttractionRadiusSqrd,
                CoreRadiusSqrd = target.TargetNotAffectingRadiusSqrd
            };

            _targetsData[index] = targetInfo;
            _targetsByInfo.Add(target, targetInfo);
        }

        private void CreateObstacleDataStructure(int index)
        {
            ObstacleGpu obstacle = _obstacles[index];
            ObstacleInfo obstacleInfo = new ObstacleInfo
            {
                Position = obstacle.Position,
                RadiusSqrd = obstacle.ObstacleRadiusSqrd
            };

            _obstaclesData[index] = obstacleInfo;
            _obstaclesByInfo.Add(obstacle, obstacleInfo);
        }

        private void InitializeBuffers()
        {
            // Vector3 has 3 float values -> Velocity, Acceleration and Position all have 3 floats -> total 9
            _boidsBuffer = new ComputeBuffer(_totalNumberOfBoids, sizeof(float) * 9);

            // Position -> 3 floats + Radius -> 1 float = 4 floats
            _obstaclesBuffer = new ComputeBuffer(_obstacles.Count, sizeof(float) * 4);

            // Position -> 3 floats + AttractionRadius, NonAttractiveRadius -> 1 float = 5 floats
            _targetsBuffer = new ComputeBuffer(_targets.Count, sizeof(float) * 5);

            // Buffer containing neighbors for each boid
            _neighborsBuffer = new ComputeBuffer(_neighbors.Length, sizeof(int));

            // Buffer containing simulation space bounds, one float for each extent (left, right, up, down ...)
            _simulationSpaceBuffer = new ComputeBuffer(1, sizeof(float) * 6);

            _boidsBuffer.SetData(_boidsData);
            _obstaclesBuffer.SetData(_obstaclesData);
            _targetsBuffer.SetData(_targetsData);
            _neighborsBuffer.SetData(_neighbors);
            _simulationSpaceBuffer.SetData(_simulationSpaceBounds);

            _boidSimulationShader.SetBuffer(0, "boids", _boidsBuffer);
            _boidSimulationShader.SetBuffer(0, "targets", _targetsBuffer);
            _boidSimulationShader.SetBuffer(0, "obstacles", _obstaclesBuffer);
            _boidSimulationShader.SetBuffer(0, "neighbors", _neighborsBuffer);
            _boidSimulationShader.SetBuffer(0, "simulationBounds", _simulationSpaceBuffer);
        }

        private void ReleaseBuffers()
        {
            if(_targetsBuffer != null)
            {
                _targetsBuffer.Release();
            }

            if(_boidsBuffer != null)
            {
                _boidsBuffer.Release();
            }

            if(_obstaclesBuffer != null)
            {
                _obstaclesBuffer.Release();
            }

            if(_neighborsBuffer != null)
            {
                _neighborsBuffer.Release();
            }

            if(_simulationSpaceBuffer != null)
            {
                _simulationSpaceBuffer.Release();
            }
        }

        private void UpdateTargetsPositions()
        {
            for(int i = 0; i < _targets.Count; i++)
            {
                TargetGpu target = _targets[i];
                TargetInfo targetInfo = _targetsByInfo[target];

                targetInfo.Position = target.transform.position;

                _targetsByInfo[target] = targetInfo;
            }

            int index = 0;
            foreach (TargetInfo targetInfo in _targetsByInfo.Values)
            {
                _targetsData[index] = targetInfo;
                index++;
            }

            _targetsBuffer.SetData(_targetsData);
        }

        private void UpdateObstaclesPositions()
        {
            for (int i = 0; i < _obstacles.Count; i++)
            {
                ObstacleGpu obstacle = _obstacles[i];
                ObstacleInfo obstacleInfo = _obstaclesByInfo[obstacle];

                obstacleInfo.Position = obstacle.transform.position;

                _obstaclesByInfo[obstacle] = obstacleInfo;
            }

            int index = 0;
            foreach(ObstacleInfo obstacleInfo in _obstaclesByInfo.Values)
            {
                _obstaclesData[index] = obstacleInfo;
                index++;
            }

            _obstaclesBuffer.SetData(_obstaclesData);
        }
    }
}