using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    public class RelevantObjectPairGenerator {
        public static IEnumerable<object[]> GetParametersList(Type[] dependencies,
            RelevantObjectCollection.RelevantObjectCollection collection, bool sequential) {
            return sequential ? GeneratePairsSequential(dependencies, collection) : GeneratePairsDense(dependencies, collection);
        }

        public static IEnumerable<IRelevantObject[]> GeneratePairsSequential(Type[] dependencies,
            RelevantObjectCollection.RelevantObjectCollection collection) {
            // Handle special case
            if (collection == null || dependencies.Length == 0) {
                return new[] {new IRelevantObject[0] };
            }

            var sortedObjects = collection.GetSortedSubset(new HashSet<Type>(dependencies));

            var combinations = new List<IRelevantObject[]>();

            var i = 0;
            var firstIndex = 0;
            var indicesFound = new List<int>();
            var combination = new IRelevantObject[dependencies.Length];
            while (i < sortedObjects.Count) {
                var obj = sortedObjects[i];

                // Ignore the uninheritable objects
                if (!obj.IsInheritable) {
                    i++;
                    continue;
                }

                var type = obj.GetType();

                var indexOfType = -1;
                for (var j = 0; j < dependencies.Length; j++) {
                    if (indicesFound.Contains(j) || type != dependencies[j]) continue;
                    indexOfType = j;
                    indicesFound.Add(j);
                    break;
                }

                if (indexOfType != -1) {
                    if (indicesFound.Count == 1) {
                        firstIndex = i;
                        combination = new IRelevantObject[dependencies.Length];
                    }

                    combination[indexOfType] = obj;

                    if (indicesFound.Count == dependencies.Length) {
                        combinations.Add(combination);

                        indicesFound.Clear();
                        i = firstIndex;
                    }
                }

                i++;
            }

            return combinations;
        }

        public static IEnumerable<IRelevantObject[]> GeneratePairsDense(Type[] dependencies,
            RelevantObjectCollection.RelevantObjectCollection collection) {
            /*Console.WriteLine("Dependencies:");
            foreach (var dependency in dependencies) {
                Console.WriteLine(dependency);
            }*/

            // Handle special case
            if (collection == null || dependencies.Length == 0) {
                return new[] {new IRelevantObject[0] };
            }

            // Count how many of every type are in the neededCombinations
            var neededCombinations = new Dictionary<Type, int>();
            foreach (var type in dependencies) {
                if (neededCombinations.ContainsKey(type)) {
                    neededCombinations[type] += 1;
                } else {
                    neededCombinations.Add(type, 1);
                }
            }
            /*Console.WriteLine("Dependencies count:");
            foreach (var dependency in neededCombinations) {
                Console.WriteLine(dependency.Key + ": " + dependency.Value);
            }*/

            // Check if the collection contains enough inheritable items to ever satisfy the needed combinations
            foreach (var neededCombination in neededCombinations) {
                if (collection.TryGetValue(neededCombination.Key, out var list)) {
                    if (list.Count(o => o.IsInheritable) < neededCombination.Value) {
                        return new IRelevantObject[0][];
                    }
                } else {
                    return new IRelevantObject[0][];
                }
            }
            //Console.WriteLine("Check succeeded");

            // Make all combinations for every individual type & only get inheritable
            var allCombinationsOfEveryType = neededCombinations.Select(kvp => CombinationsRecursion(collection[kvp.Key].Where(o => o.IsInheritable).ToArray(), kvp.Value));

            /*Console.WriteLine("combinations of every type:");
            foreach (var a in allCombinationsOfEveryType) {
                Console.WriteLine("a");
                Console.WriteLine($@"Number of combinations: {a.Count()}");
                foreach (var b in a) {
                    Console.WriteLine("b");
                    foreach (var c in b) {
                        Console.WriteLine(c);
                    }
                }
            }*/

            // Construct all parameter combinations
            // Make combinations of combinations
            var combinationCombinations = CartesianProduct(allCombinationsOfEveryType);
            /*Console.WriteLine("Cartesian product:");
            Console.WriteLine($@"Number of combinations: {combinationCombinations.Count()}");
            foreach (var a in combinationCombinations) {
                Console.WriteLine("a");
                foreach (var b in a) {
                    Console.WriteLine("b");
                    foreach (var c in b) {
                        Console.WriteLine(c);
                    }
                }
            }*/

            // Flatten collection
            var parametersCollection = combinationCombinations.Select(o => o.SelectMany(x => x).ToArray());
            /*Console.WriteLine("Flattened:");
            Console.WriteLine($@"Number of combinations: {parametersCollection.Count()}");
            foreach (var a in parametersCollection) {
                Console.WriteLine("a");
                foreach (var b in a) {
                    Console.WriteLine(b);
                }
            }*/

            return parametersCollection;
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