using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum TileType {
	GRASSLAND,
	FOREST,
	DENSEFOREST,
	BURNT,
	WATER,
    NONE
}

public enum UnitType {
    TEST,
    NONE
}

public class HexGrid : MonoBehaviour {
	public GameObject hexagon;
	public GameObject fire;
    public float fireSpreadChance;
	public GameObject[] units;
	public int width;
	public int height;

	public float noiseScale;

	public Sprite[] tileSprites;

	public struct TileInfo {
		public TileType type;
		public GameObject gameObject;
		public SpriteRenderer spriteRenderer;
        public UnitType unitType;
		public UnitScript unitScript;
        public GameObject unit;
        public bool onFire;
	};

	[HideInInspector] public TileInfo[,] tiles;

    List<Vector2Int> onFire = new List<Vector2Int>();

	Vector2Int noSelection = new Vector2Int(-1, -1);
	Vector2Int hovered;
	Vector2Int selected;
	List<TileNode> neighbors = null;

	void ClearTileColor(Vector2Int tile) {
		tiles[tile.x, tile.y].spriteRenderer.color = Color.white;
	}

	void SetTileColor(Vector2Int tile, Color color) {
		tiles[tile.x, tile.y].spriteRenderer.color = color;
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

	TileType HeightToTile(float height) {
		if (height < 0.3f) {
			return TileType.WATER;
		}
		if (height < 0.4f) {
			return TileType.GRASSLAND;
		}
		if (height < 0.7f) {
			return TileType.FOREST;
		}

		return TileType.DENSEFOREST;
	}

	float GetTileWeight(TileType type) {
		if (type == TileType.WATER) {
			return 0.0f;
		}
		if (type == TileType.GRASSLAND) {
			return 1.0f;
		}
		if (type == TileType.FOREST) {
			return 1.5f;
		}
		if (type == TileType.DENSEFOREST) {
			return 2.0f;
		}

		return 0.0f;
	}

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
		selected = noSelection;
	}

    void CreateUnitAt(Vector2Int tile) {
        int i = tile.x, j = tile.y;
		GameObject unit = Instantiate(units[0],
			tiles[i, j].gameObject.transform.position,
			Quaternion.identity, transform);
		tiles[i, j].unitType = UnitType.TEST;
		tiles[i, j].unit = unit;
		tiles [i, j].unitScript = unit.GetComponent<UnitScript>();
    }

    void CreateFireAt(Vector2Int tile) {
        int i = tile.x, j = tile.y;
		Instantiate(fire,
			tiles[i, j].gameObject.transform.position,
			Quaternion.identity, transform);
        onFire.Add(tile);
        tiles[i, j].onFire = true;
    }

