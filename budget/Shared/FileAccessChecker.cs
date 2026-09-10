namespace budget.Shared
{
    public class FileAccessChecker
    {
        /// <summary>
        /// Результат проверки
        /// </summary>
        public enum WriteAccessResult
        {
            Success,
            FileLocked,
            NoPermission,
            DirectoryNotExists,
            UnknownError
        }

        public static WriteAccessResult CheckWriteAccess(string filePath)
        {
            try
            {
                // Проверяем существование директории
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return WriteAccessResult.DirectoryNotExists;
                }

                // Пытаемся открыть файл для записи
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate,
                                                       FileAccess.Write, FileShare.None))
                {
                    // Если файл существует и открыт, проверяем атрибуты
                    if (File.Exists(filePath))
                    {
                        FileAttributes attrs = File.GetAttributes(filePath);
                        if (attrs.HasFlag(FileAttributes.ReadOnly))
                            return WriteAccessResult.NoPermission;
                    }
                    return WriteAccessResult.Success;
                }
            }
            catch (IOException)
            {
                return WriteAccessResult.FileLocked;
            }
            catch (UnauthorizedAccessException)
            {
                return WriteAccessResult.NoPermission;
            }
            catch
            {
                return WriteAccessResult.UnknownError;
            }
        }
    }
}
