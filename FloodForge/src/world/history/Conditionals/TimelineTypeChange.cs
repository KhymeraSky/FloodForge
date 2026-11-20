using FloodForge.World;

namespace FloodForge.History;

public class TimelineTypeChange : Change {
	protected List<Condition> conditions = [];
	protected List<TimelineType> conditionUndoValues = [];

	protected TimelineType redoValue;

	public TimelineTypeChange(TimelineType redoValue) {
		this.redoValue = redoValue;
	}

	public void AddCondition(Condition condition) {
		this.conditions.Add(condition);

		this.conditionUndoValues.Add(condition.timeline.timelineType);
	}

	public override void Undo() {
		for (int i = 0; i < this.conditions.Count; i++) {
			this.conditions[i].timeline.timelineType = this.conditionUndoValues[i];
			this.conditions[i].conditionPopup?.InvokeOnTimelineChange(this.conditions[i].timeline);
		}
	}

	public override void Redo() {
		foreach (Condition condition in this.conditions) {
			condition.timeline.timelineType = this.redoValue;
			condition.conditionPopup?.InvokeOnTimelineChange(condition.timeline);
		}
	}
}