using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType {
	GRASSLAND,
	FOREST,
	DENSEFOREST,
	BURNT,
	WATER
}

public class HexGrid : MonoBehaviour {
	public GameObject hexagon;
	public int width;
	public int height;

	public float noiseScale;

	public Sprite[] tileSprites;

	public struct TileInfo {
		public TileType type;
		public GameObject gameObject;
		public SpriteRenderer spriteRenderer;
	};

	[HideInInspector] public TileInfo[,] tiles;

	Vector2Int noSelection = new Vector2Int(-1, -1);
	Vector2Int hovered;
	Vector2Int selected;
	List<Vector2Int> neighbors = null;

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
			xOff += Mathf.Sqrt(3.0f) / 4.0f;
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

		//GenerateGrid (width, height, null);
	}

	public void GenerateGrid(int width, int height, TileType[,] tileTypes) {
		this.width = width;
		this.height = height;
		tiles = new TileInfo[width, height];

		float xMin = TileIndicesToPos(0, 0).x;
		float xMax = TileIndicesToPos(width - 1, 1).x;
		float yMin = TileIndicesToPos(0, 0).y;
		float yMax = TileIndicesToPos(0, height - 1).y;
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				Vector3 pos = TileIndicesToPos(i, j);
				GameObject hex = Instantiate(hexagon, pos, Quaternion.identity, transform);
				SpriteRenderer hexSprite = hex.GetComponent<SpriteRenderer>();
				float xNorm = pos.x / (xMax - xMin) + xMin;
				float yNorm = pos.y / (yMax - yMin) + yMin; // TODO bad!
				//float h = Mathf.PerlinNoise(xNorm * noiseScale, yNorm * noiseScale);
				//TileType tileType = HeightToTile(h);
				TileType tileType = tileTypes[i, j];
				hexSprite.sprite = tileSprites[(int)tileType];

				tiles[i, j].type = tileType;
				tiles[i, j].gameObject = hex;
				tiles[i, j].spriteRenderer = hexSprite;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		if (hovered != selected) {
			ClearTileColor (hovered);
		}

		hovered = GetClosestTileIndex(mousePos);

		if (Input.GetMouseButtonDown (0)) {
			if (selected != noSelection) {
				ClearTileColor(selected);
				if (neighbors != null) {
					foreach (Vector2Int tile in neighbors) {
						ClearTileColor (tile);
					}
				}
			}

			if (selected == hovered || tiles[hovered.x, hovered.y].type == TileType.WATER) {
				// Clear selection
				selected = noSelection;
				neighbors.Clear();
			} else {
				selected = hovered;
			}

			if (selected != noSelection) {
				neighbors = GetReachableTiles(selected, 4.0f);

				SetTileColor(selected, Color.cyan);
			}
		}

		if (neighbors != null) {
			foreach (Vector2Int tile in neighbors) {
				SetTileColor (tile, Color.magenta);
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

	struct TileSearchInfo {
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

	public List<Vector2Int> GetReachableTiles(Vector2Int start, float maxDist) {
		Hashtable visited = new Hashtable ();
		List<Vector2Int> output = new List<Vector2Int>();
		TileSearchInfo root;
		root.coords = start;
		root.dist = 0.0f;

		Queue<TileSearchInfo> queue = new Queue<TileSearchInfo>();
		queue.Enqueue(root);
		while (queue.Count > 0) {
			TileSearchInfo current = queue.Dequeue();
			if (!visited.ContainsKey (current.coords)) {
				visited.Add(current.coords, true);
				Vector2Int[] neighbors = GetNeighbors (current.coords);
				if (current.dist <= maxDist) {
					if (current.coords != start) {
						output.Add (current.coords);
					}
					for (int i = 0; i < neighbors.Length; i++) {
						TileInfo info = tiles[neighbors[i].x, neighbors[i].y];
						float weight = GetTileWeight(info.type);
						if (weight != 0.0f) {
							TileSearchInfo newNode;
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
}
