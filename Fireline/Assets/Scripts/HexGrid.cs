using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
	public GameObject hexagon;
	public int width;
	public int height;

	// Use this for initialization
	void Start ()
	{
		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				float x = Mathf.Sqrt(3.0f) / 4.0f;
				float y = 3.0f / 4.0f;
				Vector3 pos = new Vector3(i * x, y * j, 0.0f);
				Instantiate(hexagon, pos, Quaternion.identity);
			}
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
	}
}
