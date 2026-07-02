using FloodForge.World;

namespace FloodForge.History;

/// <summary>
/// DISTINCT FROM RoomReplacementChange!!! This is one is for changes to REPLACEROOM conditionals!
/// </summary>
public class ReplaceRoomChange : Change {
	readonly bool addingLink;
	readonly Room replacedRoom;
	readonly Room replacingRoom;
	public ReplaceRoomChange(Room replacedRoom, Room replacingRoom, bool addingLink) {
		this.replacedRoom = replacedRoom;
		this.replacingRoom = replacingRoom;
		this.addingLink = addingLink;
	}

	public void Add() {
		this.replacedRoom.replacingRooms.Add(this.replacingRoom);
		this.replacingRoom.replacedRooms.Add(this.replacedRoom);
	}

	public void Remove() {
		this.replacedRoom.replacingRooms.Remove(this.replacingRoom);
		this.replacingRoom.replacedRooms.Remove(this.replacedRoom);
	}

	public void UpdateBoth() { // probably only need to update replacedroom but eh
		this.replacedRoom.MoveUpdate();
		this.replacingRoom.MoveUpdate();
	}

	public override void Redo() {
		if (this.addingLink)
			this.Add();
		else
			this.Remove();
		this.UpdateBoth();
	}

	public override void Undo() {
		if (this.addingLink)
			this.Remove();
		else
			this.Add();
		this.UpdateBoth();
	}
}