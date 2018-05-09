using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HexGrid))]
[RequireComponent(typeof(TurnScript))]
[RequireComponent(typeof(GeneratorScript))]
public class MovementScript : MonoBehaviour
{
    public Color hoveredColor;
    public Color selectedColor;
    public Color neighborsColor;
    public Color outOfRangeColor;
    public Color outOfRangeColorFocus;
    public Color unitLineColor;
    public Color unitLineColorFocus;

    public GameObject buyUnitMenu;
	public GameObject warningPopup;

	[HideInInspector] public bool popup = false;
	UnitCommand popupDig;
	UnitScript popupUnitScript;

    HexGrid hexGrid;
    TurnScript turnScript;
    GeneratorScript generatorScript;

    Vector2Int noSelection = new Vector2Int(-1, -1);
    Vector2Int selected;
    List<TileNode> neighbors = null;

    bool buyMenu = false;
    Vector2Int campSelection;

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

    bool IsTileWalkable(Vector2Int tile, Vector2Int unitTile) {
        UnitType unitType = hexGrid.tiles[unitTile.x, unitTile.y].unitScript.type;
        if (hexGrid.GetTileMoveWeight(tile, unitType) == 0.0f) {
            return false;
        }
        TileInfo tileInfo = hexGrid.tiles[tile.x, tile.y];
        if (tileInfo.unit != null) {
            return false;
        }

        foreach (Vector2Int it in hexGrid.unitTiles) {
            if (it == unitTile) {
                continue;
            }
            UnitScript unitScript = hexGrid.tiles[it.x, it.y].unitScript;
            foreach (UnitCommand cmd in unitScript.commands) {
                if (cmd.target == tile) {
                    return false;
                }
            }
        }

        return true;
    }

    bool IsTileDiggable(Vector2Int tile, Vector2Int unitTile) {
        if (!IsTileWalkable(tile, unitTile) && tile != unitTile) {
            return false;
        }
        UnitType unitType = hexGrid.tiles[unitTile.x, unitTile.y].unitScript.type;
        if (unitType != UnitType.TEST) {
            return false;
        }

        TileInfo tileInfo = hexGrid.tiles[tile.x, tile.y];
        if (tileInfo.fire != null
        || tileInfo.camp != null 
        || tileInfo.type == TileType.WATER
        || tileInfo.type == TileType.FIRELINE
        || tileInfo.type == TileType.CITY_FIRELINE) {
            return false;
        }

        foreach (Vector2Int it in hexGrid.unitTiles) {
            if (it == unitTile) {
                continue;
            }
            UnitScript unitScript = hexGrid.tiles[it.x, it.y].unitScript;
            foreach (UnitCommand cmd in unitScript.commands) {
                if (cmd.target == tile) {
                    return false;
                }
            }
        }

        return true;
    }

    bool IsTileTruckable(Vector2Int tile, Vector2Int unitTile) {
        if (!IsTileWalkable(tile, unitTile)) {
            return false;
        }
        UnitType unitType = hexGrid.tiles[unitTile.x, unitTile.y].unitScript.type;
        if (unitType != UnitType.TRUCK) {
            return false;
        }

        TileInfo tileInfo = hexGrid.tiles[tile.x, tile.y];
        if (tileInfo.fire == null) {
            return false;
        }

        foreach (Vector2Int it in hexGrid.unitTiles) {
            if (it == unitTile) {
                continue;
            }
            UnitScript unitScript = hexGrid.tiles[it.x, it.y].unitScript;
            foreach (UnitCommand cmd in unitScript.commands) {
                if (cmd.target == tile) {
                    return false;
                }
            }
        }

        return true;
    }

    int GetUnitCost(UnitType type) {
        if (type == UnitType.TEST) {
            return 500;
        }
        if (type == UnitType.TRUCK) {
            return 1000;
        }

        Debug.LogError("unrecognized unit type");
        return 0;
    }

    public void UpdateBuyButtonColors() {
        Image unitImg = buyUnitMenu.transform.Find("BuyPanel/Person").GetComponent<Image>();
        int unitCost = GetUnitCost(UnitType.TEST);
        if (turnScript.money >= unitCost) {
            unitImg.color = Color.white;
        }
        else {
            unitImg.color = Color.red;
        }
        Image truckImg = buyUnitMenu.transform.Find("BuyPanel/Truck").GetComponent<Image>();
        int truckCost = GetUnitCost(UnitType.TRUCK);
        if (turnScript.money >= truckCost) {
            truckImg.color = Color.white;
        }
        else {
            truckImg.color = Color.red;
        }
    }
    public void CloseBuyMenu() {
        buyMenu = false;
        buyUnitMenu.SetActive(false);
    }
    IEnumerator BuyUnitDelayed(Vector2Int tile, UnitType unitType) {
        int cost = GetUnitCost(unitType);
        if (turnScript.money >= cost) {
            hexGrid.CreateUnitAt(tile, unitType);
            turnScript.money -= cost;
            turnScript.UpdateMoneyText();
        }
        else {
            yield break;
        }
        yield return null;
        CloseBuyMenu();
    }
    public void BuyNormalUnit() {
        StartCoroutine(BuyUnitDelayed(campSelection, UnitType.TEST));
    }
    public void BuyTruck() {
        StartCoroutine(BuyUnitDelayed(campSelection, UnitType.TRUCK));
    }

