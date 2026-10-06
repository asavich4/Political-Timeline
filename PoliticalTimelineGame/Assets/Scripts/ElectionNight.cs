using System;
using System.Linq;

namespace PoliticalTimeline
{
    // Presentation only: revealing returns never reruns or changes an election.
    public sealed class ElectionNight
    {
        public readonly ElectionResult result;
        public readonly bool[] called;
        readonly StateProfile[] states;
        readonly int[] order;
        float elapsed;
        public int Count { get; private set; }
        public int Latest { get; private set; } = -1;
        public int Votes { get; private set; }
        public int OppositionVotes { get; private set; }
        public int House { get; private set; }
        public int OppositionHouse { get; private set; }
        public int Senate { get; private set; }
        public int OppositionSenate { get; private set; }
        public bool Complete => Count==order.Length;
        public string PresidentialCall => Votes>=270?"Your coalition wins the presidency.":OppositionVotes>=270?"Opposition wins the presidency.":Complete?"No majority: the presidency is lost.":"270 electoral votes needed to win.";
        public ElectionNight(ElectionResult election,StateProfile[] profiles)
        {
            result=election; states=profiles; called=new bool[states.Length];
            // Safe states report first; close contests hold the suspense to the end.
            order=Enumerable.Range(0,states.Length).OrderByDescending(i=>Math.Abs(result.margins[i])).ThenBy(i=>i).ToArray();
        }
        public bool Advance(float seconds)
        {
            elapsed+=Math.Max(0,seconds); int before=Count;
            while(!Complete)
            {
                float delay=Count==0?1.0f:Math.Abs(result.margins[order[Count]])<3?.65f:.28f;
                if(elapsed<delay) break;
                elapsed-=delay; ReportNext();
            }
            return Count!=before;
        }
        public void Finish() { while(!Complete) ReportNext(); }
        void ReportNext()
        {
            int i=order[Count++]; called[i]=true; Latest=i;
            if(result.margins[i]>=0) Votes+=states[i].electoralVotes; else OppositionVotes+=states[i].electoralVotes;
            House+=result.houseByState[i]; Senate+=result.senateByState[i];
            if(states[i].abbreviation!="DC")
            {
                OppositionHouse+=states[i].electoralVotes-2-result.houseByState[i];
                OppositionSenate+=2-result.senateByState[i];
            }
        }
    }
}
