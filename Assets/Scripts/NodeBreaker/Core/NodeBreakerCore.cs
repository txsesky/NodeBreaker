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
        [SerializeField] private GameObject m_Parent;

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
                print(message: Time.realtimeSinceStartup - time);
            }

            if (GUI.Button(new Rect(0, 302, 300, 150), "Assemble", customButton))
            {
                Assemble();
            }
        }

        public void CallCustomMethod()
        {
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                m_Chunks = GetGroupedChunks(m_Geometry);
                m_ChunksBases = GetChunksBases(m_Chunks);

                /*for (int i = 0; i < m_ChunksBases.Count; i++)
                {
                    Helper.CreateBoundingCube(m_ChunksBases[i].composedCubeCollider, Color.red);
                }*/

                CalculateCenterOfMass(ref m_ChunksBases, m_Chunks);

                var entitiesMatchingName = new List<Entity>();
                Helper.GetEntitiesMatchingName(ref entitiesMatchingName,
                        Path.Combine(Application.streamingAssetsPath, m_Parent.name + "/config.json"),
                        "atlas", "table", "colName", m_Geometry);

                var movableChunks = new List<List<Entity>>();
                GetMovableChunks(ref entitiesMatchingName, ref movableChunks);

                for (int i = 0; i < movableChunks.Count; i++)
                {
                    for (int j = 0; j < movableChunks[i].Count; j++)
                    {
                        var entity = movableChunks[i][j];
                        var children = new List<Entity>();
                        Helper.GetChildrenWithMesh(in entity, ref children);

                        entity.composedCubeCollider = GetComposedCubeCollider(in children);

                        CalculateDirectionAndDistance(ref entity, m_ChunksBases[i].centerOfMass);

                        movableChunks[i][j] = entity;
                    }

                    var entities = movableChunks[i];
                    //SortMovableEntitiesInOrder(ref entities);
                    var baseE = m_ChunksBases[i];
                    CalculateBaseMinMax(in m_Geometry, ref baseE);
                    CalculateDesiredPosition(ref entities, in baseE);
                    m_ChunksBases[i] = baseE;
                    movableChunks[i] = entities;
                }

                m_Chunks.Clear();
                m_Chunks = movableChunks;

                /*StopAllCoroutines();
                StartCoroutine(DrawLines(movableChunks));*/

                m_Initialized = true;
            }
        }

        private void GetMovableChunks(ref List<Entity> entitiesMatchingName, ref List<List<Entity>> movableChunks)
        {
            for (int i = 0; i < m_Chunks.Count; i++)
            {
                movableChunks.Add(new List<Entity>());
            }

            for (int i = 0; i < entitiesMatchingName.Count; i++)
            {
                var listOfGameObjectsWithChunkId = FindChunkId(entitiesMatchingName[i].gameObject);

                if (listOfGameObjectsWithChunkId.Count <= 0)
                {
                    continue;
                }

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
                    var entity = entitiesMatchingName[i];
                    entity.chunkGroup = movableChunks.Count;
                    entitiesMatchingName[i] = entity;
                    movableChunks.Add(new List<Entity>());
                    movableChunks[entitiesMatchingName[i].chunkGroup].Add(entitiesMatchingName[i]);

                    var relativeBases = new List<Entity>();

                    for (int j = 0; j < chunkRelationList.Count; j++)
                    {
                        relativeBases.Add(m_ChunksBases[chunkRelationList[j]]);
                    }

                    var composedCubeCollider = GetComposedCubeColliderFromCollider(relativeBases);
                    var chunkBase = new Entity
                    {
                        composedCubeCollider = composedCubeCollider, centerOfMass = composedCubeCollider.center
                    };
                    m_ChunksBases.Add(chunkBase);
                }
            }

            /*print($"Movable chunks count {movableChunks.Count.ToString()}");

            for (int i = 0; i < movableChunks.Count; i++)
            {
                print($"Movable chunk {i} has {movableChunks[i].Count.ToString()} elements");
            }*/
            
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

            for (int i = 0; i < o.transform.childCount; i++)
            {
                var child = o.transform.GetChild(i);
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
                    }
                }

                if (entities[i].otherIds.Count > 0)
                {
                    for (int j = 0; j < entities[i].otherIds.Count; j++)
                    {
                        if (entities[entities[i].otherIds[j]].hasGroup)
                        {
                            var ent = entities[i];
                            ent.chunkGroup = entities[entities[i].otherIds[j]].chunkGroup;
                            ent.hasGroup = true;
                            entities[i] = ent;
                        }
                    }
                }

                if (!entities[i].hasGroup)
                {
                    groupNumber += 1;
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
                var composedCubeCollider = GetComposedCubeCollider(chunks[i]);

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

            var A = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
            var B = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
            var C = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
            var D = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));
            var E = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
            var F = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
            var G = localToWorldMatrix.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));
            var H = localToWorldMatrix.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));

            var axes = new[]
            {
                axisX, axisY, axisZ
            };

            var points = new[]
            {
                A, B, C, D, E, F, G, H
            };

            return new CubeCollider(axes, points, center, Vector3.zero);
        }

        public static CubeCollider TransformBounds(Transform _transform, Bounds _localBounds)
        {
            var center = _transform.TransformPoint(_localBounds.center);

            // transform the local extents' axes
            var extents = _localBounds.extents;

            var axisX = _transform.TransformVector(extents.x, 0, 0);
            var axisY = _transform.TransformVector(0, extents.y, 0);
            var axisZ = _transform.TransformVector(0, 0, extents.z);

            var axes = new[]
            {
                axisX, axisY, axisZ
            };

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            var min = center - extents;
            var max = center + extents;

            var A = new Vector3(min.x, min.y, min.z);
            var B = new Vector3(max.x, min.y, min.z);
            var C = new Vector3(max.x, max.y, min.z);
            var D = new Vector3(min.x, max.y, min.z);
            var E = new Vector3(min.x, min.y, max.z);
            var F = new Vector3(max.x, min.y, max.z);
            var G = new Vector3(max.x, max.y, max.z);
            var H = new Vector3(min.x, max.y, max.z);

            var points = new[]
            {
                A, B, C, D, E, F, G, H
            };
            
            return new CubeCollider(axes, points, center, extents);
        }

        private CubeCollider GetComposedCubeCollider(in List<Entity> entities)
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
                    orientedBoundingBox = OrientedBoundingBox.BruteEnclosing(points.ToArray());
                }

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

                return new CubeCollider(axes, composedPoints, obbCenter, Vector3.zero);
            }

            return GetSingleCubeCollider(entities[0]);

           /* TransformBounds(entities[0].gameObject.transform,
                entities[0].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds);

            var center = 

            var size = 

            var bounds = new Bounds(center, size);

            for (int i = 1; i < entities.Count; i++)
            {
                var centerI = entities[i].gameObject.transform
                    .TransformPoint(entities[i].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center);

                var sizeI = Vector3.Scale(entities[i].gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center,
                    entities[i].gameObject.transform.localToWorldMatrix.lossyScale);

                var boundsI = new Bounds(center, size);
                
                bounds.Encapsulate(boundsI);
            }

            center = bounds.center;
            var extents = bounds.extents;
            
            var min = center - extents;
            var max = center + extents;

            var A = new Vector3(min.x, min.y, min.z);
            var B = new Vector3(max.x, min.y, min.z);
            var C = new Vector3(max.x, max.y, min.z);
            var D = new Vector3(min.x, max.y, min.z);
            var E = new Vector3(min.x, min.y, max.z);
            var F = new Vector3(max.x, min.y, max.z);
            var G = new Vector3(max.x, max.y, max.z);
            var H = new Vector3(min.x, max.y, max.z);

            var points = new[]
            {
                A, B, C, D, E, F, G, H
            };

            return new CubeCollider(,points,);*/
        }

        private CubeCollider GetComposedCubeColliderFromCollider(List<Entity> entities)
        {
            var points = new List<Vector3>();

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];

                points.AddRange(entity.composedCubeCollider.vertices);
            }

            var orientedBoundingBox = OrientedBoundingBox.OptimalEnclosing(points.ToArray());

            if (float.IsInfinity(orientedBoundingBox.Extent.x) || float.IsInfinity(orientedBoundingBox.Extent.y) ||
                float.IsInfinity(orientedBoundingBox.Extent.z) || float.IsInfinity(orientedBoundingBox.Center.x) ||
                float.IsInfinity(orientedBoundingBox.Center.y) || float.IsInfinity(orientedBoundingBox.Center.z))
            {
                orientedBoundingBox = OrientedBoundingBox.BruteEnclosing(points.ToArray());
            }

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

            return new CubeCollider(axes, composedPoints, obbCenter, Vector3.zero);
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
            else
            {
                entity.desiredDirection = entity.baseForwardVector;
                entity.desiredDirectionAxis = "z";
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

        private void CalculateBaseMinMax(in GameObject geometry, ref Entity baseEntity)
        {
            var baseMinX = float.MaxValue;
            var baseMaxX = float.MinValue;
            var baseMinY = float.MaxValue;
            var baseMaxY = float.MinValue;
            var baseMinZ = float.MaxValue;
            var baseMaxZ = float.MinValue;

            for (int j = 0; j < baseEntity.composedCubeCollider.vertices.Length; j++)
            {
                var right = geometry.transform.right;
                var forward = geometry.transform.forward;
                var up = geometry.transform.up;

                var valX = Mathf.Sign(Vector3.Dot(baseEntity.composedCubeCollider.vertices[j], right.normalized)) *
                           Vector3.Project(
                               baseEntity.composedCubeCollider.vertices[j],
                               right.normalized).magnitude;

                if (valX < baseMinX)
                {
                    baseMinX = valX;
                }

                if (valX > baseMaxX)
                {
                    baseMaxX = valX;
                }

                var valY = Mathf.Sign(Vector3.Dot(baseEntity.composedCubeCollider.vertices[j], up.normalized)) * Vector3
                               .Project(
                                   baseEntity.composedCubeCollider.vertices[j],
                                   up.normalized).magnitude;

                if (valY < baseMinY)
                {
                    baseMinY = valY;
                }

                if (valY > baseMaxY)
                {
                    baseMaxY = valY;
                }

                var valZ = Mathf.Sign(Vector3.Dot(baseEntity.composedCubeCollider.vertices[j], forward.normalized)) *
                           Vector3.Project(
                               baseEntity.composedCubeCollider.vertices[j],
                               forward.normalized).magnitude;

                // print($"{baseEntity.composedCubeCollider.center + valZ * forward.normalized}");

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
            baseEntity = e;
        }


        private void CalculateDesiredPosition(ref List<Entity> entities, in Entity baseEntity)
        {
            float iterSizeX = 0f;
            float iterSizeY = 0f;
            float iterSizeZ = 0f;
            float iterSizeNegX = 0f;
            float iterSizeNegY = 0f;
            float iterSizeNegZ = 0f;

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];

                var dir = entity.desiredDirection;

                var min = float.MaxValue;
                var max = float.MinValue;

                for (int j = 0; j < entity.composedCubeCollider.vertices.Length; j++)
                {
                    var val = Mathf.Sign(Vector3.Dot(entity.composedCubeCollider.vertices[j], dir)) *
                              Vector3.Project(entity.composedCubeCollider.vertices[j], dir).magnitude;

                    if (val < min)
                    {
                        min = val;
                    }

                    if (val > max)
                    {
                        max = val;
                    }
                }

                var transformPosition = entity.gameObject.transform.position;
                var magnitude = Mathf.Sign(Vector3.Dot(transformPosition, dir)) *
                                Vector3.Project(transformPosition, dir).magnitude;

                var additionalSpace = 0.2f;

                var cenToSelfEdge = magnitude - min + additionalSpace / 2;
                var size = max - min + additionalSpace;

                var cenToBaseEdge = 0f;
                var compDist = 0f;

                if (entity.desiredDirectionAxis == "x")
                {
                    cenToBaseEdge = baseEntity.baseMax.x - magnitude;
                    compDist = cenToSelfEdge + cenToBaseEdge + iterSizeX;
                    iterSizeX += size;
                }
                else if (entity.desiredDirectionAxis == "-x")
                {
                    cenToBaseEdge = -baseEntity.baseMin.x - magnitude;
                    compDist = cenToSelfEdge + cenToBaseEdge + iterSizeNegX;
                    iterSizeNegX += size;
                }
                else if (entity.desiredDirectionAxis == "y")
                {
                    cenToBaseEdge = baseEntity.baseMax.y - magnitude;
                    compDist = cenToSelfEdge + cenToBaseEdge + iterSizeY;
                    iterSizeY += size;
                }
                else if (entity.desiredDirectionAxis == "-y")
                {
                    cenToBaseEdge = -baseEntity.baseMin.y - magnitude;
                    compDist = cenToSelfEdge + cenToBaseEdge + iterSizeNegY;
                    iterSizeNegY += size;
                }
                else if (entity.desiredDirectionAxis == "z")
                {
                    cenToBaseEdge = baseEntity.baseMax.z - magnitude;
                    compDist = cenToSelfEdge + cenToBaseEdge + iterSizeZ;
                    iterSizeZ += size;
                }
                else if (entity.desiredDirectionAxis == "-z")
                {
                    cenToBaseEdge = -baseEntity.baseMin.z - magnitude;
                    compDist = cenToSelfEdge + cenToBaseEdge + iterSizeNegZ;
                    iterSizeNegZ += size;
                }

                entity.desiredPosition = entity.gameObject.transform.position + compDist * dir;
                entities[i] = entity;
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
                StartCoroutine(routine: MoveEntities(chunk));
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
                movingTime += Time.deltaTime * 0.5f;
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    var entity = entities[i];
                    entity.gameObject.transform.position = Vector3.Lerp(entity.gameObject.transform.position,
                        entity.desiredPosition, movingTime);
                    entities[i] = entity;
                }

                yield return null;
            }
        }

        private IEnumerator BackEntitiesPosition(List<Entity> movableEntities)
        {
            var movingTime = 0f;
            while (movingTime < 1)
            {
                movingTime += Time.deltaTime * 0.5f;
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

                    Debug.DrawLine(entity.composedCubeCollider.center,
                        entity.composedCubeCollider.center +
                        entity.desiredDirection * (entity.distanceFromBaseCenter * 3 + 1),
                        Color.yellow,
                        Mathf.Infinity);
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