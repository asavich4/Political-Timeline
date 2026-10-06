using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoliticalTimeline
{
    [CreateAssetMenu(menuName="Political Timeline/Portrait Artwork")]
    public class PortraitArtwork : ScriptableObject
    {
        [Serializable] public class Layer
        {
            public string name;
            public bool visible=true;
            public Color color=Color.white;
            public Vector2[] points;
        }
        public List<Layer> layers=new List<Layer>();
    }
}
