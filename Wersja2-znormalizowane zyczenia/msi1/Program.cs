using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace GeneticScheduling
{
    class Program
    {
        static Random rand = new Random();
        const int populationSize = 500;
        const int generations = 100;
        public static int totallyNewChildren = 5;
        public static double mutationRate = 0.1;
        public static bool variableMutationRate = true;
        public static int mutationCount = 0;
        const int EliteCount = 5;

        static int numEmployees;
        static int numActualDays;
        const int numShiftsPerDay = 3;
        static int numTimeSlots;

        static int[] requiredWorkersPerShiftDisplay;
        static int[] requiredWorkersPerShiftNumeric;

        static int[,] employeePreferences;
        public static class FitnessConstants
        {
            public const int workersPerDayPenalty = 50;
            public const int EmployeePreferenceMultiplier = 10;
        }

        static Program()
        {
            string file = @"..\..\..\..\grafik_7d_3s_10emp.csv";

            requiredWorkersPerShiftDisplay = ReadRequirementsFromFile(file);
            employeePreferences = ReadPreferencesFromFile(file);

            numEmployees = employeePreferences.GetLength(0);
            numActualDays = (int)(requiredWorkersPerShiftDisplay.Length / numShiftsPerDay);
            numTimeSlots = numActualDays * numShiftsPerDay;

            requiredWorkersPerShiftNumeric = (int[])requiredWorkersPerShiftDisplay.Clone();
        }

        private static int[] ReadRequirementsFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);
            string[] headers = lines[0].Split(','); // nagłówki req
            string[] values = lines[1].Split(',');  // wiersz z wartościami req

            List<int> reqValues = new List<int>();
            for (int j = 0; j < headers.Length; j++)
            {
                if (headers[j].StartsWith("req") && j < values.Length)
                {
                    reqValues.Add(int.Parse(values[j]));
                }
            }

            return reqValues.ToArray();
        }

        private static int[,] ReadPreferencesFromFile(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);

            // nagłówki pref znajdują się w linii 2 (index 2)
            string[] headers = lines[2].Split(',');

            List<int> prefCols = new List<int>();
            for (int i = 0; i < headers.Length; i++)
                if (headers[i].StartsWith("pref"))
                    prefCols.Add(i);

            int numEmployees = lines.Length - 3; // linia 0 = nagłówek req, 1 = req, 2 = nagłówek pref
            int numShifts = prefCols.Count;

            int[,] preferences = new int[numEmployees, numShifts];

            for (int row = 3; row < lines.Length; row++) // wiersze z wartościami pref
            {
                string[] values = lines[row].Split(',');
                int empIndex = row - 3; // przesunięcie dla tablicy

                for (int c = 0; c < numShifts; c++)
                {
                    if (prefCols[c] < values.Length)
                        preferences[empIndex, c] = int.Parse(values[prefCols[c]]);
                }
            }

            return preferences;
        }



        static void Main(string[] args)
        {
            int[][,] population = new int[populationSize][,];
            for (int i = 0; i < populationSize; i++)
                population[i] = GenerateSchedule();

            var initpop = (int[,])population[0].Clone();
            string logFileName = GetLogFileName();

            string ResultFileName = GetResultFileName();

            using (StreamWriter writer = new StreamWriter(logFileName, false, new UTF8Encoding(true)))
            {
                writer.WriteLine($"Population size:;{populationSize}");
                writer.WriteLine($"Generations:;{generations}");
                writer.WriteLine($"Mutation rate (initial):;{mutationRate}");
                writer.WriteLine($"Elite count:;{EliteCount}");
                writer.WriteLine($"Totally new children:;{totallyNewChildren}");
                writer.WriteLine($"Variable mutation rate:;{variableMutationRate}");
                writer.WriteLine();
                writer.WriteLine($"Generation;BestFitness;AverageFitness;MutationRate;MutationCount");

                double previousAverageFitness = 0.0;
                for (int gen = 0; gen < generations; gen++)
                {
                    population = population.OrderByDescending(s => CalculateFitness(s)).ToArray();

                    int eliteCount = EliteCount;
                    int[][,] newPopulation = new int[populationSize][,];

                    for (int i = 0; i < eliteCount; i++)
                        newPopulation[i] = population[i];

                    for (int i = eliteCount; i < populationSize - totallyNewChildren; i++)
                    {
                        int[,] parent1 = population[rand.Next(populationSize / 2)];
                        int[,] parent2 = population[rand.Next(populationSize / 2)];
                        int[,] child = Crossover(parent1, parent2);
                        if (rand.NextDouble() < mutationRate)
                        {
                            child = Mutate(child);
                            mutationCount++;
                        }
                        newPopulation[i] = child;
                    }

                    for (int i = populationSize - totallyNewChildren; i < populationSize; i++)
                        newPopulation[i] = GenerateSchedule();

                    int bestFitness = CalculateFitness(population[0]);
                    double averageFitness = population.Average(s => CalculateFitness(s));

                    if (gen > 0 && variableMutationRate)
                    {
                        if (averageFitness > previousAverageFitness)
                            mutationRate = Math.Min(1.0, mutationRate + 0.05);
                        else if (averageFitness < previousAverageFitness)
                            mutationRate = Math.Max(0.01, mutationRate - 0.05);
                    }

                    previousAverageFitness = averageFitness;
                    mutationRate = Math.Round(mutationRate, 3);

                    writer.WriteLine($"{gen + 1};{bestFitness};{averageFitness:F2};{mutationRate};{mutationCount}");
                    Console.WriteLine($"Generation {gen + 1}, Best Fitness: {bestFitness}, Avg: {averageFitness:F2}, Mutation Rate: {mutationRate}, MutCount: {mutationCount}");
                    mutationCount = 0;
                    population = newPopulation;
                }

                writer.WriteLine();
                string shiftHeaderString = GetShiftHeaders(";");

                writer.WriteLine("Preferences");
                writer.WriteLine($" ;{shiftHeaderString}");
                for (int i = 0; i < numEmployees; i++)
                {
                    writer.Write($"P{i + 1};");
                    for (int j = 0; j < numTimeSlots; j++)
                    {
                        if (j >= employeePreferences.GetLength(1)) break; // blokada
                        writer.Write(employeePreferences[i, j]);
                        if (j < numTimeSlots - 1) writer.Write(';');
                    }
                    writer.WriteLine();
                }

                writer.WriteLine();
                writer.WriteLine("Requirements");
                writer.WriteLine($" ;{shiftHeaderString}");
                writer.Write($"LP;");
                for (int j = 0; j < numTimeSlots; j++)
                {
                    if (j >= requiredWorkersPerShiftDisplay.Length) break; // blokada
                    writer.Write(requiredWorkersPerShiftDisplay[j]);
                    if (j < numTimeSlots - 1) writer.Write(';');
                }
                writer.WriteLine();

                writer.WriteLine();
                writer.WriteLine("Schedule");
                writer.WriteLine($" ;{shiftHeaderString}");

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

                Console.WriteLine("\nFitness dla każdego pracownika:");
                writer.WriteLine();
                writer.WriteLine("FitnessForEachWorker");
                for (int i = 0; i < numEmployees; i++)
                {
                    int fit = CalculateEmployeeFitnessNorm(finalSchedule, i);
                    writer.WriteLine($"{fit}");
                    Console.WriteLine($"P{i + 1}: {fit}");
                }

                Console.WriteLine("\nInitial Schedule:");
                PrintScheduleToConsole(initpop);
                Console.WriteLine("\nFinal Schedule:");
                PrintScheduleToConsole(population[0]);

                using (StreamWriter writer2 = new StreamWriter(ResultFileName, false, new UTF8Encoding(true)))
                {
                    string Header = GetShiftHeaders(","); // np. D1S1,D1S2,D1S3 ...

                    writer2.Write("Id,");

                    string[] headers = Header.Split(',');

                    // P headers
                    foreach (var h in headers)
                        writer2.Write($"P_{h},");

                    // FR headers
                    foreach (var h in headers)
                        writer2.Write($"FR_{h},");
                    
                    // S headers
                    foreach (var h in headers)
                        writer2.Write($"S_{h},");

                    writer2.Write("worker_fitness,");

                    writer2.Write("mismatchWorkerRequirments,");

                    writer2.Write("mismatchFirmRequirments");

                    writer2.WriteLine();

                    // DATA ROWS
                    for (int i = 0; i < numEmployees; i++)
                    {
                        writer2.Write($"{i},");

                        // Preferences
                        for (int j = 0; j < numTimeSlots; j++)
                        {
                            if (j >= employeePreferences.GetLength(1)) break;
                            writer2.Write(employeePreferences[i, j] + ",");
                        }

                        // Requirements
                        for (int j = 0; j < numTimeSlots; j++)
                        {
                            if (j >= requiredWorkersPerShiftDisplay.Length) break;
                            writer2.Write(requiredWorkersPerShiftDisplay[j] + ",");
                        }

                        // Schedule
                        for (int j = 0; j < numTimeSlots; j++)
                        {
                            if (j >= finalSchedule.GetLength(1)) break;

                            writer2.Write(finalSchedule[i, j]);
                            if (j < numTimeSlots - 1) writer2.Write(",");
                        }

                        writer2.Write(",");

                        Console.WriteLine("\nFitness dla każdego pracownika:");
                        int[] fitArray = CalculateEmployeeFitness(finalSchedule, i);
                        int fitness = fitArray.Sum(); 
                        writer2.Write($"{fitness},");

                        Console.WriteLine("UnsolvedWorkerRequirmnts:");
                        int UnsolvedWorkerR = UnsolvedWorkerRequirmnts(finalSchedule);
                        writer2.Write($"{UnsolvedWorkerR},");

                        Console.WriteLine("UnsolvedFirmRequirmnts:");
                        int UnsolvedFirmR = UnsolvedFirmRequirmnts(finalSchedule);
                        writer2.Write($"{UnsolvedFirmR}");

                        writer2.WriteLine();
                    }
                }
            }

            static string GetResultFileName()
            {
                string logDirectory = "../../../wyniki";
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                int logNumber = 1;
                string logFileName;
                do
                {
                    logFileName = Path.Combine(logDirectory, $"genetic_Result{logNumber}.csv");
                    logNumber++;
                } while (File.Exists(logFileName));
                return logFileName;
            }

            static string GetShiftHeaders(string delimiter, bool forConsole = false)
            {
                StringBuilder sb = new StringBuilder();
                for (int d = 1; d <= numActualDays; d++)
                {
                    for (int s = 1; s <= numShiftsPerDay; s++)
                    {
                        sb.Append($"D{d}S{s}");
                        if (!(d == numActualDays && s == numShiftsPerDay))
                            sb.Append(delimiter);
                    }
                }
                return sb.ToString();
            }

            static string GetLogFileName()
            {
                string logDirectory = "../../../LOGI_ALGORYTMU";
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                string logFileName = Path.Combine(logDirectory, $"genetic_log.csv");
                return logFileName;
            }

            static int[,] GenerateSchedule()
            {
                int[,] schedule = new int[numEmployees, numTimeSlots];
                for (int i = 0; i < numEmployees; i++)
                    for (int j = 0; j < numTimeSlots; j++)
                        schedule[i, j] = rand.Next(2);
                return schedule;
            }

            static int CalculateFitness(int[,] schedule)
            {
                int workersPenalty = CalculateWorkersPenalty(schedule);
                int preferenceBonus = CalculatePreferenceBonus(schedule);
                return preferenceBonus - workersPenalty;
            }

            static int UnsolvedWorkerRequirmnts(int[,] schedule)
            {
                int workersPenalty = CalculateWorkersPenalty(schedule);
                return workersPenalty;
            }

            static int UnsolvedFirmRequirmnts(int[,] schedule)
            {
                int preferenceBonus = CalculatePreferenceBonus(schedule);
                int pref = preferenceBonus / 10;
                return pref;
            }

            static int CalculateEmployeeFitnessNorm(int[,] schedule, int employeeIndex)
            {
                int bonus = 0;
                int numPref = employeePreferences.GetLength(1);

                for (int j = 0; j < numPref; j++)
                {
                    if (schedule[employeeIndex, j] == employeePreferences[employeeIndex, j])
                        bonus += FitnessConstants.EmployeePreferenceMultiplier;
                }

                return bonus;
            }

            static int[] CalculateEmployeeFitness(int[,] schedule, int employeeIndex)
            {
                int numPref = employeePreferences.GetLength(1);
                int[] bonus = new int[numPref];  // tablica bonusów dla każdej zmiany

                for (int j = 0; j < numPref; j++)
                {
                    if (schedule[employeeIndex, j] == employeePreferences[employeeIndex, j])
                        bonus[j] = FitnessConstants.EmployeePreferenceMultiplier;
                    else
                        bonus[j] = 0;
                }

                return bonus; // zwraca tablicę, ale CSV zapisze tylko jedną wartość
            }

            static int CalculateWorkersPenalty(int[,] schedule)
            {
                int penalty = 0;
                for (int j = 0; j < numTimeSlots; j++)
                {
                    if (j >= requiredWorkersPerShiftNumeric.Length) break; // blokada
                    int actualWorkers = 0;
                    for (int i = 0; i < numEmployees; i++)
                    {
                        actualWorkers += schedule[i, j];
                    }
                    penalty += FitnessConstants.workersPerDayPenalty * Math.Abs(requiredWorkersPerShiftNumeric[j] - actualWorkers);
                }
                return penalty;
            }

            static int CalculatePreferenceBonus(int[,] schedule)
            {
                int bonus = 0;
                int numPrefShifts = employeePreferences.GetLength(1);
                for (int i = 0; i < numEmployees; i++)
                {
                    for (int j = 0; j < numPrefShifts; j++)
                    {
                        if (j >= schedule.GetLength(1)) break; // blokada
                        if (schedule[i, j] == employeePreferences[i, j])
                            bonus += FitnessConstants.EmployeePreferenceMultiplier;
                    }
                }
                return bonus;
            }

            static int[,] Crossover(int[,] parent1, int[,] parent2)
            {
                int[,] child = new int[numEmployees, numTimeSlots];
                int crossoverType = rand.Next(2);

                if (crossoverType == 0)
                {
                    int splitPoint = rand.Next(1, numTimeSlots);
                    for (int i = 0; i < numEmployees; i++)
                        for (int j = 0; j < numTimeSlots; j++)
                            child[i, j] = (j < splitPoint) ? parent1[i, j] : parent2[i, j];
                }
                else
                {
                    for (int i = 0; i < numEmployees; i++)
                        for (int j = 0; j < numTimeSlots; j++)
                            child[i, j] = rand.NextDouble() < 0.5 ? parent1[i, j] : parent2[i, j];
                }
                return child;
            }

            static int[,] Mutate(int[,] schedule)
            {
                int[,] mutatedSchedule = (int[,])schedule.Clone();
                double mutationTypeRand = rand.NextDouble();

                if (mutationTypeRand < 0.5)
                {
                    int emp = rand.Next(numEmployees);
                    int slot = rand.Next(numTimeSlots);
                    mutatedSchedule[emp, slot] = 1 - mutatedSchedule[emp, slot];
                }
                else
                {
                    int emp = rand.Next(numEmployees);
                    int slot1 = rand.Next(numTimeSlots);
                    int slot2 = rand.Next(numTimeSlots);
                    if (slot1 != slot2)
                        (mutatedSchedule[emp, slot1], mutatedSchedule[emp, slot2]) = (mutatedSchedule[emp, slot2], mutatedSchedule[emp, slot1]);
                }
                return mutatedSchedule;
            }

            static void PrintScheduleToConsole(int[,] schedule)
            {
                Console.Write("      ");
                for (int d = 1; d <= numActualDays; d++)
                    for (int s = 1; s <= numShiftsPerDay; s++)
                        Console.Write($"D{d}S{s}\t");
                Console.WriteLine();

                for (int i = 0; i < numEmployees; i++)
                {
                    Console.Write($"P{i + 1}\t");
                    for (int j = 0; j < numTimeSlots; j++)
                    {
                        if (j >= schedule.GetLength(1)) break; // blokada
                        Console.Write(schedule[i, j] + "\t");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
