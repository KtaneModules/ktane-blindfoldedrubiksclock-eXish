using System;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class RubiksClock : MonoBehaviour
{
    public KMSelectable[] GearButtons;
    public GameObject ClockPuzzle;
    public KMSelectable[] Pins;
    public GameObject[] Clocks;
    public KMSelectable TurnOverButton;
    public GameObject[] ClockLights;
    private Boolean[] _pins;
    private int[] _clocks;
    private Quaternion _targetRotation;
    private int[,,] _manualMoves = new int[4, 9, 4]
    {
        {
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
    private List<Move> _moves = new List<Move>();

    // Called once at start
    void Start()
    {
        // Gear buttons
        for (int i = 0; i < GearButtons.Length; i++)
        {
            var j = i;
            GearButtons[i].OnInteract += delegate () { PressGear(j); return false; };
        }

        // Pins
        _pins = new Boolean[Pins.Length];
        for (int i = 0; i < Pins.Length; i++)
        {
            var j = i;
            Pins[i].OnInteract += delegate () { PressPin(j); return false; };
            _pins[i] = true;
        }

        // Clocks
        // Front:     Back:
        // 0  1  2    11 10 9
        // 3  4  5    14 13 12
        // 6  7  8    17 16 15
        _clocks = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Turn over
        TurnOverButton.OnInteract += delegate () { PressTurnOver(); return false; };

        // Init target rotation
        _targetRotation = ClockPuzzle.transform.localRotation;

        // Random end position for pins
        for (int i = 0; i < 4; i++)
        {
            if (Rnd.Range(0, 2) == 1) ChangePin(i);
        }

        // Random moves for scramble
        for (int i = 0; i < 3; i++)
        {
            // Moves are applied in reverse at scramble time, so you can follow the manual to solve it
            Move move = new Move()
            {
                ClocksAtEnd = (int[])_clocks.Clone(),
                LitPin = Rnd.Range(0, 4),
                LitClock = Rnd.Range(0, 9),
            };
            _moves.Insert(0, move);

            // First rotate backwards
            RotateGear(_manualMoves[move.LitPin, move.LitClock, 2] - 1, -_manualMoves[move.LitPin, move.LitClock, 3]);

            // Then change the pins
            ChangePin(_manualMoves[move.LitPin, move.LitClock, 0] - 1);
            ChangePin(_manualMoves[move.LitPin, move.LitClock, 1] - 1);
        }

        foreach (Move move in _moves)
        {
            Debug.LogFormat("Lit clock: {0}. Lit pin: {1}.", move.LitClock + 1, move.LitPin + 1);
        }

        // Light the pin and clock for the first move
        Pins[_moves[0].LitPin].transform.Find("LightFront").GetComponent<Light>().enabled = true;
        Pins[_moves[0].LitPin].transform.Find("LightBack").GetComponent<Light>().enabled = true;
        Pins[_moves[0].LitPin].GetComponent<Renderer>().material.EnableKeyword("_EMISSION");

        //son.transform.parent = null;
        //daddy.transform.parent = son.transform;

    }

    // Called once per frame
    void Update()
    {
        ClockPuzzle.transform.localRotation = Quaternion.Lerp(ClockPuzzle.transform.localRotation, _targetRotation, 4 * Time.deltaTime);
    }

    private void PressTurnOver()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        TurnOver();
    }

    private void TurnOver()
    {
        _targetRotation *= Quaternion.AngleAxis(-180, Vector3.forward);
    }

    private void PressPin(int i)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch(.5f);

        ChangePin(i);
    }

    private void ChangePin(int i)
    {
        _pins[i] = !_pins[i];
        Pins[i].transform.Translate(0, (_pins[i] ? .014f : -.014f), 0);
    }

    private void PressGear(int i)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch(.1f);

        // 0=TL, 1=TR, 2=BL, 3=BR
        int gear = i / 2;

        // -1=CCW, 1=CW
        int amount = (i % 2) * 2 - 1;

        RotateGear(gear, amount);
        CheckState();
    }

    /// <summary>
    /// Rotate one of the gears a single step.
    /// </summary>
    /// <param name="gear">0=TL, 1=TR, 2=BL, 3=BR</param>
    /// <param name="amount">negative is counterclockwise</param>
    private void RotateGear(int gear, int amount)
    {
        switch (gear)
        {
            case 0:
                RotateClocks(amount, new Boolean[] {
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
                RotateClocks(amount, new Boolean[] {
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
                RotateClocks(amount, new Boolean[] {
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
                RotateClocks(amount, new Boolean[] {
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

    private void RotateClocks(int amount, Boolean[] conditions)
    {
        for (var i = 0; i < conditions.Length; i++)
        {
            // For clocks on the back, switch direction
            if (i == 9)
            {
                amount = -amount;
            }

            if (conditions[i])
            {
                _clocks[i] = (_clocks[i] + amount) % 12;
                Clocks[i].transform.Rotate(0, 30 * amount, 0);
            }
        }
    }

    private void CheckState()
    {
        // Try to find a clock that's not 12 o'clock. If there isn't any, the module is solved.
        int wrongClock = Array.Find(_clocks, i => i != 0);
        if (wrongClock == 0)
        {
            GetComponent<KMBombModule>().HandlePass();
        }
    }

    class Move
    {
        public int LitClock { get; set; }
        public int LitPin { get; set; }
        public int[] ClocksAtStart { get; set; }
        public int[] ClocksAtEnd { get; set; }
    }
}