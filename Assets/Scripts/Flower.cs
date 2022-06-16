using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single flower with nectar.
/// </summary>

public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full of nectar")]
    public Color fullFlowerColor = new Color(1.0f,0.0f,0.3f);

    [Tooltip("The color when the flower is empty of nectar")]
    public Color emptyFlowerColor = new Color(0.5f, 0.0f, 1.0f);

    /// <summary>
    /// The trigger collider representing the nectar
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;

    //The solid collider representing the flower petals
    private Collider flowerCollider;

    //The flower's material
    private Material flowerMaterial;

    ///<summary>
    /// A vector pointing straight out of the flower
    ///</summary>
    
    public Vector3 flowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    ///<summary>
    /// The center position of the flower's nectarCollider
    ///</summary>

    public Vector3 flowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }

    ///<summary>
    /// The amount of Nectar remining in the flower
    ///</summary>
    ///
    public float NectarAmount { get; private set; }

    ///<summary>
    /// Check if the flower has Nectar
    ///</summary>
    ///

    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }

    /// <summary>
    /// Returns the amount of nectar successfully removed
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public float Feed(float amount)
    {
        //Restrict the amount of nectar to between 0 and NectarAmount
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        //Subtract the nectar amount
        NectarAmount -= amount;

        if(NectarAmount <= 0)
        {
            //Set the Nectar Amount to zero:
            NectarAmount = 0;

            //Disable the flower and nectar collider
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            //Change the flower color to indicate that it has been empty
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);

        }

        //Return the amount of nectar that was taken
        return nectarTaken;
    }

    /// <summary>
    /// This resets the state of the flower to it's initial state.
    /// </summary>
    public void ResetFlower()
    {
        //Change the flower color to indicate that it is full
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);

        //Enable the flower and nectar collider
        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        //Set the nectar Amount back
        NectarAmount = 1f;


    }

    /// <summary>
    /// Called to initialize the flower
    /// </summary>
    private void Awake()
    {
        //Get the Mesh material for the flower
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        //Get the nectarCollider and Flower collider associated with the flower.
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();

    }



}
