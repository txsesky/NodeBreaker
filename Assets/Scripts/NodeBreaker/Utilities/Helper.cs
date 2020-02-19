using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NodeBreaker.Data;
using UnityEngine;

namespace NodeBreaker.Utilities
{
    public static class Helper
    {
        public static List<Entity> GetEntitiesMatchingName(string path, string subPath0, string subPath1,
            string subPath2, GameObject gameObject)
        {
            var names = GetNamesFromJson(path, subPath0, subPath1, subPath2);
            var entities = new List<Entity>();
            var children = new List<GameObject>();

            Traverse(ref children, gameObject.transform);

            foreach (var nodeName in names)
            {
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

            return entities;
        }

        public static List<Entity> GetChildrenWithMesh(GameObject baseObject)
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

        public static List<Entity> GetChildrenWithMesh(Entity baseEntity)
        {
            var entities = new List<Entity>();

            if (baseEntity.gameObject.GetComponent<MeshFilter>() != null)
            {
                var entity = new Entity {gameObject = baseEntity.gameObject};
                entities.Add(entity);
            }

            IterateOverChildren(ref entities, baseEntity.gameObject);

            return entities;
        }
        
        private static List<string> GetNamesFromJson(string path, string subPath0, string subPath1, string subPath2)
        {
            string file = "";
            var names = new List<string>();

            file = File.ReadAllText(path);
            var jObject = JObject.Parse(file);
            var jArray = JArray.Parse(jObject[subPath0][subPath1].ToString());

            foreach (var jToken in jArray)
            {
                var jObj = (JObject) jToken;
                var jArr = JArray.Parse(jObj[subPath2].ToString());

                foreach (var jTok in jArr)
                {
                    var colName = jTok.ToString();
                    names.Add(colName);
                }
            }

            return names;
        }

        private static void Traverse(ref List<GameObject> gameObjects, Transform tr)
        {
            if (tr == null)
                return;

            foreach (Transform child in tr)
            {
                gameObjects.Add(child.gameObject);
                Traverse(ref gameObjects, child);
            }
        }

        private static void IterateOverChildren(ref List<Entity> entities, GameObject baseObject)
        {
            if (baseObject == null)
                return;

            if (String.Compare(baseObject.name, "HUD", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return;
            }

            foreach (Transform child in baseObject.transform)
            {
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
    }
}