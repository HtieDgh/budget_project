namespace budget.Shared
{
    public class FileAccessChecker
    {
        /// <summary>
        /// Результат проверки
        /// </summary>
        public enum AccessResult
        {
            Success,
            FileLocked,
            NoPermission,
            DirectoryNotExists,
            UnknownError
        }

        public static AccessResult CheckWriteAccess(string filePath)
        {
            try
            {
                // Проверяем существование директории
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return AccessResult.DirectoryNotExists;
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
                            return AccessResult.NoPermission;
                    }
                    return AccessResult.Success;
                }
            }
            catch (IOException)
            {
                return AccessResult.FileLocked;
            }
            catch (UnauthorizedAccessException)
            {
                return AccessResult.NoPermission;
            }
            catch
            {
                return AccessResult.UnknownError;
            }
        }
    }
}
