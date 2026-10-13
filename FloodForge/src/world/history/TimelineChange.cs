using FloodForge.World;

namespace FloodForge.History;

// REVIEW - merge TimelineChange and TimelineTypeChange into one TimelineChange class
// that, or add a separate SimultaneousTimelineChange class that does everything currently done 
// within the room setting constructor lambdas for WorldWindow's Create Timeline Room (because oh stars that's a nightmare)
public class TimelineChange : MultipleRoomChange {
	protected bool add;
	protected string timeline;
	protected List<Connection> connections = [];
	protected DenLineage? lineage;
	protected ReplaceRoom? replaceRoom;

	public TimelineChange(bool add, string timeline) {
		this.add = add;
		this.timeline = timeline;
	}

	public void AddConnection(Connection connection) {
		this.connections.Add(connection);
	}

	public void AddLineage(DenLineage lineage) {
		this.lineage = lineage;
	}

	public void AddReplaceRoom(ReplaceRoom replaceRoom) {
		this.replaceRoom = replaceRoom;
	}

	private void Insert() {
		foreach (Room room in this.rooms) {
			room.timeline.timelines.Add(this.timeline);
			room.conditionalPopup?.InvokeOnTimelineChange(room.timeline);
		}
		foreach (Connection connection in this.connections) {
			connection.timeline.timelines.Add(this.timeline);
			connection.conditionalPopup?.InvokeOnTimelineChange(connection.timeline);
		}
		this.replaceRoom?.timeline.timelines.Add(this.timeline);
		if (this.lineage != null) {
			this.lineage.timeline.timelines.Add(this.timeline);
			this.lineage.conditionalPopup?.InvokeOnTimelineChange(this.lineage.timeline);
		}
	}

	private void Erase() {
		foreach (Room room in this.rooms) {
			room.timeline.timelines.Remove(this.timeline);
			room.conditionalPopup?.InvokeOnTimelineChange(room.timeline);
		}
		foreach (Connection connection in this.connections) {
			connection.timeline.timelines.Remove(this.timeline);
			connection.conditionalPopup?.InvokeOnTimelineChange(connection.timeline);
		}
		this.replaceRoom?.timeline.timelines.Remove(this.timeline);
		if (this.lineage != null) {
			this.lineage.timeline.timelines.Remove(this.timeline);
			this.lineage.conditionalPopup?.InvokeOnTimelineChange(this.lineage.timeline);
		}
	}

	public override void Undo() {
		if (this.add)
			this.Erase();
		else
			this.Insert();
	}

	public override void Redo() {
		if (this.add)
			this.Insert();
		else
			this.Erase();
	}
}
