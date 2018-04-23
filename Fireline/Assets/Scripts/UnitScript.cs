using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType {
    TEST,
    NONE
}

public enum UnitCommandType {
    MOVE,
    DIG
}

public struct UnitCommand {
    public UnitCommandType type;
    public Vector2Int target;
}

public class UnitScript : MonoBehaviour {
    public UnitType unitType;
	public float range;
	[HideInInspector] public float rangeRemaining;
    
    [HideInInspector] public List<UnitCommand> commands
        = new List<UnitCommand>();
    [HideInInspector] public List<UnitCommand> nextCommands
        = new List<UnitCommand>();

    // Updated by HexGrid
    [HideInInspector] public Vector2Int tile;

    HexGrid hexGrid;

    struct UnitCommandOrdered {
        public UnitCommand command;
        public float distToTarget;
    }
    
    static int CompareCommandsByDist(
    UnitCommandOrdered cmd1, UnitCommandOrdered cmd2) {
        return cmd1.distToTarget.CompareTo(cmd2.distToTarget);
    }

    public float GetCommandTypeCost(UnitCommandType type) {
        if (type == UnitCommandType.MOVE) {
            return 0.0f;
        }
        if (type == UnitCommandType.DIG) {
            return 1.0f;
        }

        Debug.LogError("Unrecognized command type");
        return 0.0f;
    }

    public void UpdateCommandList() {
        // Update command distances from unit tile
        // TODO pathfinding routine would be better here
        List<UnitCommandOrdered> orderedCommands
            = new List<UnitCommandOrdered>();
        List<HexGrid.TileNode> tiles
            = hexGrid.GetReachableTiles(tile, rangeRemaining);
        for (int i = 0; i < commands.Count; i++) {
            UnitCommandOrdered newCmd;
            newCmd.command = commands[i];
            newCmd.distToTarget = float.PositiveInfinity;
            int ind = -1;
            for (int j = 0; j < tiles.Count; j++) {
                if (commands[i].target == tiles[j].coords) {
                    ind = j;
                    break;
                }
            }
            if (ind != -1) {
                newCmd.distToTarget = tiles[ind].dist;
            }
            orderedCommands.Add(newCmd);
        }
        //orderedCommands.Sort(CompareCommandsByDist);

        commands.Clear();
        nextCommands.Clear();
        float r = rangeRemaining;
        float lastDist = 0.0f;
        foreach (UnitCommandOrdered cmd in orderedCommands) {
            commands.Add(cmd.command);

            float cost = GetCommandTypeCost(cmd.command.type);
            // TODO need a pathfinding routine here...
            float dist = cmd.distToTarget - lastDist;
            TileInfo targetInfo
				= hexGrid.tiles[cmd.command.target.x, cmd.command.target.y];
            if (targetInfo.unit == null && r >= dist + cost) {
                r -= dist + cost;
                lastDist = cmd.distToTarget;
                nextCommands.Add(cmd.command);
            }
        }
    }

    public void AddCommandIfNew(UnitCommand command) {
        if (!commands.Contains(command)) {
            commands.Add(command);
            UpdateCommandList();
        }
    }

    public void ClearCommands() {
        commands.Clear();
        UpdateCommandList();
    }

	// Use this for initialization
	void Start () {
        hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
		rangeRemaining = range;
	}
	
	// Update is called once per frame
	void Update () {
	}
}
