using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using static budget.Shared.FileAccessChecker;

namespace budget.Shared
{
    public class OCRReader : IReader
    {
        public class OCRConfig
        {
            public string[] ImagePaths { get; private set; }//путь до файла
            public string TessdataPath { get; private set; }// Путь "./tessdata" должен вести к папке с файлами .traineddata
            public string TessLanguage { get; private set; }// Язык распознования
            public string TessPSM { get; private set; }// Значение PSM для движка (PSM 6 - единый блок текста, PSM 3 - авто), по умолчанию должно быть 6. 

            public OCRConfig(string[] imgPaths, string tessdataPath, string tessLanguage, string tessPSM)
            {
                ImagePaths = imgPaths;
                TessdataPath = tessdataPath;
                TessLanguage = tessLanguage;
                TessPSM = tessPSM;
            }

            // Конфиг по умолчанию
            public static OCRConfig GetDefault(string dir)
            {
                return new OCRConfig(
                     imgPaths: Directory.GetFiles(dir, "*.jpg"),//поиск всех файлов в папке, возврат полных путей
                     tessdataPath: Config.i().OcrConfig.DefaultTessdataDirectory,
                     tessLanguage: "rus",
                     tessPSM: "6"
                );
            }
        }
        private OCRConfig cfg_;

        public OCRReader(string imgdir)
        {
            if (!string.IsNullOrEmpty(imgdir) && !Directory.Exists(imgdir))
            {
                throw new ArgumentException($"OCR: No access to ({imgdir})");
            }
            cfg_ = OCRConfig.GetDefault(imgdir);
        }
        public OCRReader(OCRConfig config)
        {
            cfg_ = config;
        }

        public IEnumerable<string[]> Read()
        {
            var sb = ReadTextFromJpg_();
            if (sb.Length == 0)
            {
                throw new FileNotFoundException("OCR: reader is empty");
            }
            var list = Parse_(sb.ToString());
            return list;
        }
        private StringBuilder ReadTextFromJpg_()
        {
            var sb = new StringBuilder();
            // Шаг 1: Предобработка изображения (упрощенная)
            // Для сложных случаев лучше использовать OpenCVSharp или Emgu CV
            foreach (var imgPath in cfg_.ImagePaths)
            {
                using (var originalBitmap = new Bitmap(imgPath))
                {
                    // Масштабирование до оптимального размера (если изображение большое, можно уменьшить, но 300 DPI - стандарт)
                    // Здесь мы просто конвертируем в оттенки серого и бинаризуем
                    using (var processedBitmap = PreprocessImage_(originalBitmap))
                    {
                        // Сохраняем временный файл для Tesseract (так как Pix.LoadFromMemory капризен к форматам)
                        string tempPath = Path.GetTempFileName() + ".png";
                        processedBitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                        try
                        {
                            // Шаг 2: Инициализация движка
                            // Путь "./tessdata" должен вести к папке с файлами .traineddata
                            using (var engine = new TesseractEngine(cfg_.TessdataPath, cfg_.TessLanguage, EngineMode.Default))
                            {
                                // Устанавливаем режим сегментации страницы 
                                engine.SetVariable("tessedit_pageseg_mode", cfg_.TessPSM);

                                // Шаг 3: Загрузка и обработка
                                using (var img = Pix.LoadFromFile(tempPath))
                                {
                                    using (var page = engine.Process(img))
                                    {
                                        sb.Append(page.GetText());
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (File.Exists(tempPath)) File.Delete(tempPath);
                        }
                    }
                }
            }
            return sb;
        }
        // Простой метод предобработки (Grayscale + Thresholding)
        private Bitmap PreprocessImage_(Bitmap original)
        {
            // Создаем копию для обработки
            Bitmap processed = new Bitmap(original.Width, original.Height);

            using (Graphics g = Graphics.FromImage(processed))
            {
                // Настраиваем качество
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, original.Width, original.Height);
            }

            // Конвертация в градации серого и бинаризация через ColorMatrix
            // Это базовый пример. Для серьезных задач используйте OpenCVSharp.
            for (int y = 0; y < processed.Height; y++)
            {
                for (int x = 0; x < processed.Width; x++)
                {
                    Color pixel = processed.GetPixel(x, y);
                    // Формула яркости
                    int gray = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);
                    // Простой порог (можно заменить на метод Оцу)
                    Color newColor = gray > 128 ? Color.White : Color.Black;
                    processed.SetPixel(x, y, newColor);
                }
            }

            return processed;
        }

        public static List<string[]> Parse_(string text, int year = 2026)
        {
            var list = new List<string[]> { Config.i().ExpenseConfig.HeaderShema };
            // Нормализуем пробелы (OCR часто ставит неразрывный пробел)
            text = text.Replace('\u00A0', ' ');
            // Шаблон даты: день + месяц + (опционально ", день недели")
            string monthPattern = @"(?:январ|феврал|март|апрел|ма[йя]|июн|июл|август|сентябр|октябр|ноябр|декабр)\w*";
            string datePattern = $@"\b(?<day>\d{{1,2}})\s+(?<month>{monthPattern})(?:,\s*(?<weekday>\w+))?";

            // Шаблон суммы: "3 896,90 Р" или "674,57 Р" или "741 Р"
            string moneyPattern = @"(?<amount>\d{1,3}(?:\s\d{3})*(?:,\d{2})?|\d{4,}(?:,\d{2})?|\d{1,3}(?:,\d{2})?)\s?Р";

            // Находим все даты в тексте
            var dateMatches = Regex.Matches(text, datePattern);

            for (int i = 0; i < dateMatches.Count; i++)
            {
                var dateMatch = dateMatches[i];

                // Определяем границы блока: от текущей даты до следующей (или до конца текста)
                int blockStart = dateMatch.Index;
                int blockEnd = (i + 1 < dateMatches.Count)
                    ? dateMatches[i + 1].Index
                    : text.Length;

                string block = text.Substring(blockStart, blockEnd - blockStart);

                // Извлекаем первую сумму в блоке (обычно это сумма операции)
                var moneyMatch = Regex.Match(block, moneyPattern);

                string day = dateMatch.Groups["day"].Value;
                string month = dateMatch.Groups["month"].Value;
                string sum = moneyMatch.Success ? moneyMatch.Groups["amount"].Value : "не найдено";

                if (sum == "не найдено") continue;

                list.Add([sum, DateTime.Parse($"{day} {month} {year}").ToString(Config.i().ExpenseConfig.DateFormats[0]), "OCR"]);
            }
            return list;
        }
    }
}
