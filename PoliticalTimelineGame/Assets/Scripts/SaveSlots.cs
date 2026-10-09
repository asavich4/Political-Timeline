using System;
using System.IO;
using UnityEngine;

namespace PoliticalTimeline
{
    public enum PartyTeam { Democrat, Republican }
    [Serializable] public class CampaignSave
    {
        public int version=1, party, decisions, seed, draws, houseSeats, blocked, startYear;
        public string savedAt, currentCard, lastResult, ending;
        public bool ended, holdsPresidency, electionPending, unreadOutcome;
        public int[] support, court, policies;
        public long[] policyDates;
        public int[] policyParties;
        public PolicyRecord[] policyHistory;
        public bool unreadPolicies;
        public bool[] senate;
        public float[] voterSupport;
        public float[] presidentialCampaign, houseCampaign, senateCampaign;
        public string pendingStory;
        public string[] usedCards;
        public ElectionResult election;
        public int electionReports;
    }
    public static class SaveSlots
    {
        #if UNITY_EDITOR
        public static string TestDirectory;
        #endif
        public static string DirectoryPath {
            get {
                #if UNITY_EDITOR
                if(TestDirectory!=null) return TestDirectory;
                #endif
                return Path.Combine(Application.persistentDataPath,"CampaignSaves");
            }
        }
        static string PathFor(int slot) { if(slot<0 || slot>2) throw new ArgumentOutOfRangeException(nameof(slot)); return Path.Combine(DirectoryPath,"slot-"+(slot+1)+".json"); }
        public static bool Exists(int slot) => File.Exists(PathFor(slot));
        public static CampaignSave Read(int slot)
        {
            var save=JsonUtility.FromJson<CampaignSave>(File.ReadAllText(PathFor(slot)));
            if(save==null || save.version!=1 || save.party<0 || save.party>1 || save.decisions<0 || save.decisions>100000 || save.draws<0 || save.draws>100000 || save.support==null || save.support.Length!=4 || save.court==null || save.court.Length!=9 || save.senate==null || save.senate.Length!=100 || save.voterSupport==null || save.voterSupport.Length!=51 || save.usedCards==null || save.policies==null || save.policyDates==null || save.policies.Length!=save.policyDates.Length || (save.electionPending && save.election==null)) throw new InvalidDataException("This save is damaged or from an unsupported version.");
            if(save.startYear<1 || save.startYear+save.decisions/12>9998 || save.houseSeats<0 || save.houseSeats>435) throw new InvalidDataException("Invalid campaign date or seats.");
            foreach(int value in save.support) if(value<0 || value>100) throw new InvalidDataException("Invalid support values.");
            if(save.electionPending && (save.election.margins==null || save.election.margins.Length!=51 || save.election.houseByState==null || save.election.houseByState.Length!=51 || save.election.senateByState==null || save.election.senateByState.Length!=51)) throw new InvalidDataException("Invalid election results.");
            return save;
        }
        public static void Write(int slot,CampaignSave save)
        {
            Directory.CreateDirectory(DirectoryPath); var path=PathFor(slot); var temp=path+".tmp";
            save.savedAt=DateTime.UtcNow.ToString("o");
            File.WriteAllText(temp,JsonUtility.ToJson(save,true));
            if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
        }
    }
}
