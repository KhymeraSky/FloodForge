using FloodForge.World;

namespace FloodForge.History;

public class TimelineTypeChange : MultipleRoomChange {
	protected List<TimelineType> roomUndoValues = [];
	protected List<Connection> connections = [];
	protected List<TimelineType> connectionUndoValues = [];
	protected DenLineage? lineage;
	protected TimelineType lineageUndoValue;

	protected TimelineType redoValue;

	public TimelineTypeChange(TimelineType redoValue) {
		this.redoValue = redoValue;
	}

	public override void AddRoom(Room room) {
		base.AddRoom(room);

		this.roomUndoValues.Add(room.timeline.timelineType);
	}

	public void AddConnection(Connection connection) {
		this.connections.Add(connection);

		this.connectionUndoValues.Add(connection.timeline.timelineType);
	}

	public void AddLineage(DenLineage lineage) {
		this.lineage = lineage;

		this.lineageUndoValue = this.lineage.timeline.timelineType;
	}

	public override void Undo() {
		for (int i = 0; i < this.rooms.Count; i++) {
			this.rooms[i].timeline.timelineType = this.roomUndoValues[i];
			this.rooms[i].conditionalPopup?.InvokeOnTimelineChange(this.rooms[i].timeline);
		}
		for (int i = 0; i < this.connections.Count; i++) {
			this.connections[i].timeline.timelineType = this.connectionUndoValues[i];
			this.connections[i].conditionalPopup?.InvokeOnTimelineChange(this.connections[i].timeline);
		}
		this.lineage?.timeline.timelineType = this.lineageUndoValue;
		this.lineage?.conditionalPopup?.InvokeOnTimelineChange(this.lineage.timeline);
	}

	public override void Redo() {
		foreach (Room room in this.rooms) {
			room.timeline.timelineType = this.redoValue;
			room.conditionalPopup?.InvokeOnTimelineChange(room.timeline);
		}
		foreach (Connection connection in this.connections) {
			connection.timeline.timelineType = this.redoValue;
			connection.conditionalPopup?.InvokeOnTimelineChange(connection.timeline);
		}
		this.lineage?.timeline.timelineType = this.redoValue;
		this.lineage?.conditionalPopup?.InvokeOnTimelineChange(this.lineage.timeline);
	}
}