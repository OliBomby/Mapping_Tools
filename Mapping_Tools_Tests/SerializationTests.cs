using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;

namespace Mapping_Tools_Tests {
    [TestClass]
    public class SerializationTests {
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        private static void SaveJson(string path, object obj) {
            using (StreamWriter fs = new StreamWriter(path)) {
                using (JsonTextWriter reader = new JsonTextWriter(fs)) {
                    Serializer.Serialize(reader, obj);
                }
            }
        }
        
        private static T LoadJson<T>(string path) {
            using (StreamReader fs = new StreamReader(path)) {
                using (JsonReader reader = new JsonTextReader(fs)) {
                    return Serializer.Deserialize<T>(reader);
                }
            }
        }

        private static T LoadJsonDynamic<T>(string path, T _) {
            using (StreamReader fs = new StreamReader(path)) {
                using (JsonReader reader = new JsonTextReader(fs)) {
                    return Serializer.Deserialize<T>(reader);
                }
            }
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

            SaveJson(path, expected);

            SelectionPredicate actual = LoadJson<SelectionPredicate>(path);

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

            SaveJson(path, expected);

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

            SaveJson(path, expected);

            dynamic actual = LoadJsonDynamic(path, (dynamic)expected);

            Assert.AreEqual(expected, (SelectionPredicateCollection)actual);
        }
    }
}
