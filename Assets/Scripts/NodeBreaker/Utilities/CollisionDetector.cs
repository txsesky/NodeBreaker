using NodeBreaker.Data;
using UnityEngine;

namespace NodeBreaker.Utilities
{
    public static class CollisionDetector
    {
        /// <summary>
        /// Detects if there is a collision between two ICollidable implementations.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsOverlap(
            ICollidable a,
            ICollidable b)
        {
            Vector3[] aAxes = a.axes;
            Vector3[] bAxes = b.axes;

            int aAxesLength = aAxes.Length;
            int bAxesLength = bAxes.Length;

            Vector3[] aVertices = a.vertices;
            Vector3[] bVertices = b.vertices;

            int aVertsLength = aVertices.Length;
            int bVertsLength = bVertices.Length;

            bool hasOverlap = ProjectionHasOverlap(aAxesLength, ref aAxes, bVertsLength,
                ref bVertices, aVertsLength, ref aVertices, Color.red, Color.green);
            hasOverlap = ProjectionHasOverlap(bAxesLength, ref bAxes, aVertsLength,
                             ref aVertices, bVertsLength, ref bVertices, Color.green, Color.red) && hasOverlap;

            return hasOverlap;
        }

        /// <summary>
        /// Detects whether or not there is overlap on all separating axes.
        /// </summary>
        /// <param name="aTransform"></param>
        /// <param name="bTransform"></param>
        /// <param name="aAxesLength"></param>
        /// <param name="aAxes"></param>
        /// <param name="bVertsLength"></param>
        /// <param name="bVertices"></param>
        /// <param name="aVertsLength"></param>
        /// <param name="aVertices"></param>
        /// <param name="aColor"></param>
        /// <param name="bColor"></param>
        /// <returns></returns>
        private static bool ProjectionHasOverlap(
            //Transform aTransform,
            //Transform bTransform,
            int aAxesLength,
            ref Vector3[] aAxes,
            int bVertsLength,
            ref Vector3[] bVertices,
            int aVertsLength,
            ref Vector3[] aVertices,
            Color aColor,
            Color bColor)
        {
            bool hasOverlap = true;

            for (int i = 0; i < aAxesLength; i++)
            {
                float bProjMin = float.MaxValue, aProjMin = float.MaxValue;
                float bProjMax = float.MinValue, aProjMax = float.MinValue;

                Vector3 axis = aAxes[i];

                for (int j = 0; j < bVertsLength; j++)
                {
                    float val = FindScalarProjection(bVertices[j], axis);

                    if (val < bProjMin)
                    {
                        bProjMin = val;
                    }

                    if (val > bProjMax)
                    {
                        bProjMax = val;
                    }
                }

                for (int j = 0; j < aVertsLength; j++)
                {
                    float val = FindScalarProjection(aVertices[j], axis);

                    if (val < aProjMin)
                    {
                        aProjMin = val;
                    }

                    if (val > aProjMax)
                    {
                        aProjMax = val;
                    }
                }

                float overlap = FindOverlap(aProjMin, aProjMax, bProjMin, bProjMax);
                if (overlap < Mathf.Epsilon)
                {
                    hasOverlap = false;
                }
            }

            return hasOverlap;
        }

        /// <summary>
        /// Calculates the scalar projection of one vector onto another.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="unitAxis"></param>
        /// <returns></returns>
        private static float FindScalarProjection(Vector3 point, Vector3 unitAxis)
        {
            return Vector3.Dot(point, unitAxis) / unitAxis.magnitude;
        }

        /// <summary>
        /// Calculates the amount of overlap of two intervals.
        /// </summary>
        /// <param name="astart"></param>
        /// <param name="aend"></param>
        /// <param name="bstart"></param>
        /// <param name="bend"></param>
        /// <returns></returns>
        private static float FindOverlap(float astart, float aend, float bstart, float bend)
        {
            if (astart < bstart)
            {
                if (aend < bstart)
                {
                    return 0f;
                }

                return aend - bstart;
            }

            if (bend < astart)
            {
                return 0f;
            }

            return bend - astart;
        }
    }
}