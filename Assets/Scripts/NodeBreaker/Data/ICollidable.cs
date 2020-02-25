using UnityEngine;

namespace NodeBreaker.Data
{
    public interface ICollidable
    {
        /// <summary>
        /// An array of unit length axes on which to check for separation.
        /// </summary>
        Vector3[] axes { get; }

        /// <summary>
        /// An array of vertices in local space to check against.
        /// </summary>
        Vector3[] vertices { get; }
        
        /// <summary>
        /// Center of the collider.
        /// </summary>
        Vector3 center { get; }

        Vector3 extents { get; }
    }
}