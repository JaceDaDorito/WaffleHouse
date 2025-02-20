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
using R2API;
using LOP;

namespace WaffleHouse.Content
{
    public static class WaffleHouseContent
    {
        internal const string ScenesAssetBundleFileName = "wafflehousestage";
        internal const string AssetsAssetBundleFileName = "wafflehouseassets";

        private static AssetBundle _scenesAssetBundle;
        private static AssetBundle _assetsAssetBundle;

        internal static UnlockableDef[] UnlockableDefs;
        internal static SceneDef[] SceneDefs;

        internal static SceneDef WHSceneDef;
        internal static Sprite WHSceneDefPreviewSprite;
        internal static Material WHBazaarSeer;


        internal static IEnumerator LoadAssetBundlesAsync(AssetBundle scenesAssetBundle, AssetBundle assetsAssetBundle, IProgress<float> progress, ContentPack contentPack)
        {
            _scenesAssetBundle = scenesAssetBundle;
            _assetsAssetBundle = assetsAssetBundle;
            Log.Debug($"Asset bundles found. Loading asset bundles...");

            yield return LoadAllAssetsAsync(_assetsAssetBundle, progress, (Action<Material[]>)((assets) =>
            {
                var materials = assets;

                if (materials != null)
                {
                    foreach (Material material in materials)
                    {
                        ShaderSwap.ConvertShader(material);
                    }
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
                WHSceneDef = SceneDefs.First(sd => sd.cachedName == "waffle_wafflehouse");
                Log.Debug(WHSceneDef.nameToken);
                contentPack.sceneDefs.Add(assets);
            }));

            WHBazaarSeer = StageRegistration.MakeBazaarSeerMaterial(WHSceneDefPreviewSprite.texture);
            WHSceneDef.previewTexture = WHSceneDefPreviewSprite.texture;
            WHSceneDef.portalMaterial = WHBazaarSeer;

            var dioramaPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/golemplains/GolemplainsDioramaDisplay.prefab");
            while (!dioramaPrefab.IsDone)
            {
                yield return null;
            }
            WHSceneDef.dioramaPrefab = dioramaPrefab.Result;

            var mainTrackDefRequest = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muSong13.asset");
            while (!mainTrackDefRequest.IsDone)
            {
                yield return null;
            }
            var bossTrackDefRequest = Addressables.LoadAssetAsync<MusicTrackDef>("RoR2/Base/Common/muSong05.asset");
            while (!bossTrackDefRequest.IsDone)
            {
                yield return null;
            }
            WHSceneDef.mainTrack = mainTrackDefRequest.Result;
            WHSceneDef.bossTrack = bossTrackDefRequest.Result;

            StageRegistration.RegisterSceneDefToLoop(WHSceneDef);
            Log.Debug(WHSceneDef.destinationsGroup);
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
