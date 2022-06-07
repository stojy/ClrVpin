using System;

namespace Utils
{
    public static class LevenshteinDistance
    {
        // allocate a single instance to reduce memory allocations
        // - note, this makes this method NOT thread safe
        private static readonly int[,] _matrix = new int[255, 255];

        // Calculate the difference between 2 strings using the Levenshtein distance algorithm
        // - https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560
        // - https://en.wikipedia.org/wiki/Levenshtein_distance
        public static int Calculate(string source1, string source2) //O(n*m)
        {
            var source1Length = source1.Length;
            var source2Length = source2.Length;

            //var matrix = new int[source1Length + 1, source2Length + 1];

            // First calculation, if one entry is empty return full length
            if (source1Length == 0)
                return source2Length;

            if (source2Length == 0)
                return source1Length;

            // Initialization of matrix with row size source1Length and columns size source2Length
            for (var i = 0; i <= source1Length; _matrix[i, 0] = i++){}
            for (var j = 0; j <= source2Length; _matrix[0, j] = j++){}

            // Calculate rows and columns distances
            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    _matrix[i, j] = Math.Min(
                        Math.Min(_matrix[i - 1, j] + 1, _matrix[i, j - 1] + 1),
                        _matrix[i - 1, j - 1] + cost);
                }
            }
            // return result
            return _matrix[source1Length, source2Length];
        }
    }
}