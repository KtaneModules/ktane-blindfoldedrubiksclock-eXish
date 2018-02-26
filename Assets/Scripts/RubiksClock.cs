using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KmHelper;

public class RubiksClock : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMSelectable[] GearButtons;
    public GameObject ClockPuzzle;
    public KMSelectable[] Pins;
    public GameObject[] Clocks;
    public KMSelectable TurnOverButton;
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
    private List<ModAction> _manualModActions = new List<ModAction>();
    private List<ModAmount> _manualModAmounts = new List<ModAmount>();
    private List<Move> _moves = new List<Move>();

    // Convert pin index to the other side
    private int[] _mirrorPin = new int[] { 1, 0, 3, 2 };

    // Convert clock index to the other side
    private int[] _mirrorClock = new int[] { 2, 1, 0, 5, 4, 3, 8, 7, 6, 11, 10, 9, 14, 13, 12, 17, 16, 15 };

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

        // Turn over
        TurnOverButton.OnInteract += delegate () { PressTurnOver(); return false; };

        // Init modification actions
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x big squares to the right",
            SerialCharacters = "ABC",
            MainType = ModAction.MainTypeEnum.MoveBig,
            Direction = ModAction.DirectionEnum.Right,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x small squares down",
            SerialCharacters = "DEF",
            MainType = ModAction.MainTypeEnum.MoveSmall,
            Direction = ModAction.DirectionEnum.Down,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Change other pins if x is even",
            SerialCharacters = "GHI",
            MainType = ModAction.MainTypeEnum.OtherPins,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x big squares up",
            SerialCharacters = "JKL",
            MainType = ModAction.MainTypeEnum.MoveBig,
            Direction = ModAction.DirectionEnum.Up,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x small squares to the right",
            SerialCharacters = "MNO",
            MainType = ModAction.MainTypeEnum.MoveSmall,
            Direction = ModAction.DirectionEnum.Right,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Rotate other way if x is odd",
            SerialCharacters = "PQR",
            MainType = ModAction.MainTypeEnum.InvertRotation,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x big squares to the left",
            SerialCharacters = "STU",
            MainType = ModAction.MainTypeEnum.MoveBig,
            Direction = ModAction.DirectionEnum.Left,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x small squares up",
            SerialCharacters = "VWX",
            MainType = ModAction.MainTypeEnum.MoveSmall,
            Direction = ModAction.DirectionEnum.Up,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Add x hours clockwise",
            SerialCharacters = "YZ0",
            MainType = ModAction.MainTypeEnum.AddHours,
            Direction = ModAction.DirectionEnum.Clockwise,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x big squares down",
            SerialCharacters = "123",
            MainType = ModAction.MainTypeEnum.MoveBig,
            Direction = ModAction.DirectionEnum.Down,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Move x small squares to the left",
            SerialCharacters = "456",
            MainType = ModAction.MainTypeEnum.MoveSmall,
            Direction = ModAction.DirectionEnum.Left,
        });
        _manualModActions.Add(new ModAction()
        {
            Description = "Add x hours counterclockwise",
            SerialCharacters = "789",
            MainType = ModAction.MainTypeEnum.AddHours,
            Direction = ModAction.DirectionEnum.Counterclockwise,
        });

        // Init modification amounts
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of AA batteries",
            SerialCharacters = "ABC",
            Quantifier = ModAmount.QuantifierEnum.AaBatteries,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of lit indicators",
            SerialCharacters = "DEF",
            Quantifier = ModAmount.QuantifierEnum.LitIndicators,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of batteries",
            SerialCharacters = "GHI",
            Quantifier = ModAmount.QuantifierEnum.Batteries,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of unlit indicators",
            SerialCharacters = "JKL",
            Quantifier = ModAmount.QuantifierEnum.UnlitIndicators,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of D batteries",
            SerialCharacters = "MNO",
            Quantifier = ModAmount.QuantifierEnum.DBatteries,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of indicators",
            SerialCharacters = "PQR",
            Quantifier = ModAmount.QuantifierEnum.Indicators,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of AA batteries",
            SerialCharacters = "STU",
            Quantifier = ModAmount.QuantifierEnum.AaBatteries,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of lit indicators",
            SerialCharacters = "VWX",
            Quantifier = ModAmount.QuantifierEnum.LitIndicators,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of batteries",
            SerialCharacters = "YZ0",
            Quantifier = ModAmount.QuantifierEnum.Batteries,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of unlit indicators",
            SerialCharacters = "123",
            Quantifier = ModAmount.QuantifierEnum.UnlitIndicators,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of D batteries",
            SerialCharacters = "456",
            Quantifier = ModAmount.QuantifierEnum.DBatteries,
        });
        _manualModAmounts.Add(new ModAmount()
        {
            Description = "Number of indicators",
            SerialCharacters = "789",
            Quantifier = ModAmount.QuantifierEnum.Indicators,
            Quantity = Bomb.GetIndicators().Count(),
        });

        // Clocks
        // Front:     Back:
        // 0  1  2    11 10 9
        // 3  4  5    14 13 12
        // 6  7  8    17 16 15
        _clocks = Enumerable.Repeat(0, 18).ToArray();

        // Init target rotation
        _targetRotation = ClockPuzzle.transform.localRotation;

        // Random end position for pins
        for (int i = 0; i < 4; i++)
        {
            if (Rnd.Range(0, 2) == 1) ChangePin(i);
        }

        for (int i = 0; i < _clocks.Length; i++)
        {
            _clocks[i] = i % 12;
        }
        _pins[0] = true;
        _pins[1] = false;
        _pins[2] = true;
        _pins[3] = false;
        UpdateGameObjects();


        // Scramble
        Scramble(4);

        foreach (Move move in _moves)
        {
            Debug.LogFormat("Lit clock: {0}. Lit pin: {1}.", move.LitClock + 1, move.LitPin + 1);
        }

        // If the first move is on the back, turn over
        if (!_moves[0].OnFrontSide)
        {
            TurnOver(true);
        }

        // This should light the pin and clock for the first move
        CheckState();
    }

    /// <summary>
    /// Random moves for scramble. Reverse moves and apply in reverse order, following the manual will solve it.
    /// </summary>
    private void Scramble(int numMoves)
    {
        bool onFrontSide = true;

        string serial = Bomb.GetSerialNumber();
        int firstModAction1 = (Char.IsDigit(serial[0]) ? (int)serial[0] - 22 : (int)serial[0] - 65) / 3;
        int firstModAmount1 = (Char.IsDigit(serial[1]) ? (int)serial[1] - 22 : (int)serial[1] - 65) / 3;
        int firstModAction2 = (Char.IsDigit(serial[2]) ? (int)serial[2] - 22 : (int)serial[2] - 65) / 3;
        int firstModAmount2 = (Char.IsDigit(serial[3]) ? (int)serial[3] - 22 : (int)serial[3] - 65) / 3;

        for (int i = 0; i < numMoves; i++)
        {
            Move move = new Move()
            {
                LitPin = Rnd.Range(0, 4),
                LitClock = Rnd.Range(0, 9),
                OnFrontSide = onFrontSide,
            };

            // Apply move modifications
            // ...

            // Rotate backwards
            int gear = _manualMoves[move.LitPin, move.LitClock, 2] - 1;
            int amount = -_manualMoves[move.LitPin, move.LitClock, 3];
            if (!move.OnFrontSide)
            {
                gear = _mirrorPin[gear];
                amount = -amount;
            }
            RotateGear(gear, amount);
            move.ClocksAtStart = (int[])_clocks.Clone();

            // Change pins
            int pin1 = _manualMoves[move.LitPin, move.LitClock, 0] - 1;
            int pin2 = _manualMoves[move.LitPin, move.LitClock, 1] - 1;
            if (!move.OnFrontSide)
            {
                pin1 = _mirrorPin[pin1];
                pin2 = _mirrorPin[pin2];
            }
            ChangePin(pin1);
            ChangePin(pin2);

            // Turn over
            onFrontSide = !onFrontSide;

            // Add to scramble
            _moves.Add(move);
        }
    }

    private void LightPinAndClock(Move move)
    {
        foreach (Move m in _moves)
        {
            bool enabled = m.Equals(move);
            Pins[m.OnFrontSide ? m.LitPin : _mirrorPin[m.LitPin]].transform.Find("PinLightFront").GetComponent<Light>().enabled = enabled;
            Pins[m.OnFrontSide ? m.LitPin : _mirrorPin[m.LitPin]].transform.Find("PinLightBack").GetComponent<Light>().enabled = enabled;
            Clocks[m.OnFrontSide ? m.LitClock : _mirrorClock[m.LitClock]].transform.Find("ClockLight").GetComponent<Light>().enabled = enabled;
            Clocks[m.OnFrontSide ? (m.LitClock + 9) : (_mirrorClock[m.LitClock] + 9)].transform.Find("ClockLight").GetComponent<Light>().enabled = enabled;
        }
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

    private void TurnOver(bool instant = false)
    {
        if (instant) ClockPuzzle.transform.Rotate(0, 0, 180);
        _targetRotation *= Quaternion.AngleAxis(180, Vector3.forward);
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
        // If all clocks are 12 o'clock
        if (_clocks.SequenceEqual(Enumerable.Repeat(0, 18).ToArray()))
        {
            // The module is solved
            GetComponent<KMBombModule>().HandlePass();
        }

        // If the clocks are in the starting position of a move
        foreach (Move move in _moves)
        {
            if (_clocks.SequenceEqual(move.ClocksAtStart))
            {
                LightPinAndClock(move);
                break;
            }
        }
    }

    private void UpdateGameObjects()
    {
        // Update clocks
        for (int i = 0; i < _clocks.Length; i++)
        {
            Clocks[i].transform.eulerAngles = new Vector3(0, 30 * _clocks[i], 0);
        }

        // Update pins
        for (int i = 0; i < _pins.Length; i++)
        {
            Pins[i].transform.localPosition = new Vector3(Pins[i].transform.localPosition.x, _pins[i] ? .7f : -.7f, Pins[i].transform.localPosition.z);
        }
    }

    struct Move
    {
        public int LitClock { get; set; }
        public int LitPin { get; set; }
        public int[] ClocksAtStart { get; set; }
        public bool OnFrontSide { get; set; }
        public int ModAction1 { get; set; }
        public int ModAmount1 { get; set; }
        public int ModAction2 { get; set; }
        public int ModAmount2 { get; set; }
    }

    struct ModAction
    {
        public enum MainTypeEnum { MoveBig, MoveSmall, OtherPins, InvertRotation, AddHours }
        public enum DirectionEnum { Up, Down, Left, Right, Clockwise, Counterclockwise }

        public string Description { get; set; }
        public string SerialCharacters { get; set; }
        public MainTypeEnum MainType { get; set; }
        public DirectionEnum Direction { get; set; }
    }

    struct ModAmount
    {
        public enum QuantifierEnum { Batteries, AaBatteries, DBatteries, Indicators, LitIndicators, UnlitIndicators }

        public string Description { get; set; }
        public string SerialCharacters { get; set; }
        public QuantifierEnum Quantifier { get; set; }
        public int Quantity { get; set; }
    }
}