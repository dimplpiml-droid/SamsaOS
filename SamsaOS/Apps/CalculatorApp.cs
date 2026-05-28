using System;
using System.Collections.Generic;

namespace SamsaOS.Apps
{
    public static class CalculatorApp
    {
        public static void Run()
        {
            Console.Clear();
            Console.WriteLine("--- SamsaOS Calculate ---");
            Console.WriteLine("Enter expression (e.g. 5 + 3 * 2):");
            Console.WriteLine("Type 'exit' to quit.");

            while (true)
            {
                Console.Write("\ncalc> ");
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;

                try
                {
                    double result = Evaluate(input);
                    Console.WriteLine($"Result: {result}");
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Invalid expression.");
                }
            }
        }

        private static double Evaluate(string expression)
        {
            //сначала считаем * и /, потом + и -
            List<string> tokens = new List<string>(expression.Split(' '));

            // 1. Обработка * и /
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] == "*" || tokens[i] == "/")
                {
                    double left = double.Parse(tokens[i - 1]);
                    double right = double.Parse(tokens[i + 1]);
                    double res = (tokens[i] == "*") ? left * right : left / right;

                    tokens[i - 1] = res.ToString();
                    tokens.RemoveAt(i);
                    tokens.RemoveAt(i);
                    i--;
                }
            }

            // 2. Обработка + и -
            double result = double.Parse(tokens[0]);
            for (int i = 1; i < tokens.Count; i += 2)
            {
                string op = tokens[i];
                double next = double.Parse(tokens[i + 1]);
                if (op == "+") result += next;
                else if (op == "-") result -= next;
            }

            return result;
        }
    }
}