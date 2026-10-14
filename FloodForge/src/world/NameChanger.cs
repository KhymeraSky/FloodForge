using Stride.Core.Extensions;
using FloodForge.Popups;

namespace FloodForge.World;

public static class NameChanger {
	public static void ChangeRoomName(Room room, string newName, bool askKeep = true) {
		room.oldName ??= room.name;
		room.name = newName;
		if (!room.path.IsNullOrEmpty() && askKeep)
			PopupManager.Add(new ConfirmPopup($"Delete old files?\n({room.OldName})").SetButtons("Delete", "Keep").Okay(() => room.retainOldFiles = false).Cancel(() => room.retainOldFiles = true));
		else
			room.retainOldFiles = true;
	}

	public static void ChangeRegionAcronym(string newAcronym) {
		Logger.Info($"asked to change acronym from {WorldWindow.region.acronym} to {newAcronym}");
		
		WorldWindow.region.oldAcronym ??= WorldWindow.region.acronym;
		WorldWindow.region.acronym = newAcronym;
		
		foreach (Room room in WorldWindow.region.rooms) {
			Logger.Info($"old name: {room.OldName}");
			if (room is OffscreenRoom)
				ChangeRoomName(room, room.OldName[.. ("offscreenroom".Length - 1)] + newAcronym, false);
			else {
				if (!room.data.tags.Contains("GATE")) {
					string acronymlessName = room.OldName[room.name.IndexOf('_')..];
					ChangeRoomName(room, newAcronym + acronymlessName, false);
				}
			}
			Logger.Info($"new name: {room.name}");
		}
		foreach (Room room in WorldWindow.replaceReferenceRooms) {
			string acronymlessName = room.OldName[room.name.IndexOf('_')..];
			ChangeRoomName(room, newAcronym + acronymlessName, false);
		}
	}

