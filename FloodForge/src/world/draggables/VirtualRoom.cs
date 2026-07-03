namespace FloodForge.World;

public class VirtualRoom {
	public Vector2 CanonPosition;
	public Vector2 DevPosition;
	public Vector2 Position {
		get {
			return WorldWindow.PositionType == WorldWindow.RoomPosition.Canon ? this.CanonPosition : this.DevPosition;
		}
		set {
			if (WorldWindow.PositionType == WorldWindow.RoomPosition.Canon) {
				this.CanonPosition = value;
			}
			else {
				this.DevPosition = value;
			}
		}
	}

	public Vector2 InactivePosition {
		get {
			return WorldWindow.PositionType == WorldWindow.RoomPosition.Canon ? this.DevPosition : this.CanonPosition;
		}

		set {
			if (WorldWindow.PositionType == WorldWindow.RoomPosition.Canon) {
				this.DevPosition = value;
			}
			else {
				this.CanonPosition = value;
			}
		}
	}
}