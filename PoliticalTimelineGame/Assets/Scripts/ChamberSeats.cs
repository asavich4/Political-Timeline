using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PoliticalTimeline
{
    // One dot per seat, arranged in concentric semicircles.
    [RequireComponent(typeof(CanvasRenderer))]
    public class ChamberSeats : MaskableGraphic
    {
        protected ChamberSeats() { useLegacyMeshGeneration=false; }
        public Color coalitionColor=new Color(.12f,.43f,.48f), oppositionColor=new Color(.73f,.30f,.22f);
        [Range(.5f,1.5f)] public float dotScale=1;
        [field: SerializeField] public int Total { get; private set; }
        [field: SerializeField] public int Allied { get; private set; }
        public void Present(int total,int allied) { Total=total; Allied=Mathf.Clamp(allied,0,total); SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if(Total==0) return;
            int rows=Total>100?8:4; float radiusSum=0;
            for(int row=0;row<rows;row++) radiusSum+=65+row*86f/(rows-1);
            var seats=new List<Vector3>(); int remaining=Total;
            for(int row=0;row<rows;row++)
            {
                float radius=65+row*86f/(rows-1);
                int count=row==rows-1?remaining:Mathf.RoundToInt(Total*radius/radiusSum); remaining-=count;
                for(int i=0;i<count;i++)
                {
                    float angle=Mathf.PI*(i+.5f)/count;
                    seats.Add(new Vector3(170+Mathf.Cos(angle)*radius,5+Mathf.Sin(angle)*radius,angle));
                }
            }
            seats.Sort((a,b)=>b.z.CompareTo(a.z));
            var rect=GetPixelAdjustedRect(); float dot=(Total>100?2.4f:4.3f)*dotScale;
            for(int i=0;i<seats.Count;i++)
            {
                Color c=i<Allied?coalitionColor:oppositionColor;
                int start=mesh.currentVertCount;
                for(int v=0;v<8;v++)
                {
                    float angle=v*Mathf.PI/4;
                    mesh.AddVert(new Vector3(rect.x+(seats[i].x+Mathf.Cos(angle)*dot)/340*rect.width,
                        rect.y+(seats[i].y+Mathf.Sin(angle)*dot)/160*rect.height),c,Vector2.zero);
                }
                for(int v=1;v<7;v++) mesh.AddTriangle(start,start+v,start+v+1);
            }
        }
    }
}
