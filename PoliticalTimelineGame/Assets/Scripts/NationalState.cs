using System;

namespace PoliticalTimeline
{
    public sealed class ElectionResult
    {
        public int year, electoralVotes, houseSeats, senateSeats;
        public bool presidential;
        public float[] margins;
        public int[] houseByState, senateByState;
    }

    public sealed class NationalState
    {
        public readonly StateProfile[] states;
        public readonly int[] court = {1,1,1,1,0,-1,-1,-1,-1}; // 2 vacant; 1 aligned; 0 independent; -1 opposed.
        readonly bool[] senate=new bool[100];
        readonly float advantage;
        readonly int retirementMonths;
        public int houseSeats=222;
        public int SenateSeats { get { int n=0; foreach(bool seat in senate) if(seat) n++; return n; } }
        public int Vacancy => Array.IndexOf(court,2);
        public bool ControlsCongress => houseSeats>=218 && SenateSeats>=51;
        public bool CanResolve(InstitutionRule rule) => rule==InstitutionRule.None ||
            (rule==InstitutionRule.Congress && ControlsCongress) ||
            (rule==InstitutionRule.CourtReview && AlignedJustices>=5) ||
            (rule==InstitutionRule.ConfirmJustice && Vacancy>=0 && SenateSeats>=51);
        public bool Allows(EventCondition condition) => condition==EventCondition.Always ||
            (condition==EventCondition.DividedCongress && !ControlsCongress) ||
            (condition==EventCondition.CourtVacancy && Vacancy>=0);
        public int AlignedJustices { get { int n=0; foreach(int seat in court) if(seat==1) n++; return n; } }
        public NationalState(NationalDefinition config)
        {
            states=config!=null ? config.states : NationalDefinition.CreateStates();
            advantage=config!=null ? config.incumbentAdvantage : 2;
            retirementMonths=config!=null ? Math.Max(1,config.courtRetirementMonths) : 18;
            for(int i=0;i<senate.Length;i++) senate[i]=i<52;
        }
        public float Margin(int index,int[] support)
        {
            var p=states[index]; var w=p.interests;
            float sum=Math.Max(.01f,w.x+w.y+w.z+w.w);
            float approval=(support[0]*w.x+support[1]*w.y+support[2]*w.z+support[3]*w.w)/sum;
            return Math.Max(-49,Math.Min(49,p.startingLean+advantage+(approval-50)*.85f));
        }
        public int ProjectedVotes(int[] support)
        { int total=0; for(int i=0;i<states.Length;i++) if(Margin(i,support)>=0) total+=states[i].electoralVotes; return total; }
        public ElectionResult Elect(int year,int[] support)
        {
            var result=new ElectionResult {year=year,presidential=year%4==0,margins=new float[states.Length],houseByState=new int[states.Length],senateByState=new int[states.Length]};
            int house=0, senator=0;
            for(int i=0;i<states.Length;i++)
            {
                var p=states[i]; float margin=Margin(i,support); result.margins[i]=margin;
                if(margin>=0) result.electoralVotes+=p.electoralVotes;
                if(p.abbreviation=="DC") continue;
                int seats=p.electoralVotes-2;
                result.houseByState[i]=(int)Math.Round(seats*Math.Max(0,Math.Min(1,.5+margin/60)));
                house+=result.houseByState[i];
                for(int j=0;j<2 && senator<100;j++,senator++)
                {
                    if(senator%3==(year/2)%3) senate[senator]=margin+(j==0 ? -2 : 2)>=0;
                    if(senate[senator]) result.senateByState[i]++;
                }
            }
            houseSeats=house; result.houseSeats=house; result.senateSeats=SenateSeats; return result;
        }
        public void AdvanceMonth(int months)
        { if(months>0 && months%retirementMonths==0 && Vacancy<0) court[(months/retirementMonths+3)%9]=2; }
        public bool Nominate()
        { int seat=Vacancy; if(seat<0 || SenateSeats<51) return false; court[seat]=1; return true; }
    }
}
