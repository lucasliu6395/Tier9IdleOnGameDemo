using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Tier9.EditorTools
{
    /// <summary>
    /// Generates the one asset that cannot be authored as plain text (PanelSettings)
    /// and keeps texture imports in Resources/Sprites configured as sprites.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectAssetBootstrap
    {
        const string UiDir = "Assets/Resources/UI";
        const string ThemePath = UiDir + "/GameTheme.tss";
        const string PanelSettingsPath = UiDir + "/GamePanelSettings.asset";
        const string SpritesDir = "Assets/Resources/Sprites";

        static ProjectAssetBootstrap()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        public static void EnsureAssets()
        {
            if (!Directory.Exists(UiDir)) Directory.CreateDirectory(UiDir);
            if (!Directory.Exists(SpritesDir)) Directory.CreateDirectory(SpritesDir);

            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(ThemePath);
            }

            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath) == null)
            {
                var ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.referenceResolution = new Vector2Int(1280, 720);
                AssetDatabase.CreateAsset(ps, PanelSettingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Tier9] Created " + PanelSettingsPath);
            }

            if (EditorSettings.defaultBehaviorMode != EditorBehaviorMode.Mode2D)
                EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

            EnsureUrp();
            EnsureInputActions();
            EnsureMainScene();
        }

        const string ScenesDir = "Assets/Scenes";
        const string ScenePath = ScenesDir + "/Main.unity";

        /// <summary>CLI-created projects ship without any scene; provide a saved one for convenience.</summary>
        static void EnsureMainScene()
        {
            if (File.Exists(ScenePath)) return;
            if (!Directory.Exists(ScenesDir)) Directory.CreateDirectory(ScenesDir);

            // A freshly opened CLI project sits on an unsaved, untitled scene. Save *that*
            // scene as Main rather than creating one additively — the additive path errors
            // while an untitled scene is open. If the user is already in a named scene,
            // leave it alone; they have a scene to work from.
            var active = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path)) return;

            if (EditorSceneManager.SaveScene(active, ScenePath))
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                Debug.Log("[Tier9] Saved the current scene as " + ScenePath + " and added it to Build Settings.");
            }
        }

        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>
        /// With the Input System package active, UI Toolkit reads pointer/navigation input
        /// from the project-wide actions asset; generate and assign the default one if missing.
        /// </summary>
        static void EnsureInputActions()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.InputSystem.actions != null) return;

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                var defaults = new UnityEngine.InputSystem.DefaultInputActions();
                File.WriteAllText(InputActionsPath, defaults.asset.ToJson());
                defaults.Dispose();
                AssetDatabase.ImportAsset(InputActionsPath);
                asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
                Debug.Log("[Tier9] Created default input actions at " + InputActionsPath);
            }

            if (asset != null)
            {
                UnityEngine.InputSystem.InputSystem.actions = asset;
                AssetDatabase.SaveAssets();
                Debug.Log("[Tier9] Assigned project-wide input actions.");
            }
#endif
        }

        const string SettingsDir = "Assets/Settings";
        const string RendererPath = SettingsDir + "/Renderer2D.asset";
        const string PipelinePath = SettingsDir + "/URP-Pipeline.asset";

        /// <summary>Switch the project off the deprecated built-in pipeline onto URP's 2D renderer.</summary>
        static void EnsureUrp()
        {
            if (!Directory.Exists(SettingsDir)) Directory.CreateDirectory(SettingsDir);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                    AssetDatabase.CreateAsset(rendererData, RendererPath);
                }
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                Debug.Log("[Tier9] Created URP 2D renderer pipeline assets in " + SettingsDir);
            }

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                AssetDatabase.SaveAssets();
                Debug.Log("[Tier9] Assigned URP 2D pipeline as the project default render pipeline.");
            }
        }
    }

    /// <summary>Any texture dropped into Resources/Sprites imports as a UI-ready sprite.</summary>
    public class SpriteImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Sprites/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
        }
    }

    public static class CompileCheck
    {
        /// <summary>Used by batchmode CI-style runs: if this executes, scripts compiled.</summary>
        public static void Run()
        {
            ProjectAssetBootstrap.EnsureAssets();
            Debug.Log("[Tier9] Compile check OK.");
            EditorApplication.Exit(0);
        }
    }
}
