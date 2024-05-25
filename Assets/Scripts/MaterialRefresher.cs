using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialRefresher : MonoBehaviour
{
    // Start is called before the first frame update

    public TakePhotoLogic takePhotoLogic;
    
    public void RefreshMaterial() {
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i+1);
            var renderer = child.GetChild(0).GetComponent<Renderer>();
            renderer.material = takePhotoLogic.photoMaterials[i];
        }
    }

    void OnEnable() {
        RefreshMaterial();
    }

}
