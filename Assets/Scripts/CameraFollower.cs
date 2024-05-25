using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{

    public RoomSwitcher roomSwitcher;
    public Transform m_pitch_transform;

    Camera m_camera;

    void Awake()
    {
        if (roomSwitcher == null)
            Debug.LogError("RoomSwitcher not found");
        m_camera = GetComponent<Camera>();
        if (m_camera == null)
            Debug.LogError("Camera not found");
        if (m_pitch_transform == null)
            m_pitch_transform = transform.parent.parent;
    }

    void OnEnable()
    {
        Follow();
    }

    public void Follow() {
        // follow the pitch of the detected camera
        var detector = roomSwitcher.detectorTransform;
        var eulerAngle = m_pitch_transform.eulerAngles;
        eulerAngle.x = detector.eulerAngles.x;
        m_pitch_transform.eulerAngles = eulerAngle;

        // follow the fov
        var detectorCam = detector.GetComponent<Camera>();
        m_camera.fieldOfView = detectorCam.fieldOfView;
    }
    
}
