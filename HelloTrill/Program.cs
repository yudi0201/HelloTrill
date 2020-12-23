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
            var SIZE = 1000;                                    // Size of the data set
            var list = new List<int>();                         // A list for storing the data points
            for (int i = 0; i < SIZE; i++)
            {
                list.Add(i);                                    // Populate the list with dummy data
            }
            var stream = list
                    .ToObservable()                             // Convert the data list to an Observable first 
                    .ToTemporalStreamable(e => e, e => e + 1)   // Then convert to Trill temporal stream;
                ;                                               // Each event has an integer payload 'n'
                                                                // and an interval of [n, n+1)
            var obs = stream
                    .Select(e => e + 1)                         // Set transformations on the stream.
                ;                                               // In this case, Adding 1 to each payload using Select
            obs
                .ToStreamEventObservable()                      // Convert back to Observable (of StreamEvents)
                .Where(e => e.IsData)                           // Only pick data events from the stream
                .ForEach(e => { Console.WriteLine(e); })        // Print the events to the console
                ;
        }
    }
}