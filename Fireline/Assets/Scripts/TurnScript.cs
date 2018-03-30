using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnScript : MonoBehaviour {
	[HideInInspector] public bool playerTurn;

	// Use this for initialization
	void Start () {
		playerTurn = true;
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Input.GetKeyDown(KeyCode.Space)) {
			if (playerTurn) {
				playerTurn = false;
				Debug.Log ("End turn");
			}

			if (!playerTurn) {
				Debug.Log ("Spread fire");
				Debug.Log ("People die");
				// ...

				playerTurn = true;
				Debug.Log ("Player's turn again");
			}
		}
	}
}
