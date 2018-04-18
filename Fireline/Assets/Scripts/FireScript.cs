using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireScript : MonoBehaviour {
    public int life;
	[HideInInspector] public int lifeRemaining;

	// Use this for initialization
	void Start () {
        lifeRemaining = life;
	}
	
	// Update is called once per frame
	void Update () {
    }
}
