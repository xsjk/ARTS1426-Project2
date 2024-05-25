using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTranformFollower : MonoBehaviour
{

    public Transform horizontal_control;

    float yaw_offset;
    float pitch_offset;
    float roll;
    Vector3 translation_offset;

    void Awake()
    {
        yaw_offset = horizontal_control.localRotation.eulerAngles.y - transform.localRotation.eulerAngles.y;
        translation_offset = horizontal_control.localPosition - transform.localPosition;
        roll = transform.localRotation.eulerAngles.z;
    }

    void Update()
    {
        var eularAngles = transform.localRotation.eulerAngles;
        float yaw = eularAngles.y;
        float pitch = eularAngles.x;

        horizontal_control.localRotation = Quaternion.Euler(0, yaw + yaw_offset, 0);
        horizontal_control.localPosition = transform.localPosition + translation_offset;
        transform.localRotation = Quaternion.Euler(pitch, yaw, roll);

    }
}
