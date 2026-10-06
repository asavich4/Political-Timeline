using UnityEngine;
using UnityEngine.UI;

namespace PoliticalTimeline
{
    // Flat UI geometry stays sharp at phone and desktop resolutions.
    [ExecuteAlways]
    public class AdvisorArt : MaskableGraphic
    {
        [SerializeField] string advisor="Chief of Staff", category="DOMESTIC POLICY";
        [SerializeField] PortraitDesign design;
        public bool customPalette;
        public Color backdrop=new Color(.47f,.58f,.55f), skinTone=new Color(.71f,.47f,.33f), hairColor=new Color(.24f,.22f,.20f);
        public void Present(DecisionCard card) { advisor=card.advisor; category=card.category; design=card.design; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            bool court=category=="THE COURT", congress=category=="CONGRESS";
            int seed=0; foreach(char c in advisor) seed=(seed*31+c)&0x7fffffff;
            int style=design==PortraitDesign.Default?seed%6:(int)design-1;
            Color ink=Hex("293838"), paper=Hex("E9DFC3"), hair=Hex(style==2?"D1CCB8":"3D3934");
            Color skin=Hex(style%3==0?"B87954":style%3==1?"D5A779":"8F5E45");
            Color ground=Hex(court?"777E87":congress?"AD795B":style%2==0?"77938B":"B3A47C");
            string[] palettes={"77938B","9C8769","799EA0","9A9B6C","918588","6B8A7D","AB8069"};
            if(design!=PortraitDesign.Default) ground=Hex(palettes[(int)design]);
            if(customPalette) { ground=backdrop; skin=skinTone; hair=hairColor; }
            Box(mesh,ground,0,0,100,100);
            // A quiet institutional silhouette behind the speaker.
            Color architecture=Color.Lerp(ground,paper,.22f);
            if(court || congress)
            {
                Poly(mesh,architecture,12,69,50,91,88,69);
                for(int i=0;i<5;i++) Box(mesh,architecture,17+i*15,19,6,46);
                Box(mesh,architecture,10,14,80,5);
            }
            else Box(mesh,architecture,12,14,76,72);
            switch(design)
            {
                case PortraitDesign.Engineer:
                    Box(mesh,architecture,4,15,8,52); Box(mesh,architecture,82,15,12,66); break;
                case PortraitDesign.Medic:
                    Box(mesh,paper,78,66,16,5); Box(mesh,paper,83,61,5,16); break;
                case PortraitDesign.Farmer:
                    Poly(mesh,Hex("70825D"),0,0,100,0,100,32,0,51);
                    for(int i=0;i<4;i++) Box(mesh,Hex("C9B876"),3+i*25,9,2,27); break;
                case PortraitDesign.Reporter:
                    Box(mesh,paper,75,47,19,38); for(int i=0;i<5;i++) Box(mesh,ground,78,52+i*6,12,2); break;
                case PortraitDesign.Teacher:
                    Box(mesh,Hex("3C6158"),8,46,84,43); Box(mesh,paper,14,78,19,2); Box(mesh,paper,72,70,14,2); break;
                case PortraitDesign.Organizer:
                    Poly(mesh,paper,7,76,25,76,21,88,11,88); Box(mesh,ink,15,50,2,26); break;
            }
            if(style==1 || style==4) Poly(mesh,hair,25,21,24,61,31,79,64,81,77,61,75,21);
            Poly(mesh,court?ink:Hex(style%2==0?"354F52":"654F48"),12,0,17,26,36,36,64,36,83,26,88,0);
            Box(mesh,skin,43,29,14,17);
            Poly(mesh,paper,34,34,43,37,50,28,43,17);
            Poly(mesh,paper,66,34,57,37,50,28,57,17);
            Poly(mesh,Hex("B76C4C"),50,28,54,21,52,5,48,5,46,21);
            Poly(mesh,skin,31,65,36,77,63,77,70,64,66,45,56,36,43,36,33,46);
            Poly(mesh,Color.Lerp(skin,ink,.13f),53,72,70,64,66,45,56,36,51,36);
            Poly(mesh,hair,29,62,29,75,39,84,64,82,72,72,70,61,62,73,40,71);
            if(style==3) { Box(mesh,ink,32,57,15,9); Box(mesh,ink,55,57,14,9); Box(mesh,ink,47,61,8,2); Box(mesh,skin,35,59,9,5); Box(mesh,skin,58,59,8,5); }
            Box(mesh,ink,39,60,3,2); Box(mesh,ink,59,60,3,2);
            Poly(mesh,Color.Lerp(skin,ink,.25f),50,60,47,49,54,49);
            Box(mesh,ink,45,43,12,1.4f);
            if(style==5) Poly(mesh,hair,38,46,45,41,57,41,65,47,60,37,44,35);
            if(advisor=="Labor Secretary") { Box(mesh,Hex("D8B064"),26,70,48,5); Poly(mesh,Hex("D8B064"),32,75,36,85,62,85,69,75); }
            if(court) { Box(mesh,paper,44,16,4,12); Box(mesh,paper,52,16,4,12); }
            if(congress) Box(mesh,Hex("D8B064"),69,23,4,4);
            switch(design)
            {
                case PortraitDesign.Engineer:
                    Box(mesh,Hex("D8B064"),26,70,48,5); Poly(mesh,Hex("D8B064"),32,75,36,85,62,85,69,75);
                    Box(mesh,Hex("D8B064"),25,0,6,26); Box(mesh,Hex("D8B064"),69,0,6,26); break;
                case PortraitDesign.Medic:
                    Poly(mesh,paper,12,0,17,26,35,35,44,0); Poly(mesh,paper,56,0,65,35,83,26,88,0);
                    Box(mesh,Hex("A45849"),68,15,12,4); Box(mesh,Hex("A45849"),72,11,4,12); break;
                case PortraitDesign.Farmer:
                    Poly(mesh,Hex("D5BB81"),23,71,77,71,65,78,35,78); Box(mesh,Hex("D5BB81"),35,77,30,8);
                    Box(mesh,Hex("3F666C"),31,0,38,21); Box(mesh,Hex("3F666C"),31,17,7,17); Box(mesh,Hex("3F666C"),62,17,7,17); break;
                case PortraitDesign.Reporter:
                    Box(mesh,ink,70,4,3,16); Box(mesh,paper,67,18,9,9); break;
                case PortraitDesign.Teacher:
                    Poly(mesh,Hex("B36F50"),34,33,42,36,52,25,62,36,68,33,53,14);
                    Box(mesh,paper,20,5,18,17); Box(mesh,Hex("A56D50"),22,7,14,13); break;
                case PortraitDesign.Organizer:
                    Box(mesh,Hex("D8B064"),69,20,7,7); Poly(mesh,paper,16,6,30,11,30,22,16,27); Box(mesh,ink,13,12,3,9); break;
            }
        }
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out var c); return c; }
        void Box(VertexHelper mesh,Color c,float x,float y,float w,float h) => Poly(mesh,c,x,y,x+w,y,x+w,y+h,x,y+h);
        void Poly(VertexHelper mesh,Color c,params float[] xy)
        {
            var r=GetPixelAdjustedRect(); int start=mesh.currentVertCount;
            for(int i=0;i<xy.Length;i+=2) mesh.AddVert(new Vector3(r.x+xy[i]*r.width/100,r.y+xy[i+1]*r.height/100),c,Vector2.zero);
            for(int i=1;i<xy.Length/2-1;i++) mesh.AddTriangle(start,start+i,start+i+1);
        }
    }
}
