using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType {
    TEST,
    TRUCK,
    NONE
}

public enum UnitCommandType {
    MOVE,
    DIG,
    INVALID
}

public struct UnitCommand {
    public UnitCommandType type;
    public Vector2Int target;
}

[RequireComponent(typeof(LineRenderer))]
public class UnitScript : MonoBehaviour {
    public UnitType type;
	public float range;
	[HideInInspector] public float rangeRemaining;
    
    // List of player-issued commands
    [HideInInspector] public List<UnitCommand> commands
        = new List<UnitCommand>();

    struct UnitCommandStep {
        public UnitCommandType type;
        public Vector2Int target;
        public float cost;
        // Used to indicate if the command has been completed
        // (as opposed to being a step along the way)
        // If this is true, the corresponding command will be removed
        // from the "commands" list
        public bool removeMarker;
    }
    // Generated list of step-by-step commands
    List<UnitCommandStep> stepCommands = new List<UnitCommandStep>();

    // Updated by HexGrid
    [HideInInspector] public Vector2Int tile;
    // Used for movement/command delay
    [HideInInspector] public bool doneMoving = true;

    HexGrid hexGrid;
    MovementScript movementScript;
    LineRenderer lineRenderer;

    public static float GetCommandTypeCost(UnitCommandType type) {
        if (type == UnitCommandType.MOVE) {
            return 0.0f;
        }
        if (type == UnitCommandType.DIG) {
            return 1.0f;
        }

        Debug.LogError("Unrecognized command type");
        return 0.0f;
    }

    // Append the given command to the list of step-by-step commands
    // Creates all the steps necessary to end at the command
    public void AppendToStepCommands(UnitCommand cmd) {
        Vector2Int prevTile = tile;
        if (commands.Count > 0) {
            prevTile = commands[commands.Count - 1].target;
        }
        
        if (cmd.type == UnitCommandType.MOVE) {
            List<TileNode> path = hexGrid.GetShortestPath(prevTile, cmd.target);
            if (path == null) {
                UnitCommandStep cmdInvalid;
                cmdInvalid.type = UnitCommandType.INVALID;
                cmdInvalid.target = cmd.target;
                cmdInvalid.cost = float.PositiveInfinity;
                cmdInvalid.removeMarker = false;
                stepCommands.Add(cmdInvalid);
            }
            else {
                float totalDist = 0.0f;
                foreach (TileNode node in path) {
                    UnitCommandStep cmdMoveStep;
                    cmdMoveStep.type = UnitCommandType.MOVE;
                    cmdMoveStep.target = node.coords;
                    cmdMoveStep.cost = node.dist - totalDist;
                    cmdMoveStep.removeMarker = false;
                    stepCommands.Add(cmdMoveStep);
                    totalDist += cmdMoveStep.cost;
                }
            }
        }
        else if (cmd.type == UnitCommandType.DIG) {
            Debug.Assert(cmd.target == prevTile);
            UnitCommandStep cmdDig;
            cmdDig.type = UnitCommandType.DIG;
            cmdDig.target = cmd.target;
            cmdDig.cost = GetCommandTypeCost(UnitCommandType.DIG);
            cmdDig.removeMarker = false;
            stepCommands.Add(cmdDig);
        }
        else {
            Debug.LogError("Unhandled command");
        }

        // TODO: janky, but yeah...
        UnitCommandStep cmdMoveMarker;
        cmdMoveMarker.type = cmd.type;
        cmdMoveMarker.target = cmd.target;
        cmdMoveMarker.cost = 0.0f;
        cmdMoveMarker.removeMarker = true;
        stepCommands.Add(cmdMoveMarker);
    }

    // Re-calculate the entire list of step-by-step commands
    public void UpdateStepCommands() {
        List<UnitCommand> commandsCopy = new List<UnitCommand>(commands);
        commands.Clear();
        stepCommands.Clear();
        foreach (UnitCommand cmd in commandsCopy) {
            AppendToStepCommands(cmd);
            commands.Add(cmd);
        }
    }

    public void AddCommandIfNew(UnitCommand command) {
        if (!commands.Contains(command)) {
            AppendToStepCommands(command);
            commands.Add(command);
        }
    }

