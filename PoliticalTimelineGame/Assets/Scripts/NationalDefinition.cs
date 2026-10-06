using System;
using UnityEngine;

namespace PoliticalTimeline
{
    [Serializable]
    public class StateProfile
    {
        public string abbreviation, stateName;
        public int electoralVotes, column, row;
        [Range(-30,30)] public float startingLean;
        public Vector4 interests = new Vector4(1,1,1,1);
    }

    [CreateAssetMenu(menuName="Political Timeline/National Simulation")]
    public class NationalDefinition : ScriptableObject
    {
        public StateProfile[] states=CreateStates();
        [Range(0,10)] public float incumbentAdvantage=2;
        [Min(1)] public int courtRetirementMonths=18;
        [Range(0,435)] public int startingHouseSeats=210;
        [Range(0,100)] public int startingSenateSeats=48;
        [Range(0,10)] public float electoralResistance=4;
        [Range(.1f,1)] public float supportSensitivity=.55f;

        // Electoral weights: National Archives, 2024/2028 allocation. Map positions and leans are game data.
        public static StateProfile[] CreateStates()
        {
            string data=@"AL|Alabama|9|6|6
AK|Alaska|3|0|0
AZ|Arizona|11|2|5
AR|Arkansas|6|5|5
CA|California|54|0|4
CO|Colorado|10|3|4
CT|Connecticut|7|10|2
DE|Delaware|3|10|4
DC|District of Columbia|3|11|5
FL|Florida|30|8|7
GA|Georgia|16|7|6
HI|Hawaii|4|0|7
ID|Idaho|4|2|2
IL|Illinois|19|6|3
IN|Indiana|11|7|3
IA|Iowa|6|5|3
KS|Kansas|6|4|4
KY|Kentucky|8|7|4
LA|Louisiana|8|5|6
ME|Maine|4|11|0
MD|Maryland|10|9|5
MA|Massachusetts|11|11|2
MI|Michigan|15|7|2
MN|Minnesota|10|5|1
MS|Mississippi|6|6|5
MO|Missouri|10|5|4
MT|Montana|4|3|1
NE|Nebraska|5|4|3
NV|Nevada|6|1|3
NH|New Hampshire|4|11|1
NJ|New Jersey|14|10|3
NM|New Mexico|5|3|5
NY|New York|28|9|2
NC|North Carolina|16|9|6
ND|North Dakota|3|4|1
OH|Ohio|17|8|3
OK|Oklahoma|7|4|5
OR|Oregon|8|1|2
PA|Pennsylvania|19|9|3
RI|Rhode Island|4|12|2
SC|South Carolina|9|8|6
SD|South Dakota|3|4|2
TN|Tennessee|11|7|5
TX|Texas|40|4|6
UT|Utah|6|2|3
VT|Vermont|3|10|1
VA|Virginia|13|9|4
WA|Washington|12|1|1
WV|West Virginia|4|8|4
WI|Wisconsin|10|6|2
WY|Wyoming|3|3|2";
            var lines=data.Split('\n'); var result=new StateProfile[lines.Length];
            for(int i=0;i<lines.Length;i++)
            {
                var p=lines[i].Trim().Split('|');
                result[i]=new StateProfile { abbreviation=p[0],stateName=p[1],electoralVotes=int.Parse(p[2]),column=int.Parse(p[3]),row=int.Parse(p[4]),
                    startingLean=(i*17%25)-12, interests=new Vector4(1+i%3,1+(i+1)%3,1+(i+2)%4,1+(i+3)%2) };
            }
            return result;
        }
    }
}
