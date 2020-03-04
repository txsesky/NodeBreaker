using System.Collections;
using System.Collections.Generic;
using System.IO;
using NodeBreaker.Core;
using NodeBreaker.Data;
using UnityEngine;

namespace NodeBreaker.Components
{
    public class NodeBreakerComponent : MonoBehaviour
    {
        private List<List<Entity>> m_Chunks = new List<List<Entity>>();
        private GameObject m_RootNode;

        public void Initialize(in GameObject geometryNode, ref GameObject rootNode, in string jsonPath)
        {
            NodeBreakerCore.Initialize(in geometryNode, ref rootNode, in jsonPath, ref m_Chunks);
            m_RootNode = rootNode;
        }

        public void Disassemble()
        {
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
                    entity.gameObject.transform.rotation = Quaternion.Slerp(entity.gameObject.transform.rotation,entity.initialRotation, movingTime);
                    entities[i] = entity;
                }

                m_RootNode.transform.position = Vector3.Lerp(m_RootNode.transform.position,
                    m_RootNode.GetComponent<NodeDataComponent>().position, movingTime);
                m_RootNode.transform.rotation = Quaternion.Slerp(m_RootNode.transform.rotation,
                    m_RootNode.GetComponent<NodeDataComponent>().rotation, movingTime);
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
                    entity.gameObject.transform.rotation = Quaternion.Slerp(entity.gameObject.transform.rotation,entity.initialRotation, movingTime);
                }
                m_RootNode.transform.position = Vector3.Lerp(m_RootNode.transform.position,
                    m_RootNode.GetComponent<NodeDataComponent>().position, movingTime);
                m_RootNode.transform.rotation = Quaternion.Slerp(m_RootNode.transform.rotation,
                    m_RootNode.GetComponent<NodeDataComponent>().rotation, movingTime);

                yield return null;
            }
        }
    }
}