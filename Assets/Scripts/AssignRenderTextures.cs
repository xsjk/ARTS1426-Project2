using UnityEngine;

public class AssignRenderTextures : MonoBehaviour
{

    public string folderName = "CameraTextures";
    public void AssignTextures()
    {
        var textures = Resources.LoadAll(folderName, typeof(RenderTexture));
        for (int i = 0; i < textures.Length; i++)
        {
            if (i < transform.childCount)
            {
                var childRenderer = transform.GetChild(i).GetChild(0).GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    childRenderer.sharedMaterial.mainTexture = (RenderTexture)textures[i];
                }
            }
        }
    }
}
