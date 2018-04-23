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
	List<HexGrid.TileNode> neighbors = null;

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

		if (turnScript.playerTurn) {
			// Click event
			if (Input.GetMouseButtonDown(0)) {
				Debug.Log("You clicked on: " + hovered.ToString());
                Debug.Log ("Type: "
                    + hexGrid.tiles [hovered.x, hovered.y].type.ToString ());

				// Did the player try to move a unit?
				if (selected != noSelection
				&& hexGrid.tiles[selected.x, selected.y].unit != null
				&& neighbors != null) {
					// Check if selection is in neighbors
					int ind = GetIndexInNeighbors(hovered);
					if (ind != -1) {
						// Determine whether the unit can move to the target tile
						if (hexGrid.tiles[hovered.x, hovered.y].unit == null
						&& hexGrid.tiles[hovered.x, hovered.y].fire == null) {
							// Move the unit
							TileInfo unitTile = hexGrid.tiles[selected.x, selected.y];
							UnitCommand moveCommand;
							moveCommand.type = UnitCommandType.MOVE;
							moveCommand.target = hovered;
							unitTile.unitScript.AddCommandIfNew(moveCommand);
							//MoveUnit(selected, hovered, neighbors[ind].dist);
						}
					}
				}

				// Conditions for selection to be possible
				TileInfo hoveredTile = hexGrid.tiles[hovered.x, hovered.y];
				if (selected != hovered
					&& hoveredTile.type != TileType.WATER
					&& hoveredTile.unit != null) {
					// Select hovered tile
					selected = hovered;
					TileInfo selectedTile = hexGrid.tiles[selected.x, selected.y];
					float range = selectedTile.unitScript.rangeRemaining;
					// TODO temporary
					range -= 1.0f;
					if (range > 0.0f) {
						neighbors = hexGrid.GetReachableTiles(selected, range);
					} else {
						neighbors = null;
					}
				} else {
					// Clear selection
					selected = noSelection;
					if (neighbors != null) {
						neighbors = null;
					}
				}
			}
			else if (Input.GetMouseButton(1)) {
				// Did the player try to dig?
				if (selected != noSelection
				&& hexGrid.tiles[selected.x, selected.y].unit != null
				&& neighbors != null) {
					// Determine whether the unit can move to the target tile
					TileInfo unitTile = hexGrid.tiles[selected.x, selected.y];
					TileInfo targetTile = hexGrid.tiles[hovered.x, hovered.y];
					if (targetTile.unit == null
					&& targetTile.fire == null
					&& targetTile.type != TileType.WATER) {
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
				foreach (HexGrid.TileNode tile in neighbors) {
					hexGrid.SetTileColor(tile.coords, neighborsColor);
				}
			}
		}

		foreach (Vector2Int unitTile in hexGrid.unitTiles) {
			UnitScript unitScript = hexGrid.tiles[unitTile.x, unitTile.y].unitScript;
			LineRenderer unitLine = hexGrid.tiles[unitTile.x, unitTile.y].unit.GetComponent<LineRenderer>();
			List<Vector3> positions = new List<Vector3>();
			positions.Add(unitScript.transform.position);
			foreach (UnitCommand cmd in unitScript.commands) {
				Vector3 targetPos = hexGrid.TileIndicesToPos(cmd.target.x, cmd.target.y);
				targetPos.z = -1.0f;
				positions.Add(targetPos);
				hexGrid.SetTileColor(cmd.target, digLaterColor);
			}
			unitLine.positionCount = positions.Count;
			unitLine.SetPositions(positions.ToArray());
			foreach (UnitCommand cmd in unitScript.nextCommands) {
				hexGrid.SetTileColor(cmd.target, digNextColor);
			}
		}
		if (selected != noSelection
			&& hexGrid.tiles[selected.x, selected.y].unit != null) {
			UnitScript unitScript = hexGrid.tiles[selected.x, selected.y].unitScript;
			foreach (UnitCommand cmd in unitScript.commands) {
				hexGrid.SetTileColor(cmd.target, digLaterFocusColor);
			}
			foreach (UnitCommand cmd in unitScript.nextCommands) {
				hexGrid.SetTileColor(cmd.target, digNextFocusColor);
			}
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
