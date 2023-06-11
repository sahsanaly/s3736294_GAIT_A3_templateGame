using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace Completed
{
    // Player inherits from MovingObject, our base class for objects that can move, Enemy also inherits from this.
    public class PlayerAgent : Agent
    {
        private Player player;
        private int lastAction = 0;
        private Vector2 playerPos;
        float maxReward = 1.0f;
        float negMaxReward = -1.0f;
        float nullReward = 0.0f;
        float sodaReward = 0.2f;
        float sodaCriticalReward = 0.5f;
        float foodReward = 0.1f;
        float foodCriticalReward = 0.3f;

        [SerializeField] private GameManager gameManager;

        void Start()
        {
            Application.runInBackground = true;
            player = GetComponent<Player>();

            playerPos = transform.position;
        }

        public override void OnEpisodeBegin()
        {
            // TODO: Add any necessary code
            transform.position = playerPos;
        }

        public void HandleAttemptMove()
        {
            // TODO: Change the reward below as appropriate. If you want to add a cost per move, you could change the reward to -1.0f (for example).
            AddReward(nullReward); // Add a negative reward for each move
        }

        public void HandleFinishlevel()
        {
            AddReward(maxReward); // Add a positive reward for finishing the level
        
        }

        public void HandleFoundFood(int food)
        {
            // TODO: Change the reward below as appropriate.
            if (food < 30){
                AddReward(foodCriticalReward);
            }
            
            AddReward(foodReward);
            
        }

        public void HandleFoundSoda(int food)
        {
            // TODO: Change the reward below as appropriate.
            if (food < 30){
                AddReward(sodaCriticalReward);
            }
            
            AddReward(sodaReward);
        }
        public void HandleLoseFood(int loss, int food)
        {
            int ratioLossFood = loss / food;
            float negReward = -ratioLossFood * maxReward;
            // TODO: Change the reward below as appropriate.
            
            if (food > 50){
                AddReward(negReward);
            }
            else
            {
                AddReward(negMaxReward);
            }
            
        }
        

        public void HandleLevelRestart(bool gameOver)
        {
            if (gameOver)
            {
                AddReward(negMaxReward);
                Debug.Log("Level Reached" + gameManager.level);
                Academy.Instance.StatsRecorder.Add("Level Reached", gameManager.level);
                EndEpisode();
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // TODO: Insert proper code here for collecting the observations!
            // At the moment this code just feeds in 10 observations, all hardcoded to zero, as a placeholder.

            // Player's x and y position
            sensor.AddObservation(transform.position.x);
            sensor.AddObservation(transform.position.y);

            // // Get the relative positions of the nearest enemy
            GameObject nearestEnemy = GameObject.FindWithTag("Enemy"); // You would need to implement FindNearestWithTag
            if (nearestEnemy != null)
            {
                Vector3 relativePosition = nearestEnemy.transform.position - transform.position;
                sensor.AddObservation(relativePosition.x);
                sensor.AddObservation(relativePosition.y);
            }
            else
            {
                // No enemy in range, add dummy values
                sensor.AddObservation(0.0f);
                sensor.AddObservation(0.0f);
            }

            // Get the relative positions of the goal
            GameObject soda = GameObject.FindWithTag("Soda"); 
            if (soda != null)
            {
                Vector3 relativePosition = soda.transform.position - transform.position;
                sensor.AddObservation(relativePosition.x);
                sensor.AddObservation(relativePosition.y);
            }
            else
            {
                // No goal, add dummy values
                sensor.AddObservation(0.0f);
                sensor.AddObservation(0.0f);
            }

            // Get the relative positions of the goal
            GameObject food = GameObject.FindWithTag("Food"); 
            if (food != null)
            {
                Vector3 relativePosition = food.transform.position - transform.position;
                sensor.AddObservation(relativePosition.x);
                sensor.AddObservation(relativePosition.y);
            }
            else
            {
                // No goal, add dummy values
                sensor.AddObservation(0.0f);
                sensor.AddObservation(0.0f);
            }

            // Get the relative positions of the goal
            GameObject exit = GameObject.FindWithTag("Exit"); 
            if (exit != null)
            {
                Vector3 relativePosition = exit.transform.position - transform.position;
                sensor.AddObservation(relativePosition.x);
                sensor.AddObservation(relativePosition.y);
            }
            else
            {
                // No goal, add dummy values
                sensor.AddObservation(0.0f);
                sensor.AddObservation(0.0f);
            }

            base.CollectObservations(sensor);
        }

        private bool CanMove()
        {
            return !(player.isMoving || player.levelFinished || player.gameOver || gameManager.doingSetup);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (gameManager.playerMovesSinceEnemyMove == gameManager.playerMovesPerEnemyMove && CanMove() && gameManager.playerMoving == false)
            {
                return;
            }

            gameManager.playerTurn = false;

            lastAction = (int)actions.DiscreteActions[0] + 1; // To allow standing still as an action, remove the +1 and change "Branch 0 size" to 5.

            switch (lastAction)
            {
                case 0:
                    break;
                case 1:
                    gameManager.playerMoving = true;
                    player.AttemptMove(-1, 0);
                    break;
                case 2:
                    gameManager.playerMoving = true;
                    player.AttemptMove(1, 0);
                    break;
                case 3:
                    gameManager.playerMoving = true;
                    player.AttemptMove(0, -1);
                    break;
                case 4:
                    gameManager.playerMoving = true;
                    player.AttemptMove(0, 1);
                    break;
                default:
                    break;
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            GetComponent<DecisionRequester>().DecisionPeriod = 1;
            ActionSegment<int> discreteActionsOut = actionsOut.DiscreteActions;

            //If it's not the player's turn, exit the function.
            if (!CanMove())
            {
                discreteActionsOut[0] = lastAction;
                return;
            }

            int horizontal = 0;     //Used to store the horizontal move direction.
            int vertical = 0;       //Used to store the vertical move direction.

            //Check if we are running either in the Unity editor or in a standalone build.
#if UNITY_STANDALONE || UNITY_WEBPLAYER

            //Get input from the input manager, round it to an integer and store in horizontal to set x axis move direction
            horizontal = (int)(Input.GetAxisRaw("Horizontal"));

            //Get input from the input manager, round it to an integer and store in vertical to set y axis move direction
            vertical = (int)(Input.GetAxisRaw("Vertical"));

            //Check if moving horizontally, if so set vertical to zero.
            if (horizontal != 0)
            {
                vertical = 0;
            }
            //Check if we are running on iOS, Android, Windows Phone 8 or Unity iPhone
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
			
			//Check if Input has registered more than zero touches
			if (Input.touchCount > 0)
			{
				//Store the first touch detected.
				Touch myTouch = Input.touches[0];
				
				//Check if the phase of that touch equals Began
				if (myTouch.phase == TouchPhase.Began)
				{
					//If so, set touchOrigin to the position of that touch
					touchOrigin = myTouch.position;
				}
				
				//If the touch phase is not Began, and instead is equal to Ended and the x of touchOrigin is greater or equal to zero:
				else if (myTouch.phase == TouchPhase.Ended && touchOrigin.x >= 0)
				{
					//Set touchEnd to equal the position of this touch
					Vector2 touchEnd = myTouch.position;
					
					//Calculate the difference between the beginning and end of the touch on the x axis.
					float x = touchEnd.x - touchOrigin.x;
					
					//Calculate the difference between the beginning and end of the touch on the y axis.
					float y = touchEnd.y - touchOrigin.y;
					
					//Set touchOrigin.x to -1 so that our else if statement will evaluate false and not repeat immediately.
					touchOrigin.x = -1;
					
					//Check if the difference along the x axis is greater than the difference along the y axis.
					if (Mathf.Abs(x) > Mathf.Abs(y))
						//If x is greater than zero, set horizontal to 1, otherwise set it to -1
						horizontal = x > 0 ? 1 : -1;
					else
						//If y is greater than zero, set horizontal to 1, otherwise set it to -1
						vertical = y > 0 ? 1 : -1;
				}
			}
			
#endif //End of mobile platform dependendent compilation section started above with #elif

            if (horizontal == 0 && vertical == 0)
            {
                discreteActionsOut[0] = 0;
            }
            else if (horizontal < 0)
            {
                discreteActionsOut[0] = 1;
            }
            else if (horizontal > 0)
            {
                discreteActionsOut[0] = 2;
            }
            else if (vertical < 0)
            {
                discreteActionsOut[0] = 3;
            }
            else if (vertical > 0)
            {
                discreteActionsOut[0] = 4;
            }

            discreteActionsOut[0] = discreteActionsOut[0] - 1; // TODO: Remove this line if zero movement is allowed
        }
    }
}
