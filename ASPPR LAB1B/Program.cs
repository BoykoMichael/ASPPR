using System;
using System.Globalization;

namespace SimplexMJE_Modular
{
    // 1. Точка входу
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== Програма для розв'язання задач ЛП методом МЖВ ===");

            int n = InputHandler.ReadInt("Введіть кількість змінних (n): ");
            int m = InputHandler.ReadInt("Введіть кількість обмежень (m): ");
            bool isMax = InputHandler.ReadGoal("Введіть тип цільової функції (1 - max, 2 - min): ");

            double[,] rawConstraints = new double[m, n + 1];
            bool[] isGreaterOrEqual = new bool[m];

            Console.WriteLine("\n--- Ввід системи обмежень ---");
            Console.WriteLine("Вводьте коефіцієнти кожного обмеження через пробіл (включаючи вільний член b).");

            for (int i = 0; i < m; i++)
            {
                Console.WriteLine($"\nОбмеження {i + 1}:");
                double[] rowInput = InputHandler.ReadDoubleArray(n + 1, $"Введіть {n} коефіцієнтів та число b: ");
                for (int j = 0; j <= n; j++)
                {
                    rawConstraints[i, j] = rowInput[j];
                }
                isGreaterOrEqual[i] = InputHandler.ReadBoolean("Змінити знак цього обмеження з <= на >= ? (т/н): ");
            }

            Console.WriteLine("\n--- Ввід цільової функції (Z) ---");
            double[] rawZ = InputHandler.ReadDoubleArray(n, $"Введіть {n} коефіцієнтів цільової функції через пробіл: ");

            double[,] A = new double[m + 1, n + 1];
            string[] rowH = new string[m + 1];
            string[] colH = new string[n + 1];

            for (int i = 0; i < m; i++) rowH[i] = $"y{i + 1}";
            rowH[m] = "Z";

