using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public class SceneSwitcher : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Tools/Quick Scene Switcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcher>("Scene Switcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Danh sách Scene trong Build Settings", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Lấy danh sách scene từ Build Settings
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox("Chưa có scene nào trong Build Settings. Hãy kéo scene vào File > Build Settings!", MessageType.Warning);
        }

        foreach (var scene in scenes)
        {
            if (scene.enabled)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene.path);
                
                // Tạo giao diện hàng ngang cho mỗi Scene
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button(sceneName, GUILayout.Height(30)))
                {
                    MoveToScene(scene.path);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Refresh List", GUILayout.Height(40)))
        {
            Repaint();
        }
    }

    private void MoveToScene(string path)
    {
        // Kiểm tra xem scene hiện tại đã lưu chưa trước khi chuyển
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}