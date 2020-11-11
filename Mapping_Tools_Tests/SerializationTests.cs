using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests {
    [TestClass]
    public class SerializationTests {
        private static T LoadJsonDynamic<T>(string path, T _) {
            return ProjectManager.LoadJson<T>(path);
        }
        private static T LoadJsonDynamicSavable<T>(string path, ISavable<T> _) {
            return ProjectManager.LoadJson<T>(path);
        }

        [TestMethod]
        public void SelectionPredicateSerializationTest() {
            const string path = "SelectionPredicateSave.json";

            SelectionPredicate expected = new SelectionPredicate {
                NeedSelected = true,
                NeedLocked = true,
                NeedGeneratedNotByThis = true,
                NeedGeneratedByThis = false,
                MinRelevancy = 0.66
            };

            ProjectManager.SaveJson(path, expected);

            SelectionPredicate actual = ProjectManager.LoadJson<SelectionPredicate>(path);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectionPredicateDynamicSerializationTest() {
            const string path = "SelectionPredicateDynamicSave.json";

            SelectionPredicate expected = new SelectionPredicate {
                NeedSelected = true,
                NeedLocked = true,
                NeedGeneratedNotByThis = true,
                NeedGeneratedByThis = false,
                MinRelevancy = 0.66
            };

            ProjectManager.SaveJson(path, expected);

            dynamic actual = LoadJsonDynamic(path, (dynamic)expected);

            Assert.AreEqual(expected, (SelectionPredicate)actual);
        }

        [TestMethod]
        public void SelectionPredicateCollectionDynamicSerializationTest() {
            const string path = "SelectionPredicateCollectionDynamicSave.json";

            SelectionPredicateCollection expected = new SelectionPredicateCollection();
            expected.Predicates.Add(
                new SelectionPredicate {
                    NeedSelected = true,
                    NeedLocked = true,
                    NeedGeneratedNotByThis = true,
                    NeedGeneratedByThis = false,
                    MinRelevancy = 0.66
                });
            expected.Predicates.Add(
                new SelectionPredicate {
                    NeedSelected = false,
                    NeedLocked = false,
                    NeedGeneratedNotByThis = false,
                    NeedGeneratedByThis = true,
                    MinRelevancy = 0.001
                });

            ProjectManager.SaveJson(path, expected);

            dynamic actual = LoadJsonDynamic(path, (dynamic)expected);

            Assert.AreEqual(expected, (SelectionPredicateCollection)actual);
        }

        [TestMethod]
        public void GeometryDashboardSerializationTest() {
            var tool = new SnappingToolsSavable();

            Assert.IsNotNull(tool);

            const string path = "GeometryDashboardSave.json";

            var expected = tool.GetSaveData();

            expected.CurrentPreferences.AcceptableDifference = 70.1;
            expected.CurrentPreferences.DebugEnabled = true;
            expected.CurrentPreferences.UpdateMode = UpdateMode.OsuActivated;
            expected.CurrentPreferences.LockHotkey = new Hotkey(Key.K, ModifierKeys.Shift);
            expected.CurrentPreferences.GeneratorSettings.Values.First().IsDeep = true;
            expected.CurrentPreferences.GeneratorSettings.Values.First().InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});

            ProjectManager.SaveJson(path, expected);

            var obj = LoadJsonDynamicSavable(path, (dynamic)tool);
            var actual = (SnappingToolsProject) obj;

            var actual2 = ProjectManager.LoadJson<SnappingToolsProject>(path);

            AssertSnappingToolsProjectStuff(expected, actual2);

            AssertSnappingToolsProjectStuff(expected, actual);
        }

        private static void AssertSnappingToolsProjectStuff(SnappingToolsProject expected, SnappingToolsProject actual) {
            Assert.AreEqual(expected.CurrentPreferences.AcceptableDifference, actual.CurrentPreferences.AcceptableDifference);
            Assert.AreEqual(expected.CurrentPreferences.DebugEnabled, actual.CurrentPreferences.DebugEnabled);
            Assert.AreEqual(expected.CurrentPreferences.UpdateMode, actual.CurrentPreferences.UpdateMode);
            Assert.AreEqual(expected.CurrentPreferences.LockHotkey.Key, actual.CurrentPreferences.LockHotkey.Key);
            Assert.AreEqual(expected.CurrentPreferences.LockHotkey.Modifiers, actual.CurrentPreferences.LockHotkey.Modifiers);
            Assert.AreEqual(expected.CurrentPreferences.GeneratorSettings.Values.First().IsDeep, actual.CurrentPreferences.GeneratorSettings.Values.First().IsDeep);
            Assert.AreEqual(expected.CurrentPreferences.GeneratorSettings.Values.First().InputPredicate, actual.CurrentPreferences.GeneratorSettings.Values.First().InputPredicate);
        }

        private class SnappingToolsSavable : ISavable<SnappingToolsProject> {
            private SnappingToolsProject Project { get; set; }
            private readonly ObservableCollection<RelevantObjectsGenerator> generators;

            internal SnappingToolsSavable() {
                Project = new SnappingToolsProject();

                generators = new ObservableCollection<RelevantObjectsGenerator> {
                    new SymmetryGenerator()
                };
                Project.SetGenerators(generators);
            }

            public SnappingToolsProject GetSaveData() {
                return Project.GetThis();
            }

            public void SetSaveData(SnappingToolsProject saveData) {
                Project = saveData;
                Project.SetGenerators(generators);
            }

            public string AutoSavePath => "GeometryDashboardSave.json";
            public string DefaultSaveFolder => "nuffin";
        }

        [TestMethod]
        public void GeneratorSettingsCopyToTest() {
            var expected = new GeneratorSettings {IsDeep = true, IsActive = true, RelevancyRatio = 0.77};
            expected.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});

            var actual = new GeneratorSettings();

            expected.CopyTo(actual);

            Assert.AreEqual(expected.IsDeep, actual.IsDeep);
            Assert.AreEqual(expected.IsActive, actual.IsActive);
            Assert.AreEqual(expected.RelevancyRatio, actual.RelevancyRatio, 0.001);
            Assert.AreEqual(true, expected.InputPredicate.Predicates.First().NeedSelected);
            Assert.AreEqual(true, actual.InputPredicate.Predicates.First().NeedSelected);
            Assert.AreEqual(expected.InputPredicate, actual.InputPredicate);
        }

        [TestMethod]
        public void SymmetryGeneratorSettingsCopyToTest() {
            GeneratorSettings expected = new SymmetryGeneratorSettings {IsDeep = true, IsActive = true, RelevancyRatio = 0.77};
            ((SymmetryGeneratorSettings)expected).OtherInputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.06});

            GeneratorSettings actual = new SymmetryGeneratorSettings();

            expected.CopyTo(actual);

            Assert.AreEqual(expected.IsDeep, actual.IsDeep);
            Assert.AreEqual(expected.IsActive, actual.IsActive);
            Assert.AreEqual(expected.RelevancyRatio, actual.RelevancyRatio, 0.001);
            Assert.AreEqual(((SymmetryGeneratorSettings)expected).OtherInputPredicate, ((SymmetryGeneratorSettings)actual).OtherInputPredicate);
        }

        [TestMethod]
        public void SerializationTypeRetentionTest() {
            const string path = "SerializationTypeRetentionTestSave.json";

            var symmetrySettings = new SymmetryGeneratorSettings {IsActive = true};
            symmetrySettings.OtherInputPredicate.Predicates.Add(new SelectionPredicate {MinRelevancy = 0.05});

            ProjectManager.SaveJson(path, symmetrySettings);

            var deserializedSymmetrySettings = ProjectManager.LoadJson<GeneratorSettings>(path);
 
            Assert.AreEqual(deserializedSymmetrySettings.IsActive, symmetrySettings.IsActive);
            
            var castedSymmetrySettings = deserializedSymmetrySettings as SymmetryGeneratorSettings;
            Assert.IsNotNull(castedSymmetrySettings);
            Assert.AreEqual(castedSymmetrySettings.OtherInputPredicate, symmetrySettings.OtherInputPredicate);
        }
    }
}
