using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
// cambiar transform.position a position sphere
// caso controlador desaparece
public class DragController : MonoBehaviour
{
    public SteamVR_Action_Boolean m_GrabAction = null;
    public SteamVR_Action_Single squeezeAction;

    public bool isLeft = true;

    private SteamVR_Behaviour_Pose m_Pose = null;
    private FixedJoint m_Joint = null;

    private List<DragHandle> listHandles = new List<DragHandle>();
    private int numHandles = 0;

    public float DistMin;

    private int closestHandle = -1;
    private int attachedHandle = -1;

    private void Awake()
    {
        m_Pose = GetComponent<SteamVR_Behaviour_Pose>();
        m_Joint = GetComponent<FixedJoint>();
        DistMin = 0.05f;
    }

    void Update()
    {
        if (listHandles.Count != Object.FindObjectsByType(typeof(DragHandle), FindObjectsSortMode.None).Length) {
            var objects = Object.FindObjectsByType(typeof(DragHandle), FindObjectsSortMode.None);
            numHandles = objects.Length;
            listHandles.Clear();
            for (int i = 0; i < numHandles; ++i) {
                listHandles.Add((DragHandle) objects[i]);
            }
        }
       
        int newClosestHandle = -1;
        Vector3 ballPos = this.gameObject.transform.GetChild(1).transform.position;
        float curDist = DistMin;
        for (int i = 0; i < numHandles; ++i) {
            float dist = (ballPos - listHandles[i].transform.position).sqrMagnitude;
            //Debug.Log("La distancia al handle" + i + " es de " + dist);
            //Debug.Log("La distancia minima es de " + DistMin);
            if (dist < curDist) {
                curDist = dist;
                newClosestHandle = i;
            }
        }
        //Debug.Log("El handle mas cercano es de " + newClosestHandle);

        if (closestHandle != newClosestHandle) {
            Debug.Log("Handle detectado");
            if (newClosestHandle != -1) {
                listHandles[newClosestHandle].transform.localScale *= 1.5f;
            }

            if (closestHandle != -1) {
                listHandles[closestHandle].transform.localScale /= 1.5f;
            }
        }
        closestHandle = newClosestHandle;

        if (m_GrabAction.GetStateDown(m_Pose.inputSource))
        {
            attachedHandle = closestHandle;
            if (attachedHandle != -1) {
                listHandles[newClosestHandle].HandSelected(ballPos);
            }
        }

        if (attachedHandle != -1) {
            Drag();
        }

        if (m_GrabAction.GetStateUp(m_Pose.inputSource))
        {
            Drop();
        }
    }

    void Drag() {
        Vector3 ballPos = this.gameObject.transform.GetChild(1).transform.position;
        if (attachedHandle != -1) {
            listHandles[attachedHandle].HandDrag(ballPos);
        }
    }

    void Drop() {
        if (attachedHandle != -1) {
            listHandles[attachedHandle].HandDrop();
        }
        attachedHandle = -1;
    }
}