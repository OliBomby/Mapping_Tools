using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectCollection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    public class RelevantObjectPairGenerator {
        public static IEnumerable<object[]> GetParametersList(Type[] dependencies, HitObjectCollection hitObjectCollection, RelevantHitObject hitObject) {
            // Because hitobject is new, you only need to generate the combinations of the objects without the new hitobject
            // and then add the hitobect to every combination

            // I use dependencies.Length - 1 because new hitobject satisfies one of the dependencies
            var combinations = CombinationsRecursion(
                hitObjectCollection.HitObjects.Except(new[] {hitObject}).ToArray(),
                dependencies.Length - 1);

            // Add the new hitobject to every combination
            var parametersList = combinations.Select(o => o.Concat(new[] {hitObject}).ToArray());

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