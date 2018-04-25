﻿using System.Collections;
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

	public bool disabled;
};

public struct TileNode {
    public Vector2Int coords;
    public float dist;
}

[RequireComponent(typeof(TurnScript))]
public class HexGrid : MonoBehaviour {
	public GameObject hexPrefab;
	public GameObject firePrefab;
    public float fireSpreadChance;
	public Sprite[] tileSprites;
	public GameObject[] unitPrefabs;

	public Vector2Int enabledMin;
	public Vector2Int enabledSize;

    public Animator deathSplat;

	[HideInInspector] public TileInfo[,] tiles;
    int width, height;
    [HideInInspector] public List<Vector2Int> unitTiles = new List<Vector2Int>();
    [HideInInspector] public List<Vector2Int> onFire = new List<Vector2Int>();

    TurnScript turnScript;

    public void DebugValidateTileIndex(Vector2Int tileInd) {
        Debug.Assert(0 <= tileInd.x && tileInd.x < width
            && 0 <= tileInd.y && tileInd.y < height);
    }

	public void ClearTileColors() {
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				Color color = Color.white;
				if (tiles [i, j].disabled) {
					color = Color.gray;
				}
				tiles[i, j].spriteRenderer.color = color;
			}
		}
	}

	public void SetTileColor(Vector2Int tile, Color color) {
		DebugValidateTileIndex(tile);
		tiles[tile.x, tile.y].spriteRenderer.color = color;
	}

	public void MultiplyTileColor(Vector2Int tile, Color color) {
		DebugValidateTileIndex(tile);
		tiles[tile.x, tile.y].spriteRenderer.color *= color;
	}

    public Vector2Int ToTileIndex2D(int ind) {
        return new Vector2Int(
            ind % width,
            ind / width
        );
    }

    public int ToTileIndex1D(int i, int j) {
        DebugValidateTileIndex(new Vector2Int(i, j));
        return j * width + i;
    }
    public int ToTileIndex1D(Vector2Int tile) {
        DebugValidateTileIndex(tile);
        return ToTileIndex1D(tile.x, tile.y);
    }

	public Vector2 TileIndicesToPos(int i, int j) {
		float xStride = Mathf.Sqrt(3.0f) / 2.0f;
		float xOff = 0.0f;
		if (j % 2 == 1) {
			xOff += xStride / 2.0f;
		}
		float yStride = 3.0f / 4.0f;
		return new Vector2(xOff + i * xStride, j * yStride);
	}

    float GetTileMoveWeight(Vector2Int tile) {
        TileInfo tileInfo = tiles[tile.x, tile.y];
        if (tileInfo.disabled) {
            return 0.0f;
        }
        /*if (tileInfo.fire != null) {
            return 0.0f;
        }*/
        TileType type = tileInfo.type;
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
	public Vector2Int GetClosestTileIndex(Vector2 position) {
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

    // From unit from one tile to another
    public void MoveUnit(Vector2Int from, Vector2Int to) {
        DebugValidateTileIndex(from);
        DebugValidateTileIndex(to);
        if (to == from) {
            return;
        }
        Debug.Assert(tiles[from.x, from.y].unit != null);
        Debug.Assert(tiles[to.x, to.y].unit == null);
        // Move the unit
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
		deathSplat.SetTrigger("splat");
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
				tiles [i, j].disabled = true;
				if (enabledMin.x <= i && i < enabledMin.x + enabledSize.x
				&& enabledMin.y <= j && j < enabledMin.y + enabledSize.y) {
					tiles [i, j].disabled = false;
				}
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

    // Returns shortest path from src to dst (not including src)
	public List<TileNode> GetShortestPath(Vector2Int src, Vector2Int dst) {
        float[,] distance = new float[width, height];
        Vector2Int[,] prev = new Vector2Int[width, height];
		IndexMinPQ<float> minPQ = new IndexMinPQ<float>(width * height);
        distance[src.x, src.y] = 0.0f;
        prev[src.x, src.y] = src;
        minPQ.Insert(ToTileIndex1D(src), 0.0f);
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (i == src.x && j == src.y) {
                    continue;
                }

                distance[i, j] = float.PositiveInfinity;
                prev[i, j] = new Vector2Int(-1, -1);
                minPQ.Insert(ToTileIndex1D(i, j), float.PositiveInfinity);
            }
        }

        while (!minPQ.IsEmpty) {
            float dist = minPQ.MinKey;
            Vector2Int min = ToTileIndex2D(minPQ.DelMin());
            Vector2Int[] neighbors = GetNeighbors(min);
            foreach (Vector2Int neighbor in neighbors) {
                int neighborInd = ToTileIndex1D(neighbor);
                float edgeDist = GetTileMoveWeight(neighbor);
                if (minPQ.Contains(neighborInd) && edgeDist != 0.0f) {
                    float currentDist = minPQ.KeyOf(neighborInd);
                    if (dist + edgeDist < currentDist) {
                        distance[neighbor.x, neighbor.y] = dist + edgeDist;
                        prev[neighbor.x, neighbor.y] = min;
                        minPQ.DecreaseKey(neighborInd, dist + edgeDist);
                    }
                }
            }
        }

        if (distance[dst.x, dst.y] == float.PositiveInfinity) {
            return null;
        }
        List<TileNode> path = new List<TileNode>();
        TileNode dstNode;
        dstNode.coords = dst;
        dstNode.dist = distance[dst.x, dst.y];
        path.Add(dstNode);
        Vector2Int tile = dst;
        while (prev[tile.x, tile.y] != src) {
            tile = prev[tile.x, tile.y];
            TileNode node;
            node.coords = tile;
            node.dist = distance[tile.x, tile.y];
            path.Add(node);
        }
        path.Reverse();
		return path;
	}
	public List<TileNode> GetReachableTiles(Vector2Int start, float maxDist) {
		Hashtable visited = new Hashtable ();
		Queue<TileNode> queue = new Queue<TileNode>();
        TileNode root;
        root.coords = start;
        root.dist = 0.0f;
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
					float weight = GetTileMoveWeight(neighbors[i]);
					if (weight != 0.0f) {
                        TileNode node;
                        node.coords = neighbors[i];
                        node.dist = current.dist + weight;
						queue.Enqueue(node);
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
            UnitScript unitScript = tiles[tile.x, tile.y].unitScript;
            unitScript.ExecuteCommands();
            yield return new WaitForSeconds(0.5f);
        }

        turnScript.doneWithUnits = true;
    }
    public void UpdateUnitCommands() {
        foreach (Vector2Int tile in unitTiles) {
            tiles[tile.x, tile.y].unitScript.UpdateFullCommands();
        }
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
        }
	}

	// Use this for initialization
	void Start () {
		turnScript = GetComponent<TurnScript>();
	}

	// Update is called once per frame
	void Update() {
	}
}
