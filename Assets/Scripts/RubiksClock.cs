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
            { 0, 3, 2, 6 },
            { 2, 3, 0, -2 },
            { 1, 2, 1, 1 },
            { 2, 3, 0, 4 },
            { 0, 2, 2, -1 },
            { 0, 1, 1, 5 },
            { 2, 3, 1, 4 },
            { 1, 2, 1, -1 },
            { 2, 3, 2, -3 }
        }, {
            { 0, 1, 3, 6 },
            { 0, 1, 2, 6 },
            { 0, 1, 0, 6 },
            { 0, 2, 3, 1 },
            { 0, 2, 3, -5 },
            { 2, 3, 3, -4 },
            { 2, 3, 3, 2 },
            { 0, 3, 0, -5 },
            { 1, 2, 3, 6 }
        }, {
            { 0, 3, 2, -4 },
            { 1, 2, 1, 4 },
            { 1, 3, 3, -4 },
            { 0, 2, 1, 5 },
            { 1, 3, 0, 2 },
            { 0, 3, 2, 2 },
            { 1, 2, 2, 3 },
            { 1, 3, 1, -2 },
            { 1, 3, 1, 6 }
        }, {
            { 0, 3, 3, 1 },
            { 1, 2, 1, 3 },
            { 0, 2, 0, -3 },
            { 0, 1, 0, -3 },
            { 1, 3, 2, 3 },
            { 0, 2, 3, -5 },
            { 1, 3, 2, 5 },
            { 0, 3, 0, -2 },
            { 0, 1, 0, -1 }
        }
    };
    private List<ModificationAction> _manualModActions = new List<ModificationAction>();
    private List<ModificationAmount> _manualModAmounts = new List<ModificationAmount>();
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
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x big squares to the right",
            SerialCharacters = "ABC",
            MainType = ModificationAction.MainTypeEnum.MoveBig,
            Direction = ModificationAction.DirectionEnum.Right,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x small squares down",
            SerialCharacters = "DEF",
            MainType = ModificationAction.MainTypeEnum.MoveSmall,
            Direction = ModificationAction.DirectionEnum.Down,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Change other pins if x is even",
            SerialCharacters = "GHI",
            MainType = ModificationAction.MainTypeEnum.OtherPins,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x big squares up",
            SerialCharacters = "JKL",
            MainType = ModificationAction.MainTypeEnum.MoveBig,
            Direction = ModificationAction.DirectionEnum.Up,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x small squares to the right",
            SerialCharacters = "MNO",
            MainType = ModificationAction.MainTypeEnum.MoveSmall,
            Direction = ModificationAction.DirectionEnum.Right,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Rotate other way if x is odd",
            SerialCharacters = "PQR",
            MainType = ModificationAction.MainTypeEnum.InvertRotation,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x big squares to the left",
            SerialCharacters = "STU",
            MainType = ModificationAction.MainTypeEnum.MoveBig,
            Direction = ModificationAction.DirectionEnum.Left,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x small squares up",
            SerialCharacters = "VWX",
            MainType = ModificationAction.MainTypeEnum.MoveSmall,
            Direction = ModificationAction.DirectionEnum.Up,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Add x hours clockwise",
            SerialCharacters = "YZ0",
            MainType = ModificationAction.MainTypeEnum.AddHours,
            Direction = ModificationAction.DirectionEnum.Clockwise,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x big squares down",
            SerialCharacters = "123",
            MainType = ModificationAction.MainTypeEnum.MoveBig,
            Direction = ModificationAction.DirectionEnum.Down,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Move x small squares to the left",
            SerialCharacters = "456",
            MainType = ModificationAction.MainTypeEnum.MoveSmall,
            Direction = ModificationAction.DirectionEnum.Left,
        });
        _manualModActions.Add(new ModificationAction()
        {
            Description = "Add x hours counterclockwise",
            SerialCharacters = "789",
            MainType = ModificationAction.MainTypeEnum.AddHours,
            Direction = ModificationAction.DirectionEnum.Counterclockwise,
        });

        // Init modification amounts
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of AA batteries + 1",
            SerialCharacters = "ABC",
            Quantity = Bomb.GetBatteryCount(Battery.AA) + Bomb.GetBatteryCount(Battery.AAx3) + Bomb.GetBatteryCount(Battery.AAx4) + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of lit indicators + 1",
            SerialCharacters = "DEF",
            Quantity = Bomb.GetOnIndicators().Count() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of batteries + 1",
            SerialCharacters = "GHI",
            Quantity = Bomb.GetBatteryCount() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of unlit indicators + 1",
            SerialCharacters = "JKL",
            Quantity = Bomb.GetOffIndicators().Count() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of D batteries + 1",
            SerialCharacters = "MNO",
            Quantity = Bomb.GetBatteryCount(Battery.D) + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of indicators + 1",
            SerialCharacters = "PQR",
            Quantity = Bomb.GetIndicators().Count() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of AA batteries + 1",
            SerialCharacters = "STU",
            Quantity = Bomb.GetBatteryCount(Battery.AA) + Bomb.GetBatteryCount(Battery.AAx3) + Bomb.GetBatteryCount(Battery.AAx4) + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of lit indicators + 1",
            SerialCharacters = "VWX",
            Quantity = Bomb.GetOnIndicators().Count() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of batteries + 1",
            SerialCharacters = "YZ0",
            Quantity = Bomb.GetBatteryCount() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of unlit indicators + 1",
            SerialCharacters = "123",
            Quantity = Bomb.GetOffIndicators().Count() + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of D batteries + 1",
            SerialCharacters = "456",
            Quantity = Bomb.GetBatteryCount(Battery.D) + 1,
        });
        _manualModAmounts.Add(new ModificationAmount()
        {
            Description = "Number of indicators + 1",
            SerialCharacters = "789",
            Quantity = Bomb.GetIndicators().Count() + 1,
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

        // Scramble
        Scramble(5);

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

        // Determine first modifications, using ascii conversion to convert ABC to 0, DEF to 1, etc.
        string sn = Bomb.GetSerialNumber();
        int firstModificationAction1 = (Char.IsDigit(sn[0]) ? (int)sn[0] - 22 : (int)sn[0] - 65) / 3;
        int firstModificationAmount1 = (Char.IsDigit(sn[1]) ? (int)sn[1] - 22 : (int)sn[1] - 65) / 3;
        int firstModificationAction2 = (Char.IsDigit(sn[2]) ? (int)sn[2] - 22 : (int)sn[2] - 65) / 3;
        int firstModificationAmount2 = (Char.IsDigit(sn[3]) ? (int)sn[3] - 22 : (int)sn[3] - 65) / 3;
        Debug.LogFormat(
            "First mod action1: {0}, amount1: {1}, action2: {2}, amount2: {3}",
            firstModificationAction1, firstModificationAmount1, firstModificationAction2, firstModificationAmount2
            );

        // Apply moves
        for (int curMove = 0; curMove < numMoves; curMove++)
        {
            // Turn over
            onFrontSide = !onFrontSide;

            // Random lit pin and clock
            Move move = new Move()
            {
                LitPin = Rnd.Range(0, 4),
                LitClock = Rnd.Range(0, 9),
                OnFrontSide = onFrontSide,

                // Determine modifications
                Modifications = new Modification[] {
                    new Modification() {
                        Action = _manualModActions[(firstModificationAction1 + numMoves - 1 - curMove) % 12],
                        Amount = _manualModAmounts[(firstModificationAmount1 + numMoves - 1 - curMove) % 12],
                    },
                    new Modification() {
                        Action = _manualModActions[(firstModificationAction2 + numMoves - 1 - curMove) % 12],
                        Amount = _manualModAmounts[(firstModificationAmount2 + numMoves - 1 - curMove) % 12],
                    }
                },
            };

            Debug.LogFormat(
                "Lit clock: {0}. Lit pin: {1}. Modifications: {2} for {3} ({4}), {5} for {6} ({7}).",
                move.LitClock,
                move.LitPin,
                move.Modifications[0].Action.Description,
                move.Modifications[0].Amount.Quantity,
                move.Modifications[0].Amount.Description,
                move.Modifications[1].Action.Description,
                move.Modifications[1].Amount.Quantity,
                move.Modifications[1].Amount.Description
            );

            // Apply "move" modifications
            move.BigSquare = move.LitClock;
            move.SmallSquare = move.LitPin;
            foreach (Modification modification in move.Modifications)
            {
                if (
                    modification.Action.MainType == ModificationAction.MainTypeEnum.MoveBig ||
                    modification.Action.MainType == ModificationAction.MainTypeEnum.MoveSmall
                )
                {
                    // Convert big and small to 0-5 by 0-5 coordinate for easier movement
                    int col = (move.BigSquare % 3) * 2 + (move.SmallSquare % 2);
                    int row = (move.BigSquare / 3) * 2 + (move.SmallSquare / 2);
                    int step = (modification.Action.MainType == ModificationAction.MainTypeEnum.MoveBig ? 2 : 1);

                    Debug.LogFormat("Converted to col {0}, row {1}, step {2}", col, row, step);

                    // Apply move
                    switch (modification.Action.Direction)
                    {
                        case ModificationAction.DirectionEnum.Up:
                            row = (row + (6 - step) * modification.Amount.Quantity) % 6;
                            break;
                        case ModificationAction.DirectionEnum.Down:
                            row = (row + step * modification.Amount.Quantity) % 6;
                            break;
                        case ModificationAction.DirectionEnum.Left:
                            col = (col + (6 - step) * modification.Amount.Quantity) % 6;
                            break;
                        case ModificationAction.DirectionEnum.Right:
                            col = (col + step * modification.Amount.Quantity) % 6;
                            break;
                    }

                    Debug.LogFormat("After move, col {0}, row {1}", col, row);

                    // And convert back
                    move.BigSquare = (row / 2 * 3) + (col / 2);
                    move.SmallSquare = (row % 2 * 2) + (col % 2);

                    Debug.LogFormat("Big Square: {0}. Small Square: {1}", move.BigSquare, move.SmallSquare);
                }
            }

            // Initial rotation
            int gear = _manualMoves[move.SmallSquare, move.BigSquare, 2];
            int amount = _manualMoves[move.SmallSquare, move.BigSquare, 3];

            // Apply "rotate" modifications
            foreach (Modification modification in move.Modifications)
            {
                if (
                    modification.Action.MainType == ModificationAction.MainTypeEnum.InvertRotation &&
                    (modification.Amount.Quantity % 2 == 1)
                )
                {
                    amount = -amount;
                }
            }

            // Apply "add hours" modifications
            foreach (Modification modification in move.Modifications)
            {
                if (modification.Action.MainType == ModificationAction.MainTypeEnum.AddHours)
                {
                    amount += (modification.Action.Direction == ModificationAction.DirectionEnum.Clockwise)
                        ? modification.Amount.Quantity
                        : -modification.Amount.Quantity;
                }
            }

            // Invert rotation if on back side
            if (!move.OnFrontSide)
            {
                gear = _mirrorPin[gear];
                amount = -amount;
            }

            // Rotate inversed at scramble time
            RotateGear(gear, -amount);
            move.ClocksAtStart = (int[])_clocks.Clone();

            // Initial pins to change
            int pin1 = _manualMoves[move.SmallSquare, move.BigSquare, 0];
            int pin2 = _manualMoves[move.SmallSquare, move.BigSquare, 1];
            if (!move.OnFrontSide)
            {
                pin1 = _mirrorPin[pin1];
                pin2 = _mirrorPin[pin2];
            }
            bool[] changePins = new bool[4];
            changePins[pin1] = true;
            changePins[pin2] = true;

            // Apply "change other pins" modifications
            foreach (Modification modification in move.Modifications)
            {
                if (
                    modification.Action.MainType == ModificationAction.MainTypeEnum.OtherPins &&
                    (modification.Amount.Quantity % 2 == 0)
                )
                {
                    for (int i = 0; i < changePins.Length; i++)
                    {
                        changePins[i] = !changePins[i];
                    }
                }
            }

            // Change pins
            for (int i = 0; i < 4; i++)
            {
                if (changePins[i]) ChangePin(i);
            }

            // Add to scramble
            _moves.Insert(0, move);
        }
    }

    private void LightPinAndClock(Move move)
    {
        int litPin = move.OnFrontSide ? move.LitPin : _mirrorPin[move.LitPin];
        int litClockFront = move.OnFrontSide ? move.LitClock : _mirrorClock[move.LitClock];
        int litClockBack = move.OnFrontSide ? (move.LitClock + 9) : (_mirrorClock[move.LitClock] + 9);

        for (int i = 0; i < Pins.Length; i++)
        {
            Pins[i].transform.Find("PinLightFront").GetComponent<Light>().enabled = (i == litPin);
            Pins[i].transform.Find("PinLightBack").GetComponent<Light>().enabled = (i == litPin);
        }

        for (int i = 0; i < Clocks.Length; i++)
        {
            Clocks[i].transform.Find("ClockLight").GetComponent<Light>().enabled = (i == litClockFront || i == litClockBack);
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

    /// <param name="gear">0=TL, 1=TR, 2=BL, 3=BR</param>
    /// <param name="amount">Number of hours, negative is counterclockwise</param>
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
                // Adding a large product of 12 for extreme conditions, making sure clocks stay positive ints
                _clocks[i] = (_clocks[i] + amount + 144) % 12;
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
            Debug.Log("Move clocks at start:" + string.Join(",", move.ClocksAtStart.Select(x => x.ToString()).ToArray()));
            Debug.Log("Current clocks:" + string.Join(",", _clocks.Select(x => x.ToString()).ToArray()));
            if (_clocks.SequenceEqual(move.ClocksAtStart))
            {
                Debug.Log("Hit!");
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
        // Lit clock and pin for initial instructions
        public int LitClock { get; set; }
        public int LitPin { get; set; }

        // Instructions after possible move by modification
        public int BigSquare { get; set; }
        public int SmallSquare { get; set; }

        // If the module clocks match these clocks, you arrived at this move, let's light the clock and pin
        public int[] ClocksAtStart { get; set; }

        // Is this move to be done front or back side?
        public bool OnFrontSide { get; set; }


        public Modification[] Modifications { get; set; }
    }

    struct Modification
    {
        public ModificationAction Action { get; set; }
        public ModificationAmount Amount { get; set; }
    }

    struct ModificationAction
    {
        public enum MainTypeEnum { MoveBig, MoveSmall, OtherPins, InvertRotation, AddHours }
        public enum DirectionEnum { Up, Down, Left, Right, Clockwise, Counterclockwise }

        public string Description { get; set; }
        public string SerialCharacters { get; set; }
        public MainTypeEnum MainType { get; set; }
        public DirectionEnum Direction { get; set; }
    }

    struct ModificationAmount
    {
        public string Description { get; set; }
        public string SerialCharacters { get; set; }
        public int Quantity { get; set; }
    }
}