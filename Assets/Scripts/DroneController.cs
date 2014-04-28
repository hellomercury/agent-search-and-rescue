﻿using UnityEngine;
using System.Collections;

public class DroneController : MonoBehaviour {
	public enum Strategy {
		RANDOM,
		SPREAD_OUT
	}

	// Constants
	public const float TURN_RATE = 200f;
	public const float FLYING_SPEED = 36f;
	public const float INITIAL_RADIUS = 10f;
	public const float GRAVITY = 25f;

	// Spawn Ranges
	private float MIN_X = 75f;
	private float MAX_X = 1070f;
	private float MIN_Z = 50f;
	private float MAX_Z = 700f;

	public Strategy strategy;

	public CharacterController controller;

	private bool initial = true;

	private float initialTravelAngle;
	private float initialLength;
	private Vector3 initialPosition;
	private float xOffset;
	private float zOffset;

	private Vector3 destination;

	private Vector3 previousPosition;

	private float velocity;
		
	void Start () {
		strategy = Strategy.RANDOM;

		// Determine a random angle to travel towards relative to the helicopter
		initialTravelAngle = Random.Range(0f, 360f);

		initialPosition = transform.position;
		
		destination = transform.position;
		
		xOffset = INITIAL_RADIUS * Mathf.Cos(initialTravelAngle * Mathf.Deg2Rad);
		zOffset = INITIAL_RADIUS * Mathf.Sin(initialTravelAngle * Mathf.Deg2Rad);
		
		destination.x += xOffset;
		destination.z += zOffset;

		initialLength = Vector3.Distance(initialPosition, destination);
	}
	
	void Update () {	
		// Update the destination y to the current y (as it will depend on the terrain)
		destination.y = transform.position.y;

		// If we have just spawned at the helicopter
		if (initial)
		{
			if (controller.isGrounded)
			{
				initial = false;
			}
			else
			{
				destination.x = transform.position.x + xOffset;
				destination.z = transform.position.z + zOffset;
			}
		}
		else
		{
			// Check if this drone has reached it's destination
			if (Vector3.Distance(transform.position, destination) < 1f)
			{
				
				switch (strategy) 
				{
				case Strategy.RANDOM:
					// Pick a random point within the terrain to be the next destination
					destination = new Vector3(Random.Range (MIN_X, MAX_X), 0f, Random.Range (MIN_Z, MAX_Z)); 
					break;
				case Strategy.SPREAD_OUT:
					// TODO: Implement
					break;
				}
			}
			// Check if the velocity of the drone has decreased below the allowed threshold
			else if (velocity < 1f)
			{
				switch (strategy) 
				{
				case Strategy.RANDOM:
					// Pick a random point within the terrain to be the next destination
					destination = new Vector3(Random.Range (MIN_X, MAX_X), 0f, Random.Range (MIN_Z, MAX_Z)); 
					break;
				case Strategy.SPREAD_OUT:
					// TODO: Implement
					break;
				}
			}

			// Check for soldiers in the cameras view
			GameObject[] soldiers = GameObject.FindGameObjectsWithTag("Friendly");
			for (int i = 0; i < soldiers.Length; i++)
			{
				SoldierController soldierController = soldiers[i].GetComponent<SoldierController>();
				if (soldierController.status == SoldierController.Status.NOT_FOUND)
				{
					if (CheckLineOfSight(soldiers[i]))
					{
						soldierController.status = SoldierController.Status.FOUND;

						SpawnController.missingFriendlySoldiers.Remove(soldiers[i]);
						SpawnController.foundFriendlySoldiers.Add(soldiers[i]);

						SpawnController.foundFriendlySoldierCount++;

						// Update Statistics
						StatisticsWriter.Found();
					}
				}
			}
		}
		
		previousPosition = transform.position;

		Travel (strategy);

		// Calculate the current velocity
		velocity = (Vector3.Distance (transform.position, previousPosition)) / Time.deltaTime;
	}

	private void Travel(Strategy strategy)
	{
		Vector3 moveDirection;

		transform.LookAt (destination, Vector3.up);
		
		moveDirection = FLYING_SPEED * Vector3.Normalize(destination - transform.position) * Time.deltaTime;

		// Apply gravity
		moveDirection.y -= GRAVITY * Time.deltaTime;
		
		// Move towards the position
		controller.Move(moveDirection);
	}

	/// <summary>
	/// Checks the line of sight for the target within the cameras far clip plan and field of view
	/// </summary>
	/// <returns><c>true</c>, if the target is within the cameras far clip plan and field of view, <c>false</c> otherwise.</returns>
	/// <param name="target">Target. Any GameObject in the environment</param>
	private bool CheckLineOfSight (GameObject target) 
	{
		if (Vector3.Distance(transform.position, target.transform.position) < camera.farClipPlane) // target is within camera far clip plane
		{
			if (Vector3.Angle(target.transform.position - transform.position, transform.forward) <= camera.fieldOfView) // target is within FOV
			{
				if(Physics.Linecast(transform.position, target.transform.position, Physics.DefaultRaycastLayers) == false) // target can be raycast (is visible) from the camera
				{
					// therefore, target is viewable by this drone
					return true;
				}
			}
		}

		return false;
	}
}