    void ClearNeighbors() {
        if (neighbors != null) {
            foreach (TileNode tile in neighbors) {
                hexGrid.SetIconActiveAlpha(tile.coords, UnitCommandType.DIG, false, 1.0f);
                hexGrid.SetIconActiveAlpha(tile.coords, UnitCommandType.EXTINGUISH, false, 1.0f);
            }
        }
        neighbors = null;
    }

	public void WarningPopupYes() {
		popupUnitScript.SubmitCommand(popupDig);
		warningPopup.SetActive(false);
		popup = false;
	}
	public void WarningPopupNo() {
		warningPopup.SetActive(false);
		popup = false;
	}

    // Use this for initialization
    void Start() {
        hexGrid = GetComponent<HexGrid>();
        turnScript = GetComponent<TurnScript>();
        generatorScript = GetComponent<GeneratorScript>();
        selected = noSelection;
    }
	
    // Update is called once per frame
    void Update() {
        if (!generatorScript.loaded) {
            return;
        }
        // Clear all tile colors.
        // If you want a tile to stay colored, you must set it every frame
        hexGrid.ClearTileColors();

        if (buyMenu) {
            return;
        }
		if (popup) {
			return;
		}

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int hovered = hexGrid.GetClosestTileIndex(mousePos);
        TileInfo hoveredTile = hexGrid.tiles[hovered.x, hovered.y];

        if (turnScript.playerTurn) {
            // Click event
            if (Input.GetMouseButtonDown(0)) {
				Debug.Log("You clicked on: " + hovered.ToString());
                Debug.Log ("Type: "
                    + hoveredTile.type.ToString ());

                // Conditions for selection to be possible
                if (selected != hovered
                && hoveredTile.type != TileType.WATER
                && (hoveredTile.unit != null || hoveredTile.camp != null)) {
                    // Select hovered tile
                    selected = hovered;
                    if (hoveredTile.unit != null) {
                        UnitScript unitScript = hexGrid.tiles[selected.x, selected.y].unitScript;
                        float range = unitScript.range;
                        if (range > 0.0f) {
							ClearNeighbors();
                            neighbors = hexGrid.GetReachableTiles(selected, range, unitScript.type);
                        }
                        else {
                            ClearNeighbors();
                        }
                    }
                    else if (hoveredTile.camp != null) {
                        buyMenu = true;
                        ClearNeighbors();
                        UpdateBuyButtonColors();
                        buyUnitMenu.SetActive(true);
                        campSelection = selected;
                        selected = noSelection;
                    }
                }
                else if (selected == hovered
                           && hexGrid.tiles[selected.x, selected.y].unit != null) {
                    // Clear selection
                    selected = noSelection;
                    if (neighbors != null) {
                        ClearNeighbors();
                    }
                }
            }
            if (selected != noSelection
                && hexGrid.tiles[selected.x, selected.y].unit != null) {
                // Selection is a unit
                TileInfo unitTile = hexGrid.tiles[selected.x, selected.y];
                //UnitType unitType = unitTile.unitScript.type;

                if (Input.GetMouseButtonDown(0)) {
                    // Determine whether the unit can move to the target tile
                    if (IsTileWalkable(hovered, selected)) {
                        // Move the unit
                        UnitCommand moveCommand;
                        moveCommand.type = UnitCommandType.MOVE;
                        moveCommand.target = hovered;
                        unitTile.unitScript.SubmitCommand(moveCommand);
                    }
                }
                else if (Input.GetMouseButtonDown(1)) {
                    // Determine whether the unit can dig the target tile
                    if (IsTileDiggable(hovered, selected)) {
						if (hexGrid.tiles[hovered.x, hovered.y].type == TileType.CITY) {
							warningPopup.SetActive(true);
							popup = true;
							popupDig.type = UnitCommandType.DIG;
							popupDig.target = hovered;
							popupUnitScript = unitTile.unitScript;
						} else {
							UnitCommand digCommand;
							digCommand.type = UnitCommandType.DIG;
							digCommand.target = hovered;
							unitTile.unitScript.SubmitCommand(digCommand);
						}
                    }
                    if (IsTileTruckable(hovered, selected)) {
                        UnitCommand extinguishCommand;
                        extinguishCommand.type = UnitCommandType.EXTINGUISH;
                        extinguishCommand.target = hovered;
                        unitTile.unitScript.SubmitCommand(extinguishCommand);
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
                    UnitScript unitScript = hexGrid.tiles[selected.x, selected.y].unitScript;
                    float range = unitScript.range;
                    UnitType unitType = unitScript.type;
                    if (unitType == UnitType.TEST) {
                        if (tile.dist + UnitScript.GetCommandTypeCost(UnitCommandType.DIG) <= range) {
                            hexGrid.SetIconActiveAlpha(tile.coords, UnitCommandType.DIG, true, 0.4f);
                        }
                    }
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
            ClearNeighbors();
        }
    }
}
