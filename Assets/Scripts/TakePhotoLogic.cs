using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

public class TakePhotoLogic : MonoBehaviour
{
    List<RenderTexture> photos = new List<RenderTexture>();
    public List<Material> photoMaterials = new List<Material>();
    public GameObject photoPrefab;
    public Material materialPrefab;

    public Transform album;
    public GridObjectCollection albumGrid;
    public ClippingBox clippingBox;
    public Renderer cameraRenderer;
    Camera m_camera;
    Material m_screenMaterial;
    ScrollingObjectCollection2 m_scoller;
    int curPhotoIndex;
    void Awake()
    {
        m_camera = GetComponent<Camera>();
        if (m_camera == null)
            Debug.LogError("Camera not found");

        m_screenMaterial = transform.GetChild(0).GetComponent<MeshRenderer>().material;
        if (m_screenMaterial == null)
            Debug.LogError("Screen material not found");


        if (photoPrefab == null)
            Debug.LogError("Photo prefab not found");

        // materialPrefab = photoPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().material;
        if (materialPrefab == null)
            Debug.LogError("Material prefab not found");

        m_scoller = album.parent.parent.GetComponent<ScrollingObjectCollection2>();
        if (m_scoller == null)
            Debug.LogError("Scroller not found");

        if (clippingBox == null)
            Debug.LogError("Clipping box not found");
    }

    void generateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(1080, 720, 24);
        rt.name = "Photo" + photos.Count;
        rt.Create();
        photos.Add(rt);
        curPhotoIndex = photos.Count - 1;
        Debug.Log("Current photo index: " + curPhotoIndex + " Photos count: " + photos.Count);
    }

    void Start() {
        generateRenderTexture();
        updateTexture();
    }


    void updateAlbum() {
        GameObject photo = Instantiate(photoPrefab, album);
        photo.name = "Photo" + curPhotoIndex;

        Material photoMaterial = Instantiate(materialPrefab);
        photoMaterial.SetTexture("_MainTex", photos[curPhotoIndex]);
        photoMaterials.Add(photoMaterial);

        MeshRenderer renderer = photo.transform.GetChild(0).GetComponent<MeshRenderer>();
        // renderer.material.SetTexture("_MainTex", photos[curPhotoIndex-1]);
        renderer.material = photoMaterial;
        // renderer.sharedMaterial.SetTexture("_MainTex", photos[curPhotoIndex-1]);

        // // clear the materials list in renderer
        // var materials = new Material[1];
        // materials[0] = photoMaterial;
        // renderer.materials = materials;
        // renderer.material = photoMaterial;

        // MaterialPropertyBlock block = new MaterialPropertyBlock();
        // block.SetTexture("_MainTex", photos[curPhotoIndex-1]);
        // renderer.SetPropertyBlock(block);

        renderer.enabled = false;
        renderer.enabled = true;

        albumGrid.UpdateCollection();

    }

    void updateTexture() {
        m_camera.targetTexture = photos[curPhotoIndex];
        m_screenMaterial.mainTexture = photos[curPhotoIndex];
        if (cameraRenderer != null)
            cameraRenderer.material.mainTexture = photos[curPhotoIndex];
    }

    public void takePhoto() {

        if (photos.Count == 0) {
            Debug.LogError("No render texture found");
        }
        
        bool old_state = m_scoller.enabled;
        m_scoller.enabled = false;

        updateAlbum();
        generateRenderTexture();
        updateTexture();

        // clippingBox.enabled = false;
        // clippingBox.enabled = true;

        m_scoller.enabled = old_state;
        
        // if (old_state) {
        //     m_scoller.UpdateContent();
        //     // m_scoller.UpdateContentBounds();
        //     // m_scoller.ManageVisibility1();
        //     // m_scoller.ManageVisibility2();
        // }

    }

}
