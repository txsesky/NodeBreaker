using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public struct Entity
{
    public GameObject gameObject { get; set; }
    public Bounds composedBounds { get; set; }
    public Vector3 worldInitialPosition { get; set; }
    public Quaternion worldInitialRotation { get; set; }
    public Vector3 relativeForwardVector { get; set; }
    public Vector3 relativeRightVector { get; set; }
    public Vector3 relativeUpVector { get; set; }
    public Vector3 centerOfMass { get; set; }
    public float distanceFromBaseCenter { get; set; }
    public Vector3 desiredDirection { get; set; }
    public Vector3 desiredPosition { get; set; }
    public string desiredDirectionAxis { get; set; }
}


public class NodeBreakerCore : MonoBehaviour
{
    [SerializeField] private GameObject m_Geometry;

    [SerializeField] private GameObject m_GeometryCheck;

    [SerializeField] private Material m_Material;

    //private List<Entity> m_MovableEntities;
    private Entity m_GeometryEntity;

    private bool m_Initialized;

    private void Initialize()
    {
        //initialize all variables
        m_Initialized = false;
        m_GeometryEntity = new Entity();
        m_GeometryEntity.gameObject = m_Geometry;
    }

    public void CallCustomMethod()
    {
        var enta = new Entity {gameObject = m_GeometryCheck};
        var list = GetChildrenWithMesh(enta);

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            CalculateSingleBounds(ref e);
            list[i] = e;
            //Gizmos.matrix = list[i].gameObject.transform.localToWorldMatrix;
            //Gizmos.DrawLine(list[i].composedBounds.center, Vector3.zero);
            //Gizmos.color = Color.cyan;
            //Gizmos.DrawWireCube(list[i].composedBounds.center, list[i].composedBounds.size);
            //Debug.Log("c " + list[i].composedBounds.max, list[i].gameObject);
        }
    }

    private void gizma()
    {
        /*var enta = new Entity {gameObject = m_GeometryCheck};
        var list = GetChildrenWithMesh(enta);

        Quaternion currentRotation = m_GeometryCheck.transform.rotation;
        m_GeometryCheck.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        Vector3 size = m_GeometryCheck.transform.localScale;
        m_GeometryCheck.transform.localScale = Vector3.one;

        Bounds bounds = new Bounds(m_GeometryCheck.transform.position, Vector3.zero);
        
        for (int i = 0; i < list.Count; i++)
        {
            bounds.Encapsulate(list[i].gameObject.GetComponent<Renderer>().bounds);
        }
        
        //Debug.Log(list[list.Count - 4].gameObject.name, list[list.Count - 4].gameObject);

        Vector3 localCenter = bounds.center - m_GeometryCheck.transform.position;
        bounds.center = localCenter;

        m_GeometryCheck.transform.rotation = currentRotation;
        m_GeometryCheck.transform.localScale = size;*/

        /*Gizmos.color = Color.magenta;
        Gizmos.matrix = m_GeometryCheck.gameObject.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(bounds.center, bounds.size);*/

        /*foreach (var ent in list)
        {
            Gizmos.matrix = ent.gameObject.transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(ent.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center,
                ent.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size);
        }*/

        var enta = new Entity {gameObject = m_GeometryCheck};
        var list = GetChildrenWithMesh(enta);

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            CalculateSingleBounds(ref e);
            list[i] = e;
            //Gizmos.matrix = list[i].gameObject.transform.localToWorldMatrix;
            //Gizmos.DrawLine(list[i].composedBounds.center, Vector3.zero);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(list[i].composedBounds.center, list[i].composedBounds.size);
            //Debug.Log("c " + list[i].composedBounds.max, list[i].gameObject);
        }
    }

    /// <summary>
    /// Parse json from path and get names with arguments
    /// </summary>
    /// <param name="path"></param>
    /// <param name="subPath0"></param>
    /// <param name="subPath1"></param>
    /// <param name="subPath2"></param>
    /// <returns></returns>
    private List<string> GetNamesFromJson(string path, string subPath0, string subPath1, string subPath2)
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

    /// <summary>
    /// Add to list entities that match names
    /// </summary>
    /// <param name="names"></param>
    /// <param name="geometry"></param>
    /// <returns></returns>
    private List<Entity> GetEntitiesMatchingName(List<string> names, Entity geometry)
    {
        var entities = new List<Entity>();
        var children = new List<GameObject>();

        Traverse(ref children, geometry.gameObject.transform);

        foreach (var nodeName in names)
        {
            var go = children.FirstOrDefault(item => item.name == nodeName);
            if (go != null)
            {
                var entity = new Entity
                {
                    gameObject = go,
                    worldInitialPosition = go.transform.position,
                    worldInitialRotation = go.transform.rotation,
                    relativeForwardVector = geometry.gameObject.transform.forward.normalized,
                    relativeRightVector = geometry.gameObject.transform.right.normalized,
                    relativeUpVector = geometry.gameObject.transform.up.normalized
                };
                entities.Add(entity);
            }
        }

        return entities;
    }

    private void Traverse(ref List<GameObject> gameObjects, Transform tr)
    {
        if (tr == null)
            return;

        foreach (Transform child in tr)
        {
            gameObjects.Add(child.gameObject);
            Traverse(ref gameObjects, child);
        }
    }

    /// <summary>
    /// Add to list node entity if it has mesh component and search through hierarchy and add children to list if they have mesh component
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private List<Entity> GetChildrenWithMesh(Entity entity)
    {
        var entities = new List<Entity>();

        if (entity.gameObject == null)
            return null;

        if (String.Compare(entity.gameObject.name, "HUD", StringComparison.OrdinalIgnoreCase) == 0)
        {
            return null;
        }

        foreach (Transform child in entity.gameObject.transform)
        {
            if (child == null)
                continue;

            var childNode = new Entity() {gameObject = child.gameObject};
            if (child.gameObject.GetComponent<MeshFilter>() != null)
                entities.Add(childNode);

            var list = GetChildrenWithMesh(childNode);
            if (list != null)
                entities.AddRange(list);
        }

        return entities;
    }


    /// <summary>
    /// Go through all entites with mesh components and collect bounds data
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private void CalculateAverageBounds(ref Entity entity)
    {
        var listOfMeshesInNode = GetChildrenWithMesh(entity);

        var currentRotation = entity.gameObject.transform.rotation;
        entity.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        var size = entity.gameObject.transform.localScale;
        entity.gameObject.transform.localScale = Vector3.one;

        var bounds = new Bounds(entity.gameObject.transform.position, Vector3.zero);

        foreach (var e in listOfMeshesInNode)
        {
            bounds.Encapsulate(e.gameObject.GetComponent<Renderer>().bounds);
        }

        var localCenter = bounds.center - entity.gameObject.transform.position;
        bounds.center = localCenter;

        entity.gameObject.transform.rotation = currentRotation;
        entity.gameObject.transform.localScale = size;

        entity.composedBounds = bounds;
    }

    private void CalculateSingleBounds(ref Entity entity)
    {
        //var bounds = entity.gameObject.GetComponent<Renderer>().bounds;
        var cent = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.center;
        var size = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        entity.composedBounds = CreateBoundingCube(entity);
    }


    Bounds CreateBoundingCube(Entity entity)
    {
        //Bounds bBox = CalculateBoundingBox(entity.gameObject);
        Bounds bBox = entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.GetComponent<MeshRenderer>().sharedMaterial = m_Material;
        cube.transform.position = entity.gameObject.transform.localToWorldMatrix.MultiplyPoint3x4(bBox.center);
        cube.transform.localScale = Vector3.Scale(entity.gameObject.transform.localToWorldMatrix.lossyScale, bBox.size);
        cube.transform.rotation = entity.gameObject.transform.rotation;
        return bBox;
    }

    private void OnCollisionEnter(Collision other)
    {
        throw new NotImplementedException();
    }

    public static Bounds CalculateBoundingBox(GameObject aObj)
    {
        if (aObj == null)
        {
            Debug.LogError("CalculateBoundingBox: object is null");
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Transform myTransform = aObj.transform;
        Mesh mesh = null;
        MeshFilter mF = aObj.GetComponent<MeshFilter>();
        if (mF != null)
            mesh = mF.mesh;
        else
        {
            SkinnedMeshRenderer sMR = aObj.GetComponent<SkinnedMeshRenderer>();
            if (sMR != null)
                mesh = sMR.sharedMesh;
        }

        if (mesh == null)
        {
            Debug.LogError("CalculateBoundingBox: no mesh found on the given object");
            return new Bounds(aObj.transform.position, Vector3.one);
        }

        Vector3[] vertices = mesh.vertices;
        if (vertices.Length <= 0)
        {
            Debug.LogError("CalculateBoundingBox: mesh doesn't have vertices");
            return new Bounds(aObj.transform.position, Vector3.one);
        }

        Vector3 min, max;
        min = max = myTransform.TransformPoint(vertices[0]);
        for (int i = 1; i < vertices.Length; i++)
        {
            Vector3 V = myTransform.TransformPoint(vertices[i]);
            for (int n = 0; n < 3; n++)
            {
                if (V[n] > max[n])
                    max[n] = V[n];
                if (V[n] < min[n])
                    min[n] = V[n];
            }
        }

        Bounds B = new Bounds();
        B.SetMinMax(min, max);
        return B;
    }

    private void CalculateCenterOfMass(ref Entity entity)
    {
        var boundsList = new List<Bounds>();

        var massList = new List<float>();

        var posList = new List<Vector3>();

        if (entity.gameObject.GetComponent<MeshFilter>() != null)
        {
            boundsList.Add(entity.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds);
        }

        var entities = GetChildrenWithMesh(entity);

        foreach (var e in entities)
        {
            boundsList.Add(e.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds);
        }


        for (int i = 0; i < boundsList.Count; i++)
        {
            var a = boundsList[i].size.x;
            var b = boundsList[i].size.y;
            var c = boundsList[i].size.z;

            var density = 1;

            var m = a * b * c * density;

            massList.Add(m);

            if (i == 0)
            {
                posList.Add(GetLocalToWorldPosition(boundsList[i].center,
                    entity.gameObject.GetComponent<MeshFilter>() != null
                        ? entity.gameObject.transform
                        : entities[i].gameObject.transform));
            }
            else
            {
                posList.Add(GetLocalToWorldPosition(boundsList[i].center,
                    entity.gameObject.GetComponent<MeshFilter>() != null
                        ? entities[i - 1].gameObject.transform
                        : entities[i].gameObject.transform));
            }
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

        entity.centerOfMass = new Vector3(xResult, yResult, zResult);
    }

    private void CalculateDirectionAndDistance(ref Entity entity, Vector3 relativePoint) // TODO LastStep
    {
        var distanceX = Vector3.Project(entity.centerOfMass - relativePoint, entity.relativeRightVector).magnitude;

        var distanceY = Vector3.Project(entity.centerOfMass - relativePoint, entity.relativeUpVector).magnitude;

        var distanceZ = Vector3.Project(entity.centerOfMass - relativePoint, entity.relativeForwardVector).magnitude;

        if (distanceX > distanceY && distanceX > distanceZ)
        {
            if (Mathf.Sign(Vector3.Dot(entity.centerOfMass - relativePoint, entity.relativeRightVector)) >= 0)
            {
                entity.desiredDirection = entity.relativeRightVector;
                entity.desiredDirectionAxis = "x";
            }
            else
            {
                entity.desiredDirection = -entity.relativeRightVector;
                entity.desiredDirectionAxis = "-x";
            }

            entity.distanceFromBaseCenter = distanceX;
        }
        else if (distanceY > distanceX && distanceY > distanceZ)
        {
            if (Mathf.Sign(Vector3.Dot(entity.centerOfMass - relativePoint, entity.relativeUpVector)) >= 0)
            {
                entity.desiredDirection = entity.relativeUpVector;
                entity.desiredDirectionAxis = "y";
            }
            else
            {
                entity.desiredDirection = -entity.relativeUpVector;
                entity.desiredDirectionAxis = "-y";
            }

            entity.distanceFromBaseCenter = distanceY;
        }
        else if (distanceZ > distanceX && distanceZ > distanceY)
        {
            if (Mathf.Sign(Vector3.Dot(entity.centerOfMass - relativePoint, entity.relativeForwardVector)) >= 0)
            {
                entity.desiredDirection = entity.relativeForwardVector;
                entity.desiredDirectionAxis = "z";
            }
            else
            {
                entity.desiredDirection = -entity.relativeForwardVector;
                entity.desiredDirectionAxis = "-z";
            }

            entity.distanceFromBaseCenter = distanceZ;
        }
    }

    private void SortMovableEntitiesInOrder(ref List<Entity> entities) //sort
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

        print(
            $"xPos = {xPosList.Count} xNeg = {xNegList.Count} yPos = {yPosList.Count} yNeg = {yNegList.Count} zPos = {zPosList.Count} zNeg = {zNegList.Count}");

        QuickSortAlgorithm(xPosList, 0, xPosList.Count - 1);
        QuickSortAlgorithm(yPosList, 0, yPosList.Count - 1);
        QuickSortAlgorithm(zPosList, 0, zPosList.Count - 1);
        QuickSortAlgorithm(xNegList, 0, xNegList.Count - 1);
        QuickSortAlgorithm(yNegList, 0, yNegList.Count - 1);
        QuickSortAlgorithm(zNegList, 0, zNegList.Count - 1);

        entities.Clear();

        entities.AddRange(xPosList);
        entities.AddRange(xNegList);
        entities.AddRange(yPosList);
        entities.AddRange(yNegList);
        entities.AddRange(zPosList);
        entities.AddRange(zNegList);
    }

    private void QuickSortAlgorithm(List<Entity> arr, int left, int right)
    {
        if (left >= right)
        {
            return;
        }

        var pivot = Sorting(arr, left, right);
        QuickSortAlgorithm(arr, left, pivot - 1);
        QuickSortAlgorithm(arr, pivot + 1, right);
    }

    private int Sorting(List<Entity> arr, int left, int right)
    {
        var pointer = left;

        for (int i = left; i <= right; i++)
        {
            if (arr[i].distanceFromBaseCenter < arr[right].distanceFromBaseCenter)
            {
                SwapElements(arr, pointer, i);
                pointer++;
            }
        }

        SwapElements(arr, pointer, right);
        return pointer;
    }

    private void SwapElements(List<Entity> arr, int i, int j)
    {
        var a = arr[i];
        arr[i] = arr[j];
        arr[j] = a;
    }

    private void CalculateDesiredPosition(ref List<Entity> entities, Entity baseEntity)
    {
        var previousPositionPos =
            GetLocalToWorldPosition(baseEntity.composedBounds.max, baseEntity.gameObject.transform);
        var previousPositionNeg =
            GetLocalToWorldPosition(baseEntity.composedBounds.min, baseEntity.gameObject.transform);
        var metaPrevPos = previousPositionPos;
        var metaPrevNeg = previousPositionNeg;

        for (int i = 0; i < entities.Count; i++)
        {
            if (Mathf.Abs(entities[i].desiredDirection.x) > 0)
            {
                var ent = entities[i];
                ent.desiredPosition = ent.worldInitialPosition;
                var v = ent.desiredPosition;
                if (Mathf.Sign(entities[i].desiredDirection.x) >= 0)
                {
                    v.x = previousPositionPos.x + entities[i].composedBounds.extents.x;
                    metaPrevPos.x = v.x + ent.composedBounds.extents.x;
                }
                else
                {
                    v.x = previousPositionNeg.x - entities[i].composedBounds.extents.x;
                    metaPrevNeg.x = v.x - ent.composedBounds.extents.x;
                }

                ent.desiredPosition = v;
                entities[i] = ent;
            }
            else if (Mathf.Abs(entities[i].desiredDirection.y) > 0)
            {
                var ent = entities[i];
                ent.desiredPosition = ent.worldInitialPosition;
                var v = ent.desiredPosition;
                if (Mathf.Sign(entities[i].desiredDirection.y) >= 0)
                {
                    v.y = previousPositionPos.y + entities[i].composedBounds.extents.y;
                    metaPrevPos.y = v.y + ent.composedBounds.extents.y;
                }
                else
                {
                    v.y = previousPositionNeg.y - entities[i].composedBounds.extents.y;
                    metaPrevNeg.y = v.y - ent.composedBounds.extents.y;
                }

                ent.desiredPosition = v;
                entities[i] = ent;
            }
            else if (Mathf.Abs(entities[i].desiredDirection.z) > 0)
            {
                var ent = entities[i];
                ent.desiredPosition = ent.worldInitialPosition;
                var v = ent.desiredPosition;
                if (Mathf.Sign(entities[i].desiredDirection.z) >= 0)
                {
                    v.z = previousPositionPos.z + entities[i].composedBounds.extents.z;
                    metaPrevPos.z = v.z + ent.composedBounds.extents.z;
                }
                else
                {
                    v.z = previousPositionNeg.z - entities[i].composedBounds.extents.z;
                    metaPrevNeg.z = v.z - ent.composedBounds.extents.z;
                }

                ent.desiredPosition = v;
                entities[i] = ent;
            }

            previousPositionPos = metaPrevPos;
            previousPositionNeg = metaPrevNeg;
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

    private List<List<Entity>> SplitByVolumeSet(List<Entity> entities, Entity baseGeometryNode)
    {
        var chunks = new List<List<Entity>>();

        var list = GetChildrenWithMesh(baseGeometryNode);

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                var e1 = entities[i];
                CalculateSingleBounds(ref e1);
                entities[i] = e1;

                var e2 = entities[j];
                CalculateSingleBounds(ref e2);
                entities[j] = e2;

                var threshold = 1f; //in meters

                if (ContainsIntersectsNear(entities[i].composedBounds, entities[j].composedBounds, threshold))
                {
                }
                else
                {
                }
            }
        }

        return chunks;
    }

    private bool ContainsIntersectsNear(Bounds one, Bounds two, float threshold)
    {
        var min1 = one.min;
        var max1 = one.max;

        var min2 = two.min;
        var max2 = two.max;

        if ((one.center - two.center).magnitude < threshold)
        {
            return true;
        }
        else
        {
            return (min1.x <= max2.x && max1.x >= min2.x) &&
                   (min1.y <= max2.y && max1.y >= min2.y) &&
                   (min1.z <= max2.z && max1.z >= min2.z);
        }
    }

    List<List<Entity>> chunks = new List<List<Entity>>();
    List<Entity> chunksBases = new List<Entity>();

    public void Disassemble()
    {
        if (!m_Initialized)
        {
            Initialize();
            var entitiesMatchingName =
                GetEntitiesMatchingName(GetNamesFromJson(Path.Combine(Application.streamingAssetsPath, "config.json"),
                    "atlas", "table", "colName"), m_GeometryEntity);

            //TODO calculate bounds in volume set and split on chunks and base chunks
            chunks = SplitByVolumeSet(entitiesMatchingName, m_GeometryEntity);

            //TODO calculate for each chunk base
            for (int i = 0; i < chunksBases.Count; i++)
            {
                var entity = chunksBases[i];
                CalculateCenterOfMass(
                    ref entity); //TODO another method for centerOfmass of point that is represents virtual chunks base entities
                chunksBases[i] = entity;
            }


            for (int i = 0;
                i < chunks.Count;
                i++) //TODO calculate for each chunk entities and throw relative base chunk center of mass
            {
                for (int j = 0; j < chunks[i].Count; j++)
                {
                    var entity = chunks[i][j];
                    // CalculateAverageBounds(ref entity);
                    CalculateCenterOfMass(ref entity);
                    CalculateDirectionAndDistance(ref entity, chunksBases[i].centerOfMass);
                    chunks[i][j] = entity;
                }

                var entities = chunks[i];
                SortMovableEntitiesInOrder(ref entities); //TODO sort each chunk
                CalculateDesiredPosition(ref entities, m_GeometryEntity); //TODO for each chunk
                chunks[i] = entities;
            }

            StopAllCoroutines();
            var list = chunks;
            StartCoroutine(DrawLines(list));

            m_Initialized = true;
        }

        /*StopAllCoroutines();
        StartCoroutine(MoveEntities(chunks));*/
    }

    private IEnumerator DrawLines(List<List<Entity>> chunks)
    {
        foreach (var chunk in chunks)
        {
            var entities = chunk;
            foreach (var entity in entities)
            {
                yield return new WaitForSeconds(0.3f);

                Debug.DrawLine(entity.centerOfMass,
                    entity.centerOfMass + entity.desiredDirection * (entity.distanceFromBaseCenter + 5),
                    Color.yellow,
                    20);
            }
        }
    }

    public void Assemble()
    {
        /*StopAllCoroutines();
        StartCoroutine(BackEntitiesPosition(chunks));*/
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
                    entity.worldInitialPosition, movingTime);
            }

            yield return null;
        }
    }

    private Vector3 GetLocalToWorldPosition(Vector3 vertex, Transform owner)
    {
        return owner.localToWorldMatrix.MultiplyPoint3x4(vertex);
    }
}