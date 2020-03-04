using System.IO;
using NodeBreaker.Components;
using UnityEngine;

namespace NodeBreaker.Usage
{
    public class NodeBreakerUsage : MonoBehaviour
    {
        [SerializeField] private GameObject m_Geometry;
        [SerializeField] private GameObject m_RootNode;
        [SerializeField] private NodeBreakerComponent m_NodeBreakerComponent;
        private void OnGUI()
        {
            GUIStyle customButton = new GUIStyle("Button");
            customButton.fontSize = 32;
            customButton.fontStyle = FontStyle.Bold;
            customButton.normal.textColor = Color.blue;

            if (GUI.Button(new Rect(0, 0, 300, 150), "Initialize", customButton))
            {
                var time = Time.realtimeSinceStartup;
                var name = m_RootNode.name;
                var path = Path.Combine(Application.streamingAssetsPath, $"{name}/config.json");
                m_NodeBreakerComponent.Initialize(in m_Geometry, ref m_RootNode ,in path);
                print(message: (Time.realtimeSinceStartup - time).ToString());
            }

            if (GUI.Button(new Rect(0, 151, 300, 150), "Disassemble", customButton))
            {
                m_NodeBreakerComponent.Disassemble();
            }

            if (GUI.Button(new Rect(0, 302, 300, 150), "Assemble", customButton))
            {
                m_NodeBreakerComponent.Assemble();
            }
        }
    }
}
