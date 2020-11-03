using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;
using Quantum.RekelaljoqiKolijejee;

// ReSharper disable All
namespace Quantum.RekelaljoqiKolijejee
{
    class Program
    {
        static void Main(string[] args)
        {
            using var quantumSimulator = new QuantumSimulator();
            var result = GenerateQuantumRandom.Run(quantumSimulator).Result;

            if (result == Result.One)
            {
                Console.WriteLine("1");
            }
            else
            {
                Console.WriteLine("0");
            }

            Console.Read();
        }
    }
}