    public void ClearCommands() {
        foreach (UnitCommand cmd in commands) {
            if (cmd.type == UnitCommandType.DIG) {
                hexGrid.tiles[cmd.target.x, cmd.target.y]
                    .gameObject.transform.Find("ShovelIcon")
                    .gameObject.SetActive(false);
            }
        }
        commands.Clear();
        UpdateStepCommands();
    }

    public IEnumerator ExecuteCommands() {
        doneMoving = false;
        float rangeRemaining = range;
        Vector2Int currentUnitTile = tile;
		bool isDead = false;
        foreach (UnitCommandStep cmdStep in stepCommands) {
            if (cmdStep.removeMarker) {
                // Remove command from commands list
                UnitCommand toRemove;
                toRemove.type = cmdStep.type;
                toRemove.target = cmdStep.target;
                commands.Remove(toRemove);
                if (toRemove.type == UnitCommandType.DIG) {
                    hexGrid.tiles[toRemove.target.x, toRemove.target.y]
                        .gameObject.transform.Find("ShovelIcon")
                        .gameObject.SetActive(false);
                }
                continue;
            }
            yield return new WaitForSeconds(0.2f);
            rangeRemaining -= cmdStep.cost;
            if (rangeRemaining >= 0.0f) {
                if (cmdStep.type == UnitCommandType.MOVE) {
					if (hexGrid.tiles[cmdStep.target.x, cmdStep.target.y].fire != null) {
						hexGrid.MoveUnit(tile, currentUnitTile);
						hexGrid.KillUnitAt(currentUnitTile);
						isDead = true;
						break;
					} else {
						currentUnitTile = cmdStep.target;
					}
                }
                else if (cmdStep.type == UnitCommandType.DIG) {
                    hexGrid.ChangeTileTypeAt(cmdStep.target, TileType.FIRELINE);
                }
                else if (cmdStep.type == UnitCommandType.INVALID) {
                    break;
                }
            }
            else {
                break;
            }
            transform.position = hexGrid.TileIndicesToPos(currentUnitTile.x, currentUnitTile.y);
        }
		if (!isDead) {
			hexGrid.MoveUnit(tile, currentUnitTile);
			UpdateStepCommands();
		}
        doneMoving = true;
    }

    public void DrawCommands(bool isSelected) {
        Color lineColor = movementScript.unitLineColor;
        if (isSelected) {
            lineColor = movementScript.unitLineColorFocus;
        }
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        List<Vector3> positions = new List<Vector3>();
        positions.Add(transform.position);
        float rangeRemaining = range;
        foreach (UnitCommandStep cmdStep in stepCommands) {
            if (cmdStep.removeMarker) {
                continue;
            }
            rangeRemaining -= cmdStep.cost;
            if (cmdStep.type == UnitCommandType.MOVE) {
                Vector3 targetPos = hexGrid.TileIndicesToPos(
                    cmdStep.target.x, cmdStep.target.y);
                targetPos.z = -1.0f;
                positions.Add(targetPos);
            }
            else if (cmdStep.type == UnitCommandType.DIG) {
                // draw shovel
                TileInfo tileInfo = hexGrid.tiles[cmdStep.target.x, cmdStep.target.y];
                tileInfo.gameObject.transform.Find("ShovelIcon").gameObject.SetActive(true);
                /*hexGrid.SetTileColor(cmdStep.target,
                    movementScript.digNextFocusColor);*/
            }
            else if (cmdStep.type == UnitCommandType.INVALID) {
                // draw red line or something
                hexGrid.SetTileColor(cmdStep.target, Color.red);
                Vector3 targetPos = hexGrid.TileIndicesToPos(
                    cmdStep.target.x, cmdStep.target.y);
                targetPos.z = -1.0f;
                positions.Add(targetPos);
                lineRenderer.endColor = Color.red;
            }

            if (rangeRemaining < 0.0f) {
                Color outOfRangeColor = movementScript.outOfRangeColor;
                if (isSelected) {
                    outOfRangeColor = movementScript.outOfRangeColorFocus;
                }
                hexGrid.MultiplyTileColor(cmdStep.target, outOfRangeColor);
            }
        }
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

	// Use this for initialization
	void Start () {
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
        movementScript = GameObject.Find("HexGrid").GetComponent<MovementScript>();
        lineRenderer = GetComponent<LineRenderer>();
		rangeRemaining = range;
	}
	
	// Update is called once per frame
	void Update () {
	}
}
