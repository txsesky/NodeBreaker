using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathGeoLib;
using Newtonsoft.Json.Linq;
using NodeBreaker.Data;
using UnityEngine;

namespace NodeBreaker.Utilities
{
    public static class Helper
    {
        public static void GetEntitiesMatchingName(out List<Entity> entities,string path, string subPath0, string subPath1,
            string subPath2, GameObject gameObject)
        {
            entities = new List<Entity>();
            var names = GetNamesFromJson(path, subPath0, subPath1, subPath2);
            var children = new List<GameObject>();

            children.Add(gameObject);

            Traverse(ref children, gameObject.transform);
            
            for (int i = 0; i < names.Count; i++)
            {
                var nodeName = names[i];
                
                var go = children.FirstOrDefault(item => item.name == nodeName);
                if (go != null)
                {
                    var entity = new Entity
                    {
                        gameObject = go,
                        initialPosition = go.transform.position,
                        initialRotation = go.transform.rotation,
                        baseForwardVector = gameObject.transform.forward.normalized,
                        baseRightVector = gameObject.transform.right.normalized,
                        baseUpVector = gameObject.transform.up.normalized
                    };
                    entities.Add(entity);
                }
            }
        }

        public static List<Entity> GetChildrenWithMesh(in GameObject baseObject)
        {
            var entities = new List<Entity>();

            if (baseObject.GetComponent<MeshFilter>() != null)
            {
                var entity = new Entity {gameObject = baseObject};
                entities.Add(entity);
            }

            IterateOverChildren(ref entities, baseObject);

            return entities;
        }

        public static void GetChildrenWithMesh(in Entity baseEntity, out List<Entity> entities)
        {
            entities = new List<Entity>();
            if (baseEntity.gameObject.GetComponent<MeshFilter>() != null)
            {
                var entity = new Entity {gameObject = baseEntity.gameObject};
                entities.Add(entity);
            }

            IterateOverChildren(ref entities, baseEntity.gameObject);
            
        }
        
        private static List<string> GetNamesFromJson(string path, string subPath0, string subPath1, string subPath2)
        {
            string file = "";
            var names = new List<string>();

            file = File.ReadAllText(path);
            var jObject = JObject.Parse(file);
            var jArray = JArray.Parse(jObject[subPath0][subPath1].ToString());

            for (int i = 0; i < jArray.Count; i++)
            {
                var jToken = jArray[i];
                
                var jObj = (JObject) jToken;
                var jArr = JArray.Parse(jObj[subPath2].ToString());

                for (int j = 0; j < jArr.Count; j++)
                {
                    var jTok = jArr[j];
                    var colName = jTok.ToString();
                    names.Add(colName);
                }
            }

            return names;
        }

        public static void Traverse(ref List<GameObject> gameObjects, Transform tr)
        {
            if (tr == null)
                return;

            for (int i = 0; i < tr.childCount; i++)
            {
                var child = tr.transform.GetChild(i);
                gameObjects.Add(child.gameObject);
                Traverse(ref gameObjects, child);
            }
        }