            for (int j = 0; j < n; j++) colH[j] = $"-x{j + 1}";
            colH[n] = "1";

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j <= n; j++)
                    A[i, j] = isGreaterOrEqual[i] ? -rawConstraints[i, j] : rawConstraints[i, j];
            }

            for (int j = 0; j < n; j++)
                A[m, j] = isMax ? -rawZ[j] : rawZ[j];

            A[m, n] = 0;

            // Виведення постановки задачі
            Console.WriteLine("\n");
            Console.WriteLine("Згенерований протокол обчислення:\n");
            ReportPrinter.PrintProblemStatement(rawConstraints, isGreaterOrEqual, rawZ, isMax, m, n);
            ReportPrinter.PrintSystemOfEquations(A, m, n);

            Console.WriteLine("Вхідна симплекс-таблиця:\n");
            ReportPrinter.PrintTable(A, rowH, colH, m, n);

            // Розв'язання
            Console.WriteLine("Пошук опорного розв'язку:\n");
            bool isFeasible = SimplexSolver.Phase1(A, rowH, colH, m, n);

            if (isFeasible)
            {
                Console.WriteLine("Знайдено опорний розв'язок:\n");
                ReportPrinter.PrintX(A, rowH, m, n);

                Console.WriteLine("Пошук оптимального розв'язку:\n");
                bool isOptimal = SimplexSolver.Phase2(A, rowH, colH, m, n);

                if (isOptimal)
                {
                    Console.WriteLine("Знайдено оптимальний розв'язок:\n");
                    ReportPrinter.PrintX(A, rowH, m, n);
                    ReportPrinter.PrintFinalZ(A, isMax, m, n);
                }
            }

            Console.ReadLine();
        }
    }

    // 2. Обробка входу
    static class InputHandler
    {
        public static int ReadInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int result) && result > 0)
                    return result;
                Console.WriteLine("Помилка: введіть коректне ціле додатне число.");
            }
        }

        public static bool ReadGoal(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int result) && (result == 1 || result == 2))
                    return result == 1;
                Console.WriteLine("Помилка: введіть 1 або 2.");
            }
        }

        public static bool ReadBoolean(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim().ToLower();
            return input == "т" || input == "y" || input == "так" || input == "yes";
        }

        public static double[] ReadDoubleArray(int expectedLength, string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Replace(',', '.').Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != expectedLength)
                {
                    Console.WriteLine($"Помилка: очікується {expectedLength} чисел, ви ввели {parts.Length}.");
                    continue;
                }

                double[] result = new double[expectedLength];
                bool success = true;
                for (int i = 0; i < expectedLength; i++)
                {
                    if (!double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out result[i]))
                    {
                        Console.WriteLine($"Помилка: '{parts[i]}' не є коректним числом.");
                        success = false;
                        break;
                    }
                }
                if (success) return result;
            }
        }
    }

    // 3. Математичні алгоритми
    static class SimplexSolver
    {
        // Пошук опорного розв'язку
        public static bool Phase1(double[,] A, string[] rowH, string[] colH, int m, int n)
        {
            while (true)
            {
                int target_r = -1;
                for (int i = 0; i < m; i++)
                {
                    if (A[i, n] < -1e-9) { target_r = i; break; }
                }

                if (target_r == -1) return true;

                int s = -1;
                for (int j = 0; j < n; j++)
                {
                    if (A[target_r, j] < -1e-9) { s = j; break; }
                }

                if (s == -1)
                {
                    Console.WriteLine("Система обмежень є суперечливою\n");
                    return false;
                }

                int r = FindPivotRow(A, s, m, n, true);

                Console.WriteLine($"Розв'язувальний рядок:    {rowH[r]}");
                Console.WriteLine($"Розв'язувальний стовпець: {colH[s]}\n");

                PerformMJE(A, rowH, colH, r, s, m, n);
                ReportPrinter.PrintTable(A, rowH, colH, m, n);
            }
        }
        // Пошук оптимального розв'язку
        public static bool Phase2(double[,] A, string[] rowH, string[] colH, int m, int n)
        {
            while (true)
            {
                int s = -1;
                for (int j = 0; j < n; j++)
                {
                    if (A[m, j] < -1e-9) { s = j; break; }
                }

                if (s == -1) return true;

                int r = FindPivotRow(A, s, m, n, false);

                if (r == -1)
                {
                    Console.WriteLine("Функція мети не обмежена зверху\n");
                    return false;
                }

                Console.WriteLine($"Розв'язувальний рядок:    {rowH[r]}");
                Console.WriteLine($"Розв'язувальний стовпець: {colH[s]}\n");

                PerformMJE(A, rowH, colH, r, s, m, n);
                ReportPrinter.PrintTable(A, rowH, colH, m, n);
            }
        }
        // Пошук розв'язувального рядка
        private static int FindPivotRow(double[,] A, int s, int m, int n, bool isPhase1)
        {
            int r = -1;
            double min_ratio = double.MaxValue;
            for (int i = 0; i < m; i++)
            {
                if ((isPhase1 && Math.Abs(A[i, s]) > 1e-9) || (!isPhase1 && A[i, s] > 1e-9))
                {
                    double ratio = A[i, n] / A[i, s];
                    if (ratio >= -1e-9)
                    {
                        if (Math.Abs(ratio - min_ratio) < 1e-9)
                        {
                            if (A[i, n] < -1e-9) r = i;
                        }
                        else if (ratio < min_ratio)
                        {
                            min_ratio = ratio;
                            r = i;
                        }
                    }
                }
            }
            return r;
        }
        // Крок МЖВ
        private static void PerformMJE(double[,] A, string[] rowH, string[] colH, int r, int s, int m, int n)
        {
            double[,] nextA = new double[m + 1, n + 1];
            double ars = A[r, s];

            for (int i = 0; i <= m; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    if (i == r && j == s) nextA[i, j] = 1.0 / ars;
                    else if (i == r) nextA[i, j] = A[i, j] / ars;
                    else if (j == s) nextA[i, j] = -A[i, j] / ars;
                    else nextA[i, j] = A[i, j] - (A[i, s] * A[r, j]) / ars;
                }
            }

            for (int i = 0; i <= m; i++)
                for (int j = 0; j <= n; j++)
                    A[i, j] = nextA[i, j];

            string temp = rowH[r];
            rowH[r] = colH[s].TrimStart('-');
            colH[s] = "-" + temp;
        }
    }

    // 4. Протокол обчислення
    static class ReportPrinter
    {
        public static void PrintProblemStatement(double[,] rawConstraints, bool[] isGreaterOrEqual, double[] rawZ, bool isMax, int m, int n)
        {
            Console.WriteLine("Постановка задачі:\n");

            string zFunc = "Z = ";
            bool first = true;
            for (int j = 0; j < n; j++)
            {
                double coef = rawZ[j];
                if (Math.Abs(coef) > 1e-9)
                {
                    if (!first && coef > 0) zFunc += "+";
                    if (coef == 1 && first) zFunc += $"x{j + 1}";
                    else if (coef == 1) zFunc += $"x{j + 1}";
                    else if (coef == -1) zFunc += $"-x{j + 1}";
                    else zFunc += $"{coef}x{j + 1}";
                    first = false;
                }
            }
            if (first) zFunc += "0";
            zFunc += isMax ? " -> max\n" : " -> min\n";
            Console.WriteLine(zFunc);

            Console.WriteLine("при обмеженнях:\n");
            for (int i = 0; i < m; i++)
            {
                string constraint = "";
                first = true;
                for (int j = 0; j < n; j++)
                {
                    double coef = rawConstraints[i, j];
                    if (Math.Abs(coef) > 1e-9)
                    {
                        if (!first && coef > 0) constraint += "+";
                        if (coef == 1 && first) constraint += $"x{j + 1}";
                        else if (coef == 1) constraint += $"x{j + 1}";
                        else if (coef == -1) constraint += $"-x{j + 1}";
                        else constraint += $"{coef}x{j + 1}";
                        first = false;
                    }
                }
                double b = rawConstraints[i, n];
                string sign = isGreaterOrEqual[i] ? ">=" : "<=";
                constraint += $"{sign}{b}";
                Console.WriteLine(constraint);
            }
            Console.WriteLine($"x[j]>=0, j=1,{n}\n");
        }

        public static void PrintSystemOfEquations(double[,] A, int m, int n)
        {
            Console.WriteLine("Перепишемо систему обмежень:\n");
            for (int i = 0; i < m; i++)
            {
                string eq = "";
                for (int j = 0; j < n; j++)
                {
                    double val = -A[i, j];
                    string formattedVal = val.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                    eq += $"({formattedVal}) * X[{j + 1}] + ";
                }
                string bStr = A[i, n].ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                eq += $"{bStr} >= 0";
                Console.WriteLine(eq);
            }
            Console.WriteLine();
        }

        public static void PrintTable(double[,] A, string[] rowH, string[] colH, int m, int n)
        {
            Console.Write("     ");
            for (int j = 0; j <= n; j++) Console.Write($"{colH[j],9}");
            Console.WriteLine();
            Console.WriteLine(new string('-', 5 + 9 * (n + 1)));

            for (int i = 0; i <= m; i++)
            {
                string prefix = (i == m) ? "Z  =" : $"{rowH[i],2} =";
                Console.Write(prefix);
                for (int j = 0; j <= n; j++)
                {
                    double v = A[i, j];
                    if (Math.Abs(v) < 1e-9) v = 0.0;

                    string val = v.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                    Console.Write($"{val,9}");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static void PrintX(double[,] A, string[] rowH, int m, int n)
        {
            double[] X = new double[n];
            for (int i = 0; i < m; i++)
            {
                if (rowH[i].StartsWith("x"))
                {
                    int index = int.Parse(rowH[i].Substring(1)) - 1;
                    X[index] = A[i, n];
                }
            }

            string[] xStrs = new string[n];
            for (int j = 0; j < n; j++)
            {
                double v = X[j];
                if (Math.Abs(v) < 1e-9) v = 0.0;
                xStrs[j] = v.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
            }
            Console.WriteLine($"X = ({string.Join("; ", xStrs)})\n");
        }

        public static void PrintFinalZ(double[,] A, bool isMax, int m, int n)
        {
            double optimalZValue = isMax ? A[m, n] : -A[m, n];
            if (Math.Abs(optimalZValue) < 1e-9) optimalZValue = 0.0;
            string optimalZStr = optimalZValue.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
            Console.WriteLine($"{(isMax ? "Max" : "Min")} (Z) = {optimalZStr}");
        }
    }
}