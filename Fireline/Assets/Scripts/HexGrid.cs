using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour {
	public GameObject hexagon;
	public int width;
	public int height;

	public float noiseScale;

	public Sprite[] tileSprites;

	[HideInInspector] public GameObject[,] tiles;

	Vector2Int selected;

	enum TileType {
		GRASSLAND,
		FOREST,
		DENSEFOREST,
		BURNT,
		WATER
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
		if (height < 0.1f) {
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
		tiles = new GameObject[width, height];
		
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
                float height = Mathf.PerlinNoise(xNorm * noiseScale, yNorm * noiseScale);
                TileType tileType = HeightToTile(height);
				hexSprite.sprite = tileSprites[(int)tileType];

				tiles[i, j] = hex;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

		GameObject selectedTile = tiles[selected.x, selected.y];
		SpriteRenderer sr = selectedTile.GetComponent<SpriteRenderer>();
		Color color = sr.color;
		color.a = 1.0f;
		sr.color = color;

		selected = GetClosestTileIndex(mousePos);
		Debug.Log (selected);


		selectedTile = tiles[selected.x, selected.y];
		sr = selectedTile.GetComponent<SpriteRenderer>();
		color = sr.color;
		color.a = 0.5f;
		sr.color = color;
	}
}