        private static void IterateOverChildren(ref List<Entity> entities, GameObject baseObject)
        {
            if (baseObject == null)
                return;

            if (baseObject.name.Contains("HUD", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (baseObject.name.Contains("ARROW", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            for (int i = 0; i < baseObject.transform.childCount; i++)
            {
                var child = baseObject.transform.GetChild(i);
                
                if (child == null)
                    continue;

                if (child.gameObject.GetComponent<MeshFilter>() != null)
                {
                    var childNode = new Entity() {gameObject = child.gameObject};
                    entities.Add(childNode);
                }

                IterateOverChildren(ref entities, child.gameObject);
            }
        }
        
        public static void CreateBoundingCube(OrientedBoundingBox orientedBoundingBox, Color color)
        {
            var center = orientedBoundingBox.Center;

            var axisX = orientedBoundingBox.Axis1;
            var axisY = orientedBoundingBox.Axis2;
            var axisZ = orientedBoundingBox.Axis3;
            var extends = orientedBoundingBox.Extent;

            var A = center - extends.z * axisZ - extends.x * axisX - axisY * extends.y;
            var B = center - extends.z * axisZ + extends.x * axisX - axisY * extends.y;
            var C = center - extends.z * axisZ + extends.x * axisX + axisY * extends.y;
            var D = center - extends.z * axisZ - extends.x * axisX + axisY * extends.y;

            var E = center + extends.z * axisZ - extends.x * axisX - axisY * extends.y;
            var F = center + extends.z * axisZ + extends.x * axisX - axisY * extends.y;
            var G = center + extends.z * axisZ + extends.x * axisX + axisY * extends.y;
            var H = center + extends.z * axisZ - extends.x * axisX + axisY * extends.y;

            Debug.DrawLine(A, B, color, Mathf.Infinity);
            Debug.DrawLine(B, C, color, Mathf.Infinity);
            Debug.DrawLine(C, D, color, Mathf.Infinity);
            Debug.DrawLine(D, A, color, Mathf.Infinity);

            Debug.DrawLine(E, F, color, Mathf.Infinity);
            Debug.DrawLine(F, G, color, Mathf.Infinity);
            Debug.DrawLine(G, H, color, Mathf.Infinity);
            Debug.DrawLine(H, E, color, Mathf.Infinity);

            Debug.DrawLine(A, E, color, Mathf.Infinity);
            Debug.DrawLine(B, F, color, Mathf.Infinity);
            Debug.DrawLine(D, H, color, Mathf.Infinity);
            Debug.DrawLine(C, G, color, Mathf.Infinity);
        }
        
        public static void CreateBoundingCube(CubeCollider cubeCollider, Color color)
        {
            var A = cubeCollider.vertices[0];
            var B = cubeCollider.vertices[1];
            var C = cubeCollider.vertices[2];
            var D = cubeCollider.vertices[3];

            var E = cubeCollider.vertices[4];
            var F = cubeCollider.vertices[5];
            var G = cubeCollider.vertices[6];
            var H = cubeCollider.vertices[7];

            Debug.DrawLine(A, B, color, Mathf.Infinity);
            Debug.DrawLine(B, C, color, Mathf.Infinity);
            Debug.DrawLine(C, D, color, Mathf.Infinity);
            Debug.DrawLine(D, A, color, Mathf.Infinity);

            Debug.DrawLine(E, F, color, Mathf.Infinity);
            Debug.DrawLine(F, G, color, Mathf.Infinity);
            Debug.DrawLine(G, H, color, Mathf.Infinity);
            Debug.DrawLine(H, E, color, Mathf.Infinity);

            Debug.DrawLine(A, E, color, Mathf.Infinity);
            Debug.DrawLine(B, F, color, Mathf.Infinity);
            Debug.DrawLine(D, H, color, Mathf.Infinity);
            Debug.DrawLine(C, G, color, Mathf.Infinity);
        }
        
        public static void CreateBoundingCube(Vector3[] points, Color color)
        {
            var A = points[0];
            var B = points[1];
            var C = points[2];
            var D = points[3];
            
            var E = points[4];
            var F = points[5];
            var G = points[6];
            var H = points[7];
            
            Debug.DrawLine(A, B, color, Mathf.Infinity);
            Debug.DrawLine(B, C, color, Mathf.Infinity);
            Debug.DrawLine(C, D, color, Mathf.Infinity);
            Debug.DrawLine(D, A, color, Mathf.Infinity);

            Debug.DrawLine(E, F, color, Mathf.Infinity);
            Debug.DrawLine(F, G, color, Mathf.Infinity);
            Debug.DrawLine(G, H, color, Mathf.Infinity);
            Debug.DrawLine(H, E, color, Mathf.Infinity);

            Debug.DrawLine(A, E, color, Mathf.Infinity);
            Debug.DrawLine(B, F, color, Mathf.Infinity);
            Debug.DrawLine(D, H, color, Mathf.Infinity);
            Debug.DrawLine(C, G, color, Mathf.Infinity);
        }
    }
}