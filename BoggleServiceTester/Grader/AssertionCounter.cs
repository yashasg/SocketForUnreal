using System.Net;
using System;
using BoggleService;
using System.Collections.Generic;

namespace BoggleServiceTest
{
    public class AssertionCounter
    {
        public int Count { get; private set; }
        public int Total { get; private set; }
        public int SectionCount { get; private set; }
        public int SectionTotal { get; private set; }

        public void Report()
        {
            Console.WriteLine("Passed " + SectionCount + "/" + SectionTotal + "\n");
            SectionCount = SectionTotal = 0;
        }
        public void AreEqual(string o1, string o2)
        {
            Total++;
            SectionTotal++;
            if (o1 == o2)
            {
                Count++;
                SectionCount++;
            }
        }

        public void AreEqual(int o1, int o2)
        {
            Total++;
            SectionTotal++;
            if (o1 == o2)
            {
                Count++;
                SectionCount++;
            }
        }

        public void AreEqual(HttpStatusCode o1, HttpStatusCode o2)
        {
            Total++;
            SectionTotal++;
            if (o1 == o2)
            {
                Count++;
                SectionCount++;
            }
        }

        public void AreEqual(List<WordScorePair> o1, List<WordScorePair> o2)
        {
            Total++;
            SectionTotal++;
            if (o1 == o2)
            {
                Count++;
                SectionCount++;
            }
        }

        public void IsTrue(bool b)
        {
            Total++;
            SectionTotal++;
            if (b)
            {
                Count++;
                SectionCount++;
            }
        }
    }
}