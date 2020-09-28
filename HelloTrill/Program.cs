using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.StreamProcessing;

namespace HelloTrill
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = new List<int> {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
            var stream = list
                    .ToObservable()
                    .ToTemporalStreamable(e => e, e => e + 1)
                ;
            var obs = stream
                    .Select(e => e + 1)
                ;
            obs
                .ToStreamEventObservable()
                .Where(e => e.IsData)
                .ForEach(e => { Console.WriteLine(e); })
                ;
        }
    }
}