using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoliticalTimeline.Editor
{
    public static class IPhonePreview
    {
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;

        [MenuItem("Political Timeline/Preview iPhone Portrait")]
        public static void Open()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            }
            FixSharpness();
        }

        [MenuItem("Political Timeline/Fix Game View Sharpness")]
        public static void FixSharpness()
        {
            var gameViewType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var window=EditorWindow.GetWindow(gameViewType);
            window.Show(); window.maximized=true;
            try
            {
                gameViewType.GetProperty("selectedSizeIndex",Flags).SetValue(window,EnsureSize());
                gameViewType.GetProperty("lowResolutionForAspectRatios",Flags).SetValue(window,false);
                var zoom=gameViewType.GetField("m_ZoomArea",Flags).GetValue(window);
                if(zoom!=null) zoom.GetType().GetField("m_Scale",Flags).SetValue(zoom,Vector2.one);
                window.Repaint();
            }
            catch(Exception exception)
            {
                Debug.LogWarning("Set Game view to a portrait aspect ratio, turn off Low Resolution Aspect Ratios, and reset Scale to 1x. Preview setup failed: "+exception.Message);
            }
            window.Focus();
        }

        // Aspect-ratio previews render at the available window resolution instead of resampling a fixed Retina texture.
        public static int EnsureSize()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var groupType=sizesType.GetProperty("currentGroupType",Flags).GetValue(sizes);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
            var type=group.GetType();
            int count=(int)type.GetMethod("GetBuiltinCount").Invoke(group,null)+(int)type.GetMethod("GetCustomCount").Invoke(group,null);
            for(int i=0;i<count;i++)
            {
                var size=type.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)size.GetType().GetProperty("width").GetValue(size)==195 && (int)size.GetType().GetProperty("height").GetValue(size)==422
                    && size.GetType().GetProperty("sizeType").GetValue(size).ToString()=="AspectRatio") return i;
            }
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var modeType=assembly.GetType("UnityEditor.GameViewSizeType");
            var custom=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(modeType,"AspectRatio"),195,422,"iPhone • native sharp preview"});
            type.GetMethod("AddCustomSize").Invoke(group,new[]{custom});
            return count;
        }

        public static void ValidatePreviewApi()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var type=assembly.GetType("UnityEditor.GameView");
            if(type.GetProperty("lowResolutionForAspectRatios",Flags)?.CanWrite!=true || type.GetField("m_ZoomArea",Flags)==null
                || assembly.GetType("UnityEditor.ZoomableArea").GetField("m_Scale",Flags)==null)
                throw new Exception("Native Game view sharpness controls unavailable.");
        }
    }
}
