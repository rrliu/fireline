using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
	public GameObject hexagon;
	public int width;
	public int height;

	public float noiseScale;

	public Sprite[] tileSprites;

	TileType tileType;

	enum TileType {
		GRASSLAND,
		FOREST,
		DENSEFOREST,
		BURNT,
		WATER
	}

	Vector2 TileIndicesToPos(int i, int j)
	{
		float xStride = Mathf.Sqrt(3.0f) / 2.0f;
		float xOff = 0.0f;
		if (j % 2 == 1) {
			xOff += Mathf.Sqrt(3.0f) / 4.0f;
		}
		float yStride = 3.0f / 4.0f;
		float yOff = 0.0f;
		return new Vector2(xOff + i * xStride, yOff + j * yStride);
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

	// Use this for initialization
	void Start ()
	{
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
                tileType = HeightToTile(height);
				hexSprite.sprite = tileSprites[(int)tileType];
			}
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
	}
}
