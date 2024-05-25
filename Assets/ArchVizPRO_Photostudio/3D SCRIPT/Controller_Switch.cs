using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Controller_Switch : MonoBehaviour
{
    public GameObject FPS_Controller;
    public GameObject ORBIT_Controller;
    public GameObject CINEMATIC_Controller;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp("f"))
        {
            FPS();
        }
        if (Input.GetKeyUp("o"))
        {
            ORBIT();
        }
        if (Input.GetKeyUp("t"))
        {
            CINEMATIC();
        }
        if (Input.GetKeyUp("escape"))
        {
            Application.Quit();
        }
    }

    void FPS()
    {
        FPS_Controller.SetActive(true);
        ORBIT_Controller.SetActive(false);
        CINEMATIC_Controller.SetActive(false);
    }
    void ORBIT()
    {
        FPS_Controller.SetActive(false);
        ORBIT_Controller.SetActive(true);
        CINEMATIC_Controller.SetActive(false);
    }
    void CINEMATIC()
    {
        FPS_Controller.SetActive(false);
        ORBIT_Controller.SetActive(false);
        CINEMATIC_Controller.SetActive(true);
    }


}

