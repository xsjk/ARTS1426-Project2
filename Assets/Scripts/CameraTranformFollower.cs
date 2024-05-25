using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTranformFollower : MonoBehaviour
{

    public Transform horizontal_control;
    public Transform pitch_control;

    float yaw_offset;
    float roll;
    Vector3 translation_offset;

    void Awake()
    {
        if (horizontal_control == null || pitch_control == null)
            Debug.LogError("CameraTranformFollower: horizontal_control or pitch_control is null");

        Debug.Log($"horizontal_control: {horizontal_control.gameObject.name}");
        Debug.Log($"pitch_control: {pitch_control.gameObject.name}");

        yaw_offset = horizontal_control.localRotation.eulerAngles.y - transform.localRotation.eulerAngles.y;
        translation_offset = horizontal_control.localPosition - transform.localPosition;
        roll = transform.localRotation.eulerAngles.z;
    }

    void Update()
    {
        var eulerAngles = transform.localRotation.eulerAngles;
        float yaw = eulerAngles.y;
        horizontal_control.localRotation = Quaternion.Euler(0, yaw + yaw_offset, 0);

        float pitch = eulerAngles.x;
        pitch_control.localRotation = Quaternion.Euler(pitch, 180, 0);

        horizontal_control.localPosition = transform.localPosition + translation_offset;
        transform.localRotation = Quaternion.Euler(pitch, yaw, roll);

    }
}
