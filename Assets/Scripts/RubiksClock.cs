using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubiksClock : MonoBehaviour {

    public KMSelectable[] buttons;
    public GameObject[] clocks;
    public KMSelectable[] pins;
    private Boolean[] pinsStatus;

    /**
     * JSON clock data
[
  [
    [[1,4],3,6],
    [[3,4],1,-2],
    [[2,3],2,1],
    [[3,4],1,4],
    [[1,3],3,-1],
    [[1,2],2,5],
    [[3,4],2,4],
    [[2,3],2,-1],
    [[3,4],3,-3]
  ],
  [
    [[1,2],4,6],
    [[1,2],3,6],
    [[1,2],1,6],
    [[1,3],4,1],
    [[1,3],4,-5],
    [[3,4],4,-4],
    [[3,4],4,2],
    [[1,4],1,-5],
    [[2,3],4,6]
  ],
  [
    [[1,4],3,-4],
    [[2,3],2,4],
    [[2,4],4,-4],
    [[1,3],2,5],
    [[2,4],1,2],
    [[1,4],3,2],
    [[2,3],3,3],
    [[2,4],2,-2],
    [[2,4],2,6]],
  [
    [[1,4],4,1],
    [[2,3],2,3],
    [[1,3],1,-3],
    [[1,2],1,-3],
    [[2,4],3,3],
    [[1,3],4,-5],
    [[2,4],3,5],
    [[1,4],1,-2],
    [[1,2],1,-1]
  ]
]
     * 
     */

    // Use this for initialization
    void Start () {
        for (int i = 0; i < buttons.Length; i++)
        {
            var j = i;
            buttons[i].OnInteract += delegate () { OnPressButton(j); return false; };
        }
        //pinsStatus = new Boolean[pins.Length];
        for (int i = 0; i < pins.Length; i++)
        {
            var j = i;
            pins[i].OnInteract += delegate () { OnChangePin(j); return false; };
            //pinsStatus[i] = true;
        }
    }

    private void OnChangePin(int i)
    {
        Debug.Log("Pin " + i + " changed");
        //pinsStatus[i] = !pinsStatus[i];
        //pins[i].transform.Translate(0, 1.4f * (pinsStatus[i] ? -1 : 1), 0);
    }

    // Update is called once per frame
    void Update () {
		
	}

    private void OnPressButton(int i)
    {
        Debug.Log("Button " + i + " pressed");
        clocks[i].transform.Rotate(0, 30, 0);
    }
}
