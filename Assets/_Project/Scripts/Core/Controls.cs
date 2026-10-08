using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniMayhem
{
    /// <summary>
    /// All input actions, built in code (Xbox controller first, keyboard/mouse too). UI navigation for buttons
    /// goes through the EventSystem's default UI actions; these cover gameplay and screen-specific buttons.
    /// </summary>
    public class Controls
    {
        public readonly InputActionMap Map;
        public readonly InputAction Move, Pause, Submit, Back, Reroll, Banish, Skip, Navigate, PrevTab, NextTab, Zoom, Pan, Respec, Debug;

        public Controls()
        {
            Map = new InputActionMap("MiniMayhem");

            Move = Map.AddAction("Move", InputActionType.Value);
            Move.AddBinding("<Gamepad>/leftStick");
            Move.AddBinding("<Gamepad>/dpad");
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            Navigate = Map.AddAction("Navigate", InputActionType.Value);
            Navigate.AddBinding("<Gamepad>/leftStick");
            Navigate.AddBinding("<Gamepad>/dpad");
            Navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            Pause = Button("Pause", "<Gamepad>/start", "<Keyboard>/escape");
            Submit = Button("Submit", "<Gamepad>/buttonSouth", "<Keyboard>/enter", "<Keyboard>/space");
            Back = Button("Back", "<Gamepad>/buttonEast", "<Keyboard>/escape", "<Keyboard>/backspace");
            Reroll = Button("Reroll", "<Gamepad>/buttonNorth", "<Keyboard>/r");
            Banish = Button("Banish", "<Gamepad>/buttonWest", "<Keyboard>/x");
            Skip = Button("Skip", "<Gamepad>/buttonEast", "<Keyboard>/backspace", "<Keyboard>/k");
            PrevTab = Button("PrevTab", "<Gamepad>/leftShoulder", "<Keyboard>/q");
            NextTab = Button("NextTab", "<Gamepad>/rightShoulder", "<Keyboard>/e");
            Respec = Button("Respec", "<Gamepad>/buttonNorth", "<Keyboard>/r");
            Debug = Button("Debug", "<Gamepad>/select", "<Keyboard>/f3");

            Zoom = Map.AddAction("Zoom", InputActionType.Value);
            Zoom.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/leftTrigger").With("Positive", "<Gamepad>/rightTrigger");
            Zoom.AddBinding("<Mouse>/scroll/y").WithProcessor("scale(factor=0.01)");

            Pan = Map.AddAction("Pan", InputActionType.Value);
            Pan.AddBinding("<Gamepad>/rightStick");

            Map.Enable();
        }

        InputAction Button(string name, params string[] paths)
        {
            var a = Map.AddAction(name, InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        public Vector2 MoveValue => Vector2.ClampMagnitude(Move.ReadValue<Vector2>(), 1f);

        public void Dispose()
        {
            Map.Disable();
            Map.Dispose();
        }
    }

    /// <summary>Turns a held stick into discrete menu steps (with repeat), for custom navigation like the skill tree.</summary>
    public class NavRepeater
    {
        Vector2Int last;
        float nextRepeat;

        public Vector2Int Step(Vector2 v)
        {
            Vector2Int dir = Vector2Int.zero;
            if (v.magnitude > 0.5f)
            {
                if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) dir.x = v.x > 0 ? 1 : -1;
                else dir.y = v.y > 0 ? 1 : -1;
            }
            if (dir == Vector2Int.zero) { last = dir; return dir; }
            float now = Time.unscaledTime;
            if (dir != last) { last = dir; nextRepeat = now + 0.35f; return dir; }
            if (now >= nextRepeat) { nextRepeat = now + 0.12f; return dir; }
            return Vector2Int.zero;
        }
    }
}
