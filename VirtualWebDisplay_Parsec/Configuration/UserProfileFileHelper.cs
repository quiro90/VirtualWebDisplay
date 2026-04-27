namespace VirtualWebDisplay.Configuration;

/// <summary>
/// Helpers for safely writing hidden files inside the user-profile settings directory.
/// </summary>
internal static class UserProfileFileHelper
{
    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="filePath"/> atomically using a
    /// temp file, then marks both the parent directory and the file as hidden on Windows.
    /// </summary>
    internal static void WriteAtomic(string filePath, string content)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            EnsureHiddenDirectory(directory);

        var tempFilePath = Path.Combine(
            directory ?? Path.GetTempPath(),
            $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            PrepareWritableFile(filePath);
            File.WriteAllText(tempFilePath, content);
            ReplaceFile(tempFilePath, filePath);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }

        EnsureHiddenFile(filePath);
    }

    private static void PrepareWritableFile(string filePath)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        var normalized = attributes & ~FileAttributes.ReadOnly & ~FileAttributes.Hidden & ~FileAttributes.System;
        if (normalized != attributes)
            File.SetAttributes(filePath, normalized);
    }

    private static void ReplaceFile(string tempFilePath, string destinationFilePath)
    {
        if (!File.Exists(destinationFilePath))
        {
            File.Move(tempFilePath, destinationFilePath);
            return;
        }

        try
        {
            File.Replace(tempFilePath, destinationFilePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        catch (IOException)
        {
            File.Copy(tempFilePath, destinationFilePath, overwrite: true);
        }
    }

    private static void EnsureHiddenDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            return;

        var attributes = File.GetAttributes(directory);
        if ((attributes & FileAttributes.Hidden) == 0)
            File.SetAttributes(directory, attributes | FileAttributes.Hidden);
    }

    private static void EnsureHiddenFile(string filePath)
    {
        if (!OperatingSystem.IsWindows() || !File.Exists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        if ((attributes & FileAttributes.Hidden) == 0)
            File.SetAttributes(filePath, attributes | FileAttributes.Hidden);
    }
}
