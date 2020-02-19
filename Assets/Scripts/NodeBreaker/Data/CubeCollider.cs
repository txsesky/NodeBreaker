using UnityEngine;

namespace NodeBreaker.Data
{
    public struct CubeCollider : ICollidable
    {
        public Vector3[] axes { get; private set; }
        public Vector3[] vertices { get; private set; }
        public Vector3 center { get; private set; }

        /// <summary>
        /// Constructor. Wraps a GameObject with a cube collision implementation.
        /// </summary>
        /// <param name="axes"></param>
        /// <param name="vertices"></param>
        /// <param name="center"></param>
        public CubeCollider(Vector3[] axes, Vector3[] vertices, Vector3 center)
        {
            this.axes = axes;

            this.vertices = vertices;

            this.center = center;
        }
    }
}