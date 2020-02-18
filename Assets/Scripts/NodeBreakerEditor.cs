using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeBreakerCore))]
public class NodeBreakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NodeBreakerCore nodeBreakerCore = (NodeBreakerCore) target;
        
        if (GUILayout.Button("DISASSEMBLE"))
        {
            nodeBreakerCore.Disassemble();
        }
        
        if (GUILayout.Button("ASSEMBLE"))
        {
            nodeBreakerCore.Assemble();
        }
        if (GUILayout.Button("Call"))
        {
            nodeBreakerCore.CallCustomMethod();
        }
    }
}
