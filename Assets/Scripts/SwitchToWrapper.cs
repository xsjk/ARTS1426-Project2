using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchToWrapper : MonoBehaviour
{

    public RoomSwitcher roomSwitcher;

    void Awake() 
    {
        if (roomSwitcher == null)
            Debug.LogError("RoomSwitcher not found");
    }
    
    public void switchTo(int i) 
    {
        roomSwitcher.switchTo(i);
    }

    public void disableFollower() {
        roomSwitcher.userCameraFollower = null;
    } 
    
}
