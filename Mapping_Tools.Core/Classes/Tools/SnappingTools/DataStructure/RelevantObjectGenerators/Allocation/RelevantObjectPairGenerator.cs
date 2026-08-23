using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using RelevantObjectCollectionType = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;

/// <summary>Allocates dense or sequential input tuples for generator methods.</summary>
public static class RelevantObjectPairGenerator
{
    /// <summary>Creates generator parameter arrays using the requested allocation mode.</summary>
    /// <param name="dependencies">The required concrete types in parameter order.</param>
    /// <param name="collection">The available objects, or <see langword="null"/>.</param>
    /// <param name="sequential">Whether each tuple must use adjacent ordered objects.</param>
    /// <returns>Parameter arrays compatible with reflection invocation.</returns>
    public static IEnumerable<object[]> GetParametersList(Type[] dependencies, RelevantObjectCollectionType? collection, bool sequential) =>
        sequential ? GeneratePairsSequential(dependencies, collection).Cast<object[]>() : GeneratePairsDense(dependencies, collection).Cast<object[]>();

    /// <summary>Generates tuples by walking one ordered sequence at a time.</summary>
    /// <param name="dependencies">The required concrete types.</param>
    /// <param name="collection">The available objects, or <see langword="null"/>.</param>
    /// <returns>Sequential parameter tuples.</returns>
    public static IEnumerable<IRelevantObject[]> GeneratePairsSequential(Type[] dependencies, RelevantObjectCollectionType? collection)
    {
        if (collection is null || dependencies.Length == 0)
        {
            // Handle special case
            return new[] { Array.Empty<IRelevantObject>() };
        }

        List<IRelevantObject> sortedObjects = collection.GetSortedSubset(new HashSet<Type>(dependencies));
        List<IRelevantObject[]> combinations = [];
        int i = 0;
        int firstIndex = 0;
        List<int> indicesFound = [];
        IRelevantObject[] combination = new IRelevantObject[dependencies.Length];
        while (i < sortedObjects.Count)
        {
            IRelevantObject obj = sortedObjects[i];
            if (!obj.IsInheritable)
            {
                // Ignore the uninheritable objects
                i++;
                continue;
            }

            Type type = obj.GetType();
            int indexOfType = -1;
            for (int j = 0; j < dependencies.Length; j++)
            {
                if (!indicesFound.Contains(j) && type == dependencies[j])
                {
                    indexOfType = j;
                    indicesFound.Add(j);
                    break;
                }
            }

            if (indexOfType != -1)
            {
                if (indicesFound.Count == 1)
                {
                    firstIndex = i;
                    combination = new IRelevantObject[dependencies.Length];
                }

                combination[indexOfType] = obj;
                if (indicesFound.Count == dependencies.Length)
                {
                    combinations.Add(combination);
                    indicesFound.Clear();
                    i = firstIndex;
                }
            }

            i++;
        }

        return combinations;
    }

    /// <summary>Generates every unique dense tuple of inheritable inputs.</summary>
    /// <param name="dependencies">The required concrete types.</param>
    /// <param name="collection">The available objects, or <see langword="null"/>.</param>
    /// <returns>Dense parameter tuples.</returns>
    public static IEnumerable<IRelevantObject[]> GeneratePairsDense(Type[] dependencies, RelevantObjectCollectionType? collection)
    {
        if (collection is null || dependencies.Length == 0)
        {
            // Handle special case
            return new[] { Array.Empty<IRelevantObject>() };
        }

        // Count how many of every type are in the neededCombinations
        Dictionary<Type, int> neededCombinations = new();
        foreach (Type type in dependencies)
        {
            neededCombinations[type] = neededCombinations.GetValueOrDefault(type) + 1;
        }

        // Check if the collection contains enough inheritable items to ever satisfy the needed combinations
        foreach ((Type type, int count) in neededCombinations)
        {
            if (!collection.TryGetValue(type, out List<IRelevantObject>? list) || list.Count(o => o.IsInheritable) < count)
            {
                return Array.Empty<IRelevantObject[]>();
            }
        }

        // Make all combinations for every individual type & only get inheritable
        IEnumerable<IEnumerable<IRelevantObject[]>> allCombinations = neededCombinations.Select(pair =>
            CombinationsRecursion(collection[pair.Key].Where(o => o.IsInheritable).ToArray(), pair.Value));
        // Construct all parameter combinations
        // Make combinations of combinations
        return CartesianProduct(allCombinations).Select(o => o.SelectMany(x => x).ToArray());
    }

    /// <summary>Creates the Cartesian product of a sequence of sequences.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="sequences">The sequences to combine.</param>
    /// <returns>The Cartesian product.</returns>
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
        // Flatten collection
        return sequences.Aggregate(emptyProduct, (accumulator, sequence) =>
            from accseq in accumulator
            from item in sequence
            select accseq.Concat(new[] { item }));
    }

    private static IEnumerable<int[]> CombinationsRecursion(int m, int n)
    {
        // Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        // in lexicographic order (first [0, 1, 2, ..., m-1]).
        int[] result = new int[m];
        Stack<int> stack = new(m);
        stack.Push(0);
        while (stack.Count > 0)
        {
            int index = stack.Count - 1;
            int value = stack.Pop();
            while (value < n)
            {
                result[index++] = value++;
                stack.Push(value);
                if (index != m) continue;
                yield return (int[])result.Clone();
                break;
            }
        }
    }

    /// <summary>Gets all unique combinations of a requested length.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="array">The source array.</param>
    /// <param name="m">The number of elements per combination.</param>
    /// <returns>The combinations in lexicographic source-index order.</returns>
    /// <exception cref="ArgumentException">Thrown when the requested length is invalid.</exception>
    public static IEnumerable<T[]> CombinationsRecursion<T>(T[] array, int m)
    {
        if (array.Length < m)
        {
            throw new ArgumentException("Array length can't be less than number of selected elements");
        }

        if (m < 1)
        {
            throw new ArgumentException("Number of selected elements can't be less than 1");
        }

        T[] result = new T[m];
        foreach (int[] indices in CombinationsRecursion(m, array.Length))
        {
            for (int i = 0; i < m; i++)
            {
                result[i] = array[indices[i]];
            }

            yield return result;
        }
    }
}
