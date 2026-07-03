namespace FloodForge.World;

public class ReplaceRoom : IWorldDraggable {
	public string name;
	public VirtualRoom replacingRoom;
	public Timeline timeline;
	public string[] preProcessorConditions;

	public bool Visible => true;
	public bool Draggable => true;

    protected Vector2 position;
    public Vector2 Position {
        get {
            return this.position;
        }
        set {
            this.position = value;
        }
    }

    public Vector2i size;

	public ReplaceRoom(string roomName, VirtualRoom replacingRoom, Timeline replacingTimeline, string[] preProcessorConditions) {
		this.name = roomName;
		this.replacingRoom = replacingRoom;
		this.timeline = replacingTimeline;
		this.preProcessorConditions = preProcessorConditions;
		this.size = new Vector2i(48, 25);
	}

    public void Draw() {
        Immediate.Color(Themes.RoomLayer2Solid);
		Immediate.Alpha(1f);
        Rect roomRect = new Rect(
            this.Position.x, this.Position.y - this.size.y,
            this.Position.x + this.size.x, this.Position.y
        );
        UI.FillRect(roomRect);
        Immediate.Color(Themes.BorderHighlight);
        UI.StrokeRect(roomRect);
		Immediate.Color(Themes.RoomSolid);
		UI.font.Write(this.name, this.Position.x + (this.size.x * 0.5f), this.Position.y - (this.size.y * 0.5f), 2f, Font.Align.MiddleCenter);
		Immediate.Color(Themes.Layer2Color);
		UI.Line(this.Position, this.replacingRoom.Position);
    }

	public bool Inside(Vector2 pos) {
		Vector2 position = this.Position;
		return pos.x >= position.x && pos.y >= position.y - this.size.y && pos.x < position.x + this.size.x && pos.y <= position.y;
	}

	/// what does this class need to do?
	/// ok lets get its core functionality in first
	/// it needs to:
	/// - know what room it replaces
	/// - know what timeline and preprocessorconditions it replaces it for
	/// - know what room it replaces with, initially through name i guess
	/// - then later I can figure out meshgen
	/// it doesn't need to:
	/// - keep track of dens, since the replaced room's spawns are used
	/// - keep track of connections, since the replaced room's connections are used
	/// 
	/// important aspect:
	/// - if multiple rooms are replaced by the same replaceroom, that exact same replaceroom needs to be drawn multiple times
	/// so:
	/// - region has a list of replacerooms
	/// - when a replaceroom is parsed, look up whether the replaced room's virtualRoom already exists
	/// - if it does exist, refer to it, if it doesn't, create a new virtualRoom
	/// - replacerooms refer to virtualRooms that contain information like mesh etc
	/// - normal rooms are constructed from virtualRooms
	/// this way, if a room is used both as a replacement and as a normal room, editing the normal room affects all replacements
	/// - droplet editing goes through the virtualRoom
	/// 
	/// virtualRoom:
	/// - contains geometry information and room data, but does not itself provide rendering or position
	/// 
	/// SO, WHAT CLASSES?
	/// ReplaceRoom : IWorldDraggable { VirtualRoom roomReference; }
	/// VirtualRoom {}
	/// Room : VirtualRoom, IWorldDraggable { can be constructed from VirtualRoom }
}