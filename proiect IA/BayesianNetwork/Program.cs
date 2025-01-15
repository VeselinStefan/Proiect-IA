using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json; // Asigură-te că ai adăugat Newtonsoft.Json pentru parsarea fișierului JSON

class BayesianNetwork
{
    private Dictionary<string, Dictionary<string, double>> probabilityTables;
    private Dictionary<string, Dictionary<string, double>> conditionalProbabilities;

    public BayesianNetwork(string filePath)
    {
        LoadNetwork(filePath);
    }

    private void LoadNetwork(string filePath)
    {
        var jsonData = File.ReadAllText(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);

        probabilityTables = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(data["ProbabilityTables"].ToString());
        conditionalProbabilities = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(data["ConditionalProbabilities"].ToString());
    }

    public double JointProbability(Dictionary<string, string> state)
    {
        double probability = 1.0;

        foreach (var variable in probabilityTables.Keys)
        {
            if (state.ContainsKey(variable))
            {
                probability *= probabilityTables[variable][state[variable]];
            }
        }

        foreach (var key in conditionalProbabilities.Keys)
        {
            var parts = key.Split('|');
            var targetVar = parts[0];
            var conditions = parts[1].Split(',');

            bool matches = conditions.All(cond =>
            {
                var condParts = cond.Split('=');
                return state.ContainsKey(condParts[0]) && state[condParts[0]] == condParts[1];
            });

            if (matches && state.ContainsKey(targetVar))
            {
                probability *= conditionalProbabilities[key][state[targetVar]];
            }
        }

        return probability;
    }

    public double MarginalProbability(string queryVariable, string queryValue, Dictionary<string, string> evidences)
    {
        var variables = probabilityTables.Keys.ToList();
        var unknownVariables = variables.Except(evidences.Keys.Append(queryVariable)).ToList();
        var combinations = GenerateCombinations(unknownVariables);

        double totalProbability = 0.0;
        foreach (var combination in combinations)
        {
            var fullState = new Dictionary<string, string>(evidences) { [queryVariable] = queryValue };
            foreach (var kvp in combination)
            {
                fullState[kvp.Key] = kvp.Value;
            }

            totalProbability += JointProbability(fullState);
        }

        return totalProbability;
    }

    private List<Dictionary<string, string>> GenerateCombinations(List<string> variables)
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

    public void Run()
    {
        Console.WriteLine("Interfață Bayesian Network");
        Console.WriteLine("1. Setați evidențele");
        Console.WriteLine("2. Interogați probabilitatea");
        Console.WriteLine("3. Ieșire");

        var evidences = new Dictionary<string, string>();

        while (true)
        {
            Console.Write("Alegeți o opțiune: ");
            var option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    Console.Write("Introduceți variabila și valoarea (ex: Gripa Da): ");
                    var input = Console.ReadLine().Split(' ');
                    if (input.Length == 2)
                    {
                        evidences[input[0]] = input[1];
                    }
                    break;
                case "2":
                    Console.Write("Introduceți variabila interogată și valoarea (ex: Oboseala Da): ");
                    input = Console.ReadLine().Split(' ');
                    if (input.Length == 2)
                    {
                        double probability = MarginalProbability(input[0], input[1], evidences);
                        Console.WriteLine($"P({input[0]} = {input[1]} | evidențe) = {probability}");
                    }
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Opțiune invalidă. Încercați din nou.");
                    break;
            }
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Introduceți calea fișierului JSON: ");
        string filePath = Console.ReadLine();

        BayesianNetwork network = new BayesianNetwork(filePath);
        network.Run();
    }
}
