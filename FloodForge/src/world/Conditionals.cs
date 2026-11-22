namespace FloodForge.World;

public class Conditional {
	public ConditionalPopup? conditionalPopup;
	public List<Condition> conditions = [];

	public bool ShowsOnMap(string timeline = "") {
		bool anyConditionPassed = this.conditions.Count == 0;
		foreach (Condition condition in this.conditions) {
			if (condition.timeline.timelineType == TimelineType.Only) {
				if (condition.timeline.timelines.Contains(timeline))
					anyConditionPassed = true;
			}
			if (condition.timeline.timelineType == TimelineType.Except) {
				if (!condition.timeline.timelines.Contains(timeline))
					anyConditionPassed = true;
			}
		}
		return anyConditionPassed;
	}

	// REVIEW - check for redundancy in terms of edge-case checks
	public bool CheckTimelineCull() {
		if (WorldWindow.VisibleTimeline.timelineType == TimelineType.All || this.conditions.Count == 0)
			return true;
		if (WorldWindow.VisibleTimeline.timelineType == TimelineType.Only && WorldWindow.VisibleTimeline.timelines.Count == 0 && this.conditions.Count == 0)
			return true;
		foreach (Condition condition in this.conditions) {
			if (!condition.timeline.OverlapsWith(WorldWindow.VisibleTimeline))
				return false;
		}
		return true;
	}

	public bool CheckWorldTimelineVisibility() {
		if (WorldWindow.VisibleTimeline.timelineType == TimelineType.All || this.conditions.Count == 0)
			return true;
		if (WorldWindow.VisibleTimeline.timelineType == TimelineType.Only && WorldWindow.VisibleTimeline.timelines.Count == 0)
			return true;
		bool result = false;
		foreach (Condition condition in this.conditions) {
			if (condition.timeline.timelineType == TimelineType.Only) {
				// this: ONLY; world: ONLY;
				if (WorldWindow.VisibleTimeline.timelineType == TimelineType.Only) {	
					foreach (string timeline in condition.timeline.timelines)
						result |= WorldWindow.VisibleTimeline.timelines.Contains(timeline);
				}
				// this: ONLY; world: HIDE;
				else {
					foreach (string timeline in condition.timeline.timelines)
						result |= !WorldWindow.VisibleTimeline.timelines.Contains(timeline);
				}
			}
			else if (condition.timeline.timelineType == TimelineType.Except) {
				// this: HIDE; world: ONLY;
				if (WorldWindow.VisibleTimeline.timelineType == TimelineType.Only) {	
					bool foundAll = true;
					foreach (string timeline in WorldWindow.VisibleTimeline.timelines)
						foundAll &= condition.timeline.timelines.Contains(timeline);
					if (foundAll)
						result = false;
					else
						result = true;
				}
				// this: HIDE; world: HIDE;
				else
					result = true; // as with Timeline.OverlapsWith, this case should return false only if all other possible timelines are hidden. However, to account for modded timelines, we just set the result to true.
			}
		}
		return result;
	}
}

public class Condition {
	public PreProcessors preProcessors;
	public Timeline timeline;
	public ConditionPopup? conditionPopup;

	public Condition(PreProcessors preProcessors, Timeline timeline) {
		this.preProcessors = preProcessors;
		this.timeline = timeline;
	}
}

public class PreProcessors {
	public List<string> items = [];

	public PreProcessors() { }

	public PreProcessors(IEnumerable<string> items) {
		this.items = [.. items];
	}

	public override string ToString() => ToString([.. this.items]);

	public static string ToString(string[] items) {
		if (items == null)
			throw new NullReferenceException("Could not convert null preProcessorConditions to string");
		string preProcessors = string.Join(',', items);
		if (preProcessors != "")
			preProcessors = $"{{{preProcessors}}}";
		return preProcessors;
	}
}

public class Timeline {
	public TimelineType timelineType;
	public HashSet<string> timelines;

	public Timeline() {
		this.timelineType = TimelineType.All;
		this.timelines = [];
	}

	public Timeline(Timeline timeline) {
		this.timelineType = timeline.timelineType;
		this.timelines = [..timeline.timelines];
	}

	public Timeline(TimelineType type, HashSet<string> timelines) {
		this.timelineType = type;
		this.timelines = [..timelines];
	}

	public static Timeline All => new Timeline(TimelineType.All, []);

	public bool Match(Timeline other) {
		if (this.timelineType != other.timelineType)
			return false;
		if (this.timelineType == TimelineType.All)
			return true;

		return this.timelines.SetEquals(other.timelines);
	}

	public override string ToString() {
		if (this.timelineType == TimelineType.All)
			return "ALL";
		bool first = true;
		string timelineText = "";
		foreach (string timelineEntry in this.timelines) {
			timelineText += (first ? "" : ",") + timelineEntry;
			first = false;
		}
		if (this.timelineType == TimelineType.Except)
			timelineText = "X-" + timelineText;
		return timelineText;
	}

	public Timeline And(Timeline timeline) {
		if (this.timelineType == TimelineType.All)
			return new(timeline);
		if (timeline.timelineType == TimelineType.All)
			return new(this);
		if (this.timelineType == TimelineType.Only) {
			if (timeline.timelineType == TimelineType.Only) {
				Timeline newTimeline = new(TimelineType.Only, []);
				foreach (string line in timeline.timelines)
					if (this.timelines.Contains(line))
						newTimeline.timelines.Add(line);
				return newTimeline;
			}
			if (timeline.timelineType == TimelineType.Except) {
				Timeline newTimeline = new(TimelineType.Only, [.. this.timelines]);
				foreach (string line in timeline.timelines)
					newTimeline.timelines.Remove(line);
				return newTimeline;
			}
		}
		if (this.timelineType == TimelineType.Except) {
			if (timeline.timelineType == TimelineType.Only) {
				Timeline newTimeline = new(TimelineType.Only, [.. timeline.timelines]);
				foreach (string line in this.timelines)
					newTimeline.timelines.Remove(line);
				return newTimeline;
			}
			if (timeline.timelineType == TimelineType.Except) {
				Timeline newTimeline = new(TimelineType.Except, [.. this.timelines]);
				foreach (string line in timeline.timelines)
					newTimeline.timelines.Add(line);
				return newTimeline;
			}
		}
		return new(this);
	}

	public Timeline Inverted() {
		Timeline result = new();
		if (this.timelineType == TimelineType.All) {
			result.timelines = [];
			result.timelineType = TimelineType.Only;
		}
		else {
			result.timelines = [.. this.timelines];
			result.timelineType = this.timelineType == TimelineType.Only ? TimelineType.Except : TimelineType.Only;
		}

		return result;
	}

	public bool OverlapsWith(Timeline timeline) => this.OverlapsWith(timeline.timelineType, timeline.timelines);

	public bool OverlapsWith(string timeline) => this.OverlapsWith(TimelineType.Only, [timeline]);

	public bool OverlapsWith(TimelineType timelineType, HashSet<string> timelines) {
		if (this.timelineType == TimelineType.All || timelineType == TimelineType.All)
			return true;
		switch (this.timelineType) {
			case TimelineType.Except:
				switch (timelineType) {
					case TimelineType.Except:
						return true;
					case TimelineType.Only:
						foreach (string timelineEntry in timelines)
							if (!this.timelines.Contains(timelineEntry))
								return true;
						return false;
				}
				break;
			case TimelineType.Only:
				switch (timelineType) {
					case TimelineType.Except:
						foreach (string timelineEntry in this.timelines)
							if (!timelines.Contains(timelineEntry))
								return true;
						return false;
					case TimelineType.Only:
						foreach (string timelineEntry in this.timelines)
							if (timelines.Contains(timelineEntry))
								return true;
						return false;
				}
				break;
		}
		return false;
	}

	public bool IsNone() => this.timelineType == TimelineType.Only && this.timelines.Count == 0;

	public bool IsNeutral() => this.timelineType == TimelineType.All || (this.timelineType == TimelineType.Except && this.timelines.Count == 0);
}

public enum TimelineType {
	All,
	Only,
	Except
}