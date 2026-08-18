using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Input;
using FluentAssertions;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreGeneratorSettings = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettings;
using CoreHotkey = Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization.Hotkey;
using CoreRelevantObjectsGenerator = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.RelevantObjectsGenerator;
using CoreSelectionPredicate = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection.SelectionPredicate;
using CoreSelectionPredicateCollection = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection.SelectionPredicateCollection;
using CoreSnappingToolsProject = Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization.SnappingToolsProject;
using CoreSymmetryGenerator = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators.SymmetryGenerator;
using CoreSymmetryGeneratorSettings = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses.SymmetryGeneratorSettings;
using CoreUpdateMode = Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization.UpdateMode;

[assembly: SupportedOSPlatform("Windows7.0")]

namespace Mapping_Tools_Tests {
    [TestClass]
    public class SerializationTests {
        [TestMethod]
        public void SaveJson_ComboColour_ProducesLegacyProjectFormatAndRoundTrips() {
            // Arrange
            string path = System.IO.Path.GetTempFileName();
            try {
                var expected = new ComboColour(
                    RgbaColour.FromArgb(0x7F, 0x12, 0x34, 0x56));

                // Act
                ProjectManager.SaveJson(path, expected);
                string json = System.IO.File.ReadAllText(path);
                var actual = ProjectManager.LoadJson<ComboColour>(path);

                // Assert
                json.Should().Contain("Mapping_Tools.Classes.BeatmapHelper.ComboColour, Mapping Tools");
                json.Should().Contain("\"Color\": \"#7F123456\"");
                actual.Color.Should().Be(expected.Color);
            } finally {
                System.IO.File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveJson_HitObject_ProducesLegacyProjectTypeAndRoundTripsLine() {
            // Arrange
            string path = System.IO.Path.GetTempFileName();
            try {
                const string line = "256,192,1000,1,2,0:0:0:0:";
                var expected = new HitObject(line);

                // Act
                ProjectManager.SaveJson(path, expected);
                string json = System.IO.File.ReadAllText(path);
                var actual = ProjectManager.LoadJson<HitObject>(path);

                // Assert
                json.Should().Contain("Mapping_Tools.Classes.BeatmapHelper.HitObject, Mapping Tools");
                actual.GetLine().Should().Be(line);
            } finally {
                System.IO.File.Delete(path);
            }
        }

        [TestMethod]
        public void SaveJson_MigratedCoreTypes_UsesLegacyAssemblyNameAndRoundTripsTypes() {
            // Arrange
            object[] migratedValues = {
                new RationalBeatDivisor(4),
                new Sample(),
                new TimingPoint("1000,500,4,1,0,100,1,0"),
                new HitsoundZone()
            };
            var results = new List<(object Expected, string Json, object Actual)>();

            // Act
            foreach (object expected in migratedValues) {
                string path = System.IO.Path.GetTempFileName();
                try {
                    ProjectManager.SaveJson(path, expected);
                    string json = System.IO.File.ReadAllText(path);
                    object actual = ProjectManager.LoadJson<object>(path);

                    results.Add((expected, json, actual));
                } finally {
                    System.IO.File.Delete(path);
                }
            }

            // Assert
            foreach ((object expected, string json, object actual) in results) {
                string legacyTypeName = expected.GetType().FullName!
                    .Replace("Mapping_Tools.Core.", "Mapping_Tools.", StringComparison.Ordinal);
                json.Should().Contain($"{legacyTypeName}, Mapping Tools");
                actual.GetType().Should().Be(expected.GetType());
            }
        }

        private static T LoadJsonDynamic<T>(string path, T _) {
            return ProjectManager.LoadJson<T>(path);
        }

        private static T LoadJsonDynamicSavable<T>(string path, ISavable<T> _) {
            return ProjectManager.LoadJson<T>(path);
        }

        [TestMethod]
        public void LoadJson_SelectionPredicate_ProducesEquivalentValue() {
            // Arrange
            const string path = "SelectionPredicateSave.json";
            CoreSelectionPredicate expected = new CoreSelectionPredicate {
                NeedSelected = true,
                NeedLocked = true,
                NeedGeneratedNotByThis = true,
                NeedGeneratedByThis = false,
                MinRelevancy = 0.66
            };

            // Act
            ProjectManager.SaveJson(path, expected);
            CoreSelectionPredicate actual = ProjectManager.LoadJson<CoreSelectionPredicate>(path);

            // Assert
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void LoadJson_DynamicSelectionPredicate_ProducesEquivalentValue() {
            // Arrange
            const string path = "SelectionPredicateDynamicSave.json";
            CoreSelectionPredicate expected = new CoreSelectionPredicate {
                NeedSelected = true,
                NeedLocked = true,
                NeedGeneratedNotByThis = true,
                NeedGeneratedByThis = false,
                MinRelevancy = 0.66
            };

            // Act
            ProjectManager.SaveJson(path, expected);
            dynamic actual = LoadJsonDynamic(path, (dynamic)expected);

            // Assert
            ((CoreSelectionPredicate)actual).Should().Be(expected);
        }

        [TestMethod]
        public void LoadJson_DynamicSelectionPredicateCollection_ProducesEquivalentValue() {
            // Arrange
            const string path = "SelectionPredicateCollectionDynamicSave.json";
            CoreSelectionPredicateCollection expected = new CoreSelectionPredicateCollection();
            expected.Predicates.Add(
                new CoreSelectionPredicate {
                    NeedSelected = true,
                    NeedLocked = true,
                    NeedGeneratedNotByThis = true,
                    NeedGeneratedByThis = false,
                    MinRelevancy = 0.66
                });
            expected.Predicates.Add(
                new CoreSelectionPredicate {
                    NeedSelected = false,
                    NeedLocked = false,
                    NeedGeneratedNotByThis = false,
                    NeedGeneratedByThis = true,
                    MinRelevancy = 0.001
                });

            // Act
            ProjectManager.SaveJson(path, expected);
            dynamic actual = LoadJsonDynamic(path, (dynamic)expected);

            // Assert
            ((CoreSelectionPredicateCollection)actual).Should().Be(expected);
        }

        [TestMethod]
        public void LoadJson_GeometryDashboardProject_PreservesPreferences() {
            // Arrange
            var tool = new SnappingToolsSavable();
            const string path = "GeometryDashboardSave.json";
            CoreSnappingToolsProject expected = tool.GetSaveData();
            expected.CurrentPreferences.AcceptableDifference = 70.1;
            expected.CurrentPreferences.DebugEnabled = true;
            expected.CurrentPreferences.UpdateMode = CoreUpdateMode.OsuActivated;
            expected.CurrentPreferences.LockHotkey = new CoreHotkey((int)Key.K, (int)ModifierKeys.Shift);
            expected.CurrentPreferences.GeneratorSettings.Values.First().IsDeep = true;
            expected.CurrentPreferences.GeneratorSettings.Values.First().InputPredicate.Predicates.Add(new CoreSelectionPredicate { NeedSelected = true });

            // Act
            ProjectManager.SaveJson(path, expected);
            dynamic obj = LoadJsonDynamicSavable(path, (dynamic)tool);
            CoreSnappingToolsProject actual = (CoreSnappingToolsProject)obj;
            CoreSnappingToolsProject actual2 = ProjectManager.LoadJson<CoreSnappingToolsProject>(path);

            // Assert
            tool.Should().NotBeNull();
            AssertSnappingToolsProjectStuff(expected, actual2);
            AssertSnappingToolsProjectStuff(expected, actual);
        }

        private static void AssertSnappingToolsProjectStuff(CoreSnappingToolsProject expected, CoreSnappingToolsProject actual) {
            actual.CurrentPreferences.AcceptableDifference.Should().Be(expected.CurrentPreferences.AcceptableDifference);
            actual.CurrentPreferences.DebugEnabled.Should().Be(expected.CurrentPreferences.DebugEnabled);
            actual.CurrentPreferences.UpdateMode.Should().Be(expected.CurrentPreferences.UpdateMode);
            actual.CurrentPreferences.LockHotkey.Key.Should().Be(expected.CurrentPreferences.LockHotkey.Key);
            actual.CurrentPreferences.LockHotkey.Modifiers.Should().Be(expected.CurrentPreferences.LockHotkey.Modifiers);
            actual.CurrentPreferences.GeneratorSettings.Values.First().IsDeep.Should().Be(expected.CurrentPreferences.GeneratorSettings.Values.First().IsDeep);
            actual.CurrentPreferences.GeneratorSettings.Values.First().InputPredicate.Should().Be(expected.CurrentPreferences.GeneratorSettings.Values.First().InputPredicate);
        }

        private class SnappingToolsSavable : ISavable<CoreSnappingToolsProject> {
            private CoreSnappingToolsProject Project { get; set; }
            private readonly ObservableCollection<CoreRelevantObjectsGenerator> generators;

            internal SnappingToolsSavable() {
                Project = new CoreSnappingToolsProject();
                generators = new ObservableCollection<CoreRelevantObjectsGenerator> {
                    new CoreSymmetryGenerator()
                };
                Project.SetGenerators(generators);
            }

            public CoreSnappingToolsProject GetSaveData() {
                return Project.GetThis();
            }

            public void SetSaveData(CoreSnappingToolsProject saveData) {
                Project = saveData;
                Project.SetGenerators(generators);
            }

            public string AutoSavePath => "GeometryDashboardSave.json";
            public string DefaultSaveFolder => "nuffin";
        }

        [TestMethod]
        public void CopyTo_GeneratorSettings_CopiesSerializableProperties() {
            // Arrange
            var expected = new GeneratorSettings { IsDeep = true, IsActive = true, RelevancyRatio = 0.77 };
            expected.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true });
            var actual = new GeneratorSettings();

            // Act
            expected.CopyTo(actual);

            // Assert
            actual.IsDeep.Should().Be(expected.IsDeep);
            actual.IsActive.Should().Be(expected.IsActive);
            actual.RelevancyRatio.Should().BeApproximately(expected.RelevancyRatio, 0.001);
            expected.InputPredicate.Predicates.First().NeedSelected.Should().BeTrue();
            actual.InputPredicate.Predicates.First().NeedSelected.Should().BeTrue();
            actual.InputPredicate.Should().Be(expected.InputPredicate);
        }

        [TestMethod]
        public void CopyTo_SymmetryGeneratorSettings_CopiesDerivedProperties() {
            // Arrange
            GeneratorSettings expected = new SymmetryGeneratorSettings { IsDeep = true, IsActive = true, RelevancyRatio = 0.77 };
            ((SymmetryGeneratorSettings)expected).OtherInputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.06 });
            GeneratorSettings actual = new SymmetryGeneratorSettings();

