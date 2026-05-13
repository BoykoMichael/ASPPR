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

            int original_n = InputHandler.ReadInt("Введіть кількість змінних (n): ");
            int m = InputHandler.ReadInt("Введіть кількість обмежень (m): ");
            bool isMax = InputHandler.ReadGoal("Введіть тип цільової функції (1 - max, 2 - min): ");

            double[,] rawConstraints = new double[m, original_n + 1];
            int[] constraintTypes = new int[m];

            Console.WriteLine("\n--- Ввід системи обмежень ---");
            Console.WriteLine("Вводьте коефіцієнти кожного обмеження через пробіл (включаючи вільний член b).");

            for (int i = 0; i < m; i++)
            {
                Console.WriteLine($"\nОбмеження {i + 1}:");
                double[] rowInput = InputHandler.ReadDoubleArray(original_n + 1, $"Введіть {original_n} коефіцієнтів та число b: ");
                for (int j = 0; j <= original_n; j++)
                {
                    rawConstraints[i, j] = rowInput[j];
                }
                constraintTypes[i] = InputHandler.ReadConstraintType("Введіть тип обмеження (1: <=, 2: >=, 3: =): ");
            }

            Console.WriteLine("\n--- Ввід цільової функції (Z) ---");
            double[] rawZ = InputHandler.ReadDoubleArray(original_n, $"Введіть {original_n} коефіцієнтів цільової функції через пробіл: ");

            int current_n = original_n;
            double[,] A = new double[m + 1, current_n + 1];
            string[] rowH = new string[m + 1];
            string[] colH = new string[current_n + 1];
            bool[] isExtractedRow = new bool[m + 1]; // Відстежує "приховані" рядки вилучених вільних змінних

            int yCounter = 1;
            for (int i = 0; i < m; i++)
            {
                if (constraintTypes[i] == 3) rowH[i] = "0";
                else rowH[i] = $"y{yCounter++}";
            }
            rowH[m] = "Z";

            for (int j = 0; j < current_n; j++) colH[j] = $"-x{j + 1}";
            colH[current_n] = "1";

            // Заповнення початкової таблиці
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j <= current_n; j++)
                {
                    if (constraintTypes[i] == 2) A[i, j] = -rawConstraints[i, j];
                    else A[i, j] = rawConstraints[i, j];
                }

                if (constraintTypes[i] == 3 && A[i, current_n] < -1e-9)
                {
                    for (int j = 0; j <= current_n; j++) A[i, j] = -A[i, j];
                }
            }

            for (int j = 0; j < current_n; j++) A[m, j] = isMax ? -rawZ[j] : rawZ[j];
            A[m, current_n] = 0;

            // Виведення постановки задачі
            Console.WriteLine("\n");
            Console.WriteLine("Згенерований протокол обчислення:\n");
            ReportPrinter.PrintProblemStatement(rawConstraints, constraintTypes, rawZ, isMax, m, original_n);
            ReportPrinter.PrintSystemOfEquations(A, rowH, m, current_n);

            Console.WriteLine("Вхідна симплекс-таблиця:\n");
            ReportPrinter.PrintTableFiltered(A, rowH, colH, m, current_n, isExtractedRow);

            // Фаза 0: Видалення нуль-рядків (якщо є)
            bool phase0Success = SimplexSolver.Phase0(ref A, ref rowH, ref colH, m, ref current_n, isExtractedRow);

            if (phase0Success)
            {
                // Етап: Видалення вільних змінних
                SimplexSolver.EliminateFreeVariables(A, rowH, colH, m, current_n, isExtractedRow);

                // Розв'язання
                Console.WriteLine("Пошук опорного розв'язку:\n");
                bool isFeasible = SimplexSolver.Phase1(A, rowH, colH, m, current_n, isExtractedRow);

                if (isFeasible)
                {
                    Console.WriteLine("Знайдено опорний розв'язок:\n");
                    ReportPrinter.PrintX(A, rowH, colH, m, current_n, original_n);
                    ReportPrinter.PrintY(A, rowH, m, current_n);

                    Console.WriteLine("Пошук оптимального розв'язку:\n");
                    bool isOptimal = SimplexSolver.Phase2(A, rowH, colH, m, current_n, isExtractedRow);

                    if (isOptimal)
                    {
                        Console.WriteLine("Знайдено оптимальний розв'язок:\n");
                        ReportPrinter.PrintX(A, rowH, colH, m, current_n, original_n);
                        ReportPrinter.PrintY(A, rowH, m, current_n);
                        ReportPrinter.PrintFinalZ(A, isMax, m, current_n);
                    }
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
                if (int.TryParse(Console.ReadLine(), out int result) && result > 0) return result;
                Console.WriteLine("Помилка: введіть коректне ціле додатне число.");
            }
        }

        public static bool ReadGoal(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int result) && (result == 1 || result == 2)) return result == 1;
                Console.WriteLine("Помилка: введіть 1 або 2.");
            }
        }

        public static int ReadConstraintType(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= 1 && result <= 3) return result;
                Console.WriteLine("Помилка: введіть 1 (<=), 2 (>=) або 3 (=).");
            }
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
        // Фаза 0: Видалення нуль-рядків
        public static bool Phase0(ref double[,] A, ref string[] rowH, ref string[] colH, int m, ref int n, bool[] isExtractedRow)
        {
            bool hasZeroRows = false;
            foreach (var rh in rowH) if (rh == "0") hasZeroRows = true;

            if (hasZeroRows) Console.WriteLine("Видалення нуль-рядків:\n");

            while (true)
            {
                int zeroRow = -1;
                for (int i = 0; i < m; i++)
                {
                    if (!isExtractedRow[i] && rowH[i] == "0")
                    {
                        zeroRow = i; break;
                    }
                }

                if (zeroRow == -1)
                {
                    if (hasZeroRows) Console.WriteLine("Всі нуль-рядки видалено.\n");
                    return true;
                }

                if (A[zeroRow, n] < -1e-9)
                {
                    for (int j = 0; j <= n; j++) A[zeroRow, j] = -A[zeroRow, j];
                }

                int s = -1;
                for (int j = 0; j < n; j++)
                {
                    if (A[zeroRow, j] > 1e-9) { s = j; break; }
                }

                if (s == -1)
                {
                    Console.WriteLine("Система обмежень є суперечливою\n");
                    return false;
                }

                int r = -1;
                double min_ratio = double.MaxValue;
                for (int i = 0; i < m; i++)
                {
                    if (isExtractedRow[i]) continue;

                    double a_is = A[i, s];
                    double b_i = A[i, n];

                    if ((a_is > 1e-9 && b_i > -1e-9) || (a_is < -1e-9 && b_i < -1e-9))
                    {
                        double ratio = b_i / a_is;
                        if (ratio >= 0 && ratio < min_ratio)
                        {
                            min_ratio = ratio;
                            r = i;
                        }
                    }
                }

                if (r == -1) r = zeroRow;

                Console.WriteLine($"Розв'язувальний рядок:    {rowH[r]}");
                Console.WriteLine($"Розв'язувальний стовпець: {colH[s]}\n");

                PerformMJE(A, rowH, colH, r, s, m, n);

                if (colH[s] == "-0" || colH[s] == "0")
                {
                    RemoveColumn(ref A, ref colH, s, m, ref n);
                }

                ReportPrinter.PrintTableFiltered(A, rowH, colH, m, n, isExtractedRow);
            }
        }

        // Етап: Видалення вільних змінних
        public static void EliminateFreeVariables(double[,] A, string[] rowH, string[] colH, int m, int n, bool[] isExtractedRow)
        {
            Console.WriteLine("Видалення вільних змінних:\n");
            int numExtracted = 0;

            for (int j = 0; j < n; j++)
            {
                if (colH[j].StartsWith("-x"))
                {
                    int r = -1;
                    // Шукаємо підходящий y-рядок для заміни
                    for (int i = 0; i < m; i++)
                    {
                        if (!isExtractedRow[i] && rowH[i].StartsWith("y") && Math.Abs(A[i, j]) > 1e-9)
                        {
                            r = i; break;
                        }
                    }

                    if (r != -1)
                    {
                        Console.WriteLine($"Розв'язувальний рядок:    {rowH[r]}");
                        Console.WriteLine($"Розв'язувальний стовпець: {colH[j]}\n");

                        PerformMJE(A, rowH, colH, r, j, m, n);
                        isExtractedRow[r] = true; // Помічаємо рядок як вилучений (прихований)

                        ReportPrinter.PrintTableFiltered(A, rowH, colH, m, n, isExtractedRow);

                        // Друк виразу
                        string varName = rowH[r];
                        string expr = $"Вираз для {varName} = ";
                        for (int c = 0; c < n; c++)
                        {
                            double coeff = -A[r, c];
                            string colVarName = colH[c].TrimStart('-');

                            string formattedVal = coeff.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                            string displayStr = coeff < 0 ? $"({formattedVal})" : formattedVal;

                            expr += $"{displayStr} * {colVarName} + ";
                        }
                        double bVal = A[r, n];
                        string bFormatted = bVal.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                        string bDisplay = bVal < 0 ? $"({bFormatted})" : bFormatted;
                        expr += bDisplay;

                        Console.WriteLine(expr + "\n");
                        numExtracted++;
                    }
                }
            }

            if (numExtracted > 0)
            {
                Console.WriteLine("Всі вільні змінні видалено.\n");
            }
        }

        // Пошук опорного розв'язку
        public static bool Phase1(double[,] A, string[] rowH, string[] colH, int m, int n, bool[] isExtractedRow)
        {
            while (true)
            {
                int target_r = -1;
                for (int i = 0; i < m; i++)
                {
                    if (!isExtractedRow[i] && A[i, n] < -1e-9) { target_r = i; break; }
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

                int r = FindPivotRow(A, s, m, n, true, isExtractedRow);

                Console.WriteLine($"Розв'язувальний рядок:    {rowH[r]}");
                Console.WriteLine($"Розв'язувальний стовпець: {colH[s]}\n");

                PerformMJE(A, rowH, colH, r, s, m, n);
                ReportPrinter.PrintTableFiltered(A, rowH, colH, m, n, isExtractedRow);
            }
        }

        // Пошук оптимального розв'язку
        public static bool Phase2(double[,] A, string[] rowH, string[] colH, int m, int n, bool[] isExtractedRow)
        {
            while (true)
            {
                int s = -1;
                for (int j = 0; j < n; j++)
                {
                    if (A[m, j] < -1e-9) { s = j; break; }
                }

                if (s == -1) return true;

                int r = FindPivotRow(A, s, m, n, false, isExtractedRow);

                if (r == -1)
                {
                    Console.WriteLine("Функція мети не обмежена зверху\n");
                    return false;
                }

                Console.WriteLine($"Розв'язувальний рядок:    {rowH[r]}");
                Console.WriteLine($"Розв'язувальний стовпець: {colH[s]}\n");

                PerformMJE(A, rowH, colH, r, s, m, n);
                ReportPrinter.PrintTableFiltered(A, rowH, colH, m, n, isExtractedRow);
            }
        }

        private static int FindPivotRow(double[,] A, int s, int m, int n, bool isPhase1, bool[] isExtractedRow)
        {
            int r = -1;
            double min_ratio = double.MaxValue;
            for (int i = 0; i < m; i++)
            {
                if (isExtractedRow[i]) continue; // Пропускаємо вилучені рядки

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

        private static void RemoveColumn(ref double[,] A, ref string[] colH, int colToRemove, int m, ref int n)
        {
            double[,] nextA = new double[m + 1, n];
            string[] nextColH = new string[n];

            int c = 0;
            for (int j = 0; j <= n; j++)
            {
                if (j == colToRemove) continue;
                for (int i = 0; i <= m; i++) nextA[i, c] = A[i, j];
                nextColH[c] = colH[j];
                c++;
            }
            A = nextA;
            colH = nextColH;
            n--;
        }
    }

    // 4. Протокол обчислення
    static class ReportPrinter
    {
        public static void PrintProblemStatement(double[,] rawConstraints, int[] constraintTypes, double[] rawZ, bool isMax, int m, int n)
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

            Console.WriteLine("при обмеженнях:");
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
                string sign = constraintTypes[i] == 1 ? "<=" : (constraintTypes[i] == 2 ? ">=" : "=");
                constraint += $"{sign}{b}";
                Console.WriteLine(constraint);
            }
            Console.WriteLine($"x[j]>=0, j=1,{n}\n");
        }

        public static void PrintSystemOfEquations(double[,] A, string[] rowH, int m, int n)
        {
            Console.WriteLine("Перепишемо систему обмежень:\n");
            for (int i = 0; i < m; i++)
            {
                string eq = "";
                for (int j = 0; j < n; j++)
                {
                    double val = -A[i, j];
                    string formattedVal = val.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                    eq += (j == 0 ? "" : " + ") + $"({formattedVal}) * X[{j + 1}]";
                }
                string bStr = A[i, n].ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
                string op = rowH[i] == "0" ? "=" : ">=";
                eq += $" + {bStr} {op} 0";
                Console.WriteLine(eq);
            }
            Console.WriteLine();
        }

        public static void PrintTableFiltered(double[,] A, string[] rowH, string[] colH, int m, int n, bool[] isExtractedRow)
        {
            Console.Write("     ");
            for (int j = 0; j <= n; j++) Console.Write($"{colH[j],9}");
            Console.WriteLine();
            Console.WriteLine(new string('-', 5 + 9 * (n + 1)));

            for (int i = 0; i <= m; i++)
            {
                if (i < m && isExtractedRow[i]) continue; // Не виводимо приховані рядки

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

        public static void PrintX(double[,] A, string[] rowH, string[] colH, int m, int current_n, int original_n)
        {
            double[] X = new double[original_n];
            for (int j = 0; j < original_n; j++)
            {
                string xName = $"x{j + 1}";
                double val = 0.0;
                // Шукаємо серед рядків
                for (int i = 0; i < m; i++)
                {
                    if (rowH[i] == xName) val = A[i, current_n];
                }
                X[j] = val;
            }

            string[] xStrs = new string[original_n];
            for (int j = 0; j < original_n; j++)
            {
                double v = X[j];
                if (Math.Abs(v) < 1e-9) v = 0.0;
                xStrs[j] = v.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
            }
            Console.WriteLine($"X = ({string.Join("; ", xStrs)})");
        }

        public static void PrintY(double[,] A, string[] rowH, int m, int current_n)
        {
            int numInitialY = m;
            double[] Y = new double[numInitialY];

            for (int j = 0; j < numInitialY; j++)
            {
                string yName = $"y{j + 1}";
                double val = 0.0;
                for (int i = 0; i < m; i++)
                {
                    if (rowH[i] == yName) val = A[i, current_n];
                }
                Y[j] = val;
            }

            string[] yStrs = new string[numInitialY];
            for (int j = 0; j < numInitialY; j++)
            {
                double v = Y[j];
                if (Math.Abs(v) < 1e-9) v = 0.0;
                yStrs[j] = v.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
            }
            Console.WriteLine($"Y = ({string.Join("; ", yStrs)})\n");
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