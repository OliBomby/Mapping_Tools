/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the osuTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 *
 * Contributions by Andy Gill, James Talton and Georg Wächter.
 */

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>
    /// Contains common mathematical functions and constants.
    /// </summary>
    public static class MathHelper {
        /// <summary>
        /// Defines the value of Pi as a <see cref="System.Single"/>.
        /// </summary>
        public const double Pi = 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930382f;

        /// <summary>
        /// Defines the value of Pi divided by two as a <see cref="System.Single"/>.
        /// </summary>
        public const double PiOver2 = Pi / 2;

        /// <summary>
        /// Defines the value of Pi divided by three as a <see cref="System.Single"/>.
        /// </summary>
        public const double PiOver3 = Pi / 3;

        /// <summary>
        /// Definesthe value of  Pi divided by four as a <see cref="System.Single"/>.
        /// </summary>
        public const double PiOver4 = Pi / 4;

        /// <summary>
        /// Defines the value of Pi divided by six as a <see cref="System.Single"/>.
        /// </summary>
        public const double PiOver6 = Pi / 6;

        /// <summary>
        /// Defines the value of Pi multiplied by two as a <see cref="System.Single"/>.
        /// </summary>
        public const double TwoPi = 2 * Pi;

        /// <summary>
        /// Defines the value of Pi multiplied by 3 and divided by two as a <see cref="System.Single"/>.
        /// </summary>
        public const double ThreePiOver2 = 3 * Pi / 2;

        /// <summary>
        /// Defines the value of E as a <see cref="System.Single"/>.
        /// </summary>
        public const double E = 2.71828182845904523536f;

        /// <summary>
        /// Defines the base-10 logarithm of E.
        /// </summary>
        public const double Log10E = 0.434294482f;

        /// <summary>
        /// Defines the base-2 logarithm of E.
        /// </summary>
        public const double Log2E = 1.442695041f;

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static long NextPowerOfTwo(long n) {
            if( n < 0 ) {
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            }
            return (long) System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(n, 2)));
        }

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static int NextPowerOfTwo(int n) {
            if( n < 0 ) {
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            }
            return (int) System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(n, 2)));
        }

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static double NextPowerOfTwo(double n) {
            if( n < 0 ) {
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            }
            return System.Math.Pow(2, System.Math.Ceiling(System.Math.Log((double)n, 2)));
        }

        /// <summary>Calculates the factorial of a given natural number.
        /// </summary>
        /// <param name="n">The number.</param>
        /// <returns>n!</returns>
        public static long Factorial(int n) {
            long result = 1;

            for( ; n > 1; n-- ) {
                result *= n;
            }

            return result;
        }

        /// <summary>
        /// Calculates the binomial coefficient <paramref name="n"/> above <paramref name="k"/>.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="k">The k.</param>
        /// <returns>n! / (k! * (n - k)!)</returns>
        public static long BinomialCoefficient(int n, int k) {
            return Factorial(n) / ( Factorial(k) * Factorial(n - k) );
        }

        /// <summary>
        /// Returns an approximation of the inverse square root of left number.
        /// </summary>
        /// <param name="x">A number.</param>
        /// <returns>An approximation of the inverse square root of the specified number, with an upper error bound of 0.001</returns>
        /// <remarks>
        /// This is an improved implementation of the the method known as Carmack's inverse square root
        /// which is found in the Quake III source code. This implementation comes from
        /// http://www.codemaestro.com/reviews/review00000105.html. For the history of this method, see
        /// http://www.beyond3d.com/content/articles/8/
        /// </remarks>
        public static double InverseSqrtFast(double x) {
            unsafe {
                double xhalf = 0.5f * x;
                int i = *(int*) &x;              // Read bits as integer.
                i = 0x5f375a86 - ( i >> 1 );      // Make an initial guess for Newton-Raphson approximation
                x = *(double*) &i;                // Convert bits back to double
                x *= 1.5f - xhalf * x * x; // Perform left single Newton-Raphson step.
                return x;
            }
        }

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        public static double DegreesToRadians(double degrees) {
            const double degToRad = System.Math.PI / 180.0f;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        public static double RadiansToDegrees(double radians) {
            const double radToDeg = 180.0f / System.Math.PI;
            return radians * radToDeg;
        }

        /// <summary>
        /// Swaps two double values.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        public static void Swap(ref double a, ref double b) {
            double temp = a;
            a = b;
            b = temp;
        }

        /// <summary>
        /// Returns the positive remainder of x / m.
        /// </summary>
        /// <param name="x">The number</param>
        /// <param name="m">The divisor</param>
        /// <returns></returns>
        public static int Mod(int x, int m) {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>
        /// Returns the positive remainder of x / m.
        /// </summary>
        /// <param name="x">The number</param>
        /// <param name="m">The divisor</param>
        /// <returns></returns>
        public static double Mod(double x, double m) {
            double r = x % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>
        /// Returns the normalized angle difference in radians from a1 to a2
        /// </summary>
        /// <param name="a1">The first angle</param>
        /// <param name="a2">The second angle</param>
        /// <returns>The angle difference a1 - a2</returns>
        public static double AngleDifference(double a1, double a2) {
            return Mod(a1 - a2 + Pi, TwoPi) - Pi;
        }

        /// <summary>
        /// Linearly interpolates from a1 to a2 in radians.
        /// Takes the shortest path around the circle.
        /// </summary>
        /// <param name="a1">The first angle</param>
        /// <param name="a2">The second angle</param>
        /// <param name="blend">The blending factor</param>
        /// <returns>The linear interpolation from a1 to a2</returns>
        public static double LerpAngle(double a1, double a2, double blend) {
            if (double.IsNaN(a1)) return a2;
            if (double.IsNaN(a2)) return a1;
            return blend * AngleDifference(a2, a1) + a1;
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        public static int Clamp(int n, int min, int max) {
            return Math.Max(Math.Min(n, max), min);
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        public static double Clamp(double n, double min, double max) {
            return Math.Max(Math.Min(n, max), min);
        }

        private static unsafe int DoubleToInt32Bits(double f) {
            return *(int*) &f;
        }

        /// <summary>
        /// Approximates floating point equality with a maximum number of different bits.
        /// This is typically used in place of an epsilon comparison.
        /// see: https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
        /// see: https://stackoverflow.com/questions/3874627/floating-point-comparison-functions-for-c-sharp
        /// </summary>
        /// <param name="a">the first value to compare</param>
        /// <param name="b">>the second value to compare</param>
        /// <param name="maxDeltaBits">the number of floating point bits to check</param>
        /// <returns></returns>
        public static bool ApproximatelyEqual(double a, double b, int maxDeltaBits) {
            // we use longs here, otherwise we run into a two's complement problem, causing this to fail with -2 and 2.0
            long aInt = DoubleToInt32Bits(a);
            if( aInt < 0 ) {
                aInt = Int32.MinValue - aInt;
            }

            long bInt = DoubleToInt32Bits(b);
            if( bInt < 0 ) {
                bInt = Int32.MinValue - bInt;
            }

            long intDiff = Math.Abs(aInt - bInt);
            return intDiff <= 1 << maxDeltaBits;
        }

        /// <summary>
        /// Approximates double-precision floating point equality by an epsilon (maximum error) value.
        /// This method is designed as a "fits-all" solution and attempts to handle as many cases as possible.
        /// </summary>
        /// <param name="a">The first double.</param>
        /// <param name="b">The second double.</param>
        /// <param name="epsilon">The maximum error between the two.</param>
        /// <returns><value>true</value> if the values are approximately equal within the error margin; otherwise, <value>false</value>.</returns>
        [SuppressMessage("ReSharper", "CompareOfdoublesByEqualityOperator")]
        public static bool ApproximatelyEqualEpsilon(double a, double b, double epsilon) {
            const double doubleNormal = ( 1L << 52 ) * double.Epsilon;
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);

            if( a == b ) {
                // Shortcut, handles infinities
                return true;
            }

            if( a == 0.0f || b == 0.0f || diff < doubleNormal ) {
                // a or b is zero, or both are extremely close to it.
                // relative error is less meaningful here
                return diff < epsilon * doubleNormal;
            }

            // use relative error
            return diff / Math.Min(absA + absB, double.MaxValue) < epsilon;
        }

        /// <summary>
        /// Approximates equivalence between two double-precision floating-point numbers on a direct human scale.
        /// It is important to note that this does not approximate equality - instead, it merely checks whether or not
        /// two numbers could be considered equivalent to each other within a certain tolerance. The tolerance is
        /// inclusive.
        /// </summary>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare·</param>
        /// <param name="tolerance">The tolerance within which the two values would be considered equivalent.</param>
        /// <returns>Whether or not the values can be considered equivalent within the tolerance.</returns>
        [SuppressMessage("ReSharper", "CompareOfdoublesByEqualityOperator")]
        public static bool ApproximatelyEquivalent(double a, double b, double tolerance) {
            if( a == b ) {
                // Early bailout, handles infinities
                return true;
            }

            double diff = Math.Abs(a - b);
            return diff <= tolerance;
        }

        /// <summary>
        /// Packs up to 32 little-endian bits into a signed integer.
        /// </summary>
        /// <param name="bitArray">Bits whose index corresponds to the integer bit position.</param>
        /// <returns>The packed integer.</returns>
        public static int GetIntFromBitArray(BitArray bitArray) {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        // Secant 
        /// <summary>
        /// Computes the secant of an angle in radians.
        /// </summary>
        /// <param name="x">The angle in radians.</param>
        /// <returns><c>1 / cos(x)</c>.</returns>
        public static double Sec(double x)
        {
            return 1/Math.Cos(x);
        }

        // Cosecant
        /// <summary>
        /// Computes the cosecant of an angle in radians.
        /// </summary>
        /// <param name="x">The angle in radians.</param>
        /// <returns><c>1 / sin(x)</c>.</returns>
        public static double Cosec(double x)
        {
            return 1/Math.Sin(x);
        }

        // Cotangent 
        /// <summary>
        /// Computes the cotangent of an angle in radians.
        /// </summary>
        /// <param name="x">The angle in radians.</param>
        /// <returns><c>1 / tan(x)</c>.</returns>
        public static double Cotan(double x)
        {
            return 1/Math.Tan(x);
        }

        // Inverse Sine 
        /// <summary>
        /// Computes the principal inverse sine using the ratio identity.
        /// </summary>
        /// <param name="x">The ratio whose principal inverse angle is requested.</param>
        /// <returns>An angle in radians.</returns>
        public static double Arcsin(double x)
        {
            return Math.Atan(x / Math.Sqrt(-x * x + 1));
        }

        // Inverse Cosine 
        /// <summary>
        /// Computes the principal inverse cosine using the ratio identity.
        /// </summary>
        /// <param name="x">The ratio whose principal inverse angle is requested.</param>
        /// <returns>An angle in radians.</returns>
        public static double Arccos(double x)
        {
            return Math.Atan(-x / Math.Sqrt(-x * x + 1)) + 2 * Math.Atan(1);
        }


        // Inverse Secant 
        /// <summary>
        /// Computes the principal inverse secant.
        /// </summary>
        /// <param name="x">The secant value whose principal inverse angle is requested.</param>
        /// <returns>An angle in radians.</returns>
        public static double Arcsec(double x)
        {
            return 2 * Math.Atan(1) - Math.Atan(Math.Sign(x) / Math.Sqrt(x * x - 1));
        }

        // Inverse Cosecant 
        /// <summary>
        /// Computes the principal inverse cosecant.
        /// </summary>
        /// <param name="x">The cosecant value whose principal inverse angle is requested.</param>
        /// <returns>An angle in radians.</returns>
        public static double Arccosec(double x)
        {
            return Math.Atan(Math.Sign(x) / Math.Sqrt(x * x - 1));
        }

        // Inverse Cotangent 
        /// <summary>
        /// Computes the principal inverse cotangent.
        /// </summary>
        /// <param name="x">The cotangent value whose principal inverse angle is requested.</param>
        /// <returns>An angle in radians.</returns>
        public static double Arccotan(double x)
        {
            return 2 * Math.Atan(1) - Math.Atan(x);
        } 

        // Hyperbolic Sine 
        /// <summary>
        /// Computes the hyperbolic sine.
        /// </summary>
        /// <param name="x">The real hyperbolic argument.</param>
        /// <returns><c>(e^x - e^-x) / 2</c>.</returns>
        public static double HSin(double x)
        {
            return (Math.Exp(x) - Math.Exp(-x)) / 2 ;
        }

        // Hyperbolic Cosine 
        /// <summary>
        /// Computes the hyperbolic cosine.
        /// </summary>
        /// <param name="x">The real hyperbolic argument.</param>
        /// <returns><c>(e^x + e^-x) / 2</c>.</returns>
        public static double HCos(double x)
        {
            return (Math.Exp(x) + Math.Exp(-x)) / 2 ;
        }

        // Hyperbolic Tangent 
        /// <summary>
        /// Computes the hyperbolic tangent.
        /// </summary>
        /// <param name="x">The real hyperbolic argument.</param>
        /// <returns>The ratio of hyperbolic sine to hyperbolic cosine.</returns>
        public static double HTan(double x)
        {
            return (Math.Exp(x) - Math.Exp(-x)) / (Math.Exp(x) + Math.Exp(-x));
        } 

        // Hyperbolic Secant 
        /// <summary>
        /// Computes the hyperbolic secant.
        /// </summary>
        /// <param name="x">The real hyperbolic argument.</param>
        /// <returns>The reciprocal of hyperbolic cosine.</returns>
        public static double HSec(double x)
        {
            return 2 / (Math.Exp(x) + Math.Exp(-x));
        } 

        // Hyperbolic Cosecant 
        /// <summary>
        /// Computes the hyperbolic cosecant.
        /// </summary>
        /// <param name="x">The nonzero real hyperbolic argument.</param>
        /// <returns>The reciprocal of hyperbolic sine.</returns>
        public static double HCosec(double x)
        {
            return 2 / (Math.Exp(x) - Math.Exp(-x));
        } 

        // Hyperbolic Cotangent 
        /// <summary>
        /// Computes the hyperbolic cotangent.
        /// </summary>
        /// <param name="x">The nonzero real hyperbolic argument.</param>
        /// <returns>The ratio of hyperbolic cosine to hyperbolic sine.</returns>
        public static double HCotan(double x)
        {
            return (Math.Exp(x) + Math.Exp(-x)) / (Math.Exp(x) - Math.Exp(-x));
        } 

        // Inverse Hyperbolic Sine 
        /// <summary>
        /// Computes the inverse hyperbolic sine.
        /// </summary>
        /// <param name="x">The hyperbolic-sine value to invert.</param>
        /// <returns>The real-valued inverse where defined.</returns>
        public static double HArcsin(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x + 1)) ;
        }

        // Inverse Hyperbolic Cosine 
        /// <summary>
        /// Computes the inverse hyperbolic cosine.
        /// </summary>
        /// <param name="x">The hyperbolic-cosine value to invert; must be at least one for a real result.</param>
        /// <returns>The real-valued inverse for inputs at least one.</returns>
        public static double HArccos(double x)
        {
            return Math.Log(x + Math.Sqrt(x * x - 1));
        }

        // Inverse Hyperbolic Tangent 
        /// <summary>
        /// Computes the inverse hyperbolic tangent.
        /// </summary>
        /// <param name="x">The hyperbolic-tangent value to invert; real inputs lie between -1 and 1.</param>
        /// <returns>The real-valued inverse for inputs between -1 and 1.</returns>
        public static double HArctan(double x)
        {
            return Math.Log((1 + x) / (1 - x)) / 2 ;
        }

        // Inverse Hyperbolic Secant 
        /// <summary>
        /// Computes the inverse hyperbolic secant.
        /// </summary>
        /// <param name="x">The hyperbolic-secant value to invert.</param>
        /// <returns>The logarithmic inverse where defined.</returns>
        public static double HArcsec(double x)
        {
            return Math.Log((Math.Sqrt(-x * x + 1) + 1) / x);
        } 

        // Inverse Hyperbolic Cosecant 
        /// <summary>
        /// Computes the inverse hyperbolic cosecant.
        /// </summary>
        /// <param name="x">The nonzero hyperbolic-cosecant value to invert.</param>
        /// <returns>The logarithmic inverse where defined.</returns>
        public static double HArccosec(double x)
        {
            return Math.Log((Math.Sign(x) * Math.Sqrt(x * x + 1) + 1) / x) ;
        }

        // Inverse Hyperbolic Cotangent 
        /// <summary>
        /// Computes the inverse hyperbolic cotangent.
        /// </summary>
        /// <param name="x">The hyperbolic-cotangent value to invert.</param>
        /// <returns>The logarithmic inverse where defined.</returns>
        public static double HArccotan(double x)
        {
            return Math.Log((x + 1) / (x - 1)) / 2;
        } 

        // Logarithm to base N 
        /// <summary>
        /// Computes a logarithm in an arbitrary base by change of base.
        /// </summary>
        /// <param name="x">The positive value whose logarithm is requested.</param>
        /// <param name="n">The positive base other than one.</param>
        /// <returns><c>ln(x) / ln(n)</c>.</returns>
        public static double LogN(double x, double n)
        {
            return Math.Log(x) / Math.Log(n);
        }
    }
}
