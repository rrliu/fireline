using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HexGrid))]
[RequireComponent(typeof(TurnScript))]
public class MovementScript : MonoBehaviour {
	public Color hoveredColor;
	public Color selectedColor;
	public Color neighborsColor;
	public Color digNextColor;
	public Color digNextFocusColor;
	public Color digLaterColor;
	public Color digLaterFocusColor;

	HexGrid hexGrid;
	TurnScript turnScript;

	Vector2Int noSelection = new Vector2Int(-1, -1);
	Vector2Int selected;
	List<TileNode> neighbors = null;

	// Returns the index of the given tile in the neighbors list,
	// or -1 if it's not in the list.
	int GetIndexInNeighbors(Vector2Int tile) {
		hexGrid.DebugValidateTileIndex(tile);
		int ind = -1;
		for (int i = 0; i < neighbors.Count; i++) {
			if (neighbors[i].coords == tile) {
				ind = i;
				break;
			}
		}
		return ind;
	}

	// Use this for initialization
	void Start () {
		hexGrid = GetComponent<HexGrid>();
		turnScript = GetComponent<TurnScript>();
		selected = noSelection;
	}
	
	// Update is called once per frame
	void Update () {
		// Clear all tile colors.
		// If you want a tile to stay colored, you must set it every frame
		hexGrid.ClearTileColors();

		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2Int hovered = hexGrid.GetClosestTileIndex(mousePos);
		TileInfo hoveredTile = hexGrid.tiles[hovered.x, hovered.y];

		if (turnScript.playerTurn) {
			// Click event
			if (Input.GetMouseButtonDown(0)) {
				// Debug.Log("You clicked on: " + hovered.ToString());
                // Debug.Log ("Type: "
                //     + hoveredTile.type.ToString ());

				// Conditions for selection to be possible
				if (selected != hovered
				&& hoveredTile.type != TileType.WATER
				&& hoveredTile.unit != null) {
					// Select hovered tile
					selected = hovered;
					float range = hexGrid.tiles[selected.x, selected.y].unitScript.range;
					if (range > 0.0f) {
						neighbors = hexGrid.GetReachableTiles(selected, range);
					} else {
						neighbors = null;
					}
                } else if (selected == hovered
                && hexGrid.tiles[selected.x, selected.y].unit != null) {
                    // Clear selection
					selected = noSelection;
					if (neighbors != null) {
						neighbors = null;
					}
                }
			}
            if (Input.GetMouseButton(0)) {
				// Did the player try to move a unit?
				if (selected != noSelection
				&& hexGrid.tiles[selected.x, selected.y].unit != null) {
                    // Determine whether the unit can move to the target tile
                    if (hoveredTile.unit == null
                    && hoveredTile.fire == null) {
                        // Move the unit
                        TileInfo unitTile = hexGrid.tiles[
                            selected.x, selected.y];
                        UnitCommand moveCommand;
                        moveCommand.type = UnitCommandType.MOVE;
                        moveCommand.target = hovered;
                        unitTile.unitScript.AddCommandIfNew(moveCommand);
                        //MoveUnit(selected, hovered, neighbors[ind].dist);
                    }
				}
            }
			else if (Input.GetMouseButton(1)) {
				// Did the player try to dig?
				if (selected != noSelection
				&& hexGrid.tiles[selected.x, selected.y].unit != null) {
					// Determine whether the unit can dig the target tile
					TileInfo unitTile = hexGrid.tiles[selected.x, selected.y];
					TileInfo targetTile = hoveredTile;
					if (targetTile.fire == null
					&& targetTile.type != TileType.WATER) {
                        UnitCommand moveCommand;
                        moveCommand.type = UnitCommandType.MOVE;
                        moveCommand.target = hovered;
						unitTile.unitScript.AddCommandIfNew(moveCommand);
						UnitCommand digCommand;
						digCommand.type = UnitCommandType.DIG;
						digCommand.target = hovered;
						unitTile.unitScript.AddCommandIfNew(digCommand);
					}
				}
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				// Did the player try to clear the selected unit's commands?
				if (selected != noSelection
					&& hexGrid.tiles[selected.x, selected.y].unit) {
					hexGrid.tiles[selected.x, selected.y].unitScript.ClearCommands();
				}
			}

			// Paint all tiles we want to paint, in order
			if (neighbors != null) {
				foreach (TileNode tile in neighbors) {
					hexGrid.SetTileColor(tile.coords, neighborsColor);
				}
			}
		}

		foreach (Vector2Int unitTile in hexGrid.unitTiles) {
			UnitScript unitScript = hexGrid.tiles[unitTile.x, unitTile.y].unitScript;
            unitScript.DrawCommands();
		}
		hexGrid.MultiplyTileColor(hovered, hoveredColor);
		if (selected != noSelection) {
			hexGrid.SetTileColor(selected, selectedColor);
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			// TODO janky for now
			// Deselect everything
			selected = noSelection;
			neighbors = null;
		}
	}
}
