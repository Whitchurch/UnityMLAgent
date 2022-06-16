using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flowers
/// </summary>

public class FlowerArea : MonoBehaviour
{
    //The diameter of the area where the Agent and Flowers can be in the world:
    // used for observing relative distance of agent from the flower.
    public const float AreaDiameter = 20f;

    //The list of all Flowerplants in this area (island)
    private List<GameObject> flowerPlants;

    //A lookup Dictionary for looking up a flower from a nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionay;

    /// <summary>
    /// The list of all Flowers in the FlowerArea
    /// </summary>
    public List<Flower> Flowers { get; private set; }

    /// <summary>
    /// Rotate the flower-plants and reset the flowers
    /// </summary>
    public void ResetFlowes()
    {
        
        //Rotate the Flower Plant
        foreach(GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);

            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        //Reset the Flowers.
        foreach(Flower flower in Flowers)
        {
            flower.ResetFlower();
        }


    }
    /// <summary>
    /// Gets the Flower associated with the corresponding NectarCollider.
    /// Here <Key: nectarCollider, Value: Flower>
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionay[collider];
    }

    /// <summary>
    /// Called when script instances is getting loaded: Unity specific function
    /// </summary>
    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionay = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    /// <summary>
    /// Called when the game starts
    /// </summary>
    private void Start()
    {
        //Find all flowers that are children of this GameObject/Transform.
        //So basically here, we pass the transform associated with this (FlowerArea).
        FindChildFlowers(transform);
    }

    private void FindChildFlowers(Transform parent)
    {
        for(int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if(child.CompareTag("flower_plant"))
            {
                //Found a flower plant add it to the list:
                flowerPlants.Add(child.gameObject);

                //Look for flowers within the flowerplant
                FindChildFlowers(child);
            }
            else
            {
                //Try to get flower from child vy default, 
                Flower flower = child.GetComponent<Flower>();

                if(flower != null) // meaning if it was truly a flower
                {
                    //Then add the flower to the list.
                    Flowers.Add(flower);

                    //Also update the dictionary with nectar Collider and the flower
                    nectarFlowerDictionay.Add(flower.nectarCollider, flower);
                }
                else
                {
                    //Go deeper into the object tree.
                    FindChildFlowers(child);
                }

            }
        }
    }
}
