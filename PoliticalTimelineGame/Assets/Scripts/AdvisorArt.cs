using UnityEngine;
using UnityEngine.UI;

namespace PoliticalTimeline
{
    // Flat UI geometry stays sharp at phone and desktop resolutions.
    public class AdvisorArt : MaskableGraphic
    {
        [SerializeField] string advisor="Chief of Staff", category="DOMESTIC POLICY";
        public void Present(DecisionCard card) { advisor=card.advisor; category=card.category; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            bool court=category=="THE COURT", congress=category=="CONGRESS";
            int seed=0; foreach(char c in advisor) seed=(seed*31+c)&0x7fffffff;
            int style=seed%6;
            Color ink=Hex("293838"), paper=Hex("E9DFC3"), hair=Hex(style==2?"D1CCB8":"3D3934");
            Color skin=Hex(style%3==0?"B87954":style%3==1?"D5A779":"8F5E45");
            Color ground=Hex(court?"777E87":congress?"AD795B":style%2==0?"77938B":"B3A47C");
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
