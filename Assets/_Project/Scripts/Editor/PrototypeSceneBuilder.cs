using System.IO;
using Eclipse.CameraTools;
using Eclipse.DebugTools;
using Eclipse.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eclipse.EditorTools
{
    /// <summary>
    /// Меню Eclipse -> Build Combat Prototype Scene.
    /// Серая коробка: арена, игрок, камера, DebugHud.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        const string Root = "Assets/_Project";

        [MenuItem("Eclipse/Build Combat Prototype Scene")]
        public static void Build()
        {
            if (GameObject.Find("Player") != null)
            {
                EditorUtility.DisplayDialog("Eclipse",
                    "В сцене уже есть объект Player. Открой чистую сцену (Assets/Scenes/SampleScene) и запусти команду ещё раз.",
                    "OK");
                return;
            }

            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Data");
            EnsureFolder(Root + "/Materials");

            PlayerTuning tuning = AssetDatabase.LoadAssetAtPath<PlayerTuning>(Root + "/Data/PlayerTuning.asset");
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<PlayerTuning>();
                AssetDatabase.CreateAsset(tuning, Root + "/Data/PlayerTuning.asset");
            }

            Material floorMat = MakeMaterial("Arena_Floor", new Color(0.22f, 0.22f, 0.26f));
            Material playerMat = MakeMaterial("Player_Gray", new Color(0.35f, 0.55f, 0.85f));
            Material markerMat = MakeMaterial("Marker_White", new Color(0.95f, 0.95f, 0.95f));

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Arena_Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;
            player.GetComponent<Renderer>().sharedMaterial = playerMat;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Facing_Marker";
            Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
            marker.transform.SetParent(player.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.45f, 0.55f);
            marker.transform.localScale = new Vector3(0.25f, 0.25f, 0.6f);
            marker.GetComponent<Renderer>().sharedMaterial = markerMat;

            PlayerController pc = player.AddComponent<PlayerController>();
            player.AddComponent<PlayerCombat>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 9f;

            IsoCameraFollow follow = cam.GetComponent<IsoCameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<IsoCameraFollow>();
            follow.target = player.transform;
            follow.SnapToTarget();

            SerializedObject so = new SerializedObject(pc);
            so.FindProperty("tuning").objectReferenceValue = tuning;
            so.FindProperty("cam").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject hud = new GameObject("DebugHud");
            hud.AddComponent<DebugHud>();

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/CombatPrototype.unity");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;

            Debug.Log("[Eclipse] Сцена CombatPrototype собрана. Нажми Play.");
        }

        static Material MakeMaterial(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = new Material(shader);
            m.SetColor("_BaseColor", color);
            m.color = color;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || parent == ".")
            {
                AssetDatabase.CreateFolder("Assets", name);
                return;
            }
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
