using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using Mirror.Examples.AdditiveLevels;
using Mirror;

public class MorpheusToolsEditor : MonoBehaviour
{
    static ZoneInfo zoneInfo;

    [MenuItem("Morpheus Tools/Setup Scene", false, 1)]
    static void SetupTemplateScene()
    {
        var objects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        //spawn physics simulator
        var physicsSimulator = new GameObject();
        Undo.RegisterCreatedObjectUndo(physicsSimulator, "Create PhysicsSimulator");
        physicsSimulator.AddComponent<PhysicsSimulator>();
        physicsSimulator.name = "PhysicsSimulator";
        var arr = new GameObject[4];
        arr[0] = physicsSimulator;

        //spawn start position object
        var startPos = new GameObject();
        Undo.RegisterCreatedObjectUndo(startPos, "Create StartPosition");
        startPos.name = "StartPosition";
        arr[1] = startPos;

        //spawn zone info container
        var zoneInfoContainer = new GameObject();
        Undo.RegisterCreatedObjectUndo(zoneInfoContainer, "Create ZoneInfo");
        zoneInfo = zoneInfoContainer.AddComponent<ZoneInfo>();
        zoneInfo.skyboxMaterial = RenderSettings.skybox;
        zoneInfo.defaultStartPosition = startPos.transform;
        zoneInfoContainer.name = "ZoneInfo";
        arr[2] = zoneInfoContainer;

        //spawn environment root
        var environment = new GameObject();
        Undo.RegisterCreatedObjectUndo(environment, "Create Environment");
        environment.AddComponent<NetworkIdentity>();
        environment.name = "Environment";
        arr[3] = environment;

        foreach (var obj in objects)
        {
            Undo.SetTransformParent(obj.transform, environment.transform, "Set New Parent");
        }

        //select spawned objects
        Selection.objects = arr;
    }

    [MenuItem("Morpheus Tools/Add Teleportaton Area", false, 2)]
    static void AddTeleportAreaOnSelected()
    {
        //add component TeleportationArea on all selected object
        foreach (var selected in Selection.objects)
        {
            var go = (GameObject)selected;
            TeleportationArea component = Undo.AddComponent<TeleportationArea>(go);
            component.interactionLayers = LayerMask.NameToLayer("Everything");
            component.selectMode = InteractableSelectMode.Single;
        }
    }

    [MenuItem("Morpheus Tools/Apply Skybox", false, 3)]
    static void ApplySkybox()
    {
        if (zoneInfo == null)
        {
            zoneInfo = FindObjectOfType<ZoneInfo>();
        }

        //apply scene skybox to material on MeshRenderer of object Skybox
        if (zoneInfo != null)
        {
            zoneInfo.skyboxMaterial = RenderSettings.skybox;
        }
        else
        {
            Debug.LogWarning("Object zone info does not exist on the scene");
        }
    }

    //[MenuItem("Content Creator Template/Create TemplateScene", false, 1)]
    //static void CreateScene()
    //{
    //    var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
    //    EditorSceneManager.SaveScene(scene, "Assets/Scenes/TemplateScene.unity");
    //    EditorSceneManager.CloseScene(scene, true);
    //}

    //[MenuItem("GameObject/MyCategory/Custom Game Object", false, 10)]
    //static void CreateCustomGameObject(MenuCommand menuCommand)
    //{
    //    // Create a custom game object
    //    GameObject go = new GameObject("Custom Game Object");
    //    // Ensure it gets reparented if this was a context click (otherwise does nothing)
    //    GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
    //    // Register the creation in the undo system
    //    Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
    //    Selection.activeObject = go;
    //}


    //[MenuItem("Content Creator Template/Rename Template Scene", false, 2)]
    //static void RenameTemplateScene()
    //{
    //    foreach (var asset in AssetDatabase.FindAssets("t: Scene TemplateScene"))
    //    {
    //        var path = AssetDatabase.GUIDToAssetPath(asset);
    //        Debug.Log(path);
    //        AssetDatabase.RenameAsset(path, $"A4d77dT5s2");
    //    }
    //}
}