using UnityEngine;

namespace SDF
{
    public class SDFSurface : MonoBehaviour
    {
        private static SDFSurface _instance;
        public static SDFSurface Instance => _instance;
    
        [SerializeField] private float _gravity = 1.0f;
        public float Gravity => _gravity;

        private void Awake()
        {
            Debug.Assert(_instance == null, "Can only have one SDF surface.");
            _instance = this;
        }
    }
}
