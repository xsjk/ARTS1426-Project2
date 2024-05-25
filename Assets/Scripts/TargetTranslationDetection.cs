using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTranslationDetection : MonoBehaviour
{
    public Transform target;
    public Texture targetTexture;
    private RoomSwitcher roomSwitcher;
    public Animator anmi;
    public float distanceThreshold = 0.1f;
    public float rotationThreshold = 10f;

    void Awake() {
        if (target == null)
            Debug.LogError("Target not found");

        if (anmi == null)
            Debug.LogError("Animator not found");

        roomSwitcher = transform.parent.parent.GetComponent<RoomSwitcher>();
        if (roomSwitcher == null)
            Debug.LogError("RoomSwitcher not found");
    }
    
    void Update()
    {
        if (getDistanceError() < distanceThreshold && getAngleError() < rotationThreshold) {
            Debug.Log("Target is in position");
            enabled = false;
            StartCoroutine(DetectedBehaviour());
        }
    }

    IEnumerator DetectedBehaviour() {
        // delay for 1 second
        Debug.Log("Waiting for 1 second");
        target.gameObject.GetComponent<TakePhotoLogic>().takePhoto();
        yield return new WaitForSeconds(1);
        // switch to next scene
        int lastRoomID = anmi.GetInteger("roomID");
        anmi.SetInteger("roomID", lastRoomID + 1);
        gameObject.SetActive(false);
    }

    float getDistanceError() {
        return Vector3.Distance(transform.position, target.position);
    }

    float getAngleError() {
        return Quaternion.Angle(transform.rotation, target.rotation);
    }
}
