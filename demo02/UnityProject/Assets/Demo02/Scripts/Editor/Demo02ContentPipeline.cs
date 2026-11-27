using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    public interface IDemo02ContentModule
    {
        string Id { get; }
        int Order { get; }
        void Build();
        void Validate();
    }

    public static class Demo02ContentPipeline
    {
        private static readonly Dictionary<string, IDemo02ContentModule>
            Modules = new(StringComparer.Ordinal);

        static Demo02ContentPipeline()
        {
            RegisterBuiltInModules();
        }

        public static IReadOnlyList<IDemo02ContentModule>
            RegisteredModules =>
                Modules.Values.OrderBy(module => module.Order).ToArray();

        public static void RegisterModule(IDemo02ContentModule module)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.Id))
            {
                throw new ArgumentException(
                    "Content module must have a non-empty ID.",
                    nameof(module)
                );
            }
            Modules[module.Id] = module;
        }

        public static bool UnregisterModule(string moduleId)
        {
            return !string.IsNullOrWhiteSpace(moduleId)
                && Modules.Remove(moduleId);
        }

        public static void BuildAll()
        {
            foreach (IDemo02ContentModule module in RegisteredModules)
            {
                Debug.Log($"Building content module: {module.Id}");
                module.Build();
            }
            Debug.Log(
                $"Content pipeline complete: {Modules.Count} module(s)."
            );
        }

        [MenuItem("Demo02/Content/Rebuild All Content Modules")]
        public static void BuildFromMenu()
        {
            BuildAll();
        }

        [MenuItem("Demo02/Content/Validate All Content Modules")]
        public static void ValidateFromMenu()
        {
            ValidateContent();
        }

        public static void ValidateContent()
        {
            foreach (IDemo02ContentModule module in RegisteredModules)
            {
                module.Validate();
            }
            Debug.Log(
                $"Content pipeline validation passed: " +
                $"{Modules.Count} module(s)."
            );
        }

        private static void RegisterBuiltInModules()
        {
            RegisterModule(
                new DelegateModule(
                    "definitions",
                    100,
                    () =>
                    {
                        CharacterDefinitionSeeder.BuildAll();
                        EncounterDefinitionSeeder.BuildAll();
                        WorldStreamingRegionSeeder.BuildAll();
                        GameContentSeeder.BuildAll();
                    },
                    () =>
                    {
                        CharacterDefinitionSeeder.ValidateOrThrow();
                        EncounterDefinitionSeeder.ValidateOrThrow();
                        WorldStreamingRegionSeeder.ValidateForBatch();
                        GameContentSeeder.ValidateForBatch();
                    }
                )
            );
            RegisterModule(
                new DelegateModule(
                    "characters",
                    200,
                    () =>
                    {
                        StylizedCharacterSetup.BuildAll();
                        QuaterniusAnimationLibrarySetup.BuildAll();
                        LumineCharacterSetup.BuildAll();
                        AnimeFishPilotSetup.Build();
                    },
                    () =>
                    {
                        StylizedCharacterSetup.ValidateForBatch();
                        QuaterniusAnimationLibrarySetup.ValidateForBatch();
                        LumineCharacterSetup.ValidateForBatch();
                        AnimeFishAnimationSetup.ValidateForBatch();
                    }
                )
            );
            RegisterModule(
                new DelegateModule(
                    "asset-budget",
                    350,
                    RuntimeAssetBudgetSetup.BuildAll,
                    RuntimeAssetBudgetSetup.ValidateForBatch
                )
            );
            RegisterModule(
                new DelegateModule(
                    "environment",
                    300,
                    () =>
                    {
                        RuntimeAssetBudgetSetup
                            .MakeEnvironmentMeshesReadableForBuild();
                        QuaterniusEnvironmentSetup.BuildAll();
                        ReadyMadeStreetDressingSetup.BuildAll();
                        ReadyMadeAudioSetup.BuildAll();
                    },
                    () =>
                    {
                        QuaterniusEnvironmentSetup.ValidateForBatch();
                        ReadyMadeStreetDressingSetup.ValidateForBatch();
                        ReadyMadeAudioSetup.ValidateForBatch();
                        PublicPropDressingSetup.ValidateForBatch();
                    }
                )
            );
            RegisterModule(
                new DelegateModule(
                    "world-scenes",
                    400,
                    Demo02GreyboxBuilder.BuildWorldArtifactsForPipeline,
                    () =>
                    {
                        WorldStreamingRegionSeeder.ValidateForBatch();
                        WorldStreamingBudgetAudit.ValidateForBatch();
                    }
                )
            );
            RegisterModule(
                new DelegateModule(
                    "mesh-budget",
                    500,
                    RuntimeAssetBudgetSetup.ApplyMeshBudget,
                    RuntimeAssetBudgetSetup.ValidateMeshBudget
                )
            );
            RegisterModule(
                new DelegateModule(
                    "data-audit",
                    600,
                    GameContentAudit.Generate,
                    GameContentAudit.Validate
                )
            );
        }

        private sealed class DelegateModule : IDemo02ContentModule
        {
            private readonly Action build;
            private readonly Action validate;

            public DelegateModule(
                string id,
                int order,
                Action buildAction,
                Action validateAction)
            {
                Id = id;
                Order = order;
                build = buildAction;
                validate = validateAction;
            }

            public string Id { get; }
            public int Order { get; }

            public void Build()
            {
                build?.Invoke();
            }

            public void Validate()
            {
                validate?.Invoke();
            }
        }
    }
}
