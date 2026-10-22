namespace JerichoPrototype
{
    // The first six days each have one circuit; day seven has seven.
    public sealed class MarchDays
    {
        public int Day { get; private set; } = 1;
        public int Circuits { get; private set; }
        public int Required => Day == 7 ? 7 : 1;
        public bool DayFinished => Circuits >= Required;
        public bool Complete => Day == 7 && DayFinished;
        public void Restore(int day,int circuits)
        { Day=UnityEngine.Mathf.Clamp(day,1,7);Circuits=UnityEngine.Mathf.Clamp(circuits,0,Required); }
        public void FinishCircuit() { if (!DayFinished) Circuits++; }
        public bool NextMorning()
        {
            if (!DayFinished || Complete) return false;
            Day++; Circuits = 0; return true;
        }
    }
}
