﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class BayesianNetwork
{
    private static Dictionary<string, Dictionary<string, double>> MarginalProbabilities = new Dictionary<string, Dictionary<string, double>>();
    private static Dictionary<string, Dictionary<string, Dictionary<string, double>>> ConditionalProbabilities = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();

    // Citirea rețelei bayesiene din fișier
    public static void LoadNetwork(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        string currentNode = null;

        foreach (var line in lines)
        {
            if (line.Contains(":") && !line.Contains("="))
            {
                currentNode = line.Split(':')[0].Trim();
                if (!ConditionalProbabilities.ContainsKey(currentNode))
                    ConditionalProbabilities[currentNode] = new Dictionary<string, Dictionary<string, double>>();
            }
            else if (line.Contains(":"))
            {
                var parts = line.Split(':');
                var condition = parts[0].Trim();
                var probabilities = parts[1].Trim().Split(',').ToDictionary(
                    p => p.Split('=')[0].Trim(),
                    p => double.Parse(p.Split('=')[1].Trim())
                );

                if (currentNode != null && ConditionalProbabilities.ContainsKey(currentNode))
                {
                    ConditionalProbabilities[currentNode][condition] = probabilities;
                }
                else
                {
                    currentNode = line.Split(':')[0].Trim();
                    MarginalProbabilities[currentNode] = probabilities;
                }
            }
        }
    }


    public static double JointProbability(Dictionary<string, string> evidence)
    {
        double probability = 1.0;

        foreach (var node in MarginalProbabilities.Keys.Concat(ConditionalProbabilities.Keys))
        {
            if (MarginalProbabilities.ContainsKey(node))
            {
                probability *= MarginalProbabilities[node][evidence[node]] * 0.01;
            }
            else if (ConditionalProbabilities.ContainsKey(node))
            {
                var parentValues = string.Join(",", ConditionalProbabilities[node].Keys.First().Split(',').Select(kv =>
                {
                    var parts = kv.Split('=');
                    return $"{parts[0]}={evidence[parts[0]]}";
                }));

                probability *= ConditionalProbabilities[node][parentValues][evidence[node]] * 0.01;
            }
        }

        return probability;
    }


    public static double MarginalProbability(string queryNode, string queryValue, Dictionary<string, string> evidence)
    {
        var allVariables = MarginalProbabilities.Keys.Concat(ConditionalProbabilities.Keys).ToList();
        var unknownVariables = allVariables.Except(evidence.Keys.Append(queryNode)).ToList();
        var combinations = GenerateCombinations(unknownVariables);

        double totalProbability = 0.0;
        foreach (var combination in combinations)
        {
            var fullEvidence = new Dictionary<string, string>(evidence) { [queryNode] = queryValue };
            foreach (var kvp in combination)
                fullEvidence[kvp.Key] = kvp.Value;

            totalProbability += JointProbability(fullEvidence);
        }

        return totalProbability;
    }

    private static List<Dictionary<string, string>> GenerateCombinations(List<string> variables)
    {
        var values = new[] { "Da", "Nu" };
        var combinations = new List<Dictionary<string, string>>();

        int totalCombinations = (int)Math.Pow(2, variables.Count);
        for (int i = 0; i < totalCombinations; i++)
        {
            var combination = new Dictionary<string, string>();
            for (int j = 0; j < variables.Count; j++)
            {
                combination[variables[j]] = values[(i >> j) & 1];
            }
            combinations.Add(combination);
        }

        return combinations;
    }

    public static void DisplayNetwork()
    {
        Console.WriteLine("=== Bayesian Network ===");


        Console.WriteLine("Noduri fara parinti (Probabilitati marginale):");
        foreach (var node in MarginalProbabilities)
        {
            Console.WriteLine($"  {node.Key}:");
            foreach (var prob in node.Value)
            {
                Console.WriteLine($"    {prob.Key} = {prob.Value}");
            }
        }


        Console.WriteLine("\nNoduri cu parinti (Probabilitati conditionate):");
        foreach (var node in ConditionalProbabilities)
        {
            Console.WriteLine($"  {node.Key}:");
            foreach (var condition in node.Value)
            {
                Console.WriteLine($"    Conditie: {condition.Key}");
                foreach (var prob in condition.Value)
                {
                    Console.WriteLine($"      {prob.Key} = {prob.Value}");
                }
            }
        }
    }

    public static Dictionary<string, string> ReadEvidences()
    {
        var evidences = new Dictionary<string, string>();
        Console.WriteLine("Introduceti evidentele (ex: Gripa=Nu, Abces=Da):");
        string input = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(input))
        {
            var pairs = input.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    var variable = parts[0].Trim();
                    var value = parts[1].Trim();
                    evidences[variable] = value;
                }
                else
                {
                    Console.WriteLine($"Format invalid: {pair}. Ignorat.");
                }
            }
        }

        return evidences;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("=== Bayesian Network Program ===");

        string filePath = null;

        while (true)
        {
            Console.WriteLine("Introduceți calea către fișierul rețelei bayesiene (ex: network.txt):");
            filePath = Console.ReadLine();

            if (File.Exists(filePath))
            {
                try
                {
                    LoadNetwork(filePath);
                    Console.WriteLine("\nRețeaua bayesiană a fost încărcată cu succes din fișierul: " + filePath);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Eroare la încărcarea fișierului: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Fișierul specificat nu există. Vă rugăm să introduceți o cale validă.");
            }
        }

        var evidences = new Dictionary<string, string>();

        while (true)
        {
            Console.WriteLine("\n=== Meniu ===");
            Console.WriteLine("1. Afișare rețea bayesiană");
            Console.WriteLine("2. Modificare evidențe");
            Console.WriteLine("3. Interogare probabilitate marginală");
            Console.WriteLine("4. Schimbare fișier rețea");
            Console.WriteLine("5. Ieșire");
            Console.Write("Alege o opțiune: ");
            var option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    DisplayNetwork();
                    break;

                case "2":
                    evidences = ReadEvidences();
                    Console.WriteLine("Evidențele au fost actualizate.");
                    break;

                case "3":
                    Console.WriteLine("Introduceți variabila și valoarea de interogat (ex: Oboseala=Da):");
                    var query = Console.ReadLine();
                    var queryParts = query.Split('=');
                    if (queryParts.Length == 2)
                    {
                        var queryNode = queryParts[0].Trim();
                        var queryValue = queryParts[1].Trim();

                        double result = MarginalProbability(queryNode, queryValue, evidences);
                        Console.WriteLine($"\nP({queryNode} = {queryValue} | evidences) = {result}");
                    }
                    else
                    {
                        Console.WriteLine("Format invalid pentru interogare.");
                    }
                    break;

                case "4":
                    Console.WriteLine("Introduceți noua cale către fișierul rețelei bayesiene:");
                    var newFilePath = Console.ReadLine();

                    if (File.Exists(newFilePath))
                    {
                        try
                        {
                            LoadNetwork(newFilePath);
                            filePath = newFilePath;
                            Console.WriteLine("\nRețeaua bayesiană a fost reîncărcată din noul fișier.");
                            evidences.Clear(); // Reset evidences if a new network is loaded
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Eroare la încărcarea fișierului: " + ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Fișierul specificat nu există. Vă rugăm să introduceți o cale validă.");
                    }
                    break;

                case "5":
                    Console.WriteLine("Ieșire...");
                    return;

                default:
                    Console.WriteLine("Opțiune invalidă. Vă rugăm să încercați din nou.");
                    break;
            }
        }
    }



}