            // Act
            expected.CopyTo(actual);

            // Assert
            actual.IsDeep.Should().Be(expected.IsDeep);
            actual.IsActive.Should().Be(expected.IsActive);
            actual.RelevancyRatio.Should().BeApproximately(expected.RelevancyRatio, 0.001);
            ((SymmetryGeneratorSettings)actual).OtherInputPredicate.Should().Be(((SymmetryGeneratorSettings)expected).OtherInputPredicate);
        }

        [TestMethod]
        public void LoadJson_GeneratorSettings_RetainsConcreteSettingsType() {
            // Arrange
            const string path = "SerializationTypeRetentionTestSave.json";
            var symmetrySettings = new CoreSymmetryGeneratorSettings { IsActive = true };
            symmetrySettings.OtherInputPredicate.Predicates.Add(new CoreSelectionPredicate { MinRelevancy = 0.05 });

            // Act
            ProjectManager.SaveJson(path, symmetrySettings);
            CoreGeneratorSettings deserializedSymmetrySettings = ProjectManager.LoadJson<CoreGeneratorSettings>(path);

            // Assert
            deserializedSymmetrySettings.IsActive.Should().Be(symmetrySettings.IsActive);
            deserializedSymmetrySettings.Should().BeOfType<CoreSymmetryGeneratorSettings>();
            var castedSymmetrySettings = (CoreSymmetryGeneratorSettings)deserializedSymmetrySettings;
            castedSymmetrySettings.OtherInputPredicate.Should().Be(symmetrySettings.OtherInputPredicate);
        }
    }
}
