using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class FluidVolume : MonoBehaviour
{
    // Private properties
    [SerializeField]
    private BoxCollider fluidTrigger = null;
    private readonly Dictionary<BuoyantBody, int> bodyContacts = new();
    
    [SerializeField, Min(0f)]
    private float density = 1000f;

    // Public properties
    public float Density => density;
    public float SurfaceY => fluidTrigger.center.y + fluidTrigger.size.y * 0.5f;
    
    public Vector3 LocalMin => fluidTrigger.center - fluidTrigger.size * 0.5f;
    public Vector3 LocalMax => fluidTrigger.center + fluidTrigger.size * 0.5f;
    
    /// <summary>
    /// depth > 0 -> submerged
    /// depth = 0 -> surface
    /// depth < 0 -> out of the mesh 
    /// </summary>
    public float GetDepth(Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        return SurfaceY - localPoint.y;
    }
    
    void OnValidate()
    {
        if(fluidTrigger != null) 
        {
            if (!fluidTrigger.isTrigger)
            {
                UnityEngine.Debug.LogError($"[{nameof(FluidVolume)}] FluidTrigger must be trigger");
            }
        }
        else
        {
            fluidTrigger = GetComponent<BoxCollider>();
        }
    }

    void OnTriggerEnter(Collider other)
    { 
        BuoyantBody buoyantBody = other.attachedRigidbody?.GetComponent<BuoyantBody>();
        if (buoyantBody != null)
        {
            if (bodyContacts.ContainsKey(buoyantBody))
            {
                bodyContacts[buoyantBody]++;
            }
            else
            {
                bodyContacts.Add(buoyantBody, 1);
                BuoyancySystem.Instance.RegisterBuoyantBody(buoyantBody, this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        BuoyantBody buoyantBody = other.attachedRigidbody?.GetComponent<BuoyantBody>();
        if (buoyantBody != null)
        {
            if (bodyContacts.ContainsKey(buoyantBody))
            {
                bodyContacts[buoyantBody]--;
                if (bodyContacts[buoyantBody] <= 0)
                {
                    bodyContacts.Remove(buoyantBody);
                    BuoyancySystem.Instance.UnregisterBuoyantBody(buoyantBody, this);
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"[{nameof(FluidVolume)}] Unexpected behaviour: expected buoyant body.");
            }
        }
    }

    void Start()
    {
        Assert.IsTrue(fluidTrigger != null, $"[{nameof(FluidVolume)}] FluidTrigger should be assigned");
    }
}
