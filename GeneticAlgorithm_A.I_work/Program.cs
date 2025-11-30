using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace GeneticAlgorithm
{
    public class Program
    {
        private Random random = new Random();
        private int maxEmployees = 10;
        private int populationSize = 100;
        private int generations = 100;
        private double mutationRate = 0.05;

        private static int numEmployees;
        private static int numActualDays;
        private const int numShiftsPerDay = 3;
        private static int numTimeSlots;

        private static int[] requiredWorkersPerShiftDisplay;
        private static int[] requiredWorkersPerShiftNumeric;

        private static int[,] employeePreferences;

        static Program()
        {
            string csv = "grafik_7d_3s_10emp.csv";
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Data", csv);

            if (!File.Exists(file))
            {
                Console.WriteLine($"Plik nie istnieje: {file}");
                return;
            }

            requiredWorkersPerShiftDisplay = ReadRequirementsFromFile(file);
            employeePreferences = ReadPreferencesFromFile(file);

            numEmployees = employeePreferences.GetLength(0);
            numActualDays = requiredWorkersPerShiftDisplay.Length / numShiftsPerDay;
            numTimeSlots = numActualDays * numShiftsPerDay;

            requiredWorkersPerShiftNumeric = (int[])requiredWorkersPerShiftDisplay.Clone();
        }

        private static int[] ReadRequirementsFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            string[] headers = lines[0].Split(',');
            string[] values = lines[1].Split(',');

            List<int> reqValues = new List<int>();
            for (int j = 0; j < headers.Length; j++)
            {
                if (headers[j].StartsWith("req") && j < values.Length)
                    reqValues.Add(int.Parse(values[j]));
            }

            return reqValues.ToArray();
        }

        private static int[,] ReadPreferencesFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            string[] headers = lines[2].Split(',');

            List<int> prefCols = new List<int>();
            for (int i = 0; i < headers.Length; i++)
                if (headers[i].StartsWith("pref"))
                    prefCols.Add(i);

            int numEmp = lines.Length - 3;
            int numShifts = prefCols.Count;
            int[,] preferences = new int[numEmp, numShifts];

            for (int row = 3; row < lines.Length; row++)
            {
                string[] values = lines[row].Split(',');
                int empIndex = row - 3;
                for (int c = 0; c < numShifts; c++)
                    if (prefCols[c] < values.Length)
                        preferences[empIndex, c] = int.Parse(values[prefCols[c]]);
            }
            return preferences;
        }

        private int[,] GenerateSchedule()
        {
            int[,] schedule = new int[numEmployees, numTimeSlots];
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < numTimeSlots; j++)
                    schedule[i, j] = random.Next(2);
            return schedule;
        }

        private int Fitness(int[,] schedule, List<int> clientCounts)
        {
            int totalError = 0;
            for (int j = 0; j < numTimeSlots; j++)
            {
                int sum = 0;
                for (int i = 0; i < numEmployees; i++)
                    sum += schedule[i, j];
                int ideal = Math.Max(1, clientCounts[j] / 20);
                totalError += Math.Abs(sum - ideal);
            }
            return totalError;
        }

        private int[,] Crossover(int[,] parent1, int[,] parent2)
        {
            int[,] child = new int[numEmployees, numTimeSlots];
            for (int i = 0; i < numEmployees; i++)
            {
                int split = random.Next(numTimeSlots);
                for (int j = 0; j < split; j++)
                    child[i, j] = parent1[i, j];
                for (int j = split; j < numTimeSlots; j++)
                    child[i, j] = parent2[i, j];
            }
            return child;
        }

        private void Mutate(int[,] individual)
        {
            for (int i = 0; i < numEmployees; i++)
                for (int j = 0; j < numTimeSlots; j++)
                    if (random.NextDouble() < mutationRate)
                        individual[i, j] = random.Next(2);
        }

        private int[,] Select(List<int[,]> population, List<int> clientCounts)
        {
            var tournament = population.OrderBy(x => random.Next()).Take(5).ToList();
            return tournament.OrderBy(x => Fitness(x, clientCounts)).First();
        }

        public int[,] Optimize(List<int> clientCounts, string logFile)
        {
            List<int[,]> population = new List<int[,]>();
            for (int i = 0; i < populationSize; i++)
                population.Add(GenerateSchedule());

            using StreamWriter writer = new StreamWriter(logFile, false, new UTF8Encoding(true));
            writer.WriteLine("Generation;BestFitness;AverageFitness;MutationRate;MutationCount");

            for (int gen = 0; gen < generations; gen++)
            {
                int mutationCount = 0;
                population = population.OrderBy(ind => Fitness(ind, clientCounts)).ToList();
                int bestFitness = Fitness(population[0], clientCounts);
                double avgFitness = population.Average(ind => Fitness(ind, clientCounts));

                // Nowa populacja
                List<int[,]> newPopulation = new List<int[,]>();
                while (newPopulation.Count < populationSize)
                {
                    var parent1 = Select(population, clientCounts);
                    var parent2 = Select(population, clientCounts);
                    var child = Crossover(parent1, parent2);
                    Mutate(child);
                    newPopulation.Add(child);
                }

                population = newPopulation;

                // Zapis do CSV
                writer.WriteLine($"{gen + 1};{bestFitness};{Math.Round(avgFitness, 2).ToString().Replace('.', ',')};{mutationRate.ToString().Replace('.', ',')};{mutationCount}");
            
                
            
            }

            // Preferences
            writer.WriteLine();
            writer.WriteLine("Preferences");
            for (int i = 0; i < numEmployees; i++)
            {
                writer.Write($"P{i + 1};");
                for (int j = 0; j < numTimeSlots; j++)
                {
                    if (j >= employeePreferences.GetLength(1)) break;
                    writer.Write(employeePreferences[i, j]);
                    if (j < numTimeSlots - 1) writer.Write(';');
                }
                writer.WriteLine();
            }

            // Requirements
            writer.WriteLine();
            writer.WriteLine("Requirements");
            writer.Write("LP;");
            for (int j = 0; j < numTimeSlots; j++)
            {
                if (j >= requiredWorkersPerShiftDisplay.Length) break;
                writer.Write(requiredWorkersPerShiftDisplay[j]);
                if (j < numTimeSlots - 1) writer.Write(';');
            }
            writer.WriteLine();

            // Schedule (z najlepszym osobnikiem)
            writer.WriteLine();
            writer.WriteLine("Schedule");

            int[,] finalSchedule = population[0];
            for (int i = 0; i < numEmployees; i++)
            {
                writer.Write($"P{i + 1};");
                for (int j = 0; j < numTimeSlots; j++)
                {
                    if (j >= finalSchedule.GetLength(1)) break; // blokada
                    writer.Write(finalSchedule[i, j]);
                    if (j < numTimeSlots - 1) writer.Write(';');
                }
                writer.WriteLine();
            }


            return population[0];
        }

        static string GetLogFileName()
        {
            string logDirectory = "../../../logi";
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            int logNumber = 1;
            string logFileName;
            do
            {
                logFileName = Path.Combine(logDirectory, $"genetic_log{logNumber}.csv");
                logNumber++;
            } while (File.Exists(logFileName));
            return logFileName;
        }

        static void Main(string[] args)
        {
            Program scheduler = new Program();
            Random randomClient = new Random();

            List<int> clientCounts = new List<int>();
            for (int i = 0; i < numShiftsPerDay * numActualDays; i++)
                clientCounts.Add(randomClient.Next(10, 100));

            string logFileName = GetLogFileName();
            int[,] bestSchedule = scheduler.Optimize(clientCounts, logFileName);

            Console.WriteLine("Najlepszy harmonogram:");
            for (int i = 0; i < numEmployees; i++)
            {
                for (int j = 0; j < numTimeSlots; j++)
                    Console.Write(bestSchedule[i, j] + " ");
                Console.WriteLine();
            }

            Console.WriteLine($"Log zapisany w: {logFileName}");
            Console.ReadKey();
        }
    }
}
