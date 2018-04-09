using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KmHelper;
using System.Collections;

public class RubiksClock : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMSelectable Module;
    public GameObject ClockPuzzle;
    public KMSelectable TurnOverButton;
    public KMSelectable ResetButton;
    public GameObject[] Gears;
    public KMSelectable[] GearButtons;
    public GameObject[] Pins;
    public KMSelectable[] PinButtons;
    public GameObject[] Clocks;
    public Material UnlitMaterial;
    public Material LitMaterial;

    // Front:
    // 0 1
    // 2 3
    private bool[] _pins = new bool[4];

    // Front:     Back:
    // 0  1  2    11 10 9
    // 3  4  5    14 13 12
    // 6  7  8    17 16 15
    private int[] _clocks = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    private bool _onFrontSide = true;
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
    private Queue<IAction> _animationQueue = new Queue<IAction>();
    private Stack<IAction> _resetStack = new Stack<IAction>();
    private List<IAction> _actionLog = new List<IAction>();
    private bool _isScrambling, _isResetting, _isSolved;

    // Convert pin index to the other side
    private int[] _mirror4 = new int[] { 1, 0, 3, 2 };

    // Convert clock index to the other side
    private int[] _mirror9 = new int[] { 2, 1, 0, 5, 4, 3, 8, 7, 6, 11, 10, 9, 14, 13, 12, 17, 16, 15 };

    // Helpers for logging
    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private string[] _toDir4 = new string[] { "TL", "TR", "BL", "BR" };
    private string[] _toDir9 = new string[] { "TL", "T", "TR", "L", "M", "R", "BL", "B", "BR" };

    // Twitch Plays specific
#pragma warning disable 0414
    private string TwitchManualCode = "Rubik%E2%80%99s Clock";
    private string TwitchHelpMessage =
        "Change a pin with '!{0} tl'."
        + "  Rotate a gear with '!{0} br -3'."
        + "  Turn over clock with '!{0} t' (or 'turn'), reset with '!{0} r' (or 'reset')."
        + "  Tilt the clock to see what pins are up/down with '!{0} tilt."
        + "  Commands can be combined with commas '!{0} tl, tr, br -3, t'";
#pragma warning restore 0414

    // Called once at start
    void Start()
    {
        _moduleId = _moduleIdCounter++;

        // Gear buttons
        for (int i = 0; i < GearButtons.Length; i++)
        {
            var j = i;
            GearButtons[i].OnInteract += delegate () { PressGearButton(j); return false; };
        }

        // Pin buttons
        for (var i = 0; i < PinButtons.Length; i++)
        {
            var j = i;
            PinButtons[i].OnInteract += delegate () { PressPinButton(j); return false; };
            _pins[i] = true;
        }

        // Turn over
        TurnOverButton.OnInteract += delegate () { PressTurnOver(); return false; };

        // Reset
        ResetButton.OnInteract += delegate () { PressReset(); return false; };

        Bomb.OnBombExploded += delegate
        {
            if (!_isSolved && _actionLog.Count > 0)
            {
                Debug.LogFormat("[Rubik’s Clock #{0}] Actions performed before bomb exploded:{1}", _moduleId, FormatActions());
            }
        };

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

        // Scramble
        Scramble(4);
        for (int count = 0; count < _moves.Count; count++)
        {
            var move = _moves[count];
            Debug.LogFormat("[Rubik’s Clock #{0}] Moves to solve, move {1}:", _moduleId, count + 1);
            Debug.LogFormat("[Rubik’s Clock #{0}] - Lit clock: {1}. Lit pin: {2}.", _moduleId, _toDir9[move.LitClock], _toDir4[move.LitPin]);
            Debug.LogFormat("[Rubik’s Clock #{0}] - {1}, {2} ({3}). {4}, {5} ({6}).", _moduleId,
                move.Modifications[0].Action.Description,
                move.Modifications[0].Amount.Quantity,
                move.Modifications[0].Amount.Description,
                move.Modifications[1].Action.Description,
                move.Modifications[1].Amount.Quantity,
                move.Modifications[1].Amount.Description
            );
            Debug.LogFormat("[Rubik’s Clock #{0}] - Big square: {1}, Small square: {2}.", _moduleId, _toDir9[move.BigSquare], _toDir4[move.SmallSquare]);
            Debug.LogFormat("[Rubik’s Clock #{0}] - Change pins {1}, Rotate gear {2} for {3}.", _moduleId,
                String.Join(" and ", move.Pins.ConvertAll(i => _toDir4[i]).ToArray()),
                _toDir4[move.Gear],
                move.Amount
            );
        }

        // If the first move is on the back, turn over
        if (!_moves[0].OnFrontSide)
        {
            ClockPuzzle.transform.localEulerAngles = new Vector3(
                ClockPuzzle.transform.localEulerAngles.x,
                ClockPuzzle.transform.localEulerAngles.y,
                180
            );
            _onFrontSide = !_onFrontSide;
        }

        // This should light the pin and clock for the first move
        CheckState();

        StartCoroutine(AnimateMovements());
    }

    /// <summary>
    /// Random moves for scramble. Reverse moves and apply in reverse order, following the manual will solve it.
    /// </summary>
    private void Scramble(int numMoves)
    {
        _isScrambling = true;

        bool onFrontSide = true;

        // Determine first modifications, using ascii conversion to convert ABC to 0, DEF to 1, etc.
        var sn = Bomb.GetSerialNumber();
        var firstModificationAction1 = (Char.IsDigit(sn[0]) ? (int)sn[0] - 22 : (int)sn[0] - 65) / 3;
        var firstModificationAmount1 = (Char.IsDigit(sn[1]) ? (int)sn[1] - 22 : (int)sn[1] - 65) / 3;
        var firstModificationAction2 = (Char.IsDigit(sn[2]) ? (int)sn[2] - 22 : (int)sn[2] - 65) / 3;
        var firstModificationAmount2 = (Char.IsDigit(sn[3]) ? (int)sn[3] - 22 : (int)sn[3] - 65) / 3;
        Debug.LogFormat(
            "[Rubik’s Clock #{0}] Initial modifications: Action 1: row {1}. Amount 1: row {2}. Action 2: row {3}. Amount 2: row {4}",
            _moduleId, firstModificationAction1 + 1, firstModificationAmount1 + 1, firstModificationAction2 + 1, firstModificationAmount2 + 1
        );

        // Apply moves
        for (var curMove = 0; curMove < numMoves; curMove++)
        {
            // Turn over
            onFrontSide = !onFrontSide;

            int gear;
            int amount;
            Move move;
            bool[] changePins;

            do
            {
                // Random lit pin and clock, but not the same as previous move
                int litPin;
                int litClock;
                bool same = true;
                do
                {
                    litPin = Rnd.Range(0, 4);
                    litClock = Rnd.Range(0, 9);
                    same = (_moves.Count > 0) &&
                        (_moves[0].LitPin == _mirror4[litPin]) &&
                        (_moves[0].LitClock == _mirror9[litClock]);
                }
                while (same);

                move = new Move()
                {
                    LitPin = litPin,
                    LitClock = litClock,
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
                        var col = (move.BigSquare % 3) * 2 + (move.SmallSquare % 2);
                        var row = (move.BigSquare / 3) * 2 + (move.SmallSquare / 2);
                        var step = (modification.Action.MainType == ModificationAction.MainTypeEnum.MoveBig ? 2 : 1);

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

                        // And convert back
                        move.BigSquare = (row / 2 * 3) + (col / 2);
                        move.SmallSquare = (row % 2 * 2) + (col % 2);
                    }
                }

                // Initial rotation
                gear = _manualMoves[move.SmallSquare, move.BigSquare, 2];
                amount = _manualMoves[move.SmallSquare, move.BigSquare, 3];

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
                move.Gear = gear;
                move.Amount = amount;

                // Invert rotation if on back side
                if (!move.OnFrontSide)
                {
                    gear = _mirror4[gear];
                    amount = -amount;
                }

                // Initial pins to change
                var pin1 = _manualMoves[move.SmallSquare, move.BigSquare, 0];
                var pin2 = _manualMoves[move.SmallSquare, move.BigSquare, 1];
                if (!move.OnFrontSide)
                {
                    pin1 = _mirror4[pin1];
                    pin2 = _mirror4[pin2];
                }
                changePins = new bool[4];
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
                        for (var i = 0; i < changePins.Length; i++)
                        {
                            changePins[i] = !changePins[i];
                        }
                    }
                }

            } while (amount == 0);

            // Rotate inversed at scramble time
            RotateGear(gear, -amount);

            // Change pins
            move.Pins = new List<int>();
            for (var i = 0; i < 4; i++)
            {
                if (changePins[i])
                {
                    ChangePin(i);
                    move.Pins.Add(move.OnFrontSide ? i : _mirror4[i]);
                }
            }

            // Add to scramble
            move.ClocksAtStart = (int[])_clocks.Clone();
            move.PinsAtStart = (bool[])_pins.Clone();
            _moves.Insert(0, move);
        }

        _isScrambling = false;
    }

    private void LightPinAndClock(Move move)
    {
        var litPin = -1;
        var litClockFront = -1;
        var litClockBack = -1;
        if (move.LitPin >= 0)
        {
            litPin = move.OnFrontSide ? move.LitPin : _mirror4[move.LitPin];
            litClockFront = move.OnFrontSide ? move.LitClock : _mirror9[move.LitClock];
            litClockBack = move.OnFrontSide ? (move.LitClock + 9) : (_mirror9[move.LitClock] + 9);
        }
        var lit = false;

        for (var i = 0; i < Pins.Length; i++)
        {
            lit = (i == litPin);
            Pins[i].transform.Find("LightFront").GetComponent<Light>().enabled = lit;
            Pins[i].transform.Find("LightBack").GetComponent<Light>().enabled = lit;
            Pins[i].GetComponent<Renderer>().material = (lit ? LitMaterial : UnlitMaterial);
        }

        for (var i = 0; i < Clocks.Length; i++)
        {
            lit = (i == litClockFront || i == litClockBack);
            Clocks[i].transform.Find("Light").GetComponent<Light>().enabled = lit;
            Clocks[i].GetComponent<Renderer>().material = (lit ? LitMaterial : UnlitMaterial);
        }
    }

    // Called once per frame
    void Update()
    {
    }

    private void PressTurnOver()
    {
        if (_isSolved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        TurnOver();
    }

    private void TurnOver()
    {
        _onFrontSide = !_onFrontSide;
        _animationQueue.Enqueue(new TurnOverAction() { ToFrontSide = _onFrontSide });

        if (!_isScrambling && !_isResetting)
        {
            _actionLog.Add(new TurnOverAction() { ToFrontSide = _onFrontSide });

        }
    }

    private void PressReset()
    {
        if (_isSolved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        ResetModule();
    }

    private void ResetModule()
    {
        _isResetting = true;

        // Log actions and clean action log
        Debug.LogFormat("[Rubik’s Clock #{0}] Actions performed before reset:{1}", _moduleId, FormatActions());
        _actionLog = new List<IAction>();

        // Pour reset stack into animation queue
        while (_resetStack.Count > 0)
        {
            _animationQueue.Enqueue(_resetStack.Pop());
        }
        if (_moves[0].OnFrontSide != _onFrontSide)
        {
            _animationQueue.Enqueue(new TurnOverAction() { ToFrontSide = _moves[0].OnFrontSide });
        }

        // Reset to initial scrambled state
        _clocks = (int[])_moves[0].ClocksAtStart.Clone();
        _pins = (bool[])_moves[0].PinsAtStart.Clone();
        _onFrontSide = _moves[0].OnFrontSide;
        LightPinAndClock(_moves[0]);

        _isResetting = false;
    }

    private string FormatActions()
    {
        if (_actionLog.Count == 0)
        {
            return "None.";
        }

        var msgs = new List<string>() { "" };
        foreach (var action in _actionLog)
        {
            if (action is GearAction)
            {
                var gearAction = (GearAction)action;
                msgs.Add("- Rotate gear " + (_toDir4[gearAction.OnFrontSide ? gearAction.Gear : _mirror4[gearAction.Gear]])
                    + " for " + (gearAction.OnFrontSide ? gearAction.Amount : -gearAction.Amount) + ".");
            }
            else if (action is PinAction)
            {
                var pinAction = (PinAction)action;
                msgs.Add("- Change pin " + (_toDir4[pinAction.OnFrontSide ? pinAction.Pin : _mirror4[pinAction.Pin]]) + ".");
            }
            else if (action is TurnOverAction)
            {
                var turnOverAction = (TurnOverAction)action;
                msgs.Add("- Turn over to the " + (turnOverAction.ToFrontSide ? "front" : "back") + " side.");
            }
        }
        return String.Join(string.Format("\n[Rubik’s Clock #{0}] ", _moduleId), msgs.ToArray());
    }

    private void PressPinButton(int i)
    {
        if (_isSolved) return;

        ChangePin(_onFrontSide ? i : _mirror4[i]);
    }

    private void ChangePin(int i)
    {
        _pins[i] = !_pins[i];
        _animationQueue.Enqueue(new PinAction() { Pin = i, Position = _pins[i] });

        // Record the steps so we can reset the pins later
        if (!_isScrambling && !_isResetting)
        {
            _actionLog.Add(new PinAction() { Pin = i, Position = _pins[i], OnFrontSide = _onFrontSide });

            // If the previous step was changing the same pin, they cancel eachother out
            if (
                _resetStack.Count > 0
                && _resetStack.Peek() is PinAction
                && ((PinAction)_resetStack.Peek()).Pin == i
            )
            {
                _resetStack.Pop();
            }
            else
            {
                _resetStack.Push(new PinAction() { Pin = i, Position = !_pins[i] });
            }
        }
    }

    private void PressGearButton(int i)
    {
        if (_isSolved) return;

        // 0=TL, 1=TR, 2=BL, 3=BR
        var gear = i / 2;

        // -1=CCW, 1=CW
        var amount = (i % 2) * 2 - 1;

        // Mirror if needed
        if (!_onFrontSide)
        {
            gear = _mirror4[gear];
            amount = -amount;
        }

        RotateGear(gear, amount);
        CheckState();
    }

    /// <param name="gear">0=TL, 1=TR, 2=BL, 3=BR</param>
    /// <param name="amount">Number of hours, negative is counterclockwise</param>
    private void RotateGear(int gear, int amount)
    {
        if (_isSolved) return;

        switch (gear)
        {
            case 0:
                RotateClocks(gear, amount, new Boolean[] {
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
                RotateClocks(gear, amount, new Boolean[] {
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
                RotateClocks(gear, amount, new Boolean[] {
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
                RotateClocks(gear, amount, new Boolean[] {
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

    private void RotateClocks(int gear, int amount, Boolean[] conditions)
    {
        var hourChanges = new int[18];
        for (var i = 0; i < conditions.Length; i++)
        {
            if (conditions[i])
            {
                // Adding a large product of 12 for extreme conditions, making sure clocks stay positive ints
                // Clocks on the back (9..17) rotate the other way
                _clocks[i] = (_clocks[i] + amount * (i >= 9 ? -1 : 1) + 144) % 12;
                hourChanges[i] = amount * (i >= 9 ? -1 : 1);
            }
        }

        // Enqueue the animation
        _animationQueue.Enqueue(new GearAction() { Gear = gear, Amount = amount, HourChanges = hourChanges });

        // Record the steps so we can reset the clocks later
        if (!_isScrambling && !_isResetting)
        {
            // If the previous step was a rotation on the same gear, merge it with this one
            if (
                _resetStack.Count > 0
                && _resetStack.Peek() is GearAction
                && ((GearAction)_resetStack.Peek()).Gear == gear
            )
            {
                var gearAction = (GearAction)_resetStack.Pop();
                for (var i = 0; i < gearAction.HourChanges.Length; i++)
                {
                    gearAction.HourChanges[i] -= hourChanges[i];
                }
                _resetStack.Push(gearAction);
            }
            else
            {
                _resetStack.Push(new GearAction() { Gear = gear, Amount = amount, HourChanges = hourChanges.Select(i => -i).ToArray() });
            }

            // Same for action log
            if (
                _actionLog.Count > 0
                && _actionLog[_actionLog.Count - 1] is GearAction
                && ((GearAction)_actionLog[_actionLog.Count - 1]).Gear == gear
            )
            {
                var gearAction = (GearAction)_actionLog[_actionLog.Count - 1];
                gearAction.Amount += amount;
                _actionLog.RemoveAt(_actionLog.Count - 1);
                _actionLog.Add(gearAction);
            }
            else
            {
                _actionLog.Add(new GearAction() { Gear = gear, Amount = amount, OnFrontSide = _onFrontSide });
            }
        }
    }

    private void CheckState()
    {
        // If all clocks are 12 o'clock
        if (_clocks.SequenceEqual(Enumerable.Repeat(0, 18).ToArray()))
        {
            // The module is solved
            _isSolved = true;
            LightPinAndClock(new Move() { LitClock = -1, LitPin = -1 });
            Debug.LogFormat("[Rubik’s Clock #{0}] Actions performed to solve:{1}", _moduleId, FormatActions());
        }

        // If the clocks are in the starting position of a move
        foreach (Move move in _moves)
        {
            // Light the pin and clock belonging to the move
            if (_clocks.SequenceEqual(move.ClocksAtStart) && _pins.SequenceEqual(move.PinsAtStart))
            {
                LightPinAndClock(move);
                break;
            }
        }
    }

    private IEnumerator AnimateMovements()
    {
        while (true)
        {
            while (_animationQueue.Count == 0)
            {
                if (_isSolved)
                {
                    GetComponent<KMBombModule>().HandlePass();
                    yield break;
                }

                yield return null;
            }

            var action = _animationQueue.Dequeue();

            if (action is GearAction)
            {
                var gearAction = (GearAction)action;
                var hourChanges = gearAction.HourChanges;
                var initialRotations = new Vector3[hourChanges.Length];
                var targetRotations = new Vector3[hourChanges.Length];
                for (var i = 0; i < hourChanges.Length; i++)
                {
                    initialRotations[i] = Clocks[i].transform.localEulerAngles;
                    targetRotations[i] = new Vector3(
                        Clocks[i].transform.localEulerAngles.x,
                        Clocks[i].transform.localEulerAngles.y + hourChanges[i] * 30 * (i >= 9 ? -1 : 1),
                        Clocks[i].transform.localEulerAngles.z
                    );
                }

                var hoursToChange = gearAction.HourChanges.Max(i => Math.Abs(i));
                var hoursPassed = 0;
                var duration = .2f * hoursToChange;
                var elapsed = 0f;
                var smoothStep = 0f;

                while (elapsed < duration)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    smoothStep = Mathf.SmoothStep(0.0f, 1.0f, elapsed / duration);
                    for (var i = 0; i < hourChanges.Length; i++)
                    {
                        Clocks[i].transform.localEulerAngles = Vector3.Lerp(
                            initialRotations[i],
                            targetRotations[i],
                            smoothStep
                        );
                    }
                    if (smoothStep * hoursToChange > hoursPassed)
                    {
                        hoursPassed++;
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                        GetComponent<KMSelectable>().AddInteractionPunch(.1f);
                    }
                }
            }
            else if (action is PinAction)
            {
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                GetComponent<KMSelectable>().AddInteractionPunch(.3f);

                var pinAction = (PinAction)action;
                var pin = pinAction.Pin;
                var position = pinAction.Position;
                var initialPosition = Pins[pin].transform.localPosition;
                var targetPosition = new Vector3(
                    Pins[pin].transform.localPosition.x,
                    (position ? .25f : -.25f),
                    Pins[pin].transform.localPosition.z
                );

                var duration = .2f;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    Pins[pin].transform.localPosition = Vector3.Lerp(
                        initialPosition,
                        targetPosition,
                        Mathf.SmoothStep(0.0f, 1.0f, elapsed / duration)
                    );
                }
            }
            else if (action is TurnOverAction)
            {
                var turnOverAction = (TurnOverAction)action;
                var initialRotation = ClockPuzzle.transform.localEulerAngles;
                var targetRotation = new Vector3(
                    ClockPuzzle.transform.localEulerAngles.x,
                    ClockPuzzle.transform.localEulerAngles.y,
                    turnOverAction.ToFrontSide ? 0 : 180
                );
                var initialPosition = ClockPuzzle.transform.localPosition;
                var targetPosition = new Vector3(
                    ClockPuzzle.transform.localPosition.x,
                    ClockPuzzle.transform.localPosition.y + .06f,
                    ClockPuzzle.transform.localPosition.z
                );

                var duration = 1f;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    ClockPuzzle.transform.localEulerAngles = Vector3.Lerp(
                        initialRotation,
                        targetRotation,
                        Mathf.SmoothStep(0.0f, 1.0f, elapsed / duration)
                    );
                    ClockPuzzle.transform.localPosition = Vector3.Lerp(
                        initialPosition,
                        targetPosition,
                        Mathf.SmoothStep(0.0f, 1.0f, elapsed < (duration / 2) ? (elapsed / duration * 2) : (elapsed / duration * -2 + 2))
                    );
                }
            }
        }
    }

    private IEnumerator TiltCamera()
    {
        yield return null;
        bool frontFace = transform.parent.parent.localEulerAngles.z < 45 || transform.parent.parent.localEulerAngles.z > 315;

        float Angle = -30;
        Vector3 lerpAngle = new Vector3(frontFace ? -Angle : Angle, 0, 0);

        float currentTime = Time.time;
        while (Time.time < (currentTime + 1.0f))
        {
            var lerp = Quaternion.Euler(Vector3.Lerp(Vector3.zero, lerpAngle, (Time.time - currentTime) / 1.0f));
            yield return new Quaternion[] { lerp, lerp };
            yield return null;
        }
        yield return new Quaternion[] { Quaternion.Euler(lerpAngle), Quaternion.Euler(lerpAngle) };

        yield return new WaitForSeconds(4.0f);

        currentTime = Time.time;
        while (Time.time < (currentTime + 1.0f))
        {
            var lerp = Quaternion.Euler(Vector3.Lerp(lerpAngle, Vector3.zero, (Time.time - currentTime) / 1.0f));
            yield return new Quaternion[] { lerp, lerp };
            yield return null;
        }
        yield return new Quaternion[] { Quaternion.Euler(Vector3.zero), Quaternion.Euler(Vector3.zero) };
        yield return null;
    }

    IEnumerator ProcessTwitchCommand(string commands)
    {
        var directions = new string[] { "tl", "tr", "bl", "br" };
        var actions = new List<Func<object>>();

        foreach (var command in commands.ToLowerInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int amount;
            var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // {direction} {amount} = rotate gear
            if (parts.Length == 2 && directions.Contains(parts[0]) && int.TryParse(parts[1], out amount))
            {
                actions.Add(() =>
                {
                    var index = Array.FindIndex(directions, row => row == parts[0]);
                    RotateGear(_onFrontSide ? index : _mirror4[index], _onFrontSide ? amount : -amount);
                    CheckState();
                    if (_isSolved) return "solve";
                    return 0.1f;
                });
            }

            // {direction} = change pin
            else if (parts.Length == 1 && directions.Contains(parts[0]))
            {
                actions.Add(() =>
                {
                    var index = Array.FindIndex(directions, row => row == parts[0]);
                    ChangePin(_onFrontSide ? index : _mirror4[index]);
                    return 0.1f;
                });
            }

            // t = turn over
            else if (parts.Length == 1 && (parts[0] == "t" || parts[0] == "turn"))
            {
                actions.Add(() =>
                {
                    PressTurnOver();
                    return 0.1f;
                });
            }

            // r = reset
            else if (parts.Length == 1 && (parts[0] == "r" || parts[0] == "reset"))
            {
                actions.Add(() =>
                {
                    PressReset();
                    return 0.1f;
                });
            }

            // tilt / rotate for Twitch Plays
            else if (parts.Length == 1 && (parts[0] == "tilt" || parts[0] == "rotate"))
            {
                actions.Add(TiltCamera);
            }

            else
            {
                yield return string.Format("sendtochaterror bad move: '{0}'. Use help to see what the valid commands are.", command);
                yield break;
            }
        }

        //Make sure the module is focused before performing any actions.
        if (actions.Count > 0)
            yield return null; 

        foreach (var action in actions)
        {
            var result = action();
            if (result == null)
            {
                yield return "sendtochaterror Something bad happened.";
                yield break;
            }
            else if (result is float)
                yield return new WaitForSeconds((float)result);
            else if (result is string)
                yield return result;
            else if (result is IEnumerator)
            {
                IEnumerator iResult = (IEnumerator)result;
                while (iResult.MoveNext()) yield return iResult.Current;
            }
        }
    }

    /**
     * A move that should be performed on the module in order to solve it.
     * Includes the lit clock and pin, two modifications and the final instructions to follow.
     */
    struct Move
    {
        // Lit clock and pin for initial instruction cell
        public int LitClock { get; set; }
        public int LitPin { get; set; }

        // Instruction cell after possible move by modification
        public int BigSquare { get; set; }
        public int SmallSquare { get; set; }

        // If the module clocks and pins match these clocks and pins, you arrived at this move, let's light the clock and pin
        public int[] ClocksAtStart { get; set; }
        public bool[] PinsAtStart { get; set; }

        // Is this move to be done front or back side?
        public bool OnFrontSide { get; set; }

        // Modifications applied to the move
        public Modification[] Modifications { get; set; }

        // Final instructions
        public int Gear { get; set; }
        public int Amount { get; set; }
        public List<int> Pins { get; set; }
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

    interface IAction { }

    struct GearAction : IAction
    {
        public int Gear { get; set; }
        public int Amount { get; set; }
        public int[] HourChanges { get; set; }
        public bool OnFrontSide { get; set; }
    }

    struct PinAction : IAction
    {
        public int Pin { get; set; }
        public bool Position { get; set; }
        public bool OnFrontSide { get; set; }
    }

    struct TurnOverAction : IAction
    {
        public bool ToFrontSide { get; set; }
    }
}