using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathGeoLib;
using NodeBreaker.Data;
using NodeBreaker.Utilities;
using UnityEngine;

namespace NodeBreaker.Core
{
    public class NodeBreakerCore : MonoBehaviour
    {
        [SerializeField] private GameObject m_Geometry;

        [SerializeField] private GameObject m_GeometryCheck;

        [SerializeField] private Material m_Material;

        private List<List<Entity>> m_Chunks = new List<List<Entity>>();
        private List<Entity> m_ChunksBases = new List<Entity>();

        private bool m_Initialized;

        private void OnGUI()
        {
            GUIStyle customButton = new GUIStyle("Button");
            customButton.fontSize = 32;
            customButton.fontStyle = FontStyle.Bold;
            customButton.normal.textColor = Color.blue;

            if (GUI.Button(new Rect(0, 0, 300, 150), "Custom Method", customButton))
            {
                var time = Time.realtimeSinceStartup;
                CallCustomMethod();
                print(Time.realtimeSinceStartup - time);
            }

            if (GUI.Button(new Rect(0, 151, 300, 150), "Disassemble", customButton))
            {
                var time = Time.realtimeSinceStartup;
                Disassemble();
                print(Time.realtimeSinceStartup - time);
            }

            if (GUI.Button(new Rect(0, 302, 300, 150), "Assemble", customButton))
            {
                Assemble();
            }
        }

        public void CallCustomMethod()
        {
            var enta = new Entity {gameObject = m_GeometryCheck};
            var list = Helper.GetChildrenWithMesh(enta);
            //enta.composedCubeCollider = GetComposedCubeCollider(list, Color.cyan);

            var points = new List<Vector3>();

            for (int i = 0; i < list.Count; i++)
            {
                var entity = list[i];

                var localToWorldMatrix = entity.gameObject.transform.localToWorldMatrix;
                var bounds = (entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds);

                var min = bounds.min;
                var max = bounds.max;

                var A = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
                var B = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
                var C = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
                var D = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));

                var E = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
                var F = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
                var G = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));
                var H = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));

                points.Add(A);
                points.Add(B);
                points.Add(C);
                points.Add(D);
                points.Add(E);
                points.Add(F);
                points.Add(G);
                points.Add(H);
            }

            var orientedBoundingBox = OrientedBoundingBox.OptimalEnclosing(points.ToArray());

            if (float.IsInfinity(orientedBoundingBox.Extent.x) || float.IsInfinity(orientedBoundingBox.Extent.y) ||
                float.IsInfinity(orientedBoundingBox.Extent.z) || float.IsInfinity(orientedBoundingBox.Center.x) ||
                float.IsInfinity(orientedBoundingBox.Center.y) || float.IsInfinity(orientedBoundingBox.Center.z) ||
                float.IsInfinity(orientedBoundingBox.Axis1.x) || float.IsInfinity(orientedBoundingBox.Axis1.y) ||
                float.IsInfinity(orientedBoundingBox.Axis1.z) || float.IsInfinity(orientedBoundingBox.Axis2.x) ||
                float.IsInfinity(orientedBoundingBox.Axis2.y) || float.IsInfinity(orientedBoundingBox.Axis2.z) ||
                float.IsInfinity(orientedBoundingBox.Axis3.x) || float.IsInfinity(orientedBoundingBox.Axis3.y) ||
                float.IsInfinity(orientedBoundingBox.Axis3.z))
            {
                print("is nan");
                orientedBoundingBox = OrientedBoundingBox.BruteEnclosing(points.ToArray());
            }

            Helper.CreateBoundingCube(orientedBoundingBox, Color.cyan);

            Debug.Log($"{orientedBoundingBox.Extent}");
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                m_Chunks = GetGroupedChunks(m_Geometry);
                m_ChunksBases = GetChunksBases(m_Chunks);

                for (int i = 0; i < m_ChunksBases.Count; i++)
                {
                    for (int j = i + 1; j < m_ChunksBases.Count; j++)
                    {
                        Helper.CreateBoundingCube(m_ChunksBases[i].composedCubeCollider, Color.red);
                        /*if (IsOverlapOrNear(m_ChunksBases[i], m_ChunksBases[j]))
                        {
                            
                        }*/
                    }
                }

                CalculateCenterOfMass(ref m_ChunksBases, m_Chunks);

                var entitiesMatchingName =
                    Helper.GetEntitiesMatchingName(Path.Combine(Application.streamingAssetsPath, "config.json"),
                        "atlas", "table", "colName", m_Geometry);

                //print($"Entities matching name count = {entitiesMatchingName.Count}");

                var movableChunks = GetMovableChunks(entitiesMatchingName);

                for (int i = 0; i < movableChunks.Count; i++)
                {
                    for (int j = 0; j < movableChunks[i].Count; j++)
                    {
                        var entity = movableChunks[i][j];
                        var children = Helper.GetChildrenWithMesh(entity);

                        entity.composedCubeCollider = GetComposedCubeCollider(children, Color.blue);

                        CalculateDirectionAndDistance(ref entity, m_ChunksBases[i].centerOfMass);

                        movableChunks[i][j] = entity;
                    }

                    var entities = movableChunks[i];
                    SortMovableEntitiesInOrder(ref entities);
                    var baseE = m_ChunksBases[i];
                    CalculateBaseMinMax(m_Geometry.transform, ref baseE);
                    m_ChunksBases[i] = baseE;
                    CalculateDesiredPosition(ref entities, m_ChunksBases[i]);
                    movableChunks[i] = entities;
                }

                m_Chunks.Clear();
                m_Chunks = movableChunks;

                StopAllCoroutines();
                StartCoroutine(DrawLines(movableChunks));

                m_Initialized = true;
            }
        }

        private List<List<Entity>> GetMovableChunks(List<Entity> entitiesMatchingName)
        {
            var movableChunks = new List<List<Entity>>();

            for (int i = 0; i < m_Chunks.Count; i++)
            {
                movableChunks.Add(new List<Entity>());
            }

            for (int i = 0; i < entitiesMatchingName.Count; i++)
            {
                var listOfGameObjectsWithChunkId = FindChunkId(entitiesMatchingName[i].gameObject);

                var id = listOfGameObjectsWithChunkId[0].GetComponent<ChunkGroupId>().value;

                var chunkRelationList = new List<int>();

                chunkRelationList.Add(id);

                for (int j = 1; j < listOfGameObjectsWithChunkId.Count; j++)
                {
                    if (listOfGameObjectsWithChunkId[j].GetComponent<ChunkGroupId>().value != id)
                    {
                        chunkRelationList.Add(listOfGameObjectsWithChunkId[j].GetComponent<ChunkGroupId>().value);
                        id = listOfGameObjectsWithChunkId[j].GetComponent<ChunkGroupId>().value;
                    }

                    Destroy(listOfGameObjectsWithChunkId[j].GetComponent<ChunkGroupId>());
                }

                if (chunkRelationList.Count < 2)
                {
                    var entity = entitiesMatchingName[i];
                    entity.chunkGroup = chunkRelationList[0];
                    entitiesMatchingName[i] = entity;
                    movableChunks[entitiesMatchingName[i].chunkGroup].Add(entitiesMatchingName[i]);
                }
                else
                {
                    movableChunks.Add(new List<Entity>());
                    var entity = entitiesMatchingName[i];
                    entity.chunkGroup = m_Chunks.Count;
                    entitiesMatchingName[i] = entity;
                    movableChunks[entitiesMatchingName[i].chunkGroup].Add(entitiesMatchingName[i]);

                    var relativeBases = new List<Entity>();

                    for (int j = 0; j < chunkRelationList.Count; j++)
                    {
                        relativeBases.Add(m_ChunksBases[chunkRelationList[j]]);
                    }

                    var composedCubeCollider = GetComposedCubeColliderFromCollider(relativeBases, Color.red);
                    var centerOfMass = composedCubeCollider.center;

                    var chunkBase = new Entity
                        {composedCubeCollider = composedCubeCollider, centerOfMass = centerOfMass};
                    m_ChunksBases.Add(chunkBase);
                }
            }


            print($"Movable chunks count {movableChunks.Count.ToString()}");

            for (int i = 0; i < movableChunks.Count; i++)
            {
                print($"Movable chunk {i} has {movableChunks[i].Count.ToString()} elements");
            }

            return movableChunks;
        }

        public List<GameObject> FindChunkId(GameObject go)
        {
            var list = new List<GameObject>();

            if (go.GetComponent<ChunkGroupId>() != null)
            {
                list.Add(go);
            }

            IterateOverChildren(ref list, go);

            return list;
        }

        private void IterateOverChildren(ref List<GameObject> list, GameObject o)
        {
            if (o == null)
                return;

            foreach (Transform child in o.transform)
            {
                if (child == null)
                    continue;

                if (child.gameObject.GetComponent<ChunkGroupId>() != null)
                {
                    list.Add(child.gameObject);
                }

                IterateOverChildren(ref list, child.gameObject);
            }
        }

        private List<List<Entity>> GetGroupedChunks(GameObject baseGeometryNode)
        {
            var chunks = new List<List<Entity>>();

            var entities = Helper.GetChildrenWithMesh(baseGeometryNode);

            var groupNumber = -1;

            for (int i = 0; i < entities.Count; i++)
            {
                var e1 = entities[i];

                if (i == 0)
                {
                    e1.singleCubeCollider = GetSingleCubeCollider(e1);
                }

                e1.otherIds = new List<int>();
                entities[i] = e1;

                for (int j = i + 1; j < entities.Count; j++)
                {
                    var e2 = entities[j];
                    e2.singleCubeCollider = GetSingleCubeCollider(e2);
                    entities[j] = e2;

                    if (IsOverlapOrNear(e1, e2))
                    {
                        e1.otherIds.Add(j);
                        entities[i] = e1;
                        //Debug.Log($"has overlap {e1.gameObject.name} {e2.gameObject.name}", e1.gameObject);
                        //Debug.Log($"has overlap {e2.gameObject.name} {e1.gameObject.name}", e2.gameObject);
                    }
                }

                if (!entities[i].hasGroup)
                {
                    groupNumber += 1;
                    //Debug.Log("has no group, now group number is " + groupNumber, entities[i].gameObject);
                    var ent = entities[i];
                    ent.chunkGroup = groupNumber;
                    ent.hasGroup = true;
                    entities[i] = ent;
                    chunks.Add(new List<Entity>());
                }

                var chunkGroupId = entities[i].gameObject.AddComponent<ChunkGroupId>();
                chunkGroupId.value = entities[i].chunkGroup;

                if (chunks[entities[i].chunkGroup].FirstOrDefault(e => e.gameObject == entities[i].gameObject)
                        .gameObject == null)
                {
                    //Debug.Log("element not in chunks add it to chunk number " + entities[i].chunkGroup, entities[i].gameObject);
                    chunks[entities[i].chunkGroup].Add(entities[i]);
                }

                if (entities[i].otherIds.Count > 0)
                {
                    for (int j = 0; j < entities[i].otherIds.Count; j++)
                    {
                        var ent = entities[entities[i].otherIds[j]];
                        ent.chunkGroup = entities[i].chunkGroup;
                        ent.hasGroup = true;
                        entities[entities[i].otherIds[j]] = ent;

                        if (chunks[entities[i].chunkGroup]
                                .FirstOrDefault(e => e.gameObject == entities[entities[i].otherIds[j]].gameObject)
                                .gameObject == null)
                        {
                            chunks[entities[i].chunkGroup].Add(entities[entities[i].otherIds[j]]);
                            //Debug.Log($"Add to group {entities[i].chunkGroup} element {entities[entities[i].otherIds[j]].gameObject.name}",
                            //    entities[entities[i].otherIds[j]].gameObject);
                        }
                    }
                }
            }

            return chunks;
        }

        private List<Entity> GetChunksBases(List<List<Entity>> chunks)
        {
            var chunksBases = new List<Entity>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var composedCubeCollider = GetComposedCubeCollider(chunks[i], Color.green);

                var chunkBase = new Entity {composedCubeCollider = composedCubeCollider};
                chunksBases.Add(chunkBase);
            }

            return chunksBases;
        }


        private CubeCollider GetSingleCubeCollider(Entity entity)
        {
            var localToWorldMatrix = entity.gameObject.transform.localToWorldMatrix;
            var bounds = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds;

            var center = localToWorldMatrix.MultiplyPoint3x4(bounds.center);

            var axisX = entity.gameObject.transform.right;
            var axisY = entity.gameObject.transform.up;
            var axisZ = entity.gameObject.transform.forward;

            var min = bounds.min;
            var max = bounds.max;

            var A = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z)) - center) * 1f;
            var B = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z)) - center) * 1f;
            var C = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z)) - center) * 1f;
            var D = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z)) - center) * 1f;

            var E = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z)) - center) * 1f;
            var F = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z)) - center) * 1f;
            var G = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z)) - center) * 1f;
            var H = center + (localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z)) - center) * 1f;

            var axes = new[]
            {
                axisX, axisY, axisZ
            };

            var points = new[]
            {
                A, B, C, D, E, F, G, H
            };

            return new CubeCollider(axes, points, center);
        }

        private CubeCollider GetComposedCubeCollider(List<Entity> entities, Color color)
        {
            var points = new List<Vector3>();

            if (entities.Count > 1)
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];

                    var localToWorldMatrix = entity.gameObject.transform.localToWorldMatrix;
                    var bounds = (entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds);

                    var min = bounds.min;
                    var max = bounds.max;

                    var A = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
                    var B = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
                    var C = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
                    var D = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));

                    var E = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
                    var F = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
                    var G = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));
                    var H = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));

                    points.Add(A);
                    points.Add(B);
                    points.Add(C);
                    points.Add(D);
                    points.Add(E);
                    points.Add(F);
                    points.Add(G);
                    points.Add(H);
                }

                var orientedBoundingBox = OrientedBoundingBox.OptimalEnclosing(points.ToArray());

                if (float.IsInfinity(orientedBoundingBox.Extent.x) || float.IsInfinity(orientedBoundingBox.Extent.y) ||
                    float.IsInfinity(orientedBoundingBox.Extent.z) || float.IsInfinity(orientedBoundingBox.Center.x) ||
                    float.IsInfinity(orientedBoundingBox.Center.y) || float.IsInfinity(orientedBoundingBox.Center.z))
                {
                    print("is nan");
                    orientedBoundingBox = OrientedBoundingBox.BruteEnclosing(points.ToArray());
                }

                //Helper.CreateBoundingCube(orientedBoundingBox, color);

                var obbCenter = orientedBoundingBox.Center;

                var obbAxisX = orientedBoundingBox.Axis1;
                var obbAxisY = orientedBoundingBox.Axis2;
                var obbAxisZ = orientedBoundingBox.Axis3;
                var extends = orientedBoundingBox.Extent;

                var I = obbCenter - extends.z * obbAxisZ - extends.x * obbAxisX - obbAxisY * extends.y;
                var J = obbCenter - extends.z * obbAxisZ + extends.x * obbAxisX - obbAxisY * extends.y;
                var K = obbCenter - extends.z * obbAxisZ + extends.x * obbAxisX + obbAxisY * extends.y;
                var L = obbCenter - extends.z * obbAxisZ - extends.x * obbAxisX + obbAxisY * extends.y;

                var M = obbCenter + extends.z * obbAxisZ - extends.x * obbAxisX - obbAxisY * extends.y;
                var N = obbCenter + extends.z * obbAxisZ + extends.x * obbAxisX - obbAxisY * extends.y;
                var O = obbCenter + extends.z * obbAxisZ + extends.x * obbAxisX + obbAxisY * extends.y;
                var P = obbCenter + extends.z * obbAxisZ - extends.x * obbAxisX + obbAxisY * extends.y;

                var axes = new[]
                {
                    obbAxisX, obbAxisY, obbAxisZ
                };

                var composedPoints = new[]
                {
                    I, J, K, L, M, N, O, P
                };

                return new CubeCollider(axes, composedPoints, obbCenter);
            }
            else
            {
                return GetSingleCubeCollider(entities[0]);
            }
        }

        private CubeCollider GetComposedCubeColliderFromCollider(List<Entity> entities, Color color)
        {
            var points = new List<Vector3>();

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];

                points.AddRange(entity.composedCubeCollider.vertices);
            }

            var orientedBoundingBox = OrientedBoundingBox.OptimalEnclosing(points.ToArray());

            //Helper.CreateBoundingCube(orientedBoundingBox, color);

            var obbCenter = orientedBoundingBox.Center;

            var obbAxisX = orientedBoundingBox.Axis1;
            var obbAxisY = orientedBoundingBox.Axis2;
            var obbAxisZ = orientedBoundingBox.Axis3;
            var extends = orientedBoundingBox.Extent;

            var I = obbCenter - extends.z * obbAxisZ - extends.x * obbAxisX - obbAxisY * extends.y;
            var J = obbCenter - extends.z * obbAxisZ + extends.x * obbAxisX - obbAxisY * extends.y;
            var K = obbCenter - extends.z * obbAxisZ + extends.x * obbAxisX + obbAxisY * extends.y;
            var L = obbCenter - extends.z * obbAxisZ - extends.x * obbAxisX + obbAxisY * extends.y;

            var M = obbCenter + extends.z * obbAxisZ - extends.x * obbAxisX - obbAxisY * extends.y;
            var N = obbCenter + extends.z * obbAxisZ + extends.x * obbAxisX - obbAxisY * extends.y;
            var O = obbCenter + extends.z * obbAxisZ + extends.x * obbAxisX + obbAxisY * extends.y;
            var P = obbCenter + extends.z * obbAxisZ - extends.x * obbAxisX + obbAxisY * extends.y;

            var axes = new[]
            {
                obbAxisX, obbAxisY, obbAxisZ
            };

            var composedPoints = new[]
            {
                I, J, K, L, M, N, O, P
            };

            return new CubeCollider(axes, composedPoints, obbCenter);
        }

        private void CalculateCenterOfMass(ref List<Entity> chunksBases, List<List<Entity>> chunks)
        {
            for (int k = 0; k < chunksBases.Count; k++)
            {
                var massList = new List<float>();

                var posList = new List<Vector3>();

                var entities = chunks[k];

                for (int i = 0; i < entities.Count; i++)
                {
                    var bounds = entities[i].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds;

                    var size = Vector3.Scale(bounds.size,
                        entities[i].gameObject.transform.localToWorldMatrix.lossyScale);
                    var a = size.x;
                    var b = size.y;
                    var c = size.z;

                    var density = 1;

                    var m = a * b * c * density;

                    massList.Add(m);
                    posList.Add(entities[i].gameObject.transform.localToWorldMatrix.MultiplyPoint3x4(bounds.center));
                }

                var x = 0f;
                var y = 0f;
                var z = 0f;

                var mTotal = 0f;

                for (int i = 0; i < massList.Count; i++)
                {
                    x += massList[i] * posList[i].x;
                    y += massList[i] * posList[i].y;
                    z += massList[i] * posList[i].z;
                    mTotal += massList[i];
                }

                var xResult = x / mTotal;
                var yResult = y / mTotal;
                var zResult = z / mTotal;

                var chb = chunksBases[k];
                chb.centerOfMass = new Vector3(xResult, yResult, zResult);
                chunksBases[k] = chb;
            }
        }

        private void CalculateDirectionAndDistance(ref Entity entity, Vector3 relativePoint)
        {
            //maybe center of mass not sure
            var distanceX = Vector3
                .Project(entity.composedCubeCollider.center - relativePoint, entity.baseRightVector).magnitude;

            var distanceY = Vector3
                .Project(entity.composedCubeCollider.center - relativePoint, entity.baseUpVector).magnitude;

            var distanceZ = Vector3
                .Project(entity.composedCubeCollider.center - relativePoint, entity.baseForwardVector).magnitude;

            if (distanceX > distanceY && distanceX > distanceZ)
            {
                if (Mathf.Sign(Vector3.Dot(entity.composedCubeCollider.center - relativePoint,
                        entity.baseRightVector)) >= 0)
                {
                    entity.desiredDirection = entity.baseRightVector;
                    entity.desiredDirectionAxis = "x";
                }
                else
                {
                    entity.desiredDirection = -entity.baseRightVector;
                    entity.desiredDirectionAxis = "-x";
                }

                entity.distanceFromBaseCenter = distanceX;
            }
            else if (distanceY > distanceX && distanceY > distanceZ)
            {
                if (Mathf.Sign(
                        Vector3.Dot(entity.composedCubeCollider.center - relativePoint, entity.baseUpVector)) >= 0)
                {
                    entity.desiredDirection = entity.baseUpVector;
                    entity.desiredDirectionAxis = "y";
                }
                else
                {
                    entity.desiredDirection = -entity.baseUpVector;
                    entity.desiredDirectionAxis = "-y";
                }

                entity.distanceFromBaseCenter = distanceY;
            }
            else if (distanceZ > distanceX && distanceZ > distanceY)
            {
                if (Mathf.Sign(Vector3.Dot(entity.composedCubeCollider.center - relativePoint,
                        entity.baseForwardVector)) >= 0)
                {
                    entity.desiredDirection = entity.baseForwardVector;
                    entity.desiredDirectionAxis = "z";
                }
                else
                {
                    entity.desiredDirection = -entity.baseForwardVector;
                    entity.desiredDirectionAxis = "-z";
                }

                entity.distanceFromBaseCenter = distanceZ;
            }
        }

        private void SortMovableEntitiesInOrder(ref List<Entity> entities)
        {
            var xPosList = new List<Entity>();
            var yPosList = new List<Entity>();
            var zPosList = new List<Entity>();
            var xNegList = new List<Entity>();
            var yNegList = new List<Entity>();
            var zNegList = new List<Entity>();

            xPosList.AddRange(entities.FindAll(e => e.desiredDirectionAxis == "x"));
            xNegList.AddRange(entities.FindAll(e => e.desiredDirectionAxis == "-x"));
            yPosList.AddRange(entities.FindAll(e => e.desiredDirectionAxis == "y"));
            yNegList.AddRange(entities.FindAll(e => e.desiredDirectionAxis == "-y"));
            zPosList.AddRange(entities.FindAll(e => e.desiredDirectionAxis == "z"));
            zNegList.AddRange(entities.FindAll(e => e.desiredDirectionAxis == "-z"));

            SortingAlgorithm.QuickSort(xPosList, 0, xPosList.Count - 1);
            SortingAlgorithm.QuickSort(yPosList, 0, yPosList.Count - 1);
            SortingAlgorithm.QuickSort(zPosList, 0, zPosList.Count - 1);
            SortingAlgorithm.QuickSort(xNegList, 0, xNegList.Count - 1);
            SortingAlgorithm.QuickSort(yNegList, 0, yNegList.Count - 1);
            SortingAlgorithm.QuickSort(zNegList, 0, zNegList.Count - 1);

            entities.Clear();

            entities.AddRange(xPosList);
            entities.AddRange(xNegList);
            entities.AddRange(yPosList);
            entities.AddRange(yNegList);
            entities.AddRange(zPosList);
            entities.AddRange(zNegList);
        }

        private void CalculateBaseMinMax(Transform geometry, ref Entity baseEntity)
        {
            var baseMinX = float.MaxValue;
            var baseMaxX = float.MinValue;
            var baseMinY = float.MaxValue;
            var baseMaxY = float.MinValue;
            var baseMinZ = float.MaxValue;
            var baseMaxZ = float.MinValue;

            for (int j = 0; j < baseEntity.composedCubeCollider.vertices.Length; j++)
            {
                var valX = Vector3.Dot(baseEntity.composedCubeCollider.vertices[j], geometry.right.normalized) /
                           Vector3.Dot(geometry.right.normalized, geometry.right.normalized);

                if (valX < baseMinX)
                {
                    baseMinX = valX;
                }

                if (valX > baseMaxX)
                {
                    baseMaxX = valX;
                }

                var valY = Vector3.Dot(baseEntity.composedCubeCollider.vertices[j], geometry.up.normalized) /
                           Vector3.Dot(geometry.up.normalized, geometry.up.normalized);

                if (valY < baseMinY)
                {
                    baseMinY = valY;
                }

                if (valY > baseMaxY)
                {
                    baseMaxY = valY;
                }

                var valZ = Vector3.Dot(baseEntity.composedCubeCollider.vertices[j], geometry.forward.normalized) /
                           Vector3.Dot(geometry.forward.normalized, geometry.forward.normalized);

                if (valZ < baseMinZ)
                {
                    baseMinZ = valZ;
                }

                if (valZ > baseMaxZ)
                {
                    baseMaxZ = valZ;
                }
            }

            var e = baseEntity;
            e.baseMax = new Vector3(baseMaxX, baseMaxY, baseMaxZ);
            e.baseMin = new Vector3(baseMinX, baseMinY, baseMinZ);
        }

        float iterSizeZ = 0f;

        private void CalculateDesiredPosition(ref List<Entity> entities, Entity baseEntity)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];

                var dir = Vector3.zero;

                if (entity.desiredDirectionAxis == "x")
                {
                    dir = entity.baseRightVector;
                }
                else if (entity.desiredDirectionAxis == "-x")
                {
                    dir = entity.baseRightVector;
                }
                else if (entity.desiredDirectionAxis == "y")
                {
                    dir = entity.baseUpVector;
                }
                else if (entity.desiredDirectionAxis == "-y")
                {
                    dir = entity.baseUpVector;
                }
                else if (entity.desiredDirectionAxis == "z" || entity.desiredDirectionAxis == "-z")
                {
                    dir = entity.baseForwardVector;

                    var min = float.MaxValue;
                    var max = float.MinValue;

                    for (int j = 0; j < entity.composedCubeCollider.vertices.Length; j++)
                    {
                        var val = Vector3.Dot(entity.composedCubeCollider.vertices[j], dir) /
                                  Vector3.Dot(dir, dir);

                        if (val < min)
                        {
                            min = val;
                        }

                        if (val > max)
                        {
                            max = val;
                        }
                    }

                    var position = Vector3.Dot(entity.gameObject.transform.position, dir) /
                                   Vector3.Dot(dir, dir);

                    float additionalSpace = 0.02f;

                    var cenToSelfEdge = position - min; // + additionalSpace/2;
                    print("cent to self edge " + cenToSelfEdge);

                    var cenToBaseEdge = baseEntity.baseMax.z - position; // + additionalSpace/2;
                    print("cent to base edge " + cenToBaseEdge);

                    var size = max - min + additionalSpace; // + additionalSpace;
                    print("size and add space " + size);

                    var compDist = 0f;

                    if (i == 0)
                    {
                        compDist = cenToSelfEdge + cenToBaseEdge + additionalSpace / 2 + 5;
                        entity.scalarSize = size + 10;
                    }
                    else
                    {
                        compDist = cenToSelfEdge + cenToBaseEdge + entities[i - 1].scalarSize + additionalSpace / 2;
                        entity.scalarSize = size + entities[i - 1].scalarSize;
                    }

                    //iterSizeZ += size;

                    entity.desiredPosition = entity.gameObject.transform.position + compDist * dir;
                    entities[i] = entity;
                }
                else if (entity.desiredDirectionAxis == "-z")
                {
                    dir = entity.baseForwardVector;
                }
            }
        }

        private bool IsOverlapOrNear(Entity one, Entity two)
        {
            return CollisionDetector.IsOverlap(one.singleCubeCollider, two.singleCubeCollider);
        }

        public void Disassemble()
        {
            Initialize();
            StopAllCoroutines();
            foreach (var chunk in m_Chunks)
            {
                StartCoroutine(MoveEntities(chunk));
            }
        }

        public void Assemble()
        {
            StopAllCoroutines();
            foreach (var chunk in m_Chunks)
            {
                StartCoroutine(BackEntitiesPosition(chunk));
            }
        }


        private IEnumerator MoveEntities(List<Entity> entities)
        {
            var movingTime = 0f;
            while (movingTime < 1)
            {
                movingTime += Time.deltaTime * 0.1f;

                foreach (var entity in entities)
                {
                    entity.gameObject.transform.position = Vector3.Lerp(entity.gameObject.transform.position,
                        entity.desiredPosition, movingTime);
                }

                yield return null;
            }
        }

        private IEnumerator BackEntitiesPosition(List<Entity> movableEntities)
        {
            var movingTime = 0f;
            while (movingTime < 1)
            {
                movingTime += Time.deltaTime * 0.1f;

                foreach (var entity in movableEntities)
                {
                    entity.gameObject.transform.position = Vector3.Lerp(entity.gameObject.transform.position,
                        entity.initialPosition, movingTime);
                }

                yield return null;
            }
        }

        private IEnumerator DrawLines(List<List<Entity>> chunks)
        {
            foreach (var chunk in chunks)
            {
                var entities = chunk;
                foreach (var entity in entities)
                {
                    yield return new WaitForSeconds(0.1f);

                    Debug.DrawLine(entity.gameObject.transform.position, entity.desiredPosition,
                        Color.yellow,
                        Mathf.Infinity);
                    //Debug.Log(entity.gameObject.name + " " + entity.composedCubeCollider.center + " ", entity.gameObject);
                }
            }
        }
    }
}


/*private void CalculateCenterOfMass(ref Entity chunkBase)
{
    var massList = new List<float>();

    var posList = new List<Vector3>();

    var entities = Helper.GetChildrenWithMesh(chunkBase);

    for (int i = 0; i < entities.Count; i++)
    {
        var bounds = entities[i].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds;

        var size = Vector3.Scale(bounds.size, entities[i].gameObject.transform.localToWorldMatrix.lossyScale);
        var a = size.x;
        var b = size.y;
        var c = size.z;

        var density = 1;

        var m = a * b * c * density;

        massList.Add(m);
        posList.Add(entities[i].gameObject.transform.localToWorldMatrix.MultiplyPoint3x4(bounds.center));
    }

    var x = 0f;
    var y = 0f;
    var z = 0f;

    var mTotal = 0f;

    for (int i = 0; i < massList.Count; i++)
    {
        x += massList[i] * posList[i].x;
        y += massList[i] * posList[i].y;
        z += massList[i] * posList[i].z;
        mTotal += massList[i];
    }

    var xResult = x / mTotal;
    var yResult = y / mTotal;
    var zResult = z / mTotal;

    chunkBase.centerOfMass = new Vector3(xResult, yResult, zResult);
}*/

/*private OrientedBoundingBox ComposeOBB(List<Entity> entities)
{
    var points = new List<Vector3>();

    for (int i = 0; i < entities.Count; i++)
    {
        var center = entities[i].singleObb.Center;

        var axisX = entities[i].singleObb.Axis1;
        var axisY = entities[i].singleObb.Axis2;
        var axisZ = entities[i].singleObb.Axis3;
        var extends = entities[i].singleObb.Extent;

        var A = center - extends.z * axisZ - extends.x * axisX - axisY * extends.y;
        var B = center - extends.z * axisZ + extends.x * axisX - axisY * extends.y;
        var C = center - extends.z * axisZ + extends.x * axisX + axisY * extends.y;
        var D = center - extends.z * axisZ - extends.x * axisX + axisY * extends.y;

        var E = center + extends.z * axisZ - extends.x * axisX - axisY * extends.y;
        var F = center + extends.z * axisZ + extends.x * axisX - axisY * extends.y;
        var G = center + extends.z * axisZ + extends.x * axisX + axisY * extends.y;
        var H = center + extends.z * axisZ - extends.x * axisX + axisY * extends.y;

        points.Add(A);
        points.Add(B);
        points.Add(C);
        points.Add(D);
        points.Add(E);
        points.Add(F);
        points.Add(G);
        points.Add(H);
    }

    return OrientedBoundingBox.OptimalEnclosing(points.ToArray());
}*/

/*private void ComposeOBB(ref List<Entity> entities)
{
    for (int i = 0; i < entities.Count; i++)
    {
        var children = Helper.GetChildrenWithMesh(entities[i]);
        var points = new List<Vector3>();

        for (int j = 0; j < children.Count; j++)
        {
            var ent = children[j];
            CalcucalteSingleOBB(ref ent);
            children[j] = ent;

            var center = children[j].singleObb.Center;

            var axisX = children[j].singleObb.Axis1;
            var axisY = children[j].singleObb.Axis2;
            var axisZ = children[j].singleObb.Axis3;
            var extends = children[j].singleObb.Extent;

            var A = center - extends.z * axisZ - extends.x * axisX - axisY * extends.y;
            var B = center - extends.z * axisZ + extends.x * axisX - axisY * extends.y;
            var C = center - extends.z * axisZ + extends.x * axisX + axisY * extends.y;
            var D = center - extends.z * axisZ - extends.x * axisX + axisY * extends.y;

            var E = center + extends.z * axisZ - extends.x * axisX - axisY * extends.y;
            var F = center + extends.z * axisZ + extends.x * axisX - axisY * extends.y;
            var G = center + extends.z * axisZ + extends.x * axisX + axisY * extends.y;
            var H = center + extends.z * axisZ - extends.x * axisX + axisY * extends.y;

            points.Add(A);
            points.Add(B);
            points.Add(C);
            points.Add(D);
            points.Add(E);
            points.Add(F);
            points.Add(G);
            points.Add(H);
        }

        var entity = entities[i];
        entity.composedObb = OrientedBoundingBox.OptimalEnclosing(points.ToArray());
        entities[i] = entity;
    }
}*/

/*private void CalcucalteSingleOBB(ref Entity entity)
{
    var min = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.min;
    var max = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.max;

    var localToWorldMatrix = entity.gameObject.transform.localToWorldMatrix;
    
    var a = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
    var b = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
    var c = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));
    var d = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));
    var e = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
    var f = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
    var g = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
    var h = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));

    var points = new[]
    {
        a, 
        b,
        c,
        d,
        e,
        f,
        g,
        h,
    };

    //print($"{a} {b} {c} {d} {e} {f} {g} {h}");

    entity.singleObb = OrientedBoundingBox.OptimalEnclosing(points);

    CreateBoundingCube(entity.singleObb);
}*/

/*private CubeCollider CalculateSingleCubeCollider(OrientedBoundingBox orientedBoundingBox)
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

    var axes = new[]
    {
        axisX, axisY, axisZ
    };

    var points = new[]
    {
        A, B, C, D, E, F, G, H
    };

    return new CubeCollider(axes, points, center);
}*/