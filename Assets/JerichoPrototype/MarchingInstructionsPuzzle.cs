using System.Collections.Generic;
using UnityEngine;

namespace JerichoPrototype
{
    /// <summary>Four instruction cards selected in sequence; shared by briefing UI and verification.</summary>
    public sealed class MarchingInstructionsPuzzle
    {
        public static readonly string[] Cards = {
            "March once each day for six days.",
            "On the seventh day, march around seven times.",
            "Listen for the long trumpet blast.",
            "Shout!"
        };
        public readonly List<int> Order = new List<int>();
        public bool Solved { get; private set; }
        public string Feedback { get; private set; } = "Choose the cards below in the order the events happen.";
        public void Select(int card)
        {
            if (Solved || card < 0 || card >= Cards.Length || Order.Contains(card)) return;
            Order.Add(card);
            Feedback = Order.Count == 4 ? "All four cards placed. Press Check Order." : "Choose the next instruction.";
        }
        public void Remove(int slot)
        {
            if (Solved || slot < 0 || slot >= Order.Count) return;
            Order.RemoveAt(slot); Feedback = "Card returned below. Choose the next instruction.";
        }
        public bool Check()
        {
            if (Order.Count != 4) { Feedback = "Place all four cards first."; return false; }
            string[] hints = {
                "Hint: begin with what happens on the first six days.",
                "Hint: the seventh day's seven circuits come next.",
                "Hint: listen for the long trumpet blast before shouting.",
                "Hint: the shout comes last."
            };
            for (int i = 0; i < 4; i++) if (Order[i] != i) { Feedback = hints[i]; return false; }
            Solved = true; Feedback = "Correct! You know the order. Begin the march when you are ready."; return true;
        }
        public void Reset() { Order.Clear(); Solved = false; Feedback = "Choose the cards below in the order the events happen."; }
    }
}
