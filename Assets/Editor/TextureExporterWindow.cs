using UnityEngine;
using UnityEditor;

public class TextureExporterWindow : EditorWindow
{
    private GameObject selectedGameObject;

    [MenuItem("Window/Texture Exporter")]
    public static void ShowWindow()
    {
        GetWindow<TextureExporterWindow>("Texture Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Export Texture to PNG", EditorStyles.boldLabel);

        selectedGameObject = EditorGUILayout.ObjectField("GameObject", selectedGameObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Export Texture"))
        {
            if (selectedGameObject != null)
            {
                ExportTexture();
            }
            else
            {
                Debug.LogError("No GameObject selected.");
            }
        }
    }

    private void ExportTexture()
    {
        MeshRenderer meshRenderer = selectedGameObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null && meshRenderer.sharedMaterial.mainTexture != null)
        {
            Texture2D texture = meshRenderer.sharedMaterial.mainTexture as Texture2D;
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter != null)
            {
                textureImporter.isReadable = true;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                byte[] bytes = texture.EncodeToPNG();
                string savePath = System.IO.Path.Combine("Assets", texture.name + ".png");
                System.IO.File.WriteAllBytes(savePath, bytes);
                AssetDatabase.Refresh();

                Debug.Log("Texture saved to: " + savePath);
            }
            else
            {
                Debug.LogError("Failed to locate texture importer.");
            }
        }
        else
        {
            Debug.LogError("The selected GameObject does not have a MeshRenderer component with a valid texture.");
        }
    }
}
