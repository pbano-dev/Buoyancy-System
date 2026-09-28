using UnityEngine;

[CreateAssetMenu(
    fileName = "BuoyancyMaterial",
    menuName = "Physics/Buoyancy Material"
)]

public class BuoyancyMaterial : ScriptableObject
{
    [Header("Physical Properties")]
    [Tooltip("Multiplier applied to hydrodynamic drag.")]
    [Min(0.0f)]
    [SerializeField]
    private float dragMultiplier = 1f;
    
    [Tooltip("Multiplier applied to rotational resistance while submerged.")]
    [Min(0.0f)]
    private float angularDragMultiplier = 1f;

    // 
    public float DragMultiplier => dragMultiplier;
    public float AngularDragMultiplier => angularDragMultiplier;

}
