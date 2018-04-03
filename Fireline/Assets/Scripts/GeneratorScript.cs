using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorScript : MonoBehaviour {
	public Vector2Int start;
	public int radius;

	public Sprite rivers;
	//public Sprite trees;
	public GameObject background;

	Vector2 TileIndicesToPos(int i, int j) {
		float xStride = Mathf.Sqrt(3.0f) / 2.0f;
		float xOff = 0.0f;
		if (j % 2 == 1) {
			xOff += Mathf.Sqrt(3.0f) / 4.0f;
		}
		float yStride = 3.0f / 4.0f;
		return new Vector2(xOff + i * xStride, j * yStride);
	}

	bool[,] GenerateTiles(Sprite sprite, Vector2Int start,
    int radius, float avgThreshold) {
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
					iPix = (int)(start.x + xOff + xStride * i);
				}
				int jPix = (int)(start.y + yStride * j);
				int count = 0;
				int total = 0;
				for (int it = -searchRadius; it <= searchRadius; it++) {
					for (int jr = -searchRadius; jr <= searchRadius; jr++) {
						if ((it * it + jr * jr)
                        <= searchRadius * searchRadius) {
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
				if (avg > avgThreshold) {
					result [i, j] = true;
				}
			}
		}

		return result;
	}

	// Use this for initialization
	void Start () {
		bool[,] isWater = GenerateTiles(rivers, start, radius, 0.1f);
		int width = isWater.GetLength(0);
		int height = isWater.GetLength(1);

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

        int pixWidth = rivers.texture.width;
        int pixHeight = rivers.texture.height;
        //float error = 1.03f;
        //error = 1.0f;
        Vector3 backgroundScale = new Vector3(
            //2.0f * radius / pixHeight * error,
            //2.0f * radius / pixHeight * error,
            1.0f / (2.0f * radius),
            1.0f / (2.0f * radius),
            1.0f
        );
        Vector3 backgroundPos = new Vector3(
            -start.x * backgroundScale.x,
            -start.y * backgroundScale.y,
            0.0f
        );
        backgroundPos.x += pixWidth * backgroundScale.x / 2.0f;
        backgroundPos.y += pixHeight * backgroundScale.y / 2.0f;
        background.transform.position = backgroundPos;
        background.transform.localScale = backgroundScale;
        background.SetActive(true);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
