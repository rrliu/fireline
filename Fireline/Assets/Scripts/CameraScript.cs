using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScript : MonoBehaviour {
    public float margin;
    public float maxSpeed;
    // at which multiple of margin is max speed reached
    // TODO probably not the best way to do this
    //  (what happens in fullscreen?)
    public float maxSpeedPoint;

	public float zoomMax;
	public float zoomSpeed;
	Vector2 cameraMinDefault;
	Vector2 cameraSizeDefault;
	float zoom;

	new Camera camera;
	HexGrid hexGrid;

	void Start () {
		camera = GetComponent<Camera>();
		hexGrid = GameObject.Find("HexGrid").GetComponent<HexGrid>();
		Vector2Int minTile = hexGrid.enabledMin;
		Vector2Int maxTile = minTile + hexGrid.enabledSize - Vector2Int.one;
		Vector3 minPos = hexGrid.TileIndicesToPos (minTile.x, minTile.y);
		Vector3 maxPos = hexGrid.TileIndicesToPos (maxTile.x, maxTile.y);
		Vector3 newPos = (minPos + maxPos) / 2.0f;
		newPos.z = transform.position.z;
		transform.position = newPos;

		camera.orthographicSize = (maxPos.y - minPos.y + 2.0f) / 2.0f;
		cameraSizeDefault.y = camera.orthographicSize;
		cameraSizeDefault.x = camera.orthographicSize * camera.aspect;
		cameraMinDefault = (Vector2)transform.position - cameraSizeDefault / 2.0f;
		
		zoom = 1.0f;
	}

	void Update () {
        /*Vector2 mousePos = Input.mousePosition;
        Vector3 camPos = transform.position;

        if (mousePos.x < margin) {
            float speed = Mathf.Min(maxSpeed,
                Mathf.Abs(mousePos.x - margin)
                / maxSpeedPoint * maxSpeed);
            camPos.x -= speed * Time.deltaTime;
        }
        if (mousePos.x > Screen.width - margin) {
            float speed = Mathf.Min(maxSpeed,
                Mathf.Abs(mousePos.x - Screen.width + margin)
                / maxSpeedPoint * maxSpeed);
            camPos.x += speed * Time.deltaTime;
        }
        if (mousePos.y < margin) {
            float speed = Mathf.Min(maxSpeed,
                Mathf.Abs(mousePos.y - margin)
                / maxSpeedPoint * maxSpeed);
            camPos.y -= speed * Time.deltaTime;
        }
        if (mousePos.y > Screen.height - margin) {
            float speed = Mathf.Min(maxSpeed,
                Mathf.Abs(mousePos.y - Screen.height + margin)
                / maxSpeedPoint * maxSpeed);
            camPos.y += speed * Time.deltaTime;
        }

        transform.position = camPos;*/

		Vector2 scroll = Input.mouseScrollDelta;
		if (Mathf.Abs(scroll.y) > 0.0f) {
			zoom += scroll.y * zoomSpeed;
			zoom = Mathf.Clamp(zoom, 1.0f, zoomMax);

			Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 viewMin = (cameraMinDefault - mousePos) / zoom + mousePos;
			Vector2 viewSize = cameraSizeDefault / zoom;

			camera.orthographicSize = viewSize.y / zoom;
			Vector3 newPos = viewMin + viewSize / 2.0f;
			newPos.z = transform.position.z;
			transform.position = newPos;
		}
	}
}
