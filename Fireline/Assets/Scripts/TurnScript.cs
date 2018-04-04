using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnScript : MonoBehaviour {
	public HexGrid hexGrid;
    public Animator splat;
	[HideInInspector] public bool playerTurn;

	void StartPlayerTurn() {
		int width = hexGrid.tiles.GetLength (0);
		int height = hexGrid.tiles.GetLength (1);
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (hexGrid.tiles[i, j].unitType != UnitType.NONE) {
					hexGrid.tiles[i, j].unitScript.rangeRemaining =
						hexGrid.tiles[i, j].unitScript.range;
				}
			}
		}
	}

	// Use this for initialization
	void Start () {
		playerTurn = true;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Space)) {
			if (playerTurn) {
				playerTurn = false;
				//Debug.Log ("End turn");
			}

			if (!playerTurn) {
				//Debug.Log ("Spread fire");
                hexGrid.SpreadFire();
				//Debug.Log ("People die");
		        int width = hexGrid.tiles.GetLength (0);
		        int height = hexGrid.tiles.GetLength (1);
                for (int i = 0; i < width; i++) {
                    for (int j = 0; j < height; j++) {
                        if (hexGrid.tiles[i, j].unitType != UnitType.NONE
                        && hexGrid.tiles[i, j].onFire) {
                            Destroy(hexGrid.tiles[i, j].unit);
                            hexGrid.tiles[i, j].unitType = UnitType.NONE;
                            hexGrid.tiles[i, j].unitScript = null;
                            hexGrid.tiles[i, j].unit = null;
                            splat.SetTrigger("splat");
                        }
                    }
                }

				playerTurn = true;
				//Debug.Log ("Player's turn again");
				StartPlayerTurn ();
			}
		}
	}
}
