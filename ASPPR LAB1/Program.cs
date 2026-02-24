using System;
using System.Text;

namespace MatrixPracticalWork
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            //// ---Вхідні дані(Варіант 5)-- -

            //double[,] matrixA = {

            //    { 2, 3, 2 },

            //    { 1, -2, 1 },

            //    { -1, 3, 5 }
            //};

            //double[] vectorB = { 1, 4, 1 };

            //int n = matrixA.GetLength(0);

            // 1. Введення матриці А

            Console.Write("Введіть розмірність матриці А (m x n): ");
            string[] sizeA = Console.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int m = int.Parse(sizeA[0]);
            int n = int.Parse(sizeA[1]);

            double[,] matrixA = new double[m, n];
            for (int i = 0; i < m; i++)
            {
                Console.Write($"{i + 1}р: ");
                string[] rowValues = Console.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < n; j++)
                {
                    matrixA[i, j] = double.Parse(rowValues[j]);
                }
            }

            Console.WriteLine();

            // 2. Введення матриці B

            Console.WriteLine($"Введіть розмірність матриці B ({m} x 1)");
            double[] vectorB = new double[m];
            for (int i = 0; i < m; i++)
            {
                Console.Write($"{i + 1}р: ");
                vectorB[i] = double.Parse(Console.ReadLine());
            }

            Console.WriteLine("\nВведена матриця А:");
            PrintSimpleMatrix(matrixA);

            Console.WriteLine("\nВведена матриця B:");
            foreach (var v in vectorB) Console.WriteLine($"{v,10:F2}");
            Console.WriteLine(new string('-', 40));

            bool isSquare = (m == n);

            // Завдання 1. Обернена матриця
            Console.WriteLine("Завдання 1. Знайти обернену матрицю C = A^-1:");
            if (isSquare)
            {
                double[,] inverseMatrix = CalculateInverseMatrix(matrixA);
                Console.WriteLine("Остаточна обернена матриця C = A^-1 =");
                PrintMatrix("", inverseMatrix);
            }
            else
            {
                Console.WriteLine("Помилка: Обернена матриця існує тільки для квадратних матриць (m = n).\n");
            }
            Console.WriteLine(new string('-', 40));

            // Завдання 2. Пошук рангу матриці A
            Console.WriteLine("Завдання 2. Пошук рангу матриці A:");
            int rank = CalculateRank(matrixA);
            Console.WriteLine($"R = {rank}");
            Console.WriteLine(new string('-', 40));

            // Завдання 3. СЛАР
            Console.WriteLine("Завдання 3. Розв'язати систему лінійних алгебраїчних рівнянь");
            if (isSquare)
            {
                Console.WriteLine();
                double[,] inverseC = CalculateInverseMatrix(matrixA, false);
                SolveMethod1(matrixA, inverseC, vectorB);
            }
            else
            {
                Console.WriteLine("Помилка: Розв'язання СЛАР через обернену матрицю можливе тільки для квадратних систем.");
            }

            Console.WriteLine(new string('-', 40));
            Console.WriteLine("Програму завершено. Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        // Крок ЗЖВ

        static double[,] JordanGaussStep(double[,] matrix, int pivotRow, int pivotCol)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] newMatrix = new double[rows, cols];
            double pivot = matrix[pivotRow, pivotCol];

            if (Math.Abs(pivot) < 1e-10) return newMatrix;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double val;
                    if (i == pivotRow && j == pivotCol) val = 1.0;
                    else if (i == pivotRow) val = -matrix[i, j];
                    else if (j == pivotCol) val = matrix[i, j];
                    else val = matrix[i, j] * pivot - matrix[i, pivotCol] * matrix[pivotRow, j];

                    newMatrix[i, j] = val / pivot;
                }
            }
            return newMatrix;
        }

        // Логіка для Завдання 1

        static double[,] CalculateInverseMatrix(double[,] inputMatrix, bool showSteps = true)
        {
            int n = inputMatrix.GetLength(0);
            double[,] currentMatrix = (double[,])inputMatrix.Clone();

            if (showSteps) Console.WriteLine("Протокол перетворення (ЗЖВ):");

            for (int k = 0; k < n; k++)
            {
                if (showSteps)
                {
                    Console.WriteLine($"---> Крок #{k + 1}");
                    Console.WriteLine($"Розв’язувальний елемент: A[{k + 1}, {k + 1}] = {currentMatrix[k, k]:F2}");
                }

                currentMatrix = JordanGaussStep(currentMatrix, k, k);

                if (showSteps)
                {
                    PrintSimpleMatrix(currentMatrix);
                    Console.WriteLine();
                }
            }
            return currentMatrix;
        }

        // Логіка для Завдання 2

        static int CalculateRank(double[,] inputMatrix)
        {
            int rows = inputMatrix.GetLength(0);
            int cols = inputMatrix.GetLength(1);
            double[,] mat = (double[,])inputMatrix.Clone();

            int rank = 0;
            const double EPSILON = 1e-10;
            bool[] rowSelected = new bool[rows];

            for (int j = 0; j < cols && rank < rows; j++)
            {
                int k = -1;
                for (int i = 0; i < rows; i++)
                {
                    if (!rowSelected[i] && Math.Abs(mat[i, j]) > EPSILON)
                    {
                        k = i;
                        break;
                    }
                }

                if (k != -1)
                {
                    rank++;
                    rowSelected[k] = true;
                    double pivot = mat[k, j];
                    for (int l = j; l < cols; l++) mat[k, l] /= pivot;

                    for (int i = 0; i < rows; i++)
                    {
                        if (i != k && Math.Abs(mat[i, j]) > EPSILON)
                        {
                            double factor = mat[i, j];
                            for (int l = j; l < cols; l++)
                                mat[i, l] -= factor * mat[k, l];
                        }
                    }
                }
            }
            return rank;
        }

        // Логіка для Завдання 3

        static void SolveMethod1(double[,] originalA, double[,] inverseC, double[] vectorB)
        {
            int n = originalA.GetLength(0);
            Console.WriteLine("Обчислення розв’язків (X = C * B):");

            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                Console.WriteLine($"X[{i + 1}]:");

                for (int j = 0; j < n; j++)
                {
                    double val = inverseC[i, j];
                    double bVal = vectorB[j];
                    sum += val * bVal;

                    Console.Write($"{bVal:F2}*({val:F2})");
                    if (j < n - 1) Console.Write(" + ");
                }

                Console.WriteLine($" = {sum:F2}");
            }
        }

        // Допоміжні методи

        static void PrintMatrix(string name, double[,] m)
        {
            if (!string.IsNullOrEmpty(name)) Console.WriteLine(name);
            PrintSimpleMatrix(m);
        }

        static void PrintSimpleMatrix(double[,] m)
        {
            int rows = m.GetLength(0);
            int cols = m.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write($"{m[i, j],10:F2}");
                }
                Console.WriteLine();
            }
        }
    }
}