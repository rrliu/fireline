using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HexGrid))]
[RequireComponent(typeof(TurnScript))]
public class MovementScript : MonoBehaviour {
	public Color hoveredColor;
	public Color selectedColor;
	public Color neighborsColor;
	public Color outOfRangeColor;
	public Color outOfRangeColorFocus;
    public Color unitLineColor;
    public Color unitLineColorFocus;

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

    bool IsTileWalkable(Vector2Int tile, UnitType unitType) {
        TileInfo tileInfo = hexGrid.tiles[tile.x, tile.y];
        if (tileInfo.unit == null
        && tileInfo.type != TileType.WATER) {
            return true;
        }

        return false;
    }

    bool IsTileDiggable(Vector2Int tile, UnitType unitType) {
        if (unitType != UnitType.TEST) {
            return false;
        }

        TileInfo tileInfo = hexGrid.tiles[tile.x, tile.y];
        if (tileInfo.fire == null
        && tileInfo.type != TileType.WATER
        && tileInfo.type != TileType.FIRELINE) {
            return true;
        }

        return false;
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
            if (selected != noSelection
            && hexGrid.tiles[selected.x, selected.y].unit != null) {
                // Selection is a unit
                TileInfo unitTile = hexGrid.tiles[selected.x, selected.y];
                UnitType unitType = unitTile.unitScript.type;

                if (Input.GetMouseButtonDown(0)) {
                    // Determine whether the unit can move to the target tile
                    if (IsTileWalkable(hovered, unitType)) {
                        // Move the unit
                        UnitCommand moveCommand;
                        moveCommand.type = UnitCommandType.MOVE;
                        moveCommand.target = hovered;
						unitTile.unitScript.SubmitCommand(moveCommand);
                    }
                }
                else if (Input.GetMouseButtonDown(1)) {
                    // Determine whether the unit can dig the target tile
                    if (IsTileDiggable(hovered, unitType)) {
//                        UnitCommand moveCommand;
//                        moveCommand.type = UnitCommandType.MOVE;
//                        moveCommand.target = hovered;
//                        unitTile.unitScript.SubmitCommand(moveCommand);
                        UnitCommand digCommand;
                        digCommand.type = UnitCommandType.DIG;
                        digCommand.target = hovered;
						unitTile.unitScript.SubmitCommand(digCommand);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape)) {
                    // Clear selected unit's commands
                    unitTile.unitScript.ClearCommands();
                }
            }

			if (neighbors != null) {
				foreach (TileNode tile in neighbors) {
					hexGrid.SetTileColor(tile.coords, neighborsColor);
				}
			}
		}

		foreach (Vector2Int unitTile in hexGrid.unitTiles) {
            bool isSelected = unitTile == selected;
			UnitScript unitScript = hexGrid.tiles[unitTile.x, unitTile.y].unitScript;
            unitScript.DrawCommands(isSelected);
		}
		//hexGrid.MultiplyTileColor(hovered, hoveredColor);
        hexGrid.AverageTileColorWith(hovered, hoveredColor);
		if (selected != noSelection) {
			//hexGrid.MultiplyTileColor(selected, selectedColor);
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
