using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    TEST,
    // main unit
    TRUCK,
    NONE
}

public enum UnitCommandType
{
    MOVE,
    DIG,
    EXTINGUISH,
    INVALID,
    NONE
}

public struct UnitCommand
{
    public UnitCommandType type;
    public Vector2Int target;
}

[RequireComponent(typeof(LineRenderer))]
public class UnitScript : MonoBehaviour
{
    public UnitType type;
    public float range;
    [HideInInspector] public float rangeRemaining;
    
    // List of player-issued commands
    [HideInInspector] public List<UnitCommand> commands
        = new List<UnitCommand>();

    struct UnitCommandStep
    {
        public UnitCommandType type;
        public Vector2Int target;
        public float cost;
        // Used to indicate if the command has been completed
        // (as opposed to being a step along the way)
        // If this is true, the corresponding command will be removed
        // from the "commands" list when executed
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
        if (type == UnitCommandType.EXTINGUISH) {
            return 1.0f;
        }

        Debug.LogError("Unrecognized command type");
        return 0.0f;
    }

    public IEnumerator ExecuteCommands() {
        doneMoving = false;
        float rangeRemaining = range;
        Vector2Int currentUnitTile = tile;
        bool isDead = false;
        List<UnitCommand> cmdsToRemove = new List<UnitCommand>();
        for (int i = 0; i < stepCommands.Count; i++) {
            UnitCommandStep cmdStep = stepCommands[i];
            if (cmdStep.removeMarker) {
                // Remove command from commands list
                UnitCommand toRemove;
                toRemove.type = cmdStep.type;
                toRemove.target = cmdStep.target;
                cmdsToRemove.Add(toRemove);
                continue;
            }
            yield return new WaitForSeconds(0.2f);
            rangeRemaining -= cmdStep.cost;
            if (rangeRemaining >= 0.0f) {
                if (cmdStep.type == UnitCommandType.MOVE) {
                    float range = rangeRemaining;
                    bool willExtinguish = false;
                    for (int j = i + 1; j < stepCommands.Count; j++) {
                        if (stepCommands[j].removeMarker) {
                            continue;
                        }
                        range -= stepCommands[j].cost;
                        if (range < 0.0f) {
                            break;
                        }
                        if (stepCommands[j].type == UnitCommandType.EXTINGUISH
                        && stepCommands[j].target == cmdStep.target) {
                            willExtinguish = true;
                            break;
                        }
                    }
                    if (hexGrid.tiles[cmdStep.target.x, cmdStep.target.y].fire != null && !willExtinguish) {
                        hexGrid.MoveUnit(tile, currentUnitTile);
                        hexGrid.KillUnitAt(currentUnitTile);
                        isDead = true;
                        break;
                    }
                    else {
                        currentUnitTile = cmdStep.target;
                    }
                }
                else if (cmdStep.type == UnitCommandType.DIG) {
                    TileType tileType = hexGrid.tiles[cmdStep.target.x, cmdStep.target.y].type;
                    if (tileType == TileType.CITY) {
                        hexGrid.ChangeTileTypeAt(cmdStep.target, TileType.CITY_FIRELINE);
                    }
                    else {
                        hexGrid.ChangeTileTypeAt(cmdStep.target, TileType.FIRELINE);
                    }
                }
                else if (cmdStep.type == UnitCommandType.EXTINGUISH) {
                    hexGrid.PutOutFireIfExistsAt(cmdStep.target);
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
        }
        foreach (UnitCommand toRemove in cmdsToRemove) {
            commands.Remove(toRemove);
            hexGrid.SetIconActive(toRemove.target, toRemove.type, false);
        }
        UpdateStepCommands();
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
                hexGrid.SetIconActive(cmdStep.target, cmdStep.type, true);
                continue;
            }
            rangeRemaining -= cmdStep.cost;
            if (cmdStep.type == UnitCommandType.MOVE) {
                Vector3 targetPos = hexGrid.TileIndicesToPos(
                            cmdStep.target.x, cmdStep.target.y);
                targetPos.z = -1.0f;
                positions.Add(targetPos);
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
                hexGrid.SetTileColor(cmdStep.target, outOfRangeColor);
            }
        }
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    // Append the given command to the list of step-by-step commands
    // Creates all the steps necessary to end at the command
    public void AppendToStepCommands(UnitCommand cmd) {
        if (cmd.type == UnitCommandType.DIG
        || cmd.type == UnitCommandType.EXTINGUISH) {
            UnitCommand moveCmd;
            moveCmd.type = UnitCommandType.MOVE;
            moveCmd.target = cmd.target;
            AppendToStepCommands(moveCmd);
        }

        Vector2Int prevTile = tile;
        if (commands.Count > 0) {
            prevTile = commands[commands.Count - 1].target;
        }
        
        if (cmd.type == UnitCommandType.MOVE) {
            List<TileNode> path = hexGrid.GetShortestPath(prevTile, cmd.target, type);
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
        else if (cmd.type == UnitCommandType.EXTINGUISH) {
            Debug.Assert(cmd.target == prevTile);
            UnitCommandStep cmdExtinguish;
            cmdExtinguish.type = UnitCommandType.EXTINGUISH;
            cmdExtinguish.target = cmd.target;
            cmdExtinguish.cost = GetCommandTypeCost(UnitCommandType.EXTINGUISH);
            cmdExtinguish.removeMarker = false;
            stepCommands.Add(cmdExtinguish);
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

        commands.Add(cmd);
    }

    // Re-calculate the entire list of step-by-step commands
    public void UpdateStepCommands() {
        List<UnitCommand> commandsCopy = new List<UnitCommand>(commands);
        commands.Clear();
        stepCommands.Clear();
        foreach (UnitCommand cmd in commandsCopy) {
            AppendToStepCommands(cmd);
        }
    }

    UnitCommand GetCommandWithTarget(List<UnitCommand> commands, Vector2Int target) {
        UnitCommand result;
        result.type = UnitCommandType.NONE;
        result.target = Vector2Int.zero;
        foreach (UnitCommand cmd in commands) {
            if (cmd.target == target) {
                result = cmd;
            }
        }
        return result;
    }

    // Submit the command (add or remove)
    public void SubmitCommand(UnitCommand command) {
        //Debug.Log("submitted: type " + command.type.ToString ());
        UnitCommand repeatCmd = GetCommandWithTarget(commands, command.target);
        if (repeatCmd.type != UnitCommandType.NONE) {
            // Remove repeat command
            hexGrid.SetIconActive(repeatCmd.target, repeatCmd.type, false);
            commands.Remove(repeatCmd);
            UpdateStepCommands();
        }
        else {
            bool isInPath = false;
            int commandsBefore = 0;
            foreach (UnitCommandStep cmdStep in stepCommands) {
                if (cmdStep.removeMarker) {
                    commandsBefore++;
                    continue;
                }
                if (cmdStep.target == command.target) {
                    break;
                }
            }
            if (commandsBefore != commands.Count) {
                isInPath = true;
            }

            if (isInPath && command.type == UnitCommandType.MOVE) {
                for (int i = commandsBefore; i < commands.Count; i++) {
                    UnitCommand cmd = commands[i];
                    hexGrid.SetIconActive(cmd.target, cmd.type, false);
                }
                commands.RemoveRange(commandsBefore, commands.Count - commandsBefore);
                commands.Add(command);
                UpdateStepCommands();
            }
            else if (isInPath) {
                commands.Insert(commandsBefore, command);
                UpdateStepCommands();
            }
            else {
                // Add command
                AppendToStepCommands(command);
            }
        }
    }

    public void ClearCommands() {
        foreach (UnitCommand cmd in commands) {
            hexGrid.SetIconActive(cmd.target, cmd.type, false);
        }
        commands.Clear();
        UpdateStepCommands();
    }

    // Use this for initialization
    void Start() {
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
        movementScript = GameObject.Find("HexGrid").GetComponent<MovementScript>();
        lineRenderer = GetComponent<LineRenderer>();
        rangeRemaining = range;
    }
	
    // Update is called once per frame
    void Update() {
    }
}
