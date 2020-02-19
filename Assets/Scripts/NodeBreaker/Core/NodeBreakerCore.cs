using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using g3;
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
                CallCustomMethod();
            }

            if (GUI.Button(new Rect(0, 151, 300, 150), "Disassemble", customButton))
            {
                Disassemble();
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

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                CalcucalteSingleOBB(ref e);
                list[i] = e;
            }
        }

        private void Initialize()
        {
            if (!m_Initialized)
            {
                var entitiesMatchingName =
                    Helper.GetEntitiesMatchingName(Path.Combine(Application.streamingAssetsPath, "config.json"),
                        "atlas", "table", "colName", m_Geometry);

                ComposeOBB(ref entitiesMatchingName);

                m_Chunks = GroupByVolumeSet(m_Geometry);
                m_ChunksBases = CalculateChunkBases(m_Chunks);

                CalculateCenterOfMass(ref m_ChunksBases, m_Chunks);

                var movableChunks = GetMovableChunks(entitiesMatchingName);

                for (int i = 0; i < movableChunks.Count; i++)
                {
                    for (int j = 0; j < movableChunks[i].Count; j++)
                    {
                        var entity = movableChunks[i][j];
                        //CalculateCenterOfMass(ref entity);
                        CalculateDirectionAndDistance(ref entity, m_ChunksBases[i].centerOfMass);
                        movableChunks[i][j] = entity;
                    }

                    var entities = movableChunks[i];
                    SortMovableEntitiesInOrder(ref entities);
                    CalculateDesiredPosition(ref entities, m_ChunksBases[i]);
                    movableChunks[i] = entities;
                }

                StopAllCoroutines();
                var list = m_Chunks;
                StartCoroutine(DrawLines(list));

                m_Initialized = true;
            }
        }

        private List<List<Entity>> GetMovableChunks(List<Entity> entitiesMatchingName)
        {
            var movableChunks = new List<List<Entity>>();

            for (int i = 0; i < m_Chunks.Count; i++)
            {
                for (int j = 0; j < m_Chunks[i].Count; j++)
                {
                    for (int k = 0; k < entitiesMatchingName.Count; k++)
                    {
                        if (m_Chunks[i][j].gameObject == entitiesMatchingName[k].gameObject)
                        {
                            movableChunks[i].Add(entitiesMatchingName[k]);
                        }
                    }
                }
            }

            return movableChunks;
        }

        private List<List<Entity>> GroupByVolumeSet(GameObject baseGeometryNode)
        {
            var chunks = new List<List<Entity>>();

            var entities = Helper.GetChildrenWithMesh(baseGeometryNode);

            var groupNumber = -1;

            for (int i = 0; i < entities.Count; i++)
            {
                var e1 = entities[i];

                if (i == 0)
                {
                    CalcucalteSingleOBB(ref e1);
                    e1.cubeCollider = CalculateSingleCubeCollider(e1.singleObb);
                    entities[i] = e1;
                }

                for (int j = i + 1; j < entities.Count; j++)
                {
                    var e2 = entities[j];
                    CalcucalteSingleOBB(ref e2);
                    e2.cubeCollider = CalculateSingleCubeCollider(e2.singleObb);
                    entities[j] = e2;

                    var tolerance = 1f; //in meters

                    if (IsOverlapOrNear(e1, e2, tolerance))
                    {
                        e1.otherIds.Add(j);
                        entities[i] = e1;
                    }
                }

                if (!entities[i].hasGroup)
                {
                    groupNumber += 1;
                    var ent = entities[i];
                    ent.chunkGroup = groupNumber;
                    ent.hasGroup = true;
                }

                if (!chunks[entities[i].chunkGroup].Contains(entities[i]))
                {
                    chunks[entities[i].chunkGroup].Add(entities[i]);
                }

                if (entities[i].otherIds != null)
                {
                    for (int j = 0; j < entities[i].otherIds.Count; j++)
                    {
                        chunks[entities[i].chunkGroup].Add(entities[j]);
                        var ent = entities[j];
                        ent.chunkGroup = entities[i].chunkGroup;
                        ent.hasGroup = true;
                        entities[j] = ent;
                    }
                }
            }

            return chunks;
        }

        private List<Entity> CalculateChunkBases(List<List<Entity>> chunks)
        {
            var chunksBases = new List<Entity>();

            for (int i = 0; i < chunks.Count; i++)
            {
                var obb = ComposeOBB(chunks[i]);

                var cB = new Entity {composedObb = obb};
                chunksBases.Add(cB);
            }

            return chunksBases;
        }


        private ContOrientedBox3 ComposeOBB(List<Entity> entities)
        {
            List<Vector3d> points = new List<Vector3d>();

            for (int i = 0; i < entities.Count; i++)
            {
                var center = entities[i].singleObb.Box.Center;

                var axisX = entities[i].singleObb.Box.AxisX;
                var axisY = entities[i].singleObb.Box.AxisY;
                var axisZ = entities[i].singleObb.Box.AxisZ;
                var extends = entities[i].singleObb.Box.Extent;

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

            return new ContOrientedBox3(points);
        }

        private void ComposeOBB(ref List<Entity> entities)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var children = Helper.GetChildrenWithMesh(entities[i]);
                var points = new List<Vector3d>();

                for (int j = 0; j < children.Count; j++)
                {
                    var center = entities[i].singleObb.Box.Center;

                    var axisX = entities[i].singleObb.Box.AxisX;
                    var axisY = entities[i].singleObb.Box.AxisY;
                    var axisZ = entities[i].singleObb.Box.AxisZ;
                    var extends = entities[i].singleObb.Box.Extent;

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
                entity.composedObb = new ContOrientedBox3(points);
                entities[i] = entity;
            }
        }

        private void CalcucalteSingleOBB(ref Entity entity)
        {
            if (entity.gameObject.GetComponent<MeshFilter>() == null)
                return;

            Vector3d[] points = new Vector3d[entity.gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.vertices[i];
            }

            entity.singleObb = new ContOrientedBox3(points);

            CreateBoundingCube(entity.singleObb);
        }

        private CubeCollider CalculateSingleCubeCollider(ContOrientedBox3 orientedBoundingBox)
        {
            var center = (Vector3) orientedBoundingBox.Box.Center;

            var axisX = (Vector3) orientedBoundingBox.Box.AxisX;
            var axisY = (Vector3) orientedBoundingBox.Box.AxisY;
            var axisZ = (Vector3) orientedBoundingBox.Box.AxisZ;
            var extends = (Vector3) orientedBoundingBox.Box.Extent;
            
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
        }

        private void CreateBoundingCube(ContOrientedBox3 orientedBoundingBox)
        {
            var center = (Vector3) orientedBoundingBox.Box.Center;

            var axisX = (Vector3) orientedBoundingBox.Box.AxisX;
            var axisY = (Vector3) orientedBoundingBox.Box.AxisY;
            var axisZ = (Vector3) orientedBoundingBox.Box.AxisZ;
            var extends = (Vector3) orientedBoundingBox.Box.Extent;
            
            var A = center - extends.z * axisZ - extends.x * axisX - axisY * extends.y;
            var B = center - extends.z * axisZ + extends.x * axisX - axisY * extends.y;
            var C = center - extends.z * axisZ + extends.x * axisX + axisY * extends.y;
            var D = center - extends.z * axisZ - extends.x * axisX + axisY * extends.y;

            var E = center + extends.z * axisZ - extends.x * axisX - axisY * extends.y;
            var F = center + extends.z * axisZ + extends.x * axisX - axisY * extends.y;
            var G = center + extends.z * axisZ + extends.x * axisX + axisY * extends.y;
            var H = center + extends.z * axisZ - extends.x * axisX + axisY * extends.y;
            
            Debug.DrawLine(A, B);
            Debug.DrawLine(B, C);
            Debug.DrawLine(C, D);
            Debug.DrawLine(D, A);

            Debug.DrawLine(E, F);
            Debug.DrawLine(F, G);
            Debug.DrawLine(G, H);
            Debug.DrawLine(H, E);

            Debug.DrawLine(A, E);
            Debug.DrawLine(B, F);
            Debug.DrawLine(D, H);
            Debug.DrawLine(C, G);
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

        private void CalculateCenterOfMass(ref List<Entity> chunksBases, List<List<Entity>> chunks)
        {
            for (int k = 0; k < chunksBases.Count; k++)
            {
                var massList = new List<float>();

                var posList = new List<Vector3>();

                var entities = Helper.GetChildrenWithMesh(chunksBases[k]);

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
                .Project((Vector3) entity.composedObb.Box.Center - relativePoint, entity.baseRightVector).magnitude;

            var distanceY = Vector3
                .Project((Vector3) entity.composedObb.Box.Center - relativePoint, entity.baseUpVector).magnitude;

            var distanceZ = Vector3
                .Project((Vector3) entity.composedObb.Box.Center - relativePoint, entity.baseForwardVector).magnitude;

            if (distanceX > distanceY && distanceX > distanceZ)
            {
                if (Mathf.Sign(Vector3.Dot((Vector3) entity.composedObb.Box.Center - relativePoint,
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
                        Vector3.Dot((Vector3) entity.composedObb.Box.Center - relativePoint, entity.baseUpVector)) >= 0)
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
                if (Mathf.Sign(Vector3.Dot((Vector3) entity.composedObb.Box.Center - relativePoint,
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

        private void CalculateDesiredPosition(ref List<Entity> entities, Entity baseEntity)
        {
            throw new NotImplementedException();
        }
        
        private bool IsOverlapOrNear(Entity one, Entity two, float tolerance)
        {
            /*if ((one.cubeCollider.center - two.cubeCollider.center).magnitude < tolerance)
            {
                return true;
            }
            else
            {*/
                return CollisionDetector.IsOverlap(one.cubeCollider, two.cubeCollider);
            //}
        }

        public void Disassemble()
        {
            Initialize();
            /*StopAllCoroutines();
            StartCoroutine(MoveEntities(chunks));*/
        }

        public void Assemble()
        {
            /*StopAllCoroutines();
            StartCoroutine(BackEntitiesPosition(chunks));*/
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
                    yield return new WaitForSeconds(0.3f);

                    Debug.DrawLine((Vector3) entity.composedObb.Box.Center,
                        (Vector3) entity.composedObb.Box.Center + entity.desiredDirection * (entity.distanceFromBaseCenter + 5),
                        Color.yellow,
                        20);
                }
            }
        }

        private Vector3 GetLocalToWorldPosition(Vector3 vertex, Transform owner)
        {
            return owner.localToWorldMatrix.MultiplyPoint3x4(vertex);
        }
    }
}