using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomSwitcher))]
public class RoomSwitcherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomSwitcher script = (RoomSwitcher)target;
        if (GUILayout.Button("Set Visible Room"))
        {
            script.switchTo(script.currentRoomIndex);
        }
    }
}

