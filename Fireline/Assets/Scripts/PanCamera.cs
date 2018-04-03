using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanCamera : MonoBehaviour {
    public float margin;
    public float maxSpeed;
    // at which multiple of margin is max speed reached
    // TODO probably not the best way to do this
    //  (what happens in fullscreen?)
    public float maxSpeedPoint;

	bool disabled = false;

	void Start () {
	}

	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			disabled = !disabled;
		}
		if (disabled) {
			return;
		}

        Vector2 mousePos = Input.mousePosition;
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

        transform.position = camPos;
	}
}
