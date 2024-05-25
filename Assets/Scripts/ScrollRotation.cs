using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollRotation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        // change pitch angle by scroll 
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0)
            return;
        // Debug.Log(scroll);
        float pitch = transform.localRotation.eulerAngles.x;
        pitch += scroll * 10;
        pitch = Mathf.Clamp(pitch, -90, 90);
        transform.localRotation = Quaternion.Euler(pitch, 0, 0);

    }
}
