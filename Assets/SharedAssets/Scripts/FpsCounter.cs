using TMPro;
using UnityEngine;

namespace Bansi.Boids
{
    public class FpsCounter : MonoBehaviour
    {
        private const string FPS_SUFFIX = " FPS";

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _fpsText = null;

        private int _averageFramerate = 0;

        private void Update()
        {
            float current = 0;
            current = (int)(1f / Time.unscaledDeltaTime);
            _averageFramerate = (int)current;
            _fpsText.text = _averageFramerate.ToString() + FPS_SUFFIX;
        }
    }
}