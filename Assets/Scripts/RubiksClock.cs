using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubiksClock : MonoBehaviour
{
    public KMSelectable[] GearButtons;
    public GameObject ClockPuzzle;
    public KMSelectable[] Pins;
    public GameObject[] Clocks;
    public KMSelectable TurnOverButton;
    private Boolean[] _pins;
    private int[] _clocks;
    private Quaternion _targetRotation;
    private int[,,] _manualMoves = new int[4, 9, 4] { {
            { 1, 4, 3, 6 },
            { 3, 4, 1, -2 },
            { 2, 3, 2, 1 },
            { 3, 4, 1, 4 },
            { 1, 3, 3, -1 },
            { 1, 2, 2, 5 },
            { 3, 4, 2, 4 },
            { 2, 3, 2, -1 },
            { 3, 4, 3, -3 }
        }, {
            { 1, 2, 4, 6 },
            { 1, 2, 3, 6 },
            { 1, 2, 1, 6 },
            { 1, 3, 4, 1 },
            { 1, 3, 4, -5 },
            { 3, 4, 4, -4 },
            { 3, 4, 4, 2 },
            { 1, 4, 1, -5 },
            { 2, 3, 4, 6 }
        }, {
            { 1, 4, 3, -4 },
            { 2, 3, 2, 4 },
            { 2, 4, 4, -4 },
            { 1, 3, 2, 5 },
            { 2, 4, 1, 2 },
            { 1, 4, 3, 2 },
            { 2, 3, 3, 3 },
            { 2, 4, 2, -2 },
            { 2, 4, 2, 6 }
        }, {
            { 1, 4, 4, 1 },
            { 2, 3, 2, 3 },
            { 1, 3, 1, -3 },
            { 1, 2, 1, -3 },
            { 2, 4, 3, 3 },
            { 1, 3, 4, -5 },
            { 2, 4, 3, 5 },
            { 1, 4, 1, -2 },
            { 1, 2, 1, -1 }
        }
    };
    private List<Move> _moves;

    // Use this for initialization
    void Start()
    {
        // Gear buttons
        for (int i = 0; i < GearButtons.Length; i++) {
            var j = i;
            GearButtons[i].OnInteract += delegate () {
                OnPressButton(j);
                return false;
            };
        }

        // Pins
        _pins = new Boolean[Pins.Length];
        for (int i = 0; i < Pins.Length; i++) {
            var j = i;
            Pins[i].OnInteract += delegate () {
                OnChangePin(j);
                return false;
            };
            _pins[i] = true;
        }

        // Clocks
        // Front:     Back:
        // 0  1  2    11 10 9
        // 3  4  5    14 13 12
        // 6  7  8    17 16 15
        _clocks = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Turn over
        TurnOverButton.OnInteract += delegate () {
            TurnOver();
            return false;
        };

        _targetRotation = ClockPuzzle.transform.localRotation;

        // Scramble
        System.Random rnd = new System.Random();
        for (int i = 0; i < 3; i++)
        {
            int clock = rnd.Next(0, 12);
            int pin = rnd.Next(0, 4);
            _moves.Insert(0, new Move(clock, pin, _manualMoves));
        }
    }

    // Update is called once per frame
    void Update()
    {
        ClockPuzzle.transform.localRotation = Quaternion.Lerp(ClockPuzzle.transform.localRotation, _targetRotation, 4 * Time.deltaTime);
    }

    private void TurnOver()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        _targetRotation *= Quaternion.AngleAxis(-180, Vector3.forward);
    }

    private void OnChangePin(int i)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch(.5f);

        _pins[i] = !_pins[i];
        Pins[i].transform.Translate(0, (_pins[i] ? .014f : -.014f), 0);
    }

    private void OnPressButton(int i)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch(.1f);

        // 0=TL, 1=TR, 2=BL, 3=BR
        int gear = i / 2;

        // -1=CCW, 1=CW
        int dir = (i % 2) * 2 - 1;

        switch (gear) {
            case 0:
                TurnClock(dir, new Boolean[] {
                true,
                _pins[0],
                (_pins[0] && _pins[1]) || (!_pins[0] && !_pins[1]),
                _pins[0],
                _pins[0],
                _pins[0] && (_pins[1] || _pins[3]),
                (_pins[0] && _pins[2]) || (!_pins[0] && !_pins[2]),
                _pins[0] && (_pins[2] || _pins[3]),
                (_pins[0] && _pins[3]) || (!_pins[0] && !_pins[3]),

                true,
                !_pins[0],
                (_pins[0] && _pins[1]) || (!_pins[0] && !_pins[1]),
                !_pins[0],
                !_pins[0],
                !_pins[0] && (!_pins[1] || !_pins[3]),
                (_pins[0] && _pins[2]) || (!_pins[0] && !_pins[2]),
                !_pins[0] && (!_pins[2] || !_pins[3]),
                (_pins[0] && _pins[3]) || (!_pins[0] && !_pins[3]),
            });
                break;
            case 1:
                TurnClock(dir, new Boolean[] {
                (_pins[1] && _pins[0]) || (!_pins[1] && !_pins[0]),
                _pins[1],
                true,
                _pins[1] && (_pins[0] || _pins[2]),
                _pins[1],
                _pins[1],
                (_pins[1] && _pins[2]) || (!_pins[1] && !_pins[2]),
                _pins[1] && (_pins[3] || _pins[2]),
                (_pins[1] && _pins[3]) || (!_pins[1] && !_pins[3]),

                (_pins[1] && _pins[0]) || (!_pins[1] && !_pins[0]),
                !_pins[1],
                true,
                !_pins[1] && (!_pins[0] || !_pins[2]),
                !_pins[1],
                !_pins[1],
                (_pins[1] && _pins[2]) || (!_pins[1] && !_pins[2]),
                !_pins[1] && (!_pins[3] || !_pins[2]),
                (_pins[1] && _pins[3]) || (!_pins[1] && !_pins[3]),
            });
                break;
            case 2:
                TurnClock(dir, new Boolean[] {
                (_pins[2] && _pins[0]) || (!_pins[2] && !_pins[0]),
                _pins[2] && (_pins[0] || _pins[1]),
                (_pins[2] && _pins[1]) || (!_pins[2] && !_pins[1]),
                _pins[2],
                _pins[2],
                _pins[2] && (_pins[3] || _pins[1]),
                true,
                _pins[2],
                (_pins[2] && _pins[3]) || (!_pins[2] && !_pins[3]),

                (_pins[2] && _pins[0]) || (!_pins[2] && !_pins[0]),
                !_pins[2] && (!_pins[0] || !_pins[1]),
                (_pins[2] && _pins[1]) || (!_pins[2] && !_pins[1]),
                !_pins[2],
                !_pins[2],
                !_pins[2] && (!_pins[3] || !_pins[1]),
                true,
                !_pins[2],
                (_pins[2] && _pins[3]) || (!_pins[2] && !_pins[3]),
            });
                break;
            case 3:
                TurnClock(dir, new Boolean[] {
                (_pins[3] && _pins[0]) || (!_pins[3] && !_pins[0]),
                _pins[3] && (_pins[1] || _pins[0]),
                (_pins[3] && _pins[1]) || (!_pins[3] && !_pins[1]),
                _pins[3] && (_pins[2] || _pins[0]),
                _pins[3],
                _pins[3],
                (_pins[3] && _pins[2]) || (!_pins[3] && !_pins[2]),
                _pins[3],
                true,

                (_pins[3] && _pins[0]) || (!_pins[3] && !_pins[0]),
                !_pins[3] && (!_pins[1] || !_pins[0]),
                (_pins[3] && _pins[1]) || (!_pins[3] && !_pins[1]),
                !_pins[3] && (!_pins[2] || !_pins[0]),
                !_pins[3],
                !_pins[3],
                (_pins[3] && _pins[2]) || (!_pins[3] && !_pins[2]),
                !_pins[3],
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
                _clocks[i] = (_clocks[i] + dir) % 12;
                Clocks[i].transform.Rotate(0, 30 * dir, 0);
            }
        }

        int wrongClock = Array.Find(_clocks, i => i != 0);
        if (wrongClock == 0) {
            GetComponent<KMBombModule>().HandlePass();
        }
    }

    class Move
    {
        int LitClock { get; set; }
        int LitPin { get; set; }

        public Move(int litClock, int litPin, int[,,] _manualMoves)
        {
            LitClock = litClock;
            LitPin = litPin;


        }
    }
}