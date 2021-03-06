using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HeadAimLimit : MonoBehaviour
{
    public float maxDistanceFromBack = 6;
    public Transform player;
    public Transform lookAtTarget;
    public Transform backOfPlayer;
    public Rig rigObject;

    private void Update()
    {
        float distanceToBack = Vector3.Distance(lookAtTarget.position, backOfPlayer.position);

        if(lookAtTarget.position.z < player.position.z)
        {
            rigObject.weight = Remap(distanceToBack, 0, maxDistanceFromBack, 0, 1);
        }
        else
        {
            rigObject.weight = 1;
        }
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
