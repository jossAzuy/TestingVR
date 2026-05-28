using UnityEngine;
using MoreMountains.Feedbacks;

public class EnemyDieEffects : MonoBehaviour
{
    public float floatValue = 0f;

    Material material;

    void Start()
    {
        // SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        material = meshRenderer.material;
    }

    void Update()
    {
        material.SetFloat("_FullGlowDissolveFade", floatValue);
    }

   /*  public void SetFloatValue()
    {
        material.SetFloat("_FullGlowDissolveFade", floatValue);
    } */


}