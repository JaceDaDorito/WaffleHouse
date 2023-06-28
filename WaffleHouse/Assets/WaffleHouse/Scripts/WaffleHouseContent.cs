using System;
using System.Collections;
using System.Linq;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using RoR2.Networking;

namespace WaffleHouse.Content
{
    public static class WaffleHouseContent
    {
        internal const string ScenesAssetBundleFileName = "";
        internal const string AssetsAssetBundleFileName = "";

        private static AssetBundle _scenesAssetBundle;
        private static AssetBundle _assetsAssetBundle;

        internal static UnlockableDef[] UnlockableDefs;
        internal static SceneDef[] SceneDefs;

        internal static SceneDef WHSceneDef;
        internal static Sprite WHSceneDefPreviewSprite;
        internal static Material WHBazaarSeer;

        public static List<Material> SwappedMaterials = new List<Material>(); //debug

        public static Dictionary<string, string> ShaderLookup = new Dictionary<string, string>()
        {
            {"stubbedror2/base/shaders/hgstandard", "RoR2/Base/Shaders/HGStandard.shader"},
            {"stubbedror2/base/shaders/hgsnowtopped", "RoR2/Base/Shaders/HGSnowTopped.shader"},
            {"stubbedror2/base/shaders/hgtriplanarterrainblend", "RoR2/Base/Shaders/HGTriplanarTerrainBlend.shader"},
            {"stubbedror2/base/shaders/hgintersectioncloudremap", "RoR2/Base/Shaders/HGIntersectionCloudRemap.shader" },
            {"stubbedror2/base/shaders/hgcloudremap", "RoR2/Base/Shaders/HGCloudRemap.shader" },
            {"stubbedror2/base/shaders/hgdistortion", "RoR2/Base/Shaders/HGDistortion.shader" },
            {"stubbedcalm water/calmwater - dx11 - doublesided", "Calm Water/CalmWater - DX11 - DoubleSided.shader" },
            {"stubbedcalm water/calmwater - dx11", "Calm Water/CalmWater - DX11.shader" },
            {"stubbednature/speedtree", "RoR2/Base/Shaders/SpeedTreeCustom.shader"}
        };


        internal static IEnumerator LoadAssetBundlesAsync(AssetBundle scenesAssetBundle, AssetBundle assetsAssetBundle, IProgress<float> progress, ContentPack contentPack)
        {
            _scenesAssetBundle = scenesAssetBundle;
            _assetsAssetBundle = assetsAssetBundle;

            yield return LoadAllAssetsAsync(_assetsAssetBundle, progress, (Action<Material[]>)((assets) =>
            {
                var materials = assets;


                foreach (Material material in materials)
                {
                    Log.Debug(material.name + " " + material.shader);
                    if (!material.shader.name.StartsWith("Stubbed")) { continue; }

                    var replacementShader = Addressables.LoadAssetAsync<Shader>(ShaderLookup[material.shader.name.ToLower()]).WaitForCompletion();
                    Log.Debug(replacementShader.name);
                    if (replacementShader)
                    {
                        material.shader = replacementShader;
                        SwappedMaterials.Add(material);
                    }
                    Log.Debug(material.name + " " + material.shader);
                }
            }));

            yield return LoadAllAssetsAsync(_assetsAssetBundle, progress, (Action<UnlockableDef[]>)((assets) =>
            {
                UnlockableDefs = assets;
                contentPack.unlockableDefs.Add(assets);
            }));


            yield return LoadAllAssetsAsync(_assetsAssetBundle, progress, (Action<Sprite[]>)((assets) =>
            {
                WHSceneDefPreviewSprite = assets.First(a => a.name == "texWHScenePreview");
            }));

            yield return LoadAllAssetsAsync(_assetsAssetBundle, progress, (Action<SceneDef[]>)((assets) =>
            {
                SceneDefs = assets;
                WHSceneDef = SceneDefs.First(sd => sd.baseSceneNameOverride == "wh_wafflehouse" );
                Log.Debug(WHSceneDef.nameToken);
                contentPack.sceneDefs.Add(assets);
            }));

            var matBazaarSeerWispgraveyardRequest = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarSeerWispgraveyard.mat");
            while (!matBazaarSeerWispgraveyardRequest.IsDone)
            {
                yield return null;
            }
            WHBazaarSeer = UnityEngine.Object.Instantiate(matBazaarSeerWispgraveyardRequest.Result);
            WHBazaarSeer.mainTexture = WHSceneDefPreviewSprite.texture;
            WHSceneDef.portalMaterial = WHBazaarSeer;
            var mainTrackDefRequest = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muFULLSong06.asset");
            while (!mainTrackDefRequest.IsDone)
            {
                yield return null;
            }
            var bossTrackDefRequest = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muSong16.asset");
            while (!bossTrackDefRequest.IsDone)
            {
                yield return null;
            }
            WHSceneDef.mainTrack = mainTrackDefRequest.Result;
            WHSceneDef.bossTrack = bossTrackDefRequest.Result;

            var stageSceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>("RoR2/Base/SceneGroups/sgStage2.asset");
            while (!stageSceneCollectionRequest.IsDone)
            {
                yield return null;
            }

            AddFBLSceneDefToStageSceneCollection(stageSceneCollectionRequest);

            var stageDestinationSceneCollectionRequest = Addressables.LoadAssetAsync<SceneCollection>("RoR2/Base/SceneGroups/sgStage3.asset");
            while (!stageDestinationSceneCollectionRequest.IsDone)
            {
                yield return null;
            }
            WHSceneDef.destinationsGroup = stageDestinationSceneCollectionRequest.Result;
        }

        private static void AddFBLSceneDefToStageSceneCollection(AsyncOperationHandle<SceneCollection> stageSceneCollectionRequest)
        {
            var sceneEntries = stageSceneCollectionRequest.Result._sceneEntries.ToList();
            sceneEntries.Add(new SceneCollection.SceneEntry { sceneDef = WHSceneDef, weightMinusOne = 0 });
            
            stageSceneCollectionRequest.Result._sceneEntries = sceneEntries.ToArray();
        }

        private static IEnumerator LoadAllAssetsAsync<T>(AssetBundle assetBundle, IProgress<float> progress, Action<T[]> onAssetsLoaded) where T : UnityEngine.Object
        {
            var sceneDefsRequest = assetBundle.LoadAllAssetsAsync<T>();
            while (!sceneDefsRequest.isDone)
            {
                progress.Report(sceneDefsRequest.progress);
                yield return null;
            }

            onAssetsLoaded(sceneDefsRequest.allAssets.Cast<T>().ToArray());

            yield break;
        } 
    }
}
