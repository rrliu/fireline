using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorScript : MonoBehaviour {

	public Vector2Int start;
	public int radius;

	public Sprite rivers;
	public Sprite trees;

	Vector2 TileIndicesToPos(int i, int j) {
		float xStride = Mathf.Sqrt(3.0f) / 2.0f;
		float xOff = 0.0f;
		if (j % 2 == 1) {
			xOff += Mathf.Sqrt(3.0f) / 4.0f;
		}
		float yStride = 3.0f / 4.0f;
		return new Vector2(xOff + i * xStride, j * yStride);
	}

	bool[,] GenerateTiles(Sprite sprite, Vector2Int start, int radius) {
		int width = sprite.texture.width;
		int height = sprite.texture.height;
		float xStride = Mathf.Sqrt(3.0f) * radius;
		float yStride = 3.0f / 2.0f * radius;
		float xOff = xStride / 2.0f;

		int searchRadius = radius;
		int numX = (int)((width - start.x - xOff) / xStride);
		int numY = (int)((height - start.y) / yStride);
		bool[,] result = new bool[numX - 1, numY - 1];

		for (int i = 0; i < numX - 1; i++) {
			for (int j = 0; j < numY - 1; j++) {
				int iPix = (int)(start.x + xStride * i);
				if (j % 2 == 1) {
					iPix += (int)xOff;
				}
				int jPix = (int)(start.y + yStride * j);
				int count = 0;
				int total = 0;
				for (int it = -searchRadius/2; it <= searchRadius/2; it++) {
					for (int jr = -searchRadius/2; jr <= searchRadius/2; jr++) {
						if ((it * it + jr * jr) <= searchRadius) {
							total++;
							Color pixColor = sprite.texture.GetPixel(iPix + it, jPix + jr);
							if (pixColor.a > 0.0f) {
								count++;
							}
						}
					}
				}
				float avg = (float)count / total;
				result [i, j] = false;
				if (avg > 0.0f) {
					result [i, j] = true;
				}
			}
		}

		return result;
	}

	// Use this for initialization
	void Start () {
		bool[,] isWater = GenerateTiles (rivers, start, radius);
		int width = isWater.GetLength (0);
		int height = isWater.GetLength (1);

		TileType[,] tileTypes = new TileType[width, height];
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (isWater [i, j]) {
					tileTypes [i, j] = TileType.WATER;
				} else {
					tileTypes [i, j] = TileType.GRASSLAND;
				}
			}
		}

		HexGrid hexGrid = gameObject.GetComponent<HexGrid> ();
		hexGrid.GenerateGrid (width, height, tileTypes);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
