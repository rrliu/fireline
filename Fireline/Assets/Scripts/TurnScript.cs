using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HexGrid))]
public class TurnScript : MonoBehaviour {
    public GameObject nextTurnText;
	[HideInInspector] public bool playerTurn;

	HexGrid hexGrid;
    bool turnTransition = false;

	void StartPlayerTurn() {
		int width = hexGrid.tiles.GetLength (0);
		int height = hexGrid.tiles.GetLength (1);
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (hexGrid.tiles[i, j].unit != null) {
					hexGrid.tiles[i, j].unitScript.rangeRemaining =
						hexGrid.tiles[i, j].unitScript.range;
				}
			}
		}
	}

    [HideInInspector] public bool doneWithUnits; // TODO hacky af
    IEnumerator NextTurnCoroutine() {
        turnTransition = true;
        nextTurnText.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        // Units
        doneWithUnits = false;
        StartCoroutine(hexGrid.ExecuteUnitCommands());
        while (!doneWithUnits) {
            yield return null;
        }
        //Debug.Log("Executed unit commands");

        // Fire
        hexGrid.AgeFire();
        hexGrid.SpreadFire();
        //Debug.Log("Processed fire");

        nextTurnText.SetActive(false);
        playerTurn = true;
        //Debug.Log("Player's turn again");
        StartPlayerTurn();
        turnTransition = false;
    }

	// Use this for initialization
	void Start () {
        hexGrid = GetComponent<HexGrid>();
		playerTurn = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Space)) {
			if (playerTurn) {
				playerTurn = false;
				//Debug.Log ("End turn");
			}

			if (!playerTurn && !turnTransition) {
                StartCoroutine(NextTurnCoroutine());
			}
		}
	}
}
