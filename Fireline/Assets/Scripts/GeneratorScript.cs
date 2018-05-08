﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class GeneratorScript : MonoBehaviour {
	public Vector2Int start;
	public int radius;

	public Sprite rivers;
    public float riverThreshold;
	public Sprite forests;
    public float forestThreshold;
	public Sprite cities;
    public float cityThreshold;
	public GameObject background;

    public bool cache;

	bool[,] GenerateSingleTileMask(Sprite sprite, Vector2Int start,
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

    TileType[,] GenerateTiles() {
		bool[,] isWater = GenerateSingleTileMask(rivers,
            start, radius, riverThreshold);
		bool[,] isForest = GenerateSingleTileMask(forests,
            start, radius, forestThreshold);
		bool[,] isCity = GenerateSingleTileMask(cities, 
            start, radius, cityThreshold);
		int width = isWater.GetLength(0);
		int height = isWater.GetLength(1);
        Debug.Assert(width == isForest.GetLength(0));
        Debug.Assert(width == isCity.GetLength(0));
        Debug.Assert(height == isForest.GetLength(1));
        Debug.Assert(height == isCity.GetLength(1));

		TileType[,] tileTypes = new TileType[width, height];
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (isWater[i, j]) {
					tileTypes[i, j] = TileType.WATER;
                } else if (isCity[i, j]) {
					tileTypes[i, j] = TileType.CITY;
				} else if (isForest[i, j]) {
					tileTypes[i, j] = TileType.FOREST;
				} else {
					tileTypes[i, j] = TileType.GRASSLAND;
				}
			}
		}

        return tileTypes;
    }

    const string CACHE_PATH = "Assets/SaveData/tiles.data";
    void CacheTiles(TileType[,] tileTypes) {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = File.Open(CACHE_PATH, FileMode.Create);
        bf.Serialize(fs, tileTypes);
        fs.Close();
        Debug.Log("Cached tile info");
    }
    TileType[,] LoadTiles() {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = File.Open(CACHE_PATH, FileMode.Open);
        TileType[,] tileTypes = (TileType[,])bf.Deserialize(fs);
        fs.Close();

        return tileTypes;
    }

	// Use this for initialization
	void Start () {
        TileType[,] tileTypes;
        if (cache && File.Exists(CACHE_PATH)) {
            tileTypes = LoadTiles();
            Debug.Log("Loaded tile info from cache");
        }
        else {
            tileTypes = GenerateTiles();
            Debug.Log("Generated tile info");
        }
        CacheTiles(tileTypes);

		HexGrid hexGrid = gameObject.GetComponent<HexGrid>();
		hexGrid.GenerateGrid (tileTypes);

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
