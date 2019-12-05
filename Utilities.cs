using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utilities : MonoBehaviour
{
    //Triston Guest
    //GIMM 400
    //Utilities: A collection of useful scripts and code snippets.

    private void Update()
    {
        //4. Moves an AI around 


        //The statement asks if the AI is currently doing nothing
        if (currentState == AIState.Idle)
        {
            if (switchAction)
            {
                //If there is an enemy, set the destination according to the Random Nav Sphere function and switch to the running state.
                if (enemy)
                {
                    //Run away
                    agent.SetDestination(RandomNavSphere(transform.position, Random.Range(1, 2.4f)));
                    currentState = AIState.Running;
                    SwitchAnimationState(currentState);
                }
            }

        }
        //Locates the random sphere based on the input parameters
        Vector3 RandomNavSphere(Vector3 origin, float distance)
        {
            //Selects random direction using Random.insideUnitSphere property and distance
            Vector3 randomDirection = Random.insideUnitSphere * distance;
            randomDirection += origin;
            //Looks for a NavMesh in the distance and set's destination to it's position
            NavMeshHit navHit;
            NavMesh.SamplePosition(randomDirection, out navHit, distance, NavMesh.AllAreas);

            return navHit.position;
        }


        //7. Moves a player around from user input using a rigid body, player controller, and direct transform assignments.


        void Movement()
        {
            //Player Rotation
            CharacterController controller = GetComponent<CharacterController>();

            rotationX += Input.GetAxis("Mouse X") * horizontalSpeed;//Player side to side rotation
            transform.localEulerAngles = new Vector3(0, rotationX, 0);
            rotationY += Input.GetAxis("Mouse Y") * verticalSpeed;//Camera up down rotation
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);//Set max and min camera up-down angle
            cam.transform.localEulerAngles = new Vector3(-rotationY, 0, 0);
            if (controller.isGrounded) //Player is on the ground
            {
                //Player WASD movement
                moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                moveDirection = transform.TransformDirection(moveDirection);
                moveDirection *= speed; //Set player speed
                                        //Space to jump
                if (Input.GetButton("Jump"))
                    moveDirection.y = jumpSpeed;//Jump speed
            }
            moveDirection.y -= gravity * Time.deltaTime; //Player gravity
            controller.Move(moveDirection * Time.deltaTime);
            //sprint
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                if (!isCrouching)//Cant sprint if crouching
                {
                    speed = setSpeed * 2f; //Sprint speed
                    isSprinting = true;
                }

            }


            //12. Utilizes or sets up any other AI algorithm(behavior trees, ANN, CNN, etc).


            //Initialize the AI state as Idle, and create a random timer to begin looking for a new action.
            currentState = AIState.Idle;
            actionTimer = Random.Range(0.1f, 2.0f);
            SwitchAnimationState(currentState);

            //Wait for the next course of action
            if (actionTimer > 0)
            {
                actionTimer -= Time.deltaTime;
            }
            else
            {
                switchAction = true;
            }
            //If AI is currently idle...
            if (currentState == AIState.Idle)
            {
                if (switchAction)
                {
                    if (enemy)
                    {
                        //Run away if an enemy is nearby to a randomized location
                        agent.SetDestination(RandomNavSphere(transform.position, Random.Range(1, 2.4f)));
                        currentState = AIState.Running;
                        SwitchAnimationState(currentState);
                    }
                    else
                    {
                        //No enemies nearby, start eating
                        actionTimer = Random.Range(14, 22);

                        currentState = AIState.Eating;
                        SwitchAnimationState(currentState);

                        //Keep last 5 Idle positions for future reference
                        previousIdlePoints.Add(transform.position);
                        if (previousIdlePoints.Count > 5)
                        {
                            previousIdlePoints.RemoveAt(0);
                        }
                    }
                }
            }
            //If AI is currently registered as dead, play it's falling over animation and Destroy after a few seconds.
            else if (currentState == AIState.Dead)
            {
                animator.SetBool("isDead", true);
                animator.Play("isDead");
                walkingSpeed = 0f;
                runningSpeed = 0f;
                gameObject.tag = "Player";
                Destroy(gameObject, 5);
            }
            //If AI is currently walking, check to see if it is at it's current destination. If so, stop walking and start eating.
            else if (currentState == AIState.Walking)
            {
                //Set NavMesh Agent Speed
                agent.speed = walkingSpeed;

                // Check if we've reached the destination
                if (DoneReachingDestination())
                {
                    currentState = AIState.Idle;
                }
            }
            //If AI is currently eating and switchAction is true, wait to finish eating animation and select a new destination to walk.
            else if (currentState == AIState.Eating)
            {
                if (switchAction)
                {
                    //Wait for current animation to finish playing
                    if (!animator || animator.GetCurrentAnimatorStateInfo(0).normalizedTime - Mathf.Floor(animator.GetCurrentAnimatorStateInfo(0).normalizedTime) > 0.99f)
                    {
                        //Walk to another random destination
                        agent.destination = RandomNavSphere(transform.position, Random.Range(3, 7));
                        currentState = AIState.Walking;
                        SwitchAnimationState(currentState);
                    }
                }
            }
            //If AI is currently running, set speed and run away from an enemy or to the closest NavMesh
            else if (currentState == AIState.Running)
            {
                //Set NavMesh Agent Speed
                agent.speed = runningSpeed;

                //Run away
                if (enemy)
                {
                    if (reverseFlee)
                    {
                        if (DoneReachingDestination() && timeStuck < 0)
                        {
                            reverseFlee = false;
                        }
                        else
                        {
                            timeStuck -= Time.deltaTime;
                        }
                    }
                    else
                    {
                        Vector3 runTo = transform.position + ((transform.position - enemy.position) * multiplier);
                        distance = (transform.position - enemy.position).sqrMagnitude;

                        //Find the closest NavMesh edge
                        NavMeshHit hit;
                        if (NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas))
                        {
                            closestEdge = hit.position;
                            distanceToEdge = hit.distance;
                        }

                        if (distanceToEdge < 1f)
                        {
                            if (timeStuck > 1.5f)
                            {
                                if (previousIdlePoints.Count > 0)
                                {
                                    runTo = previousIdlePoints[Random.Range(0, previousIdlePoints.Count - 1)];
                                    reverseFlee = true;
                                }
                            }
                            else
                            {
                                timeStuck += Time.deltaTime;
                            }
                        }
                        
                        if (distance < range * range)
                        {
                            agent.SetDestination(runTo);
                        }
                        else
                        {
                            enemy = null;
                        }
                    }

                    //Temporarily switch to Idle if the Agent stopped
                    if (agent.velocity.sqrMagnitude < 0.1f * 0.1f)
                    {
                        SwitchAnimationState(AIState.Idle);
                    }
                    else
                    {
                        SwitchAnimationState(AIState.Running);
                    }
                }
                else
                {
                    //Check if we've reached the destination then stop running
                    if (DoneReachingDestination())
                    {
                        actionTimer = Random.Range(1.4f, 3.4f);
                        currentState = AIState.Eating;
                        SwitchAnimationState(AIState.Idle);
                    }
                }
            }

            switchAction = false;
        }


        //1. Detects and send over a network player position and behavior


        /// <summary>
        /// Sets this (local) player's properties and synchronizes them to the other players (don't modify them directly).
        /// </summary>
        /// <remarks>
        /// While in a room, your properties are synced with the other players.
        /// CreateRoom, JoinRoom and JoinRandomRoom will all apply your player's custom properties when you enter the room.
        /// The whole Hashtable will get sent. Minimize the traffic by setting only updated key/values.
        ///
        /// If the Hashtable is null, the custom properties will be cleared.
        /// Custom properties are never cleared automatically, so they carry over to the next room, if you don't change them.
        ///
        /// Don't set properties by modifying PhotonNetwork.player.customProperties!
        /// </remarks>
        /// <param name="customProperties">Only string-typed keys will be used from this hashtable. If null, custom properties are all deleted.</param>
        public static void SetPlayerCustomProperties(Hashtable customProperties)
        {
            if (customProperties == null)
            {
                customProperties = new Hashtable();
                foreach (object k in LocalPlayer.CustomProperties.Keys)
                {
                    customProperties[(string)k] = null;
                }
            }

            if (CurrentRoom != null)
            {
                LocalPlayer.SetCustomProperties(customProperties);
            }
            else
            {
                LocalPlayer.InternalCacheProperties(customProperties);
            }
        }


        //11. Utilizes ML agents


        //On collision with the Bullet object, call the Crawler script gotShot() method and increase score.
        void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.tag == "Bullet")
            {

                gameUI.SetPlayerScore();
                crawlerScript.gotShot();
            }
        }

        //gotShot() gives a penalty to the Agent
        public void gotShot()
        {
            AddReward(-0.01f);
            Debug.Log("Nanobot got shot!");
        }
        //The penalty applies here, and updates the global variables 
        //that track the amount of rewards the crawler is getting so it can be trained.
        public void AddReward(float increment)
        {
            m_Reward += increment;
            m_CumulativeReward += increment;
        }
        //Calls the current amount of points.
        public float GetReward()
        {
            return m_Reward;
        }
    }
}

