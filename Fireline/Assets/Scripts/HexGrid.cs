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
				float xStride = Mathf.Sqrt(3.0f) / 2.0f;
                float xOff = 0.0f;
                if (j % 2 == 1) {
                    xOff += Mathf.Sqrt(3.0f) / 4.0f;
                }
				float yStride = 3.0f / 4.0f;
                float yOff = 0.0f;
				Vector3 pos = new Vector3(
                    xOff + i * xStride, yStride * j, 0.0f);
				GameObject hex = Instantiate(hexagon, pos, Quaternion.identity);
                SpriteRenderer hexSprite = hex.GetComponent<SpriteRenderer>();
                hexSprite.color = Random.ColorHSV();
			}
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
	}
}
