using UnityEngine;
using BoidInfo = Bansi.Boids.Gpu.BoidSimulationControllerGpu.BoidInfo;

namespace Bansi.Boids.Gpu
{
    public class FishInstantiator : MonoBehaviour
    {
        private const float BATCH_MAX_FLOAT = 1023f;
        private const int BATCH_MAX = 1023;

        [Header("References")]
        [SerializeField] private GameObject _fishPrefab = null;
        [SerializeField] private Material _fishMaterial = null;

        private MeshFilter _meshFilter = null;
        private MeshRenderer _meshRenderer = null;
        private Vector3 _fishScale = Vector3.zero;

        private int _totalNumberOfFishes = 0;
        private int _numberOfBatches = 0;
        private Matrix4x4[] _batchedMatrices = new Matrix4x4[BATCH_MAX];

        public void InitializeData(int totalNumberOfFishes)
        {
            _totalNumberOfFishes = totalNumberOfFishes;
            _numberOfBatches = Mathf.CeilToInt(_totalNumberOfFishes / BATCH_MAX_FLOAT);

            _meshFilter = _fishPrefab.GetComponent<MeshFilter>();
            _meshRenderer = _fishPrefab.GetComponent<MeshRenderer>();
            _fishScale = _fishPrefab.transform.localScale;

            if (_meshFilter == null || _meshRenderer == null)
            {
                Debug.LogError("Fish prefab needs to have both mesh filter and mesh renderer!");
                return;
            }
        }

        public void DrawFishInstanced(BoidInfo[] fishes)
        {
            for(int i = 0; i < _numberOfBatches; i++)
            {
                int batchCount = Mathf.Min(BATCH_MAX, _totalNumberOfFishes - (BATCH_MAX * i));
                int startingIndex = Mathf.Max(0, (i - 1) * BATCH_MAX);

                Matrix4x4[] batchedMatrices = GetBatchedMatrices(startingIndex, batchCount, fishes);
                Graphics.DrawMeshInstanced(_meshFilter.sharedMesh, 0, _fishMaterial, batchedMatrices);
            }
        }

        private Matrix4x4[] GetBatchedMatrices(int startingIndex, int count, BoidInfo[] fishes)
        {
            Matrix4x4[] batchedMatrices;

            if (count != BATCH_MAX)
            {
                batchedMatrices = new Matrix4x4[count];
            }
            else
            {
                batchedMatrices = _batchedMatrices;
            }

            for (int i = 0; i < count; ++i)
            {
                Matrix4x4 fishMatrix = Matrix4x4.identity;
                BoidInfo fishInfo = fishes[i + startingIndex];

                fishMatrix.SetTRS(fishInfo.Position, Quaternion.LookRotation(fishInfo.Velocity.normalized), _fishScale);
                batchedMatrices[i] = fishMatrix; 
            }

            return batchedMatrices;
        }
    }
}