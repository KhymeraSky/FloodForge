using FloodForge.Popups;
using FloodForge.History;
using Stride.Core.Extensions;

namespace FloodForge.World;

public class ConditionPopup : TimelinePopup {
	protected HashSet<Condition> conditions;

	protected TimelineType ConditionalTimelineType {
		get => this.conditions.First().timeline.timelineType;

		set {
			TimelineTypeChange change = new TimelineTypeChange(value);

			this.conditions.ForEach(change.AddCondition);

			WorldWindow.worldHistory.Apply(change);
		}
	}

	public void TimelineChangeCallback(TimelineType timelineType) {
		this.ConditionalTimelineType = timelineType;
	}

	public void SelectionChangeCallback(bool selected, string timeline) {
		TimelineChange change = new TimelineChange(!selected, timeline);
		this.conditions.ForEach(change.AddCondition);
		WorldWindow.worldHistory.Apply(change);
	}

	public void InvokeOnTimelineChange(Timeline timeline) {
		UpdateOnTimelineChange?.Invoke(timeline);
	}

	public event Action<Timeline>? UpdateOnTimelineChange;

	public ConditionPopup(params Condition[] conditions) : base(new(), (_)=>{}, (_,_)=>{}, true) {
		this.conditions = [.. conditions];
		this.conditions.ForEach(c => c.conditionPopup = this);
		this.timeline.timelines = conditions.First().timeline.timelines;
		this.onTimelineTypeChangeCallback = this.TimelineChangeCallback;
		this.onSelectionChangeCallback = this.SelectionChangeCallback;
		this.UpdateOnTimelineChange += this.UpdateTimeline;
		this.UpdateTimeline(new (conditions.First().timeline.timelineType, this.timeline.timelines));
	}

	public override void Draw() {
		base.Draw();

		if (this.collapsed)
			return;
		float buttonSideDist = float.Min(this.bounds.Width - 0.02f, 0.6f) * 0.5f;
		Rect editPreProcessorsRect = new(this.bounds.CenterX - buttonSideDist, this.bounds.y0 + 0.06f, this.bounds.CenterX + buttonSideDist, this.bounds.y0 + 0.01f);
		if (UI.TextButton($"Edit Condition", editPreProcessorsRect)) {
			List<PreProcessors> preProcessors = [];
			this.conditions.ForEach(i => preProcessors.Add(i.preProcessors));
			PopupManager.Add(new PreProcessorPopup([.. preProcessors])).Title("Editing preProcessors");
		}
	}
}