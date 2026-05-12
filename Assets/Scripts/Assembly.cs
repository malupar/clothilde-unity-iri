using System.Collections.Generic;
using UnityEngine;

public class Assembly : MonoBehaviour
{
    [Header("Visual parts")]
    public Transform gripperBase;
    public Transform leftJaw;
    public Transform rightJaw;


    [Header("Jaw motion")]
    public float openGap = 0.0f;
    // public float closedGap = - 0.001f * (30 - 6);
    public float closedGap = 0.04f * (30 - 6); 
    // 0.04 is the scale of the gripper assembly
    // 30 mm - 6 mm (extension length) is the gap between the grippers in my CAD model
    public float jawSpeed = 1.5f;

    private bool isOpen = true;
    private float currentGap;
    private float targetGap;

    void Start()
    {
        // leftJaw.localPosition = new Vector3(0, 1, 0);
        // leftJaw.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        // rightJaw.localPosition = new Vector3(0, 1, 0);
        // rightJaw.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        // gripperBase.localPosition = new Vector3(0, 1, 0);
        // gripperBase.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        // graspBoxVisual.localPosition = Vector3.zero;
        // graspBoxVisual.localScale = graspBoxSize;
        
        currentGap = openGap;
        targetGap = openGap;
        ApplyJawGap(currentGap);
    }

    void Update()
    {
        currentGap = Mathf.MoveTowards(
            currentGap,
            targetGap,
            jawSpeed * Time.deltaTime
        );

         ApplyJawGap(currentGap);
    }

    // if setOpen(true) is called, then targetGap = openGap and jaws open
    // if setOpen(false) is called, then targetGap = closedGap
    public void SetOpen(bool open)
    {
        isOpen = open;
        targetGap = isOpen ? openGap: closedGap; // fancy way to write an if-else statement
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    void ApplyJawGap(float gap)
    {
        if (leftJaw != null)
        {
            Vector3 p = leftJaw.localPosition;
            p.x = -gap * 0.5f;
            leftJaw.localPosition = p;
        }

        if (rightJaw != null)
        {
            Vector3 p = rightJaw.localPosition;
            p.x = gap * 0.5f;
            rightJaw.localPosition = p;
        }
    }

}

