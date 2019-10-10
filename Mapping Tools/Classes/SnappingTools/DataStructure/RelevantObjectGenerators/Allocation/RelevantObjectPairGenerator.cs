using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectCollection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    public class RelevantObjectPairGenerator {
        /// <summary>
        /// Combinations generator for RelevantHitObjects
        /// </summary>
        /// <param name="dependencies">The types of objects in every combination</param>
        /// <param name="hitObjectCollection">All the RelevantHitObjects</param>
        /// <param name="hitObject">The new RelevantHitObject</param>
        /// <returns>Enumerable of object array where the objects in the object array have the types specified in dependencies</returns>
        public static IEnumerable<object[]> GetParametersList(Type[] dependencies, List<IRelevantObject> hitObjectCollection, RelevantHitObject hitObject) {
            // Because hitobject is new, you only need to generate the combinations of the objects without the new hitobject
            // and then add the hitobject to every combination

            // I use dependencies.Length - 1 because new hitobject satisfies one of the dependencies
            // I also assume all the types in dependencies are of RelevantHitObject
            var combinations = CombinationsRecursion(
                hitObjectCollection.Except(new[] {hitObject}).ToArray(),
                dependencies.Length - 1);

            // Add the new hitobject to every combination
            var parametersList = combinations.Select(o => o.Concat(new[] {hitObject}).ToArray());

            return parametersList;
        }

        /// <summary>
        /// Generates all combinations necessary for an array of dependencies and a newly added relevant object.
        /// </summary>
        /// <param name="dependencies">Array of types indicating which types go in the generator.</param>
        /// <param name="relevantObjectCollection">All the relevant objects that are relevant to this generator.</param>
        /// <param name="relevantObject">The newly added relevant object.</param>
        /// <returns></returns>
        public static IEnumerable<object[]> GetParametersList(Type[] dependencies, RelevantObjectCollection.RelevantObjectCollection relevantObjectCollection, RelevantObject.RelevantObject relevantObject) {
            // Because relevantObject is new, you only need to generate the combinations of the objects without the new relevant object
            // and then add the relevantObject to every combination

            // Get a list of dependencies without the type of relevantObject, since that one will be added afterwards
            var neededCombinations = dependencies.ToList();
            if (!neededCombinations.Remove(relevantObject.GetType())) {
                // If relevantObject is not in the dependencies then this generator doesn't want to do anything with this new relevantObject
                return new object[0][];
            }

            // Count how many of every type are in the neededCombinations
            var neededCombinationsCount = new Dictionary<Type, int>();
            foreach (var type in neededCombinations) {
                if (neededCombinationsCount.ContainsKey(type)) {
                    neededCombinationsCount[type] += 1;
                } else {
                    neededCombinationsCount[type] = 1;
                }
            }

            var combinations = neededCombinationsCount.Select(o => CombinationsRecursion(relevantObjectCollection[o.Key].Except(new[] {relevantObject}).ToArray(), o.Value));
            /*
             * [[combi1, combi2, combi3], [combi4, combi5, combi6], [combi7, combi8]]
             * to
             * [[1+4+7], [1+4+8], 1+5+7, 1+5+8, 1+6+7, 1+6+8, 2+4+7, 2+4+8,...]
             */

            var enumerable = combinations as IEnumerable<IRelevantObject[]>[] ?? combinations.ToArray();
            var thingy = CartesianProduct(enumerable).Select(o => o.SelectMany(x => x));

            // Add the new relevantObject to every combination
            var parametersList = thingy.Select(o => o.Concat(new[] { relevantObject }).ToArray());

            return parametersList;
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences) {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item }));
        }

        // Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        // in lexicographic order (first [0, 1, 2, ..., m-1]).
        private static IEnumerable<int[]> CombinationsRecursion(int m, int n) {
            int[] result = new int[m];
            Stack<int> stack = new Stack<int>(m);
            stack.Push(0);
            while (stack.Count > 0) {
                int index = stack.Count - 1;
                int value = stack.Pop();
                while (value < n) {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index != m) continue;
                    yield return (int[])result.Clone();
                    //yield return result;
                    break;
                }
            }
        }

        /// <summary>
        /// Gets all the unique combinations with length m of an array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array to get combinations of</param>
        /// <param name="m">The number of unique elements in every combination</param>
        /// <returns>All the combinations</returns>
        public static IEnumerable<T[]> CombinationsRecursion<T>(T[] array, int m) {
            if (array.Length < m)
                throw new ArgumentException("Array length can't be less than number of selected elements");
            if (m < 1)
                throw new ArgumentException("Number of selected elements can't be less than 1");
            T[] result = new T[m];
            foreach (int[] j in CombinationsRecursion(m, array.Length)) {
                for (int i = 0; i < m; i++) {
                    result[i] = array[j[i]];
                }
                yield return result;
            }
        }
    }
}