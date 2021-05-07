using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Xml.Xsl;
using Microsoft.StreamProcessing;

namespace HelloTrill
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
             * Generating synthetic data
             */
            var SIZE = 1000; // Size of the data set
            var listA = new List<int>(); // A list for storing the data points
            var listB = new List<int>(); // Another list for storing the data points
            var listC = new List<int>{0,1,2,3,4,10,11,12,13,14,25,26,27,28,29}; //for testing exercise 3.4 and 4.1
            for (int i = 0; i < SIZE; i++)
            {
                listA.Add(i);     // Populate listA with dummy data
                if ( i % 3 == 0)  //for level 3 and 4 exercises. Introduces gaps in events or make the period of 
                                    //of each event longer.
                {
                    listB.Add(i); // Populate listB with dummy data
                }
            }
            

            /**
             * Creating lists created above to Trill streams
             */
            var streamA = listA // Creating first stream from listA 
                    .ToObservable() // Convert the data list to an Observable first 
                    .ToTemporalStreamable(e => e, e => e + 1) // Then convert to Trill temporal stream;
                ; // nth event in the stream has an integer payload 'n'
            // and an interval of [n, n+1)

            var streamB = listB // Creating streamB (not using yet) similar to streamA.
                    .ToObservable()
                    .ToTemporalStreamable(e => e, e => e + 3)
                ;

            var streamC = listC 
                .ToObservable()
                .ToTemporalStreamable(e => e, e => e + 1);
            
            /**
             * Define transformations on the stream(s) 
             */

            //Level 1 exercises
            //exercise 1.1
            var exercise11 = streamA
                    .Select(e => e + 1) // Set transformations on the stream.
                ; // In this case, Adding 1 to each payload using Select

            //exercise 1.2
            var exercise12 = streamA
                .Select(e => e * 3);

            //exercise 1.3
            var exercise13 = streamA
                .Where(e => e % 2 != 0);

            //exercise 1.4
            var exercise14 = streamA
                .Select(e => e * 3)
                .Where(e => e % 2 != 0);

            //Level 2 Exercises
            //exercise 2.1
            var exercise21 = streamA
                .TumblingWindowLifetime(10)
                .Sum(e => e);
            
            //exercise 2.2
            var exercise22 = streamA
                .Join(streamB,
                    (a, b) => a + b);

            //exercise 2.3
            var exercise23 = streamA
                .Join(streamB,
                    (a, b) => new {a, b});

            //exercise 2.4
            var exercise24 = streamB
                .TumblingWindowLifetime(10)
                .Sum(e => e)
                .Join(streamA,
                    (left, right) => new {left, right});

            //exercise 2.5
            var exercise25 = streamA
                .Multicast(s => s.TumblingWindowLifetime((10))
                    .Sum(e => e)
                    .Join(s, (left, right) => new {left, right}));

            //exercise 2.6
            var mean = streamA
                .TumblingWindowLifetime(10)
                .Average(e => e);  //Calculate mean of each 10-sized window

            var temp = mean.Join(streamA.TumblingWindowLifetime(10),
                (sampleMean, individualEvent) => new {sampleMean, individualEvent});

            var stdev = temp.Multicast(s => 
                s.SumSquares(e => e.sampleMean - e.individualEvent)
                .Join(s.Count(),
                    (sumsquare, numevent) => Math.Sqrt(sumsquare/(numevent - 1))));  //Calculate the standard deviation of each 10-sized window

            var zscore = temp.Join(stdev,
                (left, sampleStdev) =>
                    (left.individualEvent - left.sampleMean)/sampleStdev);   //Calculate the z-score of each event from the mean and the stdev.

            
            //Level 2.5 Exercise
            long winsize = 10;
            long strilen = 5;
            var rollingMean = streamA
                .HoppingWindowLifetime(winsize, strilen)
                .Average(e=>e);

            //Level 3 exercises
            //exercise 3.1
            var exercise31 = streamA.Multicast(s=>s 
                    .AlterEventDuration(StreamEvent.InfinitySyncTime)
                    .ClipEventDuration(s));
            
            //exercise 3.2
            var fixedinterval = new[] {StreamEvent.CreateInterval(0, 1000, Unit.Default)}
                .ToObservable().ToStreamable();
            var exercise32 = streamA.Multicast(s => s
                    .AlterEventDuration(StreamEvent.InfinitySyncTime)
                    .ClipEventDuration(s))
                .Join(fixedinterval,(left,right)=>left)
                .Chop(0,1);
            
            //exercise 3.3
            var exercise33 = streamA.Multicast(s => s
                    .AlterEventDuration(StreamEvent.InfinitySyncTime)
                    .ClipEventDuration(s))
                .Join(fixedinterval, (left, right) => left)
                .Chop(0, 1)
                .Select((origTime, e) => new{e = origTime});
            
            //exercise 3.4
            var period = 1;
            long gap_tolerance = 10;
            var extendedEvents = streamC.AlterEventLifetime(startTime => startTime, 
                (startTime,endTime)=>endTime-startTime+gap_tolerance);
            var filled = extendedEvents.ClipEventDuration(extendedEvents)
                .AlterEventLifetime(startTime=>startTime,
                    (startTime,endTime)=>(endTime-startTime)>gap_tolerance ? period : (endTime-startTime))
              .Chop(0, period);
            
            //Exercise 4.1
            var W = 6;
            //var period = 1;
            //long gap_tolerance = 10;
            //var extendedEvents = streamC.AlterEventLifetime(startTime => startTime, 
            //    (startTime,endTime)=>endTime-startTime+gap_tolerance);
            //var filled = extendedEvents.ClipEventDuration(extendedEvents)
            //    .AlterEventLifetime(startTime => startTime,
            //        (startTime, endTime) => (endTime - startTime) > gap_tolerance ? period : (endTime - startTime))
            //    .Chop(0, period);
            var rollingAverage = streamC.HoppingWindowLifetime(W, period).Average(e => e);
            var temp1 = filled.Join(rollingAverage,(left,right)=>right);
            var filledAverage=temp1.WhereNotExists(streamC)
                .Union(streamC.Select(e=>(double)e));

            //Exercise 4.2
            var input_T = 3; //input period
            var output_T = 2;  //output period
            var inputStream = streamB.Select((startTime,e) => startTime % 2 == 0 ? e * 2 + 1 : e); //make the payload a little irregular at places for testing purposes.
            var ttt = inputStream.Chop(0, output_T);
            var slope = inputStream.ShiftEventLifetime(input_T)
                .Join(inputStream,(left,right)=>(float)(right-left)/input_T)
                .ShiftEventLifetime(-input_T);
            var aaa = ttt.Join(slope, (left, right) => new {left, right});
            var bbb = aaa.Select((startTime, e) => 
                e.left + e.right * (startTime % input_T));
            var result = bbb.Select((startTime, e) => (startTime % output_T == 0) ? e : (float) 1/0)
                .Where(e=>e < (float) 1/0)
                .AlterEventDuration(output_T);
            


            /**
                 * Print out the result
                 */
            result
                .ToStreamEventObservable() // Convert back to Observable (of StreamEvents)
                .Where(e => e.IsData) // Only pick data events from the stream
                .ForEachAsync(e => { Console.WriteLine(e); }); // Print the events to the console;
        }
    }
}