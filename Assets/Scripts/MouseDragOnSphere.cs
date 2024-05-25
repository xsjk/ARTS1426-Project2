using UnityEngine;

public class MouseDragOnSphere : MonoBehaviour
{
    public float radius = 5f; // 球的半径
    public Vector3 sphereCenter = Vector3.zero; // 球心的位置

    private Vector3 lastMousePos;
    private bool isDragging = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckObjectUnderMouse();
        }

        if (isDragging)
        {
            DragObject();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void CheckObjectUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                // 当鼠标点击到这个物体时，记录鼠标位置并开始拖动
                lastMousePos = Input.mousePosition;
                isDragging = true;
            }
        }
    }

    void DragObject()
    {
        Vector3 delta = Input.mousePosition - lastMousePos;
        lastMousePos = Input.mousePosition;
        MoveOnSphere(delta);
    }

    void MoveOnSphere(Vector3 delta)
    {
        Vector3 axis = Vector3.Cross(delta, Camera.main.transform.forward).normalized;
        float angle = delta.magnitude / radius;
        transform.position = Quaternion.AngleAxis(angle, axis) * (transform.position - sphereCenter) + sphereCenter;
    }
}
