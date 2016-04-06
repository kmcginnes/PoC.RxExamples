using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace PoC.RxExamples
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }

        public static void StopAndPrint(this Stopwatch stopwatch)
        {
            stopwatch.Stop();
            decimal elapsedSeconds = ((decimal)stopwatch.ElapsedMilliseconds) / 1000;
            Console.WriteLine("Finished in {0} seconds", elapsedSeconds);
        }

        public static string Truncate(this string source, int numCharacters)
        {
            return string.Join("", source.Take(numCharacters));
        }

        /// <summary>
        /// Filters the original stream to values produced when the conditional stream's latest value is true.
        /// </summary>
        /// <typeparam name="T">Original stream's type.</typeparam>
        /// <param name="originalStream">The original stream.</param>
        /// <param name="conditionalStream">The stream used to filter the original stream.</param>
        /// <returns>A filtered version of the original stream.</returns>
        public static IObservable<T> WhenTrue<T>(this IObservable<T> originalStream, IObservable<bool> conditionalStream)
        {
            return originalStream
                .CombineLatest(conditionalStream, (orig, condition) => new { orig, condition })
                .Where(x => x.condition)
                .Select(x => x.orig);
        }

        /// <summary>
        /// Filters the original stream to values produced when the conditional stream's latest value is false.
        /// </summary>
        /// <typeparam name="T">Original stream's type.</typeparam>
        /// <param name="originalStream">The original stream.</param>
        /// <param name="conditionalStream">The stream used to filter the original stream.</param>
        /// <returns>A filtered version of the original stream.</returns>
        public static IObservable<T> WhenFalse<T>(this IObservable<T> originalStream, IObservable<bool> conditionalStream)
        {
            return originalStream
                .CombineLatest(conditionalStream, (orig, condition) => new { orig, condition })
                .Where(x => !x.condition)
                .Select(x => x.orig);
        }
    }
}