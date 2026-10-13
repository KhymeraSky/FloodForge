using FloodForge.Popups;
using FloodForge.History;
using Stride.Core.Extensions;

namespace FloodForge.World;

public class ConditionalPopup : TimelinePopup {
	protected HashSet<Room>? rooms;
	protected HashSet<Connection>? connections;
	protected DenLineage? lineage;

	protected TimelineType ConditionalTimelineType {
		get {
			return this.connections?.First().timeline.timelineType ?? this?.lineage?.timeline.timelineType ?? this.rooms!.First().timeline.timelineType;
		}

		set {
			TimelineTypeChange change = new TimelineTypeChange(value);

			this.rooms?.ForEach(change.AddRoom);
			this.connections?.ForEach(change.AddConnection);
			if (this.lineage != null) change.AddLineage(this.lineage);

			WorldWindow.worldHistory.Apply(change);
		}
	}

	// REVIEW - check if a timeline change makes sense - for example, a connection set to "X-Red" while it connects to a room with timeline "Red" doesn't make sense
	public void TimelineChangeCallback(TimelineType timelineType) {
		this.ConditionalTimelineType = timelineType;
	}

	public void SelectionChangeCallback(bool selected, string timeline) {
		TimelineChange change = new TimelineChange(!selected, timeline);
		this.rooms?.ForEach(change.AddRoom);
		this.connections?.ForEach(change.AddConnection);
		if (this.lineage != null) change.AddLineage(this.lineage);
		WorldWindow.worldHistory.Apply(change);
	}
	
	public void InvokeOnTimelineChange(Timeline timeline) {
		UpdateOnTimelineChange?.Invoke(timeline);
	}

	public event Action<Timeline>? UpdateOnTimelineChange;

	private ConditionalPopup(IEnumerable<Connection>? connections = null, IEnumerable<Room>? rooms = null, DenLineage? lineage = null) : base(new(), (_)=>{}, (_,_)=>{}) {
		this.timeline.timelines = connections?.First().timeline.timelines ?? lineage?.timeline.timelines ?? rooms?.First()?.timeline.timelines ?? [];
		this.onTimelineTypeChangeCallback = this.TimelineChangeCallback;
		this.onSelectionChangeCallback = this.SelectionChangeCallback;
		this.bounds = new Rect(-0.4f, -0.4f, 0.4f, 0.4f);
		this.UpdateOnTimelineChange += this.UpdateTimeline;
		this.UpdateTimeline(new (connections?.First().timeline.timelineType ?? lineage?.timeline.timelineType ?? rooms!.First().timeline.timelineType, this.timeline.timelines));
	}

	public ConditionalPopup(IEnumerable<Connection> connections, IEnumerable<Room> rooms) : this(connections, rooms, null) {
		this.connections = [ ..connections ];
		this.rooms = [ ..rooms ];
	}

	public ConditionalPopup(IEnumerable<Connection> connections) : this(connections, null, null) {
		this.connections = [ ..connections ];
	}

	public ConditionalPopup(IEnumerable<Room> rooms) : this(null, rooms, null) {
		this.rooms = [ ..rooms ];
	}

	public ConditionalPopup(DenLineage lineage) : this(null, null, lineage) {
		this.lineage = lineage;
	}
}