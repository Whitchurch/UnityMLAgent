using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class HummingbirdAgent : Agent 
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to turn around y axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agents Camera")]
    public Camera agentCamera;

    [Tooltip("Whether Training mode or Game Mode")]
    public bool trainingMode;

    //The rigidbody of the agent
    new private Rigidbody rigidbody;

    //The flower are that the agent is in:
    private FlowerArea flowerArea;

    //The nearest flower to the agent
    private Flower nearestFlower;

    //Allows for smoother pitch changes
    private float smoothPitchChange = 0f;

    //Allows for smoother yaw change
    private float smoothYawChange = 0f;

    //Maximum angle that the bird can pitch up or down
    private const float MaxPitchAngle = 80f;

    //Maximum distance from the beak tip to accept nectar collision
    private const float BeakTipRadius = 0.008f;

    //Whether the agent is frozen
    private bool frozen = false;

    /// <summary>
    /// The amount of nectar the agent has obtained this episode
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    /// Initialize the Agent
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        if (!trainingMode) MaxStep = 0;

    }

    /// <summary>
    /// Reset an agent when a training Episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if(trainingMode)
        {
            //Reset the flowers in the area
            flowerArea.ResetFlowes();
        }

        //Reset NectarObtained
        NectarObtained = 0f;

        //Zero out velocities so that movement stops before a new episode begins:
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        //Default to spawning in front of a flower
        bool inFrontOfFlower = true;
        if(trainingMode)
        {
            //Spawn in front of flower 50% of the time during training
            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        //Move the agent to a new random position
        MoveToSageRandomPosition(inFrontOfFlower);

        //Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }

    /// <summary>
    /// Calledn when an action is received from either the player or the neural network.
    /// Index 0 : move vector x (+1 = right, -1 = left)
    /// Index 1 : move vector y (+1 = up, -1 = down)
    /// Index 2 : move vector x (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        //Don't take any action
        if (frozen) return;

        //Calculate movement vector
        Vector3 move = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);

        //Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);

        //Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        //Calculate the pitch and yaw rotations
        float pitchChange = actions.ContinuousActions[3];
        float yawChange = actions.ContinuousActions[4];

        //Calculate smooth rotation changes
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        //Calculate new pitch and yaw based on smoothed values
        //Clamp pith to avoid flipping upside down
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        //Apply the new rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        //Convert keyboard inputs to movement and turning:
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        if (Input.GetKey(KeyCode.J)) up = transform.up;
        else if (Input.GetKey(KeyCode.K)) up = -transform.up;

        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;


        Vector3 combined = (forward + left + up).normalized;

        actionsOut.ContinuousActions.Array[0] = combined.x;
        actionsOut.ContinuousActions.Array[1] = combined.y;
        actionsOut.ContinuousActions.Array[2] = combined.z;
        actionsOut.ContinuousActions.Array[3] = pitch;
        actionsOut.ContinuousActions.Array[4] = yaw;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if(nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        //Observe the agent's local rotation (4 observations)
        sensor.AddObservation(transform.localPosition.normalized);

        //Get vector from beaktip to nearest flower.
        Vector3 toFlower = nearestFlower.flowerCenterPosition - beakTip.position;

        //Observe normalzed vector fro beaktip to nearest flowe(we normalize, so that , magnitude is one less feature to 
        //worry about when training, it is not needed) (3 Observations)
        sensor.AddObservation(toFlower.normalized);

        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.flowerUpVector.normalized)); //(1 observation)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.flowerUpVector.normalized)); //(1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter); //(1 observation)

        //10 total observations
    }

    public void FreezingAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    public void UnFreezingAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }


    private void UpdateNearestFlower()
    {
       foreach(Flower flower in flowerArea.Flowers)
        {
            //If we don' have any current nearest flowers, and flower being looked at has nectar, take it
            if(nearestFlower == null && flower.HasNectar)
            {
                nearestFlower = flower;
            }
            else if(flower.HasNectar) //We already have a nearest flower, but we want to see if this one is closer to replace current
            {
                //Calculate distance to current nearest flower, calculate distance to flower.
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                //If current nearest flower is empty, or if this new flower is closer, update the flower
                if(!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }

    private void MoveToSageRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attempsRemaining = 100;
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while(!safePositionFound && attempsRemaining > 0)
        {
            attempsRemaining--;
            if(inFrontOfFlower)
            {
                //Pick a random flower
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                //Position 10 to 20 coms in front of the flower
                float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.flowerUpVector * distanceFromFlower;

                //Point beak at flower (bird's head is center of transform)
                Vector3 toFlower = randomFlower.flowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                //Pick a random height from the ground
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                //Pick a random radius from center of the area
                float radius = UnityEngine.Random.Range(2f, 7f);

                //Pick a random direction rotated around the y axis
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                //Combine height, radius and direction to pick a potential position
                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                //Choose and set random pitch and yaw
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            //Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            //Safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length == 0; //aka the list is empty, means no collisions happened.
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        //Set the position and rotation
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void TriggerEnterOrStay(Collider collider)
    {
        if(collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            if(Vector3.Distance(beakTip.position,closestPointToBeakTip) < BeakTipRadius)
            {
                //Look up the flower for this nectar collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                //Attempt to feed on nectar , .01 nectar
                float nectarReceived = flower.Feed(.01f);

                //Keep track of nectar obtained
                NectarObtained += nectarReceived;
            
            if(trainingMode)
                {
                    //Calculate the reward for getting the nectar.
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, nearestFlower.flowerUpVector.normalized));
                    AddReward(.01f + bonus);

                    //If flower is empty , update the nearest flower
                    if(!flower.HasNectar)
                    {
                        UpdateNearestFlower();
                    }
                }
            
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(trainingMode && collision.collider.CompareTag("boundary"))
        {
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    private void Update()
    {
        if(nearestFlower != null)
        {
            Debug.DrawLine(beakTip.position, nearestFlower.flowerCenterPosition, Color.green);
        }
    }

}