	public void GenerateGrid(TileType[,] tileTypes) {
        int width = tileTypes.GetLength(0);
        int height = tileTypes.GetLength(1);

        Debug.Log("Generating hex grid ("
            + width.ToString() + "x" + height.ToString() + ")");
		this.width = width;
		this.height = height;
		tiles = new TileInfo[width, height];

		/*float xMin = TileIndicesToPos(0, 0).x;
		float xMax = TileIndicesToPos(width - 1, 1).x;
		float yMin = TileIndicesToPos(0, 0).y;
		float yMax = TileIndicesToPos(0, height - 1).y;*/
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				Vector3 pos = TileIndicesToPos(i, j);
				GameObject hex = Instantiate(hexagon,
                    pos, Quaternion.identity, transform);
				SpriteRenderer hexSprite = hex.GetComponent<SpriteRenderer>();
				//float xNorm = pos.x / (xMax - xMin) + xMin;
				//float yNorm = pos.y / (yMax - yMin) + yMin; // TODO bad!
				//float h = Mathf.PerlinNoise(xNorm * noiseScale, yNorm * noiseScale);
				//TileType tileType = HeightToTile(h);
				TileType tileType = tileTypes[i, j];
				hexSprite.sprite = tileSprites[(int)tileType];

				tiles[i, j].type = tileType;
				tiles[i, j].gameObject = hex;
				tiles[i, j].spriteRenderer = hexSprite;

                tiles[i, j].unitType = UnitType.NONE;
                tiles[i, j].unit = null;
				tiles[i, j].unitScript = null;

				tiles[i, j].onFire = false;
                if (tileType == TileType.DENSEFOREST) {
                    if (Random.Range(0.0f, 1.0f) < 0.05f) {
                        CreateFireAt(new Vector2Int(i, j));
                    }
                }
                /*if (tileType != TileType.WATER) {
                    if (Random.Range(0.0f, 1.0f) < 0.1f) {
                        CreateUnitAt(new Vector2Int(i, j));
                    }
                }*/
			}
		}

        CreateUnitAt(new Vector2Int(11, 37));
        CreateUnitAt(new Vector2Int(12, 37));
        CreateUnitAt(new Vector2Int(12, 38));

        CreateFireAt(new Vector2Int(11, 31));
        CreateFireAt(new Vector2Int(40, 45));
        CreateFireAt(new Vector2Int(46, 41));
	}
	
	// Update is called once per frame
	void Update () {
		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		if (hovered != selected) {
			ClearTileColor (hovered);
		}

		hovered = GetClosestTileIndex(mousePos);

		if (Input.GetMouseButtonDown (0)) {
			Debug.Log("You clicked on: " + hovered.ToString());
			if (selected != noSelection) {
				ClearTileColor(selected);
				if (neighbors != null) {
					foreach (TileNode tile in neighbors) {
						ClearTileColor (tile.coords);
					}
				}
			}

			if (neighbors != null) {
			    // Check if selection is in neighbors
				bool isNeighbor = false;
				int ind = -1;
				for (int i = 0; i < neighbors.Count; i++) {
					if (neighbors[i].coords == hovered) {
						isNeighbor = true;
						ind = i;
						break;
					}
				}
				if (isNeighbor) {
                    // Determine whether the unit can move to the target tile
					TileInfo unitTile = tiles [selected.x, selected.y];
					Debug.Assert (unitTile.unitType != UnitType.NONE);
					TileInfo targetTile = tiles [hovered.x, hovered.y];
					if (targetTile.unitType == UnitType.NONE
                    && !targetTile.onFire) {
						unitTile.unitScript.rangeRemaining -= neighbors [ind].dist;
						unitTile.unit.transform.position = targetTile.gameObject.transform.position;
						tiles [hovered.x, hovered.y].unitType = unitTile.unitType;
						tiles [hovered.x, hovered.y].unitScript = unitTile.unitScript;
						tiles [hovered.x, hovered.y].unit = unitTile.unit;
						tiles [selected.x, selected.y].unitType = UnitType.NONE;
						tiles [selected.x, selected.y].unitScript = null;
						tiles [selected.x, selected.y].unit = null;
					}
				}
			}

            // Conditions for selection to be possible
            TileInfo hoveredTile = tiles[hovered.x, hovered.y];
            if (selected != hovered
            && hoveredTile.type != TileType.WATER
            && hoveredTile.unitType != UnitType.NONE) {
				selected = hovered;
			} else {
				// Clear selection
				selected = noSelection;
                if (neighbors != null) {
				    neighbors.Clear();
                }
			}

			if (selected != noSelection) {
				TileInfo unitTile = tiles [selected.x, selected.y];
				float range = unitTile.unitScript.rangeRemaining;
				if (range > 0.0f) {
					neighbors = GetReachableTiles (selected, range);
				} else {
					neighbors = null;
				}

				SetTileColor(selected, Color.cyan);
			}
		}

		if (neighbors != null) {
			foreach (TileNode tile in neighbors) {
				SetTileColor (tile.coords, Color.magenta);
			}
		}

		if (hovered != selected) {
			if (tiles [hovered.x, hovered.y].type == TileType.WATER) {
				SetTileColor (hovered, Color.red);
			} else {
				SetTileColor (hovered, Color.gray);
			}
		}
	}

	public struct TileNode {
		public Vector2Int coords;
		public float dist;
	}

	Vector2Int[] GetNeighbors(Vector2Int node) {
		Vector2Int[] result = new Vector2Int[6];
		int i = node.x;
		int j = node.y;
		result [0].x = i - 1;
		result [0].y = j;
		result [1].x = i + 1;
		result [1].y = j;
		result [2].x = i;
		result [2].y = j + 1;
		result [3].x = i;
		result [3].y = j - 1;
		if (j % 2 == 0) {
			result [4].x = i - 1;
			result [4].y = j - 1;
			result [5].x = i - 1;
			result [5].y = j + 1;
		} else {
			result [4].x = i + 1;
			result [4].y = j - 1;
			result [5].x = i + 1;
			result [5].y = j + 1;
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
		List<TileNode> output = new List<TileNode>();
		TileNode root;
		root.coords = start;
		root.dist = 0.0f;

		Queue<TileNode> queue = new Queue<TileNode>();
		queue.Enqueue(root);
		while (queue.Count > 0) {
			TileNode current = queue.Dequeue();
			if (!visited.ContainsKey (current.coords)) {
				visited.Add(current.coords, true);
				if (current.dist <= maxDist) {
				    Vector2Int[] neighbors = GetNeighbors(current.coords);
					if (current.coords != start) {
						// TODO there could be a shorter path...
						output.Add (current);
					}
					for (int i = 0; i < neighbors.Length; i++) {
						TileInfo info = tiles[neighbors[i].x, neighbors[i].y];
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
		}

		return output;
	}

    public void SpreadFire() {
        List<Vector2Int> onFirePrev = new List<Vector2Int>(onFire);
        foreach (Vector2Int tile in onFirePrev) {
            Vector2Int[] neighbors = GetNeighbors(tile);
            for (int i = 0; i < neighbors.Length; i++) {
                TileInfo neighborInfo = tiles[neighbors[i].x, neighbors[i].y];
                if (!neighborInfo.onFire
                && neighborInfo.type != TileType.WATER) {
                    if (Random.Range(0.0f, 1.0f) < fireSpreadChance) {
                        CreateFireAt(neighbors[i]);
                    }
                }
            }
        }
    }
}
