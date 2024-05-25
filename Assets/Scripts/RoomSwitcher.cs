using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSwitcher : MonoBehaviour
{

    // range from 0 to 3
    [Range(0, 3)]
    public int currentRoomIndex = 0;

    public CameraFollower userCameraFollower;
    public GameObject targetDisplay;
    public Material [] targetMaterials;
    private int lastRoomIndex = 0;

    void Awake() 
    {
        if (transform.childCount != 4) 
            Debug.LogError("RoomSwitcher should have 4 children");

    }

    void Start()
    {
        TryFollow();
    }

    void TryFollow() {
        if (userCameraFollower != null)
            userCameraFollower.Follow();
    }
    
    void Update()
    {
        if (currentRoomIndex != lastRoomIndex) 
        {
            TryFollow();
            Debug.Log($"Switched from room {lastRoomIndex} to room {currentRoomIndex}");
            transform.GetChild(lastRoomIndex).gameObject.SetActive(false);
            transform.GetChild(currentRoomIndex).gameObject.SetActive(true);
            lastRoomIndex = currentRoomIndex;
            updateMaterial();
        }
    }

    void updateMaterial() {
        targetDisplay.GetComponent<Renderer>().material = targetMaterials[currentRoomIndex];
    }


    public void switchTo(int roomIndex) 
    {
        currentRoomIndex = roomIndex;
        lastRoomIndex = currentRoomIndex;
        Debug.Log($"Switch to room {roomIndex}");
        for (int i = 0; i < transform.childCount; i++) 
        {
            if (i == roomIndex) {
                transform.GetChild(i).gameObject.SetActive(true);
            }
            else 
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        updateMaterial();
        TryFollow();
        
    }

    public void switchToNextRoom() 
    {
        int nextRoomIndex = currentRoomIndex + 1;
        if (nextRoomIndex >= transform.childCount) {
            nextRoomIndex = 0;
        }
        switchTo(nextRoomIndex);
    }

    public void switchToPreviousRoom() 
    {
        int previousRoomIndex = currentRoomIndex - 1;
        if (previousRoomIndex < 0) {
            previousRoomIndex = transform.childCount - 1;
        }
        switchTo(previousRoomIndex);
    }

    public void Next() {
        if (currentRoomIndex == transform.childCount - 1) {
            Debug.Log("All rooms visited!");
        }
        else {
            switchTo(currentRoomIndex + 1);
        }
        
    }

    public Transform detectorTransform {
        get {
            return transform.GetChild(currentRoomIndex).GetChild(0);
        }
    }

}
