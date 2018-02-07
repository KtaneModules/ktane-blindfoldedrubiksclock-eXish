using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubiksClock : MonoBehaviour
{
	public KMSelectable[] buttons;
	public GameObject clockPuzzle;
	public KMSelectable[] pins;
	public GameObject[] clocks;
	public KMSelectable otherSide;
	private Boolean[] pin;
	private int[] clock;
	private Quaternion targetRotation;

	// Use this for initialization
	void Start()
	{
		// Gear buttons
		for (int i = 0; i < buttons.Length; i++) {
			var j = i;
			buttons[i].OnInteract += delegate () {
				OnPressButton(j);
				return false;
			};
		}

		// Pins
		pin = new Boolean[pins.Length];
		for (int i = 0; i < pins.Length; i++) {
			var j = i;
			pins[i].OnInteract += delegate () {
				OnChangePin(j);
				return false;
			};
			pin[i] = true;
		}

		// Clocks
		// Front:     Back:
		// 0  1  2    11 10 9
		// 3  4  5    14 13 12
		// 6  7  8    17 16 15
		clock = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		// Turn over
		otherSide.OnInteract += delegate () {
			TurnOverToOtherSide();
			return false;
		};

		targetRotation = clockPuzzle.transform.rotation;
	}

	private void TurnOverToOtherSide()
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();

		targetRotation *= Quaternion.AngleAxis(180, Vector3.forward);
	}

	private void OnChangePin(int i)
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();

		pin[i] = !pin[i];
		pins[i].transform.Translate(0, (pin[i] ? 0.014f : -0.014f), 0);
	}

	// Update is called once per frame
	void Update()
	{
		clockPuzzle.transform.rotation = Quaternion.Lerp(clockPuzzle.transform.rotation, targetRotation, 5 * Time.deltaTime); 
	}

	private void OnPressButton(int i)
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();

		// 0=TL, 1=TR, 2=BL, 3=BR
		int gear = i / 2;

		// -1=CCW, 1=CW
		int dir = (i % 2) * 2 - 1;

		switch (gear) {
		case 0:
			TurnClock(dir, new Boolean[] {
				true,
				pin[0],
				(pin[0] && pin[1]) || (!pin[0] && !pin[1]),
				pin[0],
				pin[0],
				pin[0] && (pin[1] || pin[3]),
				(pin[0] && pin[2]) || (!pin[0] && !pin[2]),
				pin[0] && (pin[2] || pin[3]),
				(pin[0] && pin[3]) || (!pin[0] && !pin[3]),

				true,
				!pin[0],
				(pin[0] && pin[1]) || (!pin[0] && !pin[1]),
				!pin[0],
				!pin[0],
				!pin[0] && (!pin[1] || !pin[3]),
				(pin[0] && pin[2]) || (!pin[0] && !pin[2]),
				!pin[0] && (!pin[2] || !pin[3]),
				(pin[0] && pin[3]) || (!pin[0] && !pin[3]),
			});
			break;
		case 1:
			TurnClock(dir, new Boolean[] {
				(pin[1] && pin[0]) || (!pin[1] && !pin[0]),
				pin[1],
				true,
				pin[1] && (pin[0] || pin[2]),
				pin[1],
				pin[1],
				(pin[1] && pin[2]) || (!pin[1] && !pin[2]),
				pin[1] && (pin[3] || pin[2]),
				(pin[1] && pin[3]) || (!pin[1] && !pin[3]),

				(pin[1] && pin[0]) || (!pin[1] && !pin[0]),
				!pin[1],
				true,
				!pin[1] && (!pin[0] || !pin[2]),
				!pin[1],
				!pin[1],
				(pin[1] && pin[2]) || (!pin[1] && !pin[2]),
				!pin[1] && (!pin[3] || !pin[2]),
				(pin[1] && pin[3]) || (!pin[1] && !pin[3]),
			});
			break;
		case 2:
			TurnClock(dir, new Boolean[] {
				(pin[2] && pin[0]) || (!pin[2] && !pin[0]),
				pin[2] && (pin[0] || pin[1]),
				(pin[2] && pin[1]) || (!pin[2] && !pin[1]),
				pin[2],
				pin[2],
				pin[2] && (pin[3] || pin[1]),
				true,
				pin[2],
				(pin[2] && pin[3]) || (!pin[2] && !pin[3]),

				(pin[2] && pin[0]) || (!pin[2] && !pin[0]),
				!pin[2] && (!pin[0] || !pin[1]),
				(pin[2] && pin[1]) || (!pin[2] && !pin[1]),
				!pin[2],
				!pin[2],
				!pin[2] && (!pin[3] || !pin[1]),
				true,
				!pin[2],
				(pin[2] && pin[3]) || (!pin[2] && !pin[3]),
			});
			break;
		case 3:
			TurnClock(dir, new Boolean[] {
				(pin[3] && pin[0]) || (!pin[3] && !pin[0]),
				pin[3] && (pin[1] || pin[0]),
				(pin[3] && pin[1]) || (!pin[3] && !pin[1]),
				pin[3] && (pin[2] || pin[0]),
				pin[3],
				pin[3],
				(pin[3] && pin[2]) || (!pin[3] && !pin[2]),
				pin[3],
				true,

				(pin[3] && pin[0]) || (!pin[3] && !pin[0]),
				!pin[3] && (!pin[1] || !pin[0]),
				(pin[3] && pin[1]) || (!pin[3] && !pin[1]),
				!pin[3] && (!pin[2] || !pin[0]),
				!pin[3],
				!pin[3],
				(pin[3] && pin[2]) || (!pin[3] && !pin[2]),
				!pin[3],
				true,
			});
			break;
		}

	}

	private void TurnClock(int dir, Boolean[] conditions)
	{
		for (var i = 0; i < conditions.Length; i++) {

			// For clocks on the back, switch direction
			if (i == 9) {
				dir = -dir;
			}

			if (conditions[i]) {
				clock[i] = (clock[i] + dir) % 12;
				clocks[i].transform.Rotate(0, 30 * dir, 0);
			}
		}

		int wrongClock = Array.Find(clock, i => i != 0);
		if (wrongClock == 0) {
			GetComponent<KMBombModule>().HandlePass();
		}
	}
}
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
