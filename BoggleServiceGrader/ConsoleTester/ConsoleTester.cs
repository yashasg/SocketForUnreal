using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoggleService;
using BoggleServiceTest;
using System.Net;

namespace ConsoleTester
{
    static class ConsoleTester
    {
        static void Main(string[] args)
        {
            Test();
        }

        public static void Test()
        {
            BoggleServiceGrader tests = new BoggleServiceGrader();
            tests.Assert = new AssertionCounter();
            
            try
            {
                tests.UseCase1();
                tests.Assert.Report();
                tests.UseCase2();
                tests.Assert.Report();
                tests.UseCase3();
                tests.Assert.Report();
                tests.UseCase4();
                tests.Assert.Report();
                tests.UseCase5();
                tests.Assert.Report();
                tests.UseCase6();
                tests.Assert.Report();
                tests.JoinGameErrorTests();
                tests.Assert.Report();
                tests.DeleteGameErrorTests();
                tests.Assert.Report();
                tests.StatusErrorTests();
                tests.Assert.Report();
                tests.BriefStatusErrorTests();
                tests.Assert.Report();
                tests.PlayWordErrorTests();
                tests.Assert.Report();
            }
            finally
            {
                Console.WriteLine("Summary:");
                Console.WriteLine("Passed: " + tests.Assert.Count + "/" + tests.Assert.Total);
            }
        }
    }
}
