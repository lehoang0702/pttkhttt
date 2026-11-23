using System;
using System.IO;
using System.Text;

namespace MedievalRPG.Data
{
    /// <summary>
    /// Tiện ích đọc/ghi file lưu trữ an toàn:
    /// - Ghi atomic qua file tạm (.tmp) với flush xuống đĩa trước khi thay thế file chính.
    /// - Luôn giữ 1 bản sao lưu hợp lệ gần nhất (.bak).
    /// - Nếu file chính bị hỏng/cắt cụt, cách ly sang (.corrupt) và tự động khôi phục từ (.bak).
    /// </summary>
    public static class SaveFileUtility
    {
        public const string BackupSuffix = ".bak";
        public const string TempSuffix = ".tmp";
        public const string CorruptSuffix = ".corrupt";

        public static string GetBackupPath(string path) => path + BackupSuffix;
        public static string GetTempPath(string path) => path + TempSuffix;
        public static string GetCorruptPath(string path) => path + CorruptSuffix;

        public static bool LooksLikeJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string trimmed = text.Trim();
            return trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[trimmed.Length - 1] == '}';
        }

        public static void WriteAtomicWithBackup(string path, string content, Action<string> onWarning = null)
        {
            if (string.IsNullOrEmpty(path) || content == null) return;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tmpPath = GetTempPath(path);
            string bakPath = GetBackupPath(path);

            try
            {
                WriteTextWithFlush(tmpPath, content);

                if (File.Exists(path))
                {
                    ReplaceWithBackup(tmpPath, path, bakPath);
                }
                else
                {
                    File.Move(tmpPath, path);
                }
            }
            catch (Exception ex)
            {
                onWarning?.Invoke($"Lỗi khi lưu file '{Path.GetFileName(path)}': {ex.Message}");
            }
            finally
            {
                TryDeleteFile(tmpPath);
            }
        }

        public static T LoadWithBackup<T>(
            string path,
            Func<string, T> tryDeserialize,
            out bool recoveredFromBackup,
            out bool corruptionDetected,
            Action<string> onWarning = null) where T : class
        {
            recoveredFromBackup = false;
            corruptionDetected = false;

            if (string.IsNullOrEmpty(path) || tryDeserialize == null) return null;

            string bakPath = GetBackupPath(path);

            if (File.Exists(path))
            {
                T primary = TryReadAndDeserialize(path, tryDeserialize, out string primaryError);
                if (primary != null)
                {
                    return primary;
                }

                corruptionDetected = true;
                QuarantineCorruptFile(path, onWarning);
                onWarning?.Invoke(
                    $"File dữ liệu '{Path.GetFileName(path)}' bị lỗi ({primaryError}). Đã cách ly sang '{Path.GetFileName(GetCorruptPath(path))}' và kiểm tra bản sao lưu.");
            }

            if (File.Exists(bakPath))
            {
                T backup = TryReadAndDeserialize(bakPath, tryDeserialize, out string backupError, out string backupJson);
                if (backup != null)
                {
                    recoveredFromBackup = true;
                    onWarning?.Invoke($"Đã tự động khôi phục '{Path.GetFileName(path)}' từ bản sao lưu '{Path.GetFileName(bakPath)}'.");
                    TryRestorePrimaryFromBackupText(path, backupJson);
                    return backup;
                }

                corruptionDetected = true;
                QuarantineCorruptFile(bakPath, onWarning);
                onWarning?.Invoke(
                    $"Bản sao lưu '{Path.GetFileName(bakPath)}' cũng bị lỗi ({backupError}). Sẽ khởi tạo dữ liệu mặc định.");
            }

            return null;
        }

        public static void DeleteSaveAndBackups(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            TryDeleteFile(path);
            TryDeleteFile(GetBackupPath(path));
            TryDeleteFile(GetTempPath(path));
            TryDeleteFile(GetCorruptPath(path));
            TryDeleteFile(GetCorruptPath(GetBackupPath(path)));
        }

        private static T TryReadAndDeserialize<T>(string filePath, Func<string, T> tryDeserialize, out string error) where T : class
        {
            return TryReadAndDeserialize(filePath, tryDeserialize, out error, out _);
        }

        private static T TryReadAndDeserialize<T>(string filePath, Func<string, T> tryDeserialize, out string error, out string rawText) where T : class
        {
            error = null;
            rawText = null;
            try
            {
                rawText = File.ReadAllText(filePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    error = "file rỗng";
                    return null;
                }

                T result = tryDeserialize(rawText);
                if (result == null)
                {
                    error = "dữ liệu không hợp lệ";
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static void WriteTextWithFlush(string filePath, string content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
#if UNITY_WEBGL && !UNITY_EDITOR
                fs.Flush();
#else
                fs.Flush(true);
#endif
            }
        }

        private static void ReplaceWithBackup(string tmpPath, string targetPath, string bakPath)
        {
            try
            {
                File.Replace(tmpPath, targetPath, bakPath, ignoreMetadataErrors: true);
            }
            catch
            {
                if (File.Exists(bakPath))
                {
                    File.Delete(bakPath);
                }
                File.Move(targetPath, bakPath);
                File.Move(tmpPath, targetPath);
            }
        }

        private static void TryRestorePrimaryFromBackupText(string primaryPath, string backupJson)
        {
            if (string.IsNullOrEmpty(primaryPath) || string.IsNullOrEmpty(backupJson)) return;
            string tmpPath = GetTempPath(primaryPath);
            try
            {
                WriteTextWithFlush(tmpPath, backupJson);
                if (File.Exists(primaryPath)) File.Delete(primaryPath);
                File.Move(tmpPath, primaryPath);
            }
            catch
            {
                // Nếu khôi phục file chính thất bại, dữ liệu vẫn đã được nạp vào bộ nhớ từ .bak
            }
            finally
            {
                TryDeleteFile(tmpPath);
            }
        }

        private static void QuarantineCorruptFile(string sourcePath, Action<string> onWarning)
        {
            string corruptPath = GetCorruptPath(sourcePath);
            try
            {
                if (File.Exists(corruptPath))
                {
                    File.Delete(corruptPath);
                }
                File.Move(sourcePath, corruptPath);
            }
            catch (Exception ex)
            {
                onWarning?.Invoke($"Không thể cách ly file lỗi '{Path.GetFileName(sourcePath)}': {ex.Message}");
            }
        }

        private static void TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch
            {
                // Bỏ qua lỗi dọn dẹp file tạm
            }
        }
    }
}
