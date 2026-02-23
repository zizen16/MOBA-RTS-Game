using UnityEngine;
using System.Collections;
using DG.Tweening; // Animation PlugIN
public class ShaderTest : MonoBehaviour
{
    // 1. Create slots so you can DRAG your materials here in the Inspector
    public Material builderMaterial; 
    public Material bubbleMaterial;

    void Start()
    {
         MeshRenderer renderer = GetComponent<MeshRenderer>();

        // Apply material
        renderer.material = builderMaterial;

        // Make sure starting value is 0.5 pero di ning start ug zero piste
        builderMaterial.SetFloat("_Slider", 0.5f);

        // Animate from 0 → 1 over 5 seconds
        builderMaterial.DOFloat(1f, "_Slider", 5f)
                       .SetEase(Ease.Linear);
    }

    private IEnumerator SetSliderAfterDelay(MeshRenderer renderer, int slotIndex, float value, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Access the specific material in the array
        Material[] currentMats = renderer.materials;
        
        // Use the REFERENCE NAME from Shader Graph (e.g., "_Slider")
        currentMats[slotIndex].SetFloat("_Slider", value);

        // Put the array back so Unity updates the visuals
        renderer.materials = currentMats;
        
        Debug.Log("Changed Slider on " + currentMats[slotIndex].name);
    }
}