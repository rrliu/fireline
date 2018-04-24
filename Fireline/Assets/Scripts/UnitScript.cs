using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType {
    TEST,
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
    public UnitType unitType;
	public float range;
	[HideInInspector] public float rangeRemaining;
    
    [HideInInspector] public List<UnitCommand> commands
        = new List<UnitCommand>();

    struct UnitCommandFull {
        public UnitCommandType type;
        public Vector2Int target;
        public float cost;
        public bool removeMarker;
    }
    List<UnitCommandFull> fullCommands = new List<UnitCommandFull>();

    // Updated by HexGrid
    [HideInInspector] public Vector2Int tile;

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

    public void UpdateFullCommands(UnitCommand cmd) {
        Vector2Int prevTile = tile;
        if (commands.Count > 0) {
            prevTile = commands[commands.Count - 1].target;
        }
        
        if (cmd.type == UnitCommandType.MOVE) {
            List<TileNode> path = hexGrid.GetShortestPath(prevTile, cmd.target);
            if (path == null) {
                UnitCommandFull cmdInvalid;
                cmdInvalid.type = UnitCommandType.INVALID;
                cmdInvalid.target = cmd.target;
                cmdInvalid.cost = float.PositiveInfinity;
                cmdInvalid.removeMarker = false;
                fullCommands.Add(cmdInvalid);
            }
            else {
                float totalDist = 0.0f;
                foreach (TileNode node in path) {
                    UnitCommandFull cmdMoveStep;
                    cmdMoveStep.type = UnitCommandType.MOVE;
                    cmdMoveStep.target = node.coords;
                    cmdMoveStep.cost = node.dist - totalDist;
                    cmdMoveStep.removeMarker = false;
                    fullCommands.Add(cmdMoveStep);
                    totalDist += cmdMoveStep.cost;
                }
            }
        }
        else if (cmd.type == UnitCommandType.DIG) {
            Debug.Assert(cmd.target == prevTile);
            UnitCommandFull cmdDig;
            cmdDig.type = UnitCommandType.DIG;
            cmdDig.target = cmd.target;
            cmdDig.cost = GetCommandTypeCost(UnitCommandType.DIG);
            cmdDig.removeMarker = false;
            fullCommands.Add(cmdDig);
        }
        else {
            Debug.LogError("Unhandled command");
        }

        // TODO: janky, but yeah...
        UnitCommandFull cmdMoveMarker;
        cmdMoveMarker.type = cmd.type;
        cmdMoveMarker.target = cmd.target;
        cmdMoveMarker.cost = 0.0f;
        cmdMoveMarker.removeMarker = true;
        fullCommands.Add(cmdMoveMarker);
    }

    public void UpdateFullCommands() {
        List<UnitCommand> commandsCopy = new List<UnitCommand>(commands);
        commands.Clear();
        fullCommands.Clear();
        foreach (UnitCommand cmd in commandsCopy) {
            UpdateFullCommands(cmd);
            commands.Add(cmd);
        }
    }

    public void AddCommandIfNew(UnitCommand command) {
        if (!commands.Contains(command)) {
            UpdateFullCommands(command);
            commands.Add(command);
        }
    }

    public void ClearCommands() {
        commands.Clear();
        UpdateFullCommands();
    }

    public void ExecuteCommands() {
        float rangeRemaining = range;
        foreach (UnitCommandFull cmdFull in fullCommands) {
            if (cmdFull.removeMarker) {
                UnitCommand toRemove;
                toRemove.type = cmdFull.type;
                toRemove.target = cmdFull.target;
                commands.Remove(toRemove);
                continue;
            }
            rangeRemaining -= cmdFull.cost;
            if (rangeRemaining >= 0.0f) {
                if (cmdFull.type == UnitCommandType.MOVE) {
                    hexGrid.MoveUnit(tile, cmdFull.target);
                }
                else if (cmdFull.type == UnitCommandType.DIG) {
                    hexGrid.ChangeTileTypeAt(cmdFull.target, TileType.FIRELINE);
                }
                else if (cmdFull.type == UnitCommandType.INVALID) {
                    break;
                }
            }
            else {
                break;
            }
        }

        UpdateFullCommands();
    }

    public void DrawCommands() {
        List<Vector3> positions = new List<Vector3>();
        positions.Add(transform.position);
        float rangeRemaining = range;
        foreach (UnitCommandFull cmdFull in fullCommands) {
            if (cmdFull.removeMarker) {
                continue;
            }
            rangeRemaining -= cmdFull.cost;
            if (cmdFull.type == UnitCommandType.MOVE) {
                Vector3 targetPos = hexGrid.TileIndicesToPos(
                    cmdFull.target.x, cmdFull.target.y);
                targetPos.z = -1.0f;
                positions.Add(targetPos);
            }
            else if (cmdFull.type == UnitCommandType.DIG) {
                // draw shovel
                hexGrid.SetTileColor(cmdFull.target,
                    movementScript.digNextFocusColor);
            }
            else if (cmdFull.type == UnitCommandType.INVALID) {
                // draw red line or something
                hexGrid.SetTileColor(cmdFull.target, Color.red);
            }

            if (rangeRemaining < 0.0f) {
                hexGrid.MultiplyTileColor(cmdFull.target, Color.red);
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
