using UnityEngine;

namespace SDF
{
    public class SDFSurface : MonoBehaviour
    {
        private static SDFSurface _instance;
        public static SDFSurface Instance => _instance;

        private void Awake()
        {
            Debug.Assert(_instance == null, "Can only have one SDF surface.");
            _instance = this;
        }
    }
}
