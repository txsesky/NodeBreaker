using System.Collections.Generic;
using MathGeoLib;
using UnityEngine;

namespace NodeBreaker.Data
{
    public struct Entity
    {
        public GameObject gameObject { get; set; }
        
        public CubeCollider composedCubeCollider { get; set; }
        public CubeCollider singleCubeCollider { get; set; }
        public Vector3 initialPosition { get; set; }
        public Quaternion initialRotation { get; set; }
        public Vector3 baseForwardVector { get; set; }
        public Vector3 baseRightVector { get; set; }
        public Vector3 baseUpVector { get; set; }
        public Vector3 centerOfMass { get; set; }
        public float distanceFromBaseCenter { get; set; }
        public Vector3 desiredDirection { get; set; }
        public Vector3 desiredPosition { get; set; }
        public string desiredDirectionAxis { get; set; }
        public List<int> otherIds { get; set; }
        public int chunkGroup { get; set; }
        public bool hasGroup { get; set; }
        public float scalarSize { get; set; }
        public Vector3 baseMin { get; set; }
        public Vector3 baseMax { get; set; }
    }
}