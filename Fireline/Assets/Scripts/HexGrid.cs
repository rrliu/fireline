using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum TileType {
	GRASSLAND,
	FOREST,
	DENSEFOREST,
	BURNT,
	FIRELINE,
	WATER,
    NONE
}

[RequireComponent(typeof(TurnScript))]
public class HexGrid : MonoBehaviour {
	public GameObject hexPrefab;
	public GameObject firePrefab;
    public float fireSpreadChance;
	public Sprite[] tileSprites;
	public GameObject[] unitPrefabs;

    public Color hoveredColor;
    public Color selectedColor;
    public Color neighborsColor;
    public Color digNextColor;
    public Color digNextFocusColor;
    public Color digLaterColor;
    public Color digLaterFocusColor;

    public Animator deathSplat;

	public struct TileInfo {
        // Should all be set once, during initialization
		public TileType type;
		public GameObject gameObject;
		public SpriteRenderer spriteRenderer;

        // null if no unit
        public GameObject unit;
		public UnitScript unitScript;

        // null if no fire
        public GameObject fire;
        public FireScript fireScript;
	};

	[HideInInspector] public TileInfo[,] tiles;
    int width, height;
    List<Vector2Int> unitTiles = new List<Vector2Int>();
    List<Vector2Int> onFire = new List<Vector2Int>();

    TurnScript turnScript;

	Vector2Int noSelection = new Vector2Int(-1, -1);
	Vector2Int selected;
	List<TileNode> neighbors = null;

	void ClearTileColors() {
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				tiles[i, j].spriteRenderer.color = Color.white;
			}
		}
	}
	void ClearTileColor(Vector2Int tile) {
        DebugValidateTileIndex(tile);
		tiles[tile.x, tile.y].spriteRenderer.color = Color.white;
	}

	void SetTileColor(Vector2Int tile, Color color) {
        DebugValidateTileIndex(tile);
		tiles[tile.x, tile.y].spriteRenderer.color = color;
	}

	void MultiplyTileColor(Vector2Int tile, Color color) {
        DebugValidateTileIndex(tile);
		tiles[tile.x, tile.y].spriteRenderer.color *= color;
	}

    void DebugValidateTileIndex(Vector2Int tileInd) {
        Debug.Assert(0 <= tileInd.x && tileInd.x < width
            && 0 <= tileInd.y && tileInd.y < height);
    }

    // Returns the index of the given tile in the neighbors list,
    // or -1 if it's not in the list.
    int GetIndexInNeighbors(Vector2Int tile) {
        DebugValidateTileIndex(tile);
        int ind = -1;
        for (int i = 0; i < neighbors.Count; i++) {
            if (neighbors[i].coords == tile) {
                ind = i;
                break;
            }
        }
        return ind;
    }

	Vector2 TileIndicesToPos(int i, int j) {
		float xStride = Mathf.Sqrt(3.0f) / 2.0f;
		float xOff = 0.0f;
		if (j % 2 == 1) {
			xOff += xStride / 2.0f;
		}
		float yStride = 3.0f / 4.0f;
		return new Vector2(xOff + i * xStride, j * yStride);
	}

	float GetTileWeight(TileType type) {
		if (type == TileType.WATER) {
			return 0.0f;
		}
		if (type == TileType.GRASSLAND) {
            return 1.0f;
        }
		if (type == TileType.FIRELINE) {
            return 1.0f;
        }
        if (type == TileType.BURNT) {
			return 1.0f;
		}
		if (type == TileType.FOREST) {
			return 1.5f;
		}
		if (type == TileType.DENSEFOREST) {
			return 2.0f;
		}

		Debug.LogError("Unrecognized tile type!");
		return 0.0f;
	}

    // Get the index of the tile closest to the given world position.
    // Snaps to the edges of the hex grid if out of bounds.
	Vector2Int GetClosestTileIndex(Vector2 position) {
		float xStride = Mathf.Sqrt(3.0f) / 2.0f;
		float yStride = 3.0f / 4.0f;
		Vector2Int result;
		int gridI = (int)(position.x * 2.0f / xStride);
		int gridJ = (int)(position.y / yStride);
		result = new Vector2Int (gridI / 2, gridJ);
		if ((gridI + gridJ) % 2 == 0) {
			// bottom left and top right are hex centers
			Vector2 bottomLeft = new Vector2(gridI * xStride / 2.0f, gridJ * yStride);
			Vector2 topRight = new Vector2((gridI + 1) * xStride / 2.0f, (gridJ + 1) * yStride);
			float bottomLeftDist = Vector2.Distance(position, bottomLeft);
			float topRightDist = Vector2.Distance(position, topRight);
			if (bottomLeftDist < topRightDist) {
				result.x = gridI / 2;
				result.y = gridJ;
			} else {
				result.x = (gridI + 1) / 2;
				result.y = gridJ + 1;
			}
		} else {
			// top left and bottom right are hex centers
			Vector2 topLeft = new Vector2(gridI * xStride / 2.0f, (gridJ + 1) * yStride);
			Vector2 bottomRight = new Vector2((gridI + 1) * xStride / 2.0f, gridJ * yStride);
			float topLeftDist = Vector2.Distance(position, topLeft);
			float bottomRightDist = Vector2.Distance(position, bottomRight);
			if (topLeftDist < bottomRightDist) {
				result.x = gridI / 2;
				result.y = gridJ + 1;
			} else {
				result.x = (gridI + 1) / 2;
				result.y = gridJ;
			}
		}

		if (result.x < 0) {
			result.x = 0;
		}
		if (result.x >= width) {
			result.x = width - 1;
		}
		if (result.y < 0) {
			result.y = 0;
		}
		if (result.y >= height) {
			result.y = height - 1;
		}

		return result;
	}

	// Use this for initialization
	void Start () {
        turnScript = GetComponent<TurnScript>();
		selected = noSelection;
	}

    public void ChangeTileTypeAt(Vector2Int tile, TileType newType) {
        DebugValidateTileIndex(tile);
        int i = tile.x, j = tile.y;
        tiles[i, j].type = newType;
        tiles[i, j].spriteRenderer.sprite = tileSprites[(int)newType];
    }

    public void CreateUnitAt(Vector2Int tile) {
        DebugValidateTileIndex(tile);
        int i = tile.x, j = tile.y;
		GameObject unit = Instantiate(unitPrefabs[0],
			tiles[i, j].gameObject.transform.position,
			Quaternion.identity, transform);
		tiles[i, j].unit = unit;
		tiles[i, j].unitScript = unit.GetComponent<UnitScript>();
        tiles[i, j].unitScript.tile = tile;
        unitTiles.Add(tile);
    }

    // From unit from one tile to another, and subtract dist from
    // the unit's remaining "actions" or range
    public void MoveUnit(Vector2Int from, Vector2Int to, float dist) {
        DebugValidateTileIndex(from);
        DebugValidateTileIndex(to);
        Debug.Assert(tiles[from.x, from.y].unit != null);
        Debug.Assert(tiles[to.x, to.y].unit == null);
        // Move the unit
        tiles[from.x, from.y].unitScript.rangeRemaining -= dist;
        tiles[from.x, from.y].unit.transform.position =
            tiles[to.x, to.y].gameObject.transform.position;
        tiles[to.x, to.y].unit = tiles[from.x, from.y].unit;
        tiles[to.x, to.y].unitScript = tiles[from.x, from.y].unitScript;
        tiles[to.x, to.y].unitScript.tile = to;
        tiles[from.x, from.y].unit = null;
        tiles[from.x, from.y].unitScript = null;
        unitTiles.Remove(from);
        unitTiles.Add(to);
    }

    public void KillUnitAt(Vector2Int tile) {
        DebugValidateTileIndex(tile);
        int i = tile.x, j = tile.y;
        Debug.Assert(tiles[i, j].unit != null);
        Destroy(tiles[i, j].unit);
        tiles[i, j].unit = null;
        unitTiles.Remove(tile);
    }

    public void CreateFireAt(Vector2Int tile) {
        DebugValidateTileIndex(tile);
        int i = tile.x, j = tile.y;
		GameObject fire = Instantiate(firePrefab,
			tiles[i, j].gameObject.transform.position,
			Quaternion.identity, transform);
        tiles[i, j].fire = fire;
        tiles[i, j].fireScript = fire.GetComponent<FireScript>();
        onFire.Add(tile);
    }

    public void PutOutFireIfExistsAt(Vector2Int tile) {
        DebugValidateTileIndex(tile);
        int i = tile.x, j = tile.y;
        if (tiles[i, j].fire != null) {
            ChangeTileTypeAt(tile, TileType.BURNT);
            Destroy(tiles[i, j].fire);
            tiles[i, j].fire = null;
            onFire.Remove(tile);
        }
    }

	public void GenerateGrid(TileType[,] tileTypes) {
        int width = tileTypes.GetLength(0);
        int height = tileTypes.GetLength(1);

        Debug.Log("Generating hex grid ("
            + width.ToString() + "x" + height.ToString() + ")");
		this.width = width;
		this.height = height;
		tiles = new TileInfo[width, height];

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				Vector3 pos = TileIndicesToPos(i, j);
				GameObject hex = Instantiate(hexPrefab,
                    pos, Quaternion.identity, transform);
				SpriteRenderer hexSprite = hex.GetComponent<SpriteRenderer>();
				TileType tileType = tileTypes[i, j];
				hexSprite.sprite = tileSprites[(int)tileType];

				tiles[i, j].type = tileType;
				tiles[i, j].gameObject = hex;
				tiles[i, j].spriteRenderer = hexSprite;

                tiles[i, j].unit = null;
				tiles[i, j].fire = null;
                if (tileType == TileType.DENSEFOREST) {
                    if (Random.Range(0.0f, 1.0f) < 0.05f) {
                        CreateFireAt(new Vector2Int(i, j));
                    }
                }
                if (tileType != TileType.WATER) {
                    if (Random.Range(0.0f, 1.0f) < 0.1f) {
                        // CreateUnitAt(new Vector2Int(i, j));
                    }
                }
			}
		}

        CreateUnitAt(new Vector2Int(11, 37));
        CreateUnitAt(new Vector2Int(12, 37));
        CreateUnitAt(new Vector2Int(12, 38));

        CreateFireAt(new Vector2Int(11, 31));
        CreateFireAt(new Vector2Int(40, 45));
        CreateFireAt(new Vector2Int(46, 41));
	}
	public struct TileNode {
		public Vector2Int coords;
		public float dist;
	}

	Vector2Int[] GetNeighbors(Vector2Int node) {
		Vector2Int[] result = new Vector2Int[6];
		int i = node.x;
		int j = node.y;
		result[0].x = i - 1;
		result[0].y = j;
		result[1].x = i + 1;
		result[1].y = j;
		result[2].x = i;
		result[2].y = j + 1;
		result[3].x = i;
		result[3].y = j - 1;
		if (j % 2 == 0) {
			result[4].x = i - 1;
			result[4].y = j - 1;
			result[5].x = i - 1;
			result[5].y = j + 1;
		} else {
			result[4].x = i + 1;
			result[4].y = j - 1;
			result[5].x = i + 1;
			result[5].y = j + 1;
		}

		if (i > 1 && i < (width - 1) && j > 1 && j < (height - 1)) {
			return result;
		}

		List<int> indices = new List<int>();
		for (int k = 0; k < 6; k++) {
			if (result[k].x < 0  || result[k].x >= width
				|| result[k].y < 0 || result[k].y >= height) {
				indices.Add(k);
			}
		}
		Vector2Int[] realResult = new Vector2Int[6 - indices.Count];
		int count = 0;
		for (int k = 0; k < 6; k++) {
			if (!indices.Contains(k)) {
				realResult[count] = result[k];
				count++;
			}
		}

		return realResult;
	}

	public List<TileNode> GetReachableTiles(Vector2Int start, float maxDist) {
		Hashtable visited = new Hashtable ();
		TileNode root;
		root.coords = start;
		root.dist = 0.0f;

		Queue<TileNode> queue = new Queue<TileNode>();
		queue.Enqueue(root);
		while (queue.Count > 0) {
			TileNode current = queue.Dequeue();
			if (current.dist <= maxDist) {
				if (!visited.ContainsKey(current.coords)) {
					// Not visited
					visited.Add(current.coords, current.dist);
				} else {
					// Already visited
					if (current.dist < (float)visited[current.coords]) {
						visited[current.coords] = current.dist;
					}
				}
				Vector2Int[] neighbors = GetNeighbors(current.coords);
				for (int i = 0; i < neighbors.Length; i++) {
					TileInfo info = tiles [neighbors[i].x, neighbors[i].y];
					float weight = GetTileWeight(info.type);
					if (weight != 0.0f) {
						TileNode newNode;
						newNode.coords = neighbors[i];
						newNode.dist = current.dist + weight;
						queue.Enqueue(newNode);
					}
				}
			}
		}

		List<TileNode> output = new List<TileNode>();
		foreach (DictionaryEntry entry in visited) {
			if ((Vector2Int)entry.Key == start) {
				continue;
			}
			TileNode node;
			node.coords = (Vector2Int)entry.Key;
			node.dist = (float)entry.Value;
			output.Add(node);
		}
		return output;
	}

    public IEnumerator ExecuteUnitCommands() {
        List<Vector2Int> oldUnitTiles = new List<Vector2Int>(unitTiles);
        foreach (Vector2Int tile in oldUnitTiles) {
            Vector2Int unitTile = tile;
            UnitScript unitScript = tiles[tile.x, tile.y].unitScript;
            foreach (UnitCommand cmd in unitScript.nextCommands) {
                if (cmd.type == UnitCommandType.MOVE) {
                    MoveUnit(unitTile, cmd.target, 0.0f);
                    unitTile = cmd.target;
                }
                else if (cmd.type == UnitCommandType.DIG) {
                    MoveUnit(unitTile, cmd.target, 0.0f);
                    unitTile = cmd.target;
                    ChangeTileTypeAt(cmd.target, TileType.FIRELINE);
                }

                yield return new WaitForSeconds(0.3f);
            }
            
            foreach (UnitCommand cmd in unitScript.nextCommands) {
                unitScript.commands.Remove(cmd);
            }
            unitScript.UpdateCommandList();
        }

        turnScript.doneWithUnits = true;
    }

    public void AgeFire() {
        List<Vector2Int> firesToRemove = new List<Vector2Int>();
        foreach (Vector2Int tile in onFire) {
            int i = tile.x, j = tile.y;
            tiles[i, j].fireScript.life -= 1;
            if (tiles[i, j].fireScript.life <= 0) {
                firesToRemove.Add(tile);
            }
        }

        foreach (Vector2Int tile in firesToRemove) {
            PutOutFireIfExistsAt(tile);
        }
    }

    public void SpreadFire() {
        List<Vector2Int> onFirePrev = new List<Vector2Int>(onFire);
        foreach (Vector2Int tile in onFirePrev) {
            Vector2Int[] neighbors = GetNeighbors(tile);
            for (int i = 0; i < neighbors.Length; i++) {
                TileInfo neighborInfo = tiles[neighbors[i].x, neighbors[i].y];
                if (neighborInfo.fire == null
                && neighborInfo.type != TileType.WATER
				&& neighborInfo.type != TileType.FIRELINE
                && neighborInfo.type != TileType.BURNT) {
                    if (Random.Range(0.0f, 1.0f) < fireSpreadChance) {
                        CreateFireAt(neighbors[i]);
                    }
                }
            }
        }

        List<Vector2Int> deadUnitTiles = new List<Vector2Int>();
        foreach (Vector2Int tile in unitTiles) {
            if (tiles[tile.x, tile.y].fire != null) {
                deadUnitTiles.Add(tile);
            }
        }

        foreach (Vector2Int tile in deadUnitTiles) {
            KillUnitAt(tile);
            deathSplat.SetTrigger("splat");
        }
    }

	// Update is called once per frame
	void Update() {
		// Clear all tile colors.
        // If you want a tile to stay colored, you must set it every frame
		ClearTileColors();

		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Vector2Int hovered = GetClosestTileIndex(mousePos);

        if (turnScript.playerTurn) {
            // Click event
            if (Input.GetMouseButtonDown(0)) {
                /*Debug.Log("You clicked on: " + hovered.ToString());
                Debug.Log ("Type: "
                    + tiles [hovered.x, hovered.y].type.ToString ());*/

                // Did the player try to move a unit?
                if (selected != noSelection
                && tiles[selected.x, selected.y].unit != null
                && neighbors != null) {
                    // Check if selection is in neighbors
                    int ind = GetIndexInNeighbors(hovered);
                    if (ind != -1) {
                        // Determine whether the unit can move to the target tile
                        if (tiles[hovered.x, hovered.y].unit == null
                        && tiles[hovered.x, hovered.y].fire == null) {
                            // Move the unit
                            //MoveUnit(selected, hovered, neighbors[ind].dist);
                        }
                    }
                }

                // Conditions for selection to be possible
                TileInfo hoveredTile = tiles[hovered.x, hovered.y];
                if (selected != hovered
                && hoveredTile.type != TileType.WATER
                && hoveredTile.unit != null) {
                    // Select hovered tile
                    selected = hovered;
                    TileInfo selectedTile = tiles[selected.x, selected.y];
                    float range = selectedTile.unitScript.rangeRemaining;
					// TODO temporary
					range -= 1.0f;
                    if (range > 0.0f) {
                        neighbors = GetReachableTiles(selected, range);
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
                && tiles[selected.x, selected.y].unit != null
                && neighbors != null) {
                    // Determine whether the unit can move to the target tile
                    TileInfo unitTile = tiles[selected.x, selected.y];
                    TileInfo targetTile = tiles[hovered.x, hovered.y];
                    if (targetTile.unit == null
                    && targetTile.fire == null
                    && targetTile.type != TileType.WATER) {
                        UnitCommand digCommand;
                        digCommand.type = UnitCommandType.DIG;
                        digCommand.target = hovered;
						bool exists = false;
						foreach (Vector2Int ut in unitTiles) {
							foreach (UnitCommand cmd in tiles[ut.x, ut.y].unitScript.commands) {
								if (cmd.type == digCommand.type && cmd.target == digCommand.target) {
									exists = true;
									break;
								}
							}
						}
						if (!exists) {
							unitTile.unitScript.AddCommandIfNew (digCommand);
						}
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                // Did the player try to clear the selected unit's commands?
                if (selected != noSelection
                && tiles[selected.x, selected.y].unit) {
                    tiles[selected.x, selected.y].unitScript.ClearCommands();
                }
            }

            // Old dig method
            /*if (Input.GetKeyDown(KeyCode.D)) {
                if (selected != noSelection) {
                    TileInfo selectedTile = tiles [selected.x, selected.y];
                    if (selectedTile.unit != null
                    && selectedTile.type != TileType.FIRELINE) {
                        ChangeTileTypeAt(selected, TileType.FIRELINE);
                        // TODO fix: you can dig after using all your movement
                        float range = tiles[selected.x, selected.y]
                            .unitScript.rangeRemaining;
                        if (range >= 1.0f) {
                            tiles[selected.x, selected.y]
                                .unitScript.rangeRemaining -= 1.0f;
                            neighbors = GetReachableTiles(selected, range - 1.0f);
                        }
                    }
                }
            }*/

            // Paint all tiles we want to paint, in order
            if (neighbors != null) {
                foreach (TileNode tile in neighbors) {
                    SetTileColor(tile.coords, neighborsColor);
                }
            }
        }

        foreach (Vector2Int unitTile in unitTiles) {
            UnitScript unitScript = tiles[unitTile.x, unitTile.y].unitScript;
			LineRenderer unitLine = tiles[unitTile.x, unitTile.y].unit.GetComponent<LineRenderer>();
			List<Vector3> positions = new List<Vector3>();
			positions.Add(unitScript.transform.position);
            foreach (UnitCommand cmd in unitScript.commands) {
				Vector3 targetPos = TileIndicesToPos(cmd.target.x, cmd.target.y);
				targetPos.z = -1.0f;
				positions.Add(targetPos);
                SetTileColor(cmd.target, digLaterColor);
            }
			unitLine.positionCount = positions.Count;
			unitLine.SetPositions(positions.ToArray());
            foreach (UnitCommand cmd in unitScript.nextCommands) {
                SetTileColor(cmd.target, digNextColor);
            }
        }
        if (selected != noSelection
        && tiles[selected.x, selected.y].unit != null) {
            UnitScript unitScript = tiles[selected.x, selected.y].unitScript;
            foreach (UnitCommand cmd in unitScript.commands) {
                SetTileColor(cmd.target, digLaterFocusColor);
            }
            foreach (UnitCommand cmd in unitScript.nextCommands) {
                SetTileColor(cmd.target, digNextFocusColor);
            }
        }
		MultiplyTileColor(hovered, hoveredColor);
		if (selected != noSelection) {
			SetTileColor(selected, selectedColor);
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			// TODO janky for now
			// Deselect everything
			selected = noSelection;
			neighbors = null;
		}
	}
}
