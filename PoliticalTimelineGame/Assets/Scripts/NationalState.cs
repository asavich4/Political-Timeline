using System;

namespace PoliticalTimeline
{
    [Serializable] public sealed class ElectionResult
    {
        public int year, electoralVotes, houseSeats, senateSeats;
        public bool presidential;
        public float[] margins, houseMargins, senateMargins;
        public int[] senateContested, senateWon;
        public int[] houseByState, senateByState;
    }

    public sealed class NationalState
    {
        public readonly StateProfile[] states;
        public readonly int[] court = {1,1,1,1,0,-1,-1,-1,-1}; // 2 vacant; 1 aligned; 0 independent; -1 opposed.
        readonly bool[] senate=new bool[100];
        readonly float advantage;
        readonly int retirementMonths;
        readonly float resistance, sensitivity;
        readonly PartyTeam party;
        public bool HoldsPresidency { get; set; } = true;
        readonly float[] voterSupport, presidentialCampaign, houseCampaign, senateCampaign;
        public readonly float CampaignInfluence;
        float[] CampaignBonus(CampaignRace race) => race==CampaignRace.Presidency?presidentialCampaign:race==CampaignRace.House?houseCampaign:race==CampaignRace.Senate?senateCampaign:voterSupport;
        public float Outreach(int index,CampaignRace race) => race==CampaignRace.All?voterSupport[index]:Math.Max(-12,Math.Min(12,voterSupport[index]+CampaignBonus(race)[index]));
        public float ChoiceBonus(int index,CampaignRace race) => CampaignBonus(race)[index];
        public int BlockedGovernmentBills { get; private set; }
        public float VoterSupportBonus(int index) => voterSupport[index];
        public void ApplyVoterEffect(PolicyChoice choice)
        {
            var target=CampaignBonus(choice.campaignRace);
            for(int i=0;i<states.Length;i++) if(choice.voterStates!=null && Array.IndexOf(choice.voterStates,states[i].abbreviation)>=0)
                target[i]=Math.Max(-12,Math.Min(12,target[i]+choice.voterSupportChange));
            if(choice.institution==InstitutionRule.BlockGovernment) BlockedGovernmentBills++;
        }
        public int houseSeats=222;
        public int SenateSeats { get { int n=0; foreach(bool seat in senate) if(seat) n++; return n; } }
        public int Vacancy => Array.IndexOf(court,2);
        public bool ControlsCongress => houseSeats>=218 && SenateSeats>=51;
        public bool CanResolve(InstitutionRule rule) => rule==InstitutionRule.None ||
            (rule==InstitutionRule.Congress && ControlsCongress && HoldsPresidency) ||
            (rule==InstitutionRule.CourtReview && AlignedJustices>=5) ||
            (rule==InstitutionRule.ConfirmJustice && Vacancy>=0 && SenateSeats>=51 && HoldsPresidency) ||
            (rule==InstitutionRule.BlockGovernment && !HoldsPresidency && (houseSeats>=218 || SenateSeats>=41));
        public bool CanResolve(PolicyChoice choice) => CanResolve(choice.institution) &&
            ((choice.enactPolicy==PolicyId.None && choice.repealPolicy==PolicyId.None) || HoldsPresidency);
        public bool Allows(EventCondition condition) => condition==EventCondition.Always ||
            (condition==EventCondition.DividedCongress && !ControlsCongress) ||
            (condition==EventCondition.CourtVacancy && Vacancy>=0 && HoldsPresidency) ||
            (condition==EventCondition.InOpposition && !HoldsPresidency) ||
            condition==EventCondition.CampaignSeason || (condition==EventCondition.InGovernment && HoldsPresidency);
        public int AlignedJustices { get { int n=0; foreach(int seat in court) if(seat==1) n++; return n; } }
        public NationalState(NationalDefinition config,PartyTeam party=PartyTeam.Democrat)
        {
            this.party=party;
            states=config!=null ? config.states : NationalDefinition.CreateStates();
            voterSupport=new float[states.Length];
            presidentialCampaign=new float[states.Length]; houseCampaign=new float[states.Length]; senateCampaign=new float[states.Length];
            CampaignInfluence=config!=null?config.campaignInfluence:1.8f;
            advantage=config!=null ? config.incumbentFatigue : 2;
            retirementMonths=config!=null ? Math.Max(1,config.courtRetirementMonths) : 18;
            resistance=config!=null?config.electoralResistance:4;
            sensitivity=config!=null?config.supportSensitivity:.55f;
            houseSeats=config!=null?config.startingHouseSeats:210;
            for(int i=0;i<senate.Length;i++) senate[i]=i<(config!=null?config.startingSenateSeats:48);
        }
        public float Margin(int index,int[] support) => RaceMargin(index,support,CampaignRace.Presidency);
        public float RaceMargin(int index,int[] support,CampaignRace race)
        {
            var p=states[index]; var w=p.interests;
            float sum=Math.Max(.01f,w.x+w.y+w.z+w.w);
            float approval=(support[0]*w.x+support[1]*w.y+support[2]*w.z+support[3]*w.w)/sum;
            return Math.Max(-49,Math.Min(49,p.startingLean*(party==PartyTeam.Republican?-1.15f:1.15f)+(HoldsPresidency?-advantage:advantage)-resistance+6*(float)Math.Tanh((approval-50)*sensitivity/8)+CampaignInfluence*Outreach(index,race)+(race==CampaignRace.House?((index*7)%9-4):race==CampaignRace.Senate?((index*11)%9-4):0)));
        }
        public int ProjectedVotes(int[] support)
        { int total=0; for(int i=0;i<states.Length;i++) if(Margin(i,support)>=0) total+=states[i].electoralVotes; return total; }
        public ElectionResult Elect(int year,int[] support)
        {
            var result=new ElectionResult {year=year,presidential=year%4==0,margins=new float[states.Length],houseByState=new int[states.Length],senateByState=new int[states.Length],houseMargins=new float[states.Length],senateMargins=new float[states.Length],senateContested=new int[states.Length],senateWon=new int[states.Length]};
            int house=0, senator=0;
            for(int i=0;i<states.Length;i++)
            {
                var p=states[i]; float margin=Margin(i,support); result.margins[i]=margin;
                if(margin>=0) result.electoralVotes+=p.electoralVotes;
                result.houseMargins[i]=RaceMargin(i,support,CampaignRace.House); result.senateMargins[i]=RaceMargin(i,support,CampaignRace.Senate);
                if(p.abbreviation=="DC") continue;
                int seats=p.electoralVotes-2;
                result.houseByState[i]=ProjectedHouseInState(i,support);
                house+=result.houseByState[i];
                for(int j=0;j<2 && senator<100;j++,senator++)
                {
                    if(senator%3==(year/2)%3)
                    {
                        result.senateContested[i]++; senate[senator]=result.senateMargins[i]+(j==0?-2:2)>=0;
                        if(senate[senator]) result.senateWon[i]++;
                    }
                    if(senate[senator]) result.senateByState[i]++;
                }
            }
            houseSeats=house; result.houseSeats=house; result.senateSeats=SenateSeats; return result;
        }
        public int ProjectedHouseInState(int index,int[] support)
        {
            int seats=states[index].abbreviation=="DC"?0:states[index].electoralVotes-2, won=0;
            float margin=RaceMargin(index,support,CampaignRace.House);
            for(int district=0;district<seats;district++) if(margin+(seats==1?0:-22+44f*district/(seats-1))>=0) won++;
            return won;
        }
        int SenateOffset(int index) { int n=0; for(int i=0;i<index;i++) if(states[i].abbreviation!="DC") n+=2; return n; }
        public int SenateContests(int index,int year)
        { if(states[index].abbreviation=="DC") return 0; int start=SenateOffset(index),n=0; for(int j=0;j<2;j++) if((start+j)%3==(year/2)%3) n++; return n; }
        public int ProjectedSenateInState(int index,int year,int[] support)
        {
            if(states[index].abbreviation=="DC") return 0; int start=SenateOffset(index),won=0;
            for(int j=0;j<2;j++) if((start+j)%3==(year/2)%3 ? RaceMargin(index,support,CampaignRace.Senate)+(j==0?-2:2)>=0 : senate[start+j]) won++;
            return won;
        }
        public void AdvanceMonth(int months)
        {
            foreach(var values in new[]{voterSupport,presidentialCampaign,houseCampaign,senateCampaign}) for(int i=0;i<values.Length;i++) values[i]*=.97f;
            if(months>0 && months%retirementMonths==0 && Vacancy<0) court[(months/retirementMonths+3)%9]=2;
        }
        public bool Nominate()
        { int seat=Vacancy; if(seat<0 || SenateSeats<51 || !HoldsPresidency) return false; court[seat]=1; return true; }
        public void Export(CampaignSave save) { save.presidentialCampaign=(float[])presidentialCampaign.Clone(); save.houseCampaign=(float[])houseCampaign.Clone(); save.senateCampaign=(float[])senateCampaign.Clone(); save.senate=(bool[])senate.Clone(); save.court=(int[])court.Clone(); save.voterSupport=(float[])voterSupport.Clone(); save.houseSeats=houseSeats; save.holdsPresidency=HoldsPresidency; save.blocked=BlockedGovernmentBills; }
        public void Restore(CampaignSave save) { if(save.presidentialCampaign!=null) Array.Copy(save.presidentialCampaign,presidentialCampaign,Math.Min(states.Length,save.presidentialCampaign.Length)); if(save.houseCampaign!=null) Array.Copy(save.houseCampaign,houseCampaign,Math.Min(states.Length,save.houseCampaign.Length)); if(save.senateCampaign!=null) Array.Copy(save.senateCampaign,senateCampaign,Math.Min(states.Length,save.senateCampaign.Length)); Array.Copy(save.senate,senate,100); Array.Copy(save.court,court,9); Array.Copy(save.voterSupport,voterSupport,voterSupport.Length); houseSeats=save.houseSeats; HoldsPresidency=save.holdsPresidency; BlockedGovernmentBills=save.blocked; }
    }
}
