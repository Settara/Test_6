using System;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;

namespace Laba_6
{
    public abstract class Filter
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);

        public virtual Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / resultImage.Width * MaxPercent) + add);
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

        protected int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    public class InvertFilter : Filter
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
        }
    }

    public class GrayScaleFilter : Filter
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int Intensity = (int)(0.299 * sourceColor.R + 0.5876 * sourceColor.G + 0.114 * sourceColor.B);
            return Color.FromArgb(Intensity, Intensity, Intensity);
        }
    }

    public class BinarizationFilter : Filter
    {
        private int threshold; // Пороговое значение для бинаризации

        public BinarizationFilter(int threshold)
        {
            this.threshold = threshold; // Убедимся, что порог в пределах [0, 255]
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);

            // Рассчитываем интенсивность (яркость) пикселя
            int intensity = (int)(0.299 * sourceColor.R + 0.5876 * sourceColor.G + 0.114 * sourceColor.B);

            // Определяем, чёрный или белый пиксель
            int binaryColor = 0;
            if (intensity >= threshold)
            {
                binaryColor = 255;
            }

            // Возвращаем результат как чёрно-белый пиксель
            return Color.FromArgb(binaryColor, binaryColor, binaryColor);
        }
    }

    public class BrightnessFilter : Filter
    {
        private int amount;

        public BrightnessFilter(int amount)
        {
            this.amount = amount;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(Clamp(sourceColor.R + amount, 0, 255),
                Clamp(sourceColor.G + amount, 0, 255),
                Clamp(sourceColor.B + amount, 0, 255));
        }
    }

    public class ContrastFilter : GlobalFilter
    {
        protected int brightness = 0;

        private double amount;

        public ContrastFilter(double amount)
        {
            this.amount = amount;
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            brightness = GetBrightness(sourceImage, worker, 50);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / resultImage.Width * 50) + 50);
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            double c = amount;
            Color sourceColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(Clamp((int)(brightness + (sourceColor.R - brightness) * c), 0, 255),
                                  Clamp((int)(brightness + (sourceColor.G - brightness) * c), 0, 255),
                                  Clamp((int)(brightness + (sourceColor.B - brightness) * c), 0, 255));
        }
    }

    public abstract class GlobalFilter : Filter
    {
        /// <summary>
        /// Возвращает среднюю яркость по всем каналам
        /// </summary>
        public int GetBrightness(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100)
        {
            long brightness = 0;
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((double)i / sourceImage.Width * MaxPercent));
                if (worker.CancellationPending)
                    return 0;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    long pix = 0;
                    Color color = sourceImage.GetPixel(i, j);
                    pix += color.R;
                    pix += color.G;
                    pix += color.B;
                    pix /= 3;
                    brightness += pix;
                }
            }
            brightness /= sourceImage.Width * sourceImage.Height;
            return (int)brightness;
        }
    }

    public class MatrixFilter : Filter
    {
        protected double[,] kernel = null;

        protected MatrixFilter() { }
        public MatrixFilter(double[,] kernel)
        {
            this.kernel = kernel;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            double resultR = 0;
            double resultG = 0;
            double resultB = 0;

            for (int l = -radiusX; l <= radiusX; l++)
            {
                for (int k = -radiusY; k <= radiusY; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighbourColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            }

            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
            );
        }
    }

    //Шумоподавление Гаусса
    public class GaussianFilter : MatrixFilter
    {
        public GaussianFilter(int radius)
        {
            double sigma = radius / 3;

            int size = radius * 2 + 1; // Размер ядра
            kernel = new double[size, size];
            double norm = 0;

            // Заполняем ядро значениями гауссовой функции
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = Math.Exp(-(i * i + j * j) / (2 * sigma * sigma));
                    norm += kernel[i + radius, j + radius];
                }
            }

            // Нормализация ядра
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= norm;
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage);

            for (int x = 0; x < sourceImage.Width; x++)
            {
                worker.ReportProgress((int)((double)x / sourceImage.Width * MaxPercent) + add);
                if (worker.CancellationPending)
                    return null;

                for (int y = 0; y < sourceImage.Height; y++)
                {
                    resultImage.SetPixel(x, y, calculateNewPixelColor(sourceImage, x, y));
                }
            }

            return resultImage;
        }
    }

    // Медианный фильтр шумоподавления
    public class MedianFilter : Filter
    {
        private int apertureSize;

        public MedianFilter(int apertureSize)
        {
            if (apertureSize % 2 == 0 || apertureSize < 3)
                throw new ArgumentException("Размер апертуры должен быть нечетным числом, большим или равным 3.");

            this.apertureSize = apertureSize;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radius = apertureSize / 2;
            List<int> neighborhoodR = new List<int>();
            List<int> neighborhoodG = new List<int>();
            List<int> neighborhoodB = new List<int>();

            // Формируем рабочую выборку текущего шага
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    int neighborX = Clamp(x + offsetX, 0, sourceImage.Width - 1);
                    int neighborY = Clamp(y + offsetY, 0, sourceImage.Height - 1);

                    Color neighborColor = sourceImage.GetPixel(neighborX, neighborY);
                    neighborhoodR.Add(neighborColor.R);
                    neighborhoodG.Add(neighborColor.G);
                    neighborhoodB.Add(neighborColor.B);
                }
            }

            // Упорядочиваем выборку по возрастанию
            neighborhoodR.Sort();
            neighborhoodG.Sort();
            neighborhoodB.Sort();

            // Определяем медиану выборки
            int medianIndex = neighborhoodR.Count / 2;

            // Возвращаем медианное значение для текущей точки кадра
            return Color.FromArgb(
                neighborhoodR[medianIndex],
                neighborhoodG[medianIndex],
                neighborhoodB[medianIndex]
            );
        }

        private int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }

    //Резкость
    public class SharpnessFilter : MatrixFilter
    {
        public SharpnessFilter(double k)
        {
            kernel = new double[3, 3] {
                { -k/8, -k/8, -k/8 },
                { -k/8, 1+k, -k/8 },
                { -k/8, -k/8, -k/8 } };
        }
    }

    //Фильтр для тиснения
    public class EmbossFilter : Filter
    {
        int offset;
        public  EmbossFilter(int off)
        {
            offset = off;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int kernelSize = 3;
            int[,] kernel =
            {
                { -1, 0, 1 },
                { -1, 0, 1 },
                { -1, 0, 1 }
            };

            double resultR = 0, resultG = 0, resultB = 0;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int idX = Clamp(x + j, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + i, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[i + 1, j + 1];
                    resultG += neighborColor.G * kernel[i + 1, j + 1];
                    resultB += neighborColor.B * kernel[i + 1, j + 1];
                }
            }

            return Color.FromArgb(
                Clamp((int)(resultR + offset), 0, 255),
                Clamp((int)(resultG + offset), 0, 255),
                Clamp((int)(resultB + offset), 0, 255)
            );
        }
    }

    //Шум точки
    public class NoiseDotsFilter : Filter
    {
        protected readonly Random random = new Random();
        protected double p_white; // Вероятность белых точек
        protected double p_black; // Вероятность черных точек

        public NoiseDotsFilter(double pWhite = 0.005, double pBlack = 0.005)
        {
            p_white = pWhite;
            p_black = pBlack;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            double p = random.NextDouble(); // Случайное число от 0 до 1
            if (p < p_white)
                return Color.White; // Белый шум
            else if (p + p_black > 1)
                return Color.Black; // Черный шум
            else
                return sourceImage.GetPixel(x, y); // Оригинальный пиксель
        }
    }

    //Гауссов шум
    public class NoiseGaussianFilter : Filter
    {
        private readonly Random random = new Random();
        private readonly double mean;    // Среднее значение (μ)
        private readonly double stdDev; // Стандартное отклонение (σ)

        public NoiseGaussianFilter(double mean = 0, double stdDev = 10)
        {
            this.mean = mean;
            this.stdDev = stdDev;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color color = sourceImage.GetPixel(x, y);

            // Генерация случайного значения с нормальным распределением (Box-Muller Transform)
            double noise = mean + stdDev * Math.Sqrt(-2.0 * Math.Log(random.NextDouble())) * Math.Cos(2.0 * Math.PI * random.NextDouble());

            // Добавляем шум к каждому каналу
            int r = Clamp((int)(color.R + noise), 0, 255);
            int g = Clamp((int)(color.G + noise), 0, 255);
            int b = Clamp((int)(color.B + noise), 0, 255);

            return Color.FromArgb(r, g, b);
        }
    }

    //Шум линии
    public class NoiseLinesFilter : Filter
    {
        protected readonly Random random = new Random();
        protected int numberOfLines; // Количество линий

        public NoiseLinesFilter(int numberOfLines = 1000, int maxLength = 40)
        {
            this.numberOfLines = numberOfLines;
            this.maxLength = maxLength;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // Вернем оригинальный цвет, линии обрабатываются в процессе
            return sourceImage.GetPixel(x, y);
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage);

            for (int i = 0; i < numberOfLines; i++)
            {

                // Случайная начальная точка
                int startX = random.Next(0, sourceImage.Width);
                int startY = random.Next(0, sourceImage.Height);

                // Случайный угол и длина
                double angle = random.NextDouble() * 2 * Math.PI; // Угол в радианах
                int length = random.Next(10, maxLength);

                // Случайный цвет линии
                bool isBlack = random.Next(2) == 0;
                Color lineColor = isBlack ? Color.Black : Color.White;

                // Рисуем линию
                DrawLine(resultImage, startX, startY, angle, length, lineColor);

            }

            return resultImage;
        }

        private void DrawLine(Bitmap image, int startX, int startY, double angle, int length, Color color)
        {
            for (int i = 0; i < length; i++)
            {
                // Вычисляем координаты следующего пикселя вдоль линии
                int x = startX + (int)(i * Math.Cos(angle));
                int y = startY + (int)(i * Math.Sin(angle));

                // Проверяем, находится ли пиксель в границах изображения
                if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                {
                    image.SetPixel(x, y, color);
                }
                else
                {
                    break; // Выходим, если линия выходит за границы изображения
                }
            }
        }

        private int maxLength;
    }

    //Шум окружности
    public class NoiseCirclesFilter : Filter
    {
        protected readonly Random random = new Random();
        private readonly int numberOfCircles; // Количество окружностей
        private readonly int maxRadius;      // Максимальный радиус окружности
        private readonly double p_white;     // Вероятность белой окружности
        private readonly double p_black;     // Вероятность черной окружности

        public NoiseCirclesFilter(int numberOfCircles = 600, int maxRadius = 30, double pWhite = 0.5, double pBlack = 0.5)
        {
            this.numberOfCircles = numberOfCircles;
            this.maxRadius = maxRadius;
            this.p_white = pWhite;
            this.p_black = pBlack;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            // Окружности рисуются в процессе обработки изображения
            return sourceImage.GetPixel(x, y);
        }

        public override Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker, int MaxPercent = 100, int add = 0)
        {
            Bitmap resultImage = new Bitmap(sourceImage);

            for (int i = 0; i < numberOfCircles; i++)
            {
                // Случайные параметры окружности
                int centerX = random.Next(0, sourceImage.Width);   // Координата центра
                int centerY = random.Next(0, sourceImage.Height);  // Координата центра
                int radius = random.Next(5, maxRadius);           // Радиус окружности

                // Случайный цвет
                Color circleColor = random.NextDouble() < p_white
                    ? Color.White
                    : random.NextDouble() < p_black ? Color.Black : Color.Transparent;

                if (circleColor != Color.Transparent)
                {
                    DrawCircle(resultImage, centerX, centerY, radius, circleColor);
                }
            }

            return resultImage;
        }

        private void DrawCircle(Bitmap image, int centerX, int centerY, int radius, Color color)
        {
            for (int angle = 0; angle < 360; angle++)
            {
                // Вычисляем координаты точки на окружности
                int x = centerX + (int)(radius * Math.Cos(angle * Math.PI / 180.0));
                int y = centerY + (int)(radius * Math.Sin(angle * Math.PI / 180.0));

                // Проверяем границы изображения
                if (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }
    }
}
