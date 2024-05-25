using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AssignRenderTextures))]
public class AssignRenderTexturesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AssignRenderTextures script = (AssignRenderTextures)target;
        if (GUILayout.Button("Assign Textures"))
        {
            script.AssignTextures();
        }
    }
}