	public static void HandleRegionAcronymChange() {
		string oldAcronym = WorldWindow.region.OldAcronym;
		List<(string, string)> filesToCopy = [];
		foreach (string path in Directory.GetFiles(WorldWindow.region.importFolderPath, "", SearchOption.AllDirectories)) {
			string pathRelativeToParent = Path.GetRelativePath(WorldWindow.region.importFolderPath, path);
			string fileName = Path.GetFileName(path);
			string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
			string extension = fileName[fileNameNoExt.Length..];
			string relativeFileParent = pathRelativeToParent[.. ^fileName.Length];
			if (relativeFileParent.EndsWith(Path.DirectorySeparatorChar))
				relativeFileParent = relativeFileParent[.. ^1];
			
			Logger.Info($"checking file at {path}; filename: {fileName}");
			Logger.Info($"pathRelativeToParent: {pathRelativeToParent}");
			// only copy files not already exported
			if (fileName.Equals($"map_{oldAcronym}.txt") || fileNameNoExt.StartsWith($"map_{oldAcronym}-") && extension.Equals(".txt"))
				continue;
			if (fileName.Equals($"world_{oldAcronym}.txt"))
				continue;
			if (fileName.Equals($"map_{oldAcronym}.png") || fileNameNoExt.StartsWith($"map_{oldAcronym}-") && extension.Equals(".png"))
				continue;
			if (fileName.Equals($"map_image_{oldAcronym}.txt") || fileNameNoExt.StartsWith($"map_image_{oldAcronym}-") && extension.Equals(".txt"))
				continue;
			if (fileName.Equals("properties.txt") || fileName.Equals("displayname.txt"))
				continue;
			if (fileName.StartsWith($"{oldAcronym}_")) {
				string[] splitFileName = fileName.Split('_');
				string newFileName = string.Join('_', WorldWindow.region.acronym, string.Join('_', splitFileName[1 ..]));
				filesToCopy.Add((path, Path.Combine(WorldWindow.region.exportFolderPath, relativeFileParent, newFileName)));
				continue;
			}
			if (fileNameNoExt.EndsWith($"_{oldAcronym}")) {
				string[] splitFileName = fileName.Split('_');
				string newFileName = string.Join('_', string.Join('_', splitFileName[.. ^1]), WorldWindow.region.acronym) + extension;
				filesToCopy.Add((path, Path.Combine(WorldWindow.region.exportFolderPath, relativeFileParent, newFileName)));
				continue;
			}

			Logger.Info($"  added to copy; dest: {Path.Combine(WorldWindow.region.exportFolderPath, relativeFileParent, fileName)}");
			filesToCopy.Add((path, Path.Combine(WorldWindow.region.exportFolderPath, relativeFileParent, fileName)));
		}
		foreach (string path in Directory.GetFiles(WorldWindow.region.importRoomsPath, "", SearchOption.AllDirectories)) {
			string pathRelativeToParent = Path.GetRelativePath(WorldWindow.region.importRoomsPath, path);
			string fileName = Path.GetFileName(path);
			string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
			string extension = fileName[fileNameNoExt.Length..];
			string relativeFileParent = pathRelativeToParent[.. ^fileName.Length];
			if (relativeFileParent.EndsWith(Path.DirectorySeparatorChar))
				relativeFileParent = relativeFileParent[.. ^1];
			
			Logger.Info($"checking file at {path}; filename: {fileName}");
			Logger.Info($"pathRelativeToParent: {pathRelativeToParent}");
			string[] splitFileName = fileNameNoExt.Split('_');
			if (fileName.Contains('_') && splitFileName[0].Equals(oldAcronym)) {
				string roomName = string.Join('_', splitFileName[0], splitFileName[1]);
				string hypotheticalNewRoomName = string.Join('_', WorldWindow.region.acronym, string.Join('_', splitFileName[1 ..])) + extension;
				Logger.Info($"  roomName: {roomName}; hypotheticalNewRoomName: {hypotheticalNewRoomName}");
				if (WorldWindow.region.rooms.Any(r => r.OldName == roomName) || WorldWindow.replaceReferenceRooms.Any(r => r.OldName == roomName)) {
					Logger.Info($"  room with name {roomName} found, continuing");
					continue;
				}
				else {
					Logger.Info($"  added to copy; dest: {Path.Combine(WorldWindow.region.exportRoomsPath, relativeFileParent, hypotheticalNewRoomName)}");
					filesToCopy.Add((path, Path.Combine(WorldWindow.region.exportRoomsPath, relativeFileParent, hypotheticalNewRoomName)));
					continue;
				}
			}

			Logger.Info($"  added to copy; dest: {Path.Combine(WorldWindow.region.exportFolderPath, relativeFileParent, fileName)}");
			filesToCopy.Add((path, Path.Combine(WorldWindow.region.exportRoomsPath, relativeFileParent, fileName)));
		}

		foreach ((string from, string to) in filesToCopy) {
			try {
				Logger.Info($"copying {from} to {to}");
				if (!Path.Exists(PathUtil.Parent(to))) {
					Directory.CreateDirectory(PathUtil.Parent(to));
				}
				File.Copy(from, to, true);
			}
			catch (Exception e) {
				Logger.Error($"Error encountered while copying {from} to {to}");
				Logger.Error($"{e}");
			}
		}
		
		bool samePathExport = Path.GetDirectoryName(WorldWindow.region.importFolderPath)!.Equals(Path.GetDirectoryName(WorldWindow.region.exportFolderPath));
		if (!WorldWindow.region.retainOldFiles && !samePathExport) {
			Backup.FolderAndDelete(WorldWindow.region.importFolderPath);
			Backup.FolderAndDelete(WorldWindow.region.importRoomsPath);
			PersistentData.RemovePersistentData(WorldWindow.region.OldAcronym);
			WorldWindow.region.importFolderPath = WorldWindow.region.exportFolderPath;
			WorldWindow.region.importRoomsPath = WorldWindow.region.exportRoomsPath;
		}
		WorldWindow.region.oldAcronym = null;
		WorldWindow.region.retainOldFiles = true;
	}
}