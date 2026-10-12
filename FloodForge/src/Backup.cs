namespace FloodForge;

public static class Backup {
	private static readonly string BackupDir = "backups";
	private static readonly string TimestampFormat = "-yyyyMMdd-HHmmss";

	public static void File(string? filePath) {
		if (filePath == null)
			return;
		if (!System.IO.File.Exists(filePath))
			return;

		Directory.CreateDirectory(BackupDir);

		string fileName = Path.GetFileNameWithoutExtension(filePath);
		string fileExtension = Path.GetExtension(filePath);

		// Delete old backups
		string[] existingFiles = Directory.GetFiles(BackupDir);
		List<FileInfo> matchingFiles = [];
		foreach (string path in existingFiles) {
			FileInfo existingFile = new FileInfo(path);
			string existingFileName = Path.GetFileNameWithoutExtension(path);
			if (existingFileName.EndsWith(fileExtension) && existingFileName[..^TimestampFormat.Length].Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
				matchingFiles.Add(existingFile);
		}
		matchingFiles = [.. matchingFiles.OrderBy(f => f.LastWriteTime)];
		
		while (matchingFiles.Count >= 5) {
			matchingFiles[0].Delete();
			matchingFiles.RemoveAt(0);
		}

		// Create backup
		string timestamp = DateTime.Now.ToString(TimestampFormat);
		string backupFileName = $"{fileName}{timestamp}{fileExtension}";
		string destinationPath = Path.Combine(BackupDir, backupFileName);

		System.IO.File.Copy(filePath, destinationPath, true);
	}

	public static void Folder(string? folderPath) {
		if (folderPath == null)
			return;
		if (!Directory.Exists(folderPath))
			return;
		
		Directory.CreateDirectory(BackupDir);

		string folderName = folderPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[^1];

		string[] allPaths = Directory.GetFiles(folderPath, "", SearchOption.AllDirectories);
		List<string> relativePaths = [];
		foreach (string path in allPaths)
			relativePaths.Add(Path.GetRelativePath(folderPath, path));
		
		// Delete old backups
		string[] existingPaths = Directory.GetDirectories(BackupDir);
		List<DirectoryInfo> matchingDirectories = [];
		foreach (string path in existingPaths) {
			DirectoryInfo existingDirectory = new DirectoryInfo(path);
			if (existingDirectory.Name[..^TimestampFormat.Length].Equals(folderName, StringComparison.InvariantCultureIgnoreCase))
				matchingDirectories.Add(existingDirectory);
		}
		matchingDirectories = [.. matchingDirectories.OrderBy(d => d.LastWriteTime)];

		while (matchingDirectories.Count >= 5) {
			matchingDirectories[0].Delete(true);
			matchingDirectories.RemoveAt(0);
		}
		
		// Create backup
		string timestamp = DateTime.Now.ToString(TimestampFormat);
		string backupFolderName = $"{folderName}{timestamp}";
		string destinationFolder = Path.Combine(BackupDir, backupFolderName);

		Directory.CreateDirectory(destinationFolder);
		for (int i = 0; i < allPaths.Length; i++) {
			System.IO.File.Copy(allPaths[i], Path.Combine(destinationFolder, relativePaths[i]));
		}
	}
}
