using UnityEngine;
using UnityEngine.UI;

namespace PoliticalTimeline
{
    // Small, filled pictograms drawn directly as UI geometry: crisp at every screen size.
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public class PowerIcon : MaskableGraphic
    {
        protected PowerIcon() { useLegacyMeshGeneration=false; }
        // Keep the values stable so the four saved scene icons update in place.
        public enum Symbol { Hammer = 0, Briefcase = 1, Dollar = 2, Eye = 3 }
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
                case Symbol.Briefcase:
                    Box(mesh,20,46,7,10); Box(mesh,37,46,7,10); Box(mesh,20,53,24,6);
                    Polygon(mesh,new Vector2(7,31),new Vector2(7,45),new Vector2(11,49),new Vector2(53,49),new Vector2(57,45),new Vector2(57,31));
                    Box(mesh,7,9,50,18); Box(mesh,28,23,8,12);
                    break;
                case Symbol.Dollar:
                    // An angular S with a single vertical stroke reads cleanly at 40px.
                    Polygon(mesh,new Vector2(49,51),new Vector2(41,57),new Vector2(22,57),new Vector2(13,48),new Vector2(13,36),new Vector2(22,28),new Vector2(39,28),new Vector2(42,25),new Vector2(42,19),new Vector2(38,15),new Vector2(24,15),new Vector2(17,20),new Vector2(11,12),new Vector2(21,5),new Vector2(41,5),new Vector2(52,15),new Vector2(52,29),new Vector2(43,38),new Vector2(26,38),new Vector2(23,41),new Vector2(23,46),new Vector2(26,49),new Vector2(38,49),new Vector2(43,45));
                    Box(mesh,28,1,7,61);
                    break;
                case Symbol.Eye:
                    // Open almond silhouette: the gaps are transparent in both color themes.
                    Polygon(mesh,new Vector2(2,32),new Vector2(14,45),new Vector2(27,51),new Vector2(37,51),new Vector2(50,45),new Vector2(62,32),new Vector2(50,37),new Vector2(37,43),new Vector2(27,43),new Vector2(14,37));
                    Polygon(mesh,new Vector2(2,32),new Vector2(14,27),new Vector2(27,21),new Vector2(37,21),new Vector2(50,27),new Vector2(62,32),new Vector2(50,19),new Vector2(37,13),new Vector2(27,13),new Vector2(14,19));
                    Polygon(mesh,new Vector2(22,28),new Vector2(22,36),new Vector2(28,42),new Vector2(36,42),new Vector2(42,36),new Vector2(42,28),new Vector2(36,22),new Vector2(28,22));
                    break;
            }
        }
        void Box(VertexHelper mesh,float x,float y,float w,float h) => Polygon(mesh,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        void Polygon(VertexHelper mesh,params Vector2[] points)
        {
            // Ear clipping preserves the concave silhouettes.
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
