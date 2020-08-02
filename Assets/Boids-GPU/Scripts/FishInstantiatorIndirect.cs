using UnityEngine;
using BoidInfo = Bansi.Boids.Gpu.BoidSimulationControllerGpu.BoidInfo;

namespace Bansi.Boids.Gpu
{
    public class FishInstantiatorIndirect : MonoBehaviour
    {
        private const string BOIDS_BUFFER_NAME = "BoidsBuffer";
        private const int NUMBER_OF_ARGUMENTS = 5;
        private const int SUBMESH_INDEX = 0;

        [Header("References")]
        [SerializeField] private GameObject _fishPrefab = null;
        [SerializeField] private Material _fishMaterial = null;

        private Mesh _instancedFishMesh = null;
        private Material _instancedMaterial = null;
        private ComputeBuffer _argumentsBuffer = null;
        private uint[] _arguments = new uint[NUMBER_OF_ARGUMENTS] { 0, 0, 0, 0, 0 };

        private int _totalNumberOfFishes = 0;
        private Bounds _fishSimulationBounds = new Bounds();

        private void OnDestroy()
        {
            if(_argumentsBuffer != null)
            {
                _argumentsBuffer.Dispose();
                _argumentsBuffer = null;
            }
        }

        public void InitializeData(int totalNumberOfFishes, BoidSimulationParametersGpu boidSimulationParameters, Vector3 simulationCenter)
        {
            _totalNumberOfFishes = totalNumberOfFishes;

            _instancedMaterial = new Material(_fishMaterial);

            InitializeInstancedMesh();

            InitializeArgumentsBuffer();

            InitializeFishSimulationBounds(boidSimulationParameters, simulationCenter);
        }

        private void InitializeInstancedMesh()
        {
            MeshFilter meshFilter = _fishPrefab.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = _fishPrefab.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null)
            {
                Debug.LogError("Fish prefab needs to have both mesh filter and mesh renderer!");
                return;
            }

            _instancedFishMesh = meshFilter.sharedMesh;
        }

        private void InitializeArgumentsBuffer()
        {
            _argumentsBuffer = new ComputeBuffer(1, NUMBER_OF_ARGUMENTS * sizeof(uint), ComputeBufferType.IndirectArguments);

            if(_instancedFishMesh != null)
            {
                _arguments[0] = (uint)_instancedFishMesh.GetIndexCount(SUBMESH_INDEX);
                _arguments[1] = (uint)_totalNumberOfFishes;
                _arguments[2] = (uint)_instancedFishMesh.GetIndexStart(SUBMESH_INDEX);
                _arguments[3] = (uint)_instancedFishMesh.GetBaseVertex(SUBMESH_INDEX);
                _arguments[4] = (uint)0;
            }

            _argumentsBuffer.SetData(_arguments);
        }

        private void InitializeFishSimulationBounds(BoidSimulationParametersGpu boidSimulationParameters, Vector3 simulationCenter)
        {
            _fishSimulationBounds.center = simulationCenter;
            _fishSimulationBounds.size = Vector3.one * boidSimulationParameters.SimulationBoxSize;
        }

        public void DrawFishInstanced(ComputeBuffer boidsInfoBuffer)
        {
            _instancedMaterial.SetBuffer(BOIDS_BUFFER_NAME, boidsInfoBuffer);
            Graphics.DrawMeshInstancedIndirect(_instancedFishMesh, SUBMESH_INDEX, _instancedMaterial, _fishSimulationBounds, _argumentsBuffer);
        }        
    }
}

