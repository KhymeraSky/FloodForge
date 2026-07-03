namespace FloodForge.World;

public class ReplaceRoom : IWorldDraggable {
	public string name;
	// here would also be a reference to the VirtualRoom whose visuals the replaceroom inherits
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

		int i = 0;
		Immediate.Color(1f, 1f, 1f);
		foreach (string timeline in this.timeline.timelines) {
			UI.CenteredTexture(Mods.GetTimelineTexture(timeline), (float) (this.Position.x + (i * WorldWindow.SelectorScale) + 1.5f), (float) (this.Position.y - 1.5f), WorldWindow.SelectorScale);
			i++;
		}

		if (this.timeline.timelines.Count > 0 && this.timeline.timelineType == TimelineType.Except) {
			Immediate.Color(1f, 0f, 0f);
			UI.Line(this.Position.x + 2f - WorldWindow.SelectorScale * 0.5f, this.Position.y - 2f, this.Position.x + 2f + WorldWindow.SelectorScale * 0.5f + (this.timeline.timelines.Count - 1) * WorldWindow.SelectorScale, this.Position.y - 2f, WorldWindow.SelectorScale * 4f);
		}

		if (this.preProcessorConditions.Length != 0) {
			Immediate.Color(1f, 1f, 0f);
			float x0 = this.Position.x + 2f - WorldWindow.SelectorScale * 0.5f;
			float y0 = this.Position.y - 2f - WorldWindow.SelectorScale * 0.5f;
			float y1 = this.Position.y - 2f + WorldWindow.SelectorScale * 0.5f;
			UI.Line(x0, y0, x0, y1, WorldWindow.SelectorScale * 3f);
		}
    }

	public bool Inside(Vector2 pos) {
		Vector2 position = this.Position;
		return pos.x >= position.x && pos.y >= position.y - this.size.y && pos.x < position.x + this.size.x && pos.y <= position.y;
	}

	/// what does this class need to do?
	/// ok lets get its core functionality in first
	/// it needs to:
	/// X know what room it replaces
	/// X know what timeline and preprocessorconditions it replaces it for
	/// \ know what room it replaces with, initially through name i guess
	/// - then later I can figure out meshgen
	/// it doesn't need to:
	/// - keep track of dens, since the replaced room's spawns are used
	/// - keep track of connections, since the replaced room's connections are used
	/// 
	/// important aspect:
	/// - if multiple rooms are replaced by the same replaceroom, that exact same replaceroom needs to be drawn multiple times
	/// so:
	/// X region has a list of replacerooms
	/// - when a replaceroom is parsed, look up whether the replaced room's virtualRoom already exists
	/// - if it does exist, refer to it, if it doesn't, create a new virtualRoom
	/// - replacerooms refer to virtualRooms that contain information like mesh etc
	/// - normal rooms are constructed from virtualRooms
	/// this way, if a room is used both as a replacement and as a normal room, editing the normal room affects all replacements
	/// - droplet editing goes through the virtualRoom <<- this may be a good jumping-off point
	/// 
	/// virtualRoom:
	/// - contains geometry information and room data, but does not itself provide rendering or position
	/// 
	/// SO, WHAT CLASSES?
	/// ReplaceRoom : IWorldDraggable { VirtualRoom roomReference; }
	/// VirtualRoom {}
	/// Room : VirtualRoom, IWorldDraggable { can be constructed from VirtualRoom }
}