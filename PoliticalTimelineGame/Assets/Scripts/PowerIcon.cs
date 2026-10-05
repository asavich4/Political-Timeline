using UnityEngine;
using UnityEngine.UI;

namespace PoliticalTimeline
{
    // Small, filled pictograms drawn directly as UI geometry: crisp at every screen size.
    public class PowerIcon : MaskableGraphic
    {
        public enum Symbol { Hammer, Home, Shield, Coins }
        public Symbol symbol;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch(symbol)
            {
                case Symbol.Hammer:
                    Polygon(mesh,new Vector2(16,8),new Vector2(24,4),new Vector2(45,43),new Vector2(37,48));
                    Polygon(mesh,new Vector2(20,45),new Vector2(42,57),new Vector2(55,48),new Vector2(53,39),new Vector2(43,35),new Vector2(34,43),new Vector2(25,37));
                    break;
                case Symbol.Home:
                    Polygon(mesh,new Vector2(4,33),new Vector2(32,57),new Vector2(60,33),new Vector2(54,27),new Vector2(32,46),new Vector2(10,27));
                    Box(mesh,13,8,15,27); Box(mesh,37,8,14,27); Box(mesh,27,26,11,13);
                    break;
                case Symbol.Shield:
                    Polygon(mesh,new Vector2(8,53),new Vector2(32,59),new Vector2(56,53),new Vector2(53,25),new Vector2(44,13),new Vector2(32,5),new Vector2(20,13),new Vector2(11,25));
                    break;
                case Symbol.Coins:
                    Box(mesh,7,9,25,7); Box(mesh,7,20,25,7); Box(mesh,7,31,25,7);
                    Box(mesh,36,9,22,7); Box(mesh,36,20,22,7); Box(mesh,36,31,22,7); Box(mesh,36,42,22,7);
                    break;
            }
        }
        void Box(VertexHelper mesh,float x,float y,float w,float h) => Polygon(mesh,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        void Polygon(VertexHelper mesh,params Vector2[] points)
        {
            // Ear clipping preserves the concave hammer and roof silhouettes.
            var indices=new System.Collections.Generic.List<int>();
            float area=0;
            for(int i=0;i<points.Length;i++) { indices.Add(i); area+=Cross(points[i],points[(i+1)%points.Length]); }
            if(area<0) indices.Reverse();
            var rect=GetPixelAdjustedRect(); int first=mesh.currentVertCount;
            foreach(var point in points) mesh.AddVert(new Vector3(rect.x+point.x/64*rect.width,rect.y+point.y/64*rect.height),color,Vector2.zero);
            int guard=0;
            while(indices.Count>2 && guard++<100)
            {
                bool found=false;
                for(int i=0;i<indices.Count;i++)
                {
                    int a=indices[(i+indices.Count-1)%indices.Count], b=indices[i], c=indices[(i+1)%indices.Count];
                    if(Cross(points[b]-points[a],points[c]-points[b])<=0) continue;
                    bool inside=false;
                    foreach(int p in indices) if(p!=a&&p!=b&&p!=c && Cross(points[b]-points[a],points[p]-points[a])>=0 && Cross(points[c]-points[b],points[p]-points[b])>=0 && Cross(points[a]-points[c],points[p]-points[c])>=0) { inside=true; break; }
                    if(inside) continue;
                    mesh.AddTriangle(first+a,first+b,first+c); indices.RemoveAt(i); found=true; break;
                }
                if(!found) break;
            }
        }
        static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
    }
}
