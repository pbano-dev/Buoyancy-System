using UnityEngine;
using Unity.Collections;

/// <summary>
/// 
/// </summary>
public class FloatabilitySystem : MonoBehaviour
{

    private NativeList<IBuoyantBody> _bodies;

    internal void RegisterBuoyantBody(IBuoyantBody buoyantBody)
    {
        _bodies.Add(buoyantBody);
    }

    internal void UnregisterBuoyantBody(IBuoyantBody buoyantBody)
    {
        
    }
    
}
