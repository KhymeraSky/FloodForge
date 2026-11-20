using FloodForge.World;

namespace FloodForge.History;

// REVIEW - merge TimelineChange and TimelineTypeChange into one TimelineChange class
// that, or add a separate SimultaneousTimelineChange class that does everything currently done 
// within the room setting constructor lambdas for WorldWindow's Create Timeline Room (because oh stars that's a nightmare)
public class TimelineChange : Change {
	protected bool add;
	protected string timeline;
	protected List<Condition> conditions = [];
	protected ReplaceRoom? replaceRoom;

	public TimelineChange(bool add, string timeline) {
		this.add = add;
		this.timeline = timeline;
	}

	public void AddCondition(Condition condition) {
		this.conditions.Add(condition);
	}

	private void Insert() {
		foreach (Condition condition in this.conditions) {
			condition.timeline.timelines.Add(this.timeline);
			condition.conditionPopup?.InvokeOnTimelineChange(condition.timeline);
		}
	}

	private void Erase() {
		foreach (Condition condition in this.conditions) {
			condition.timeline.timelines.Remove(this.timeline);
			condition.conditionPopup?.InvokeOnTimelineChange(condition.timeline);
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
