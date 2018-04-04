using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitScript : MonoBehaviour {
	public float range;
	[HideInInspector] public float rangeRemaining;

	// Use this for initialization
	void Start () {
		rangeRemaining = range;
	}
	
	// Update is called once per frame
	void Update () {
	}
}
