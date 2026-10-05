using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PoliticalTimeline.Editor
{
    public static class IPhonePreview
    {
        [MenuItem("Political Timeline/Preview iPhone Portrait")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PresidencySceneBuilder.ScenePath);
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var gameViewType=assembly.GetType("UnityEditor.GameView");
            var window=EditorWindow.GetWindow(gameViewType);
            try
            {
                int index=EnsureSize();
                gameViewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(window,index);
            }
            catch(Exception)
            {
                UnityEngine.Debug.LogWarning("Set the Game view to 1170 x 2532 portrait using its resolution dropdown. The preview preset API is unavailable in this editor version.");
            }
            window.Show(); window.maximized=true; window.Focus();
        }

        // Unity exposes Game view presets through editor-only reflection, isolated here from game code.
        public static int EnsureSize()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var groupType=sizesType.GetProperty("currentGroupType",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sizes);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
            var type=group.GetType();
            int count=(int)type.GetMethod("GetBuiltinCount").Invoke(group,null)+(int)type.GetMethod("GetCustomCount").Invoke(group,null);
            for(int i=0;i<count;i++)
            {
                var size=type.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)size.GetType().GetProperty("width").GetValue(size)==1170 && (int)size.GetType().GetProperty("height").GetValue(size)==2532) return i;
            }
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var modeType=assembly.GetType("UnityEditor.GameViewSizeType");
            var custom=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(modeType,"FixedResolution"),1170,2532,"iPhone Retina Portrait"});
            type.GetMethod("AddCustomSize").Invoke(group,new[]{custom});
            return count;
        }
    }
}
