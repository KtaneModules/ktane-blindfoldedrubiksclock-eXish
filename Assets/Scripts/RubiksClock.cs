using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubiksClock : MonoBehaviour {

    public KMSelectable[] buttons;

    public GameObject[] clocks;

    // Use this for initialization
    void Start () {
        for (int i = 0; i < buttons.Length; i++)
        {
            Debug.Log("button " + i);
            var j = i;
            buttons[i].OnInteract += delegate () { OnPressButton(j); return false; };
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    private void OnPressButton(int i)
    {
        Debug.Log(i + " pressed");
        clocks[i].transform.Rotate(0, 30, 0);
    }
}
