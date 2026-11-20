using FloodForge.World;

namespace FloodForge.Popups;

public class TimelinePopup : Popup {
	protected const float buttonSize = 0.07f;
	protected const float buttonPadding = 0.02f;
	protected const float totalSidePadding = 0.1f;

	protected float scroll;

	protected Timeline timeline;

	public Action<bool, string> onSelectionChangeCallback;
	public Action<TimelineType> onTimelineTypeChangeCallback;
	protected bool updateSelf;
	protected string allText = "ALL", onlyText = "ONLY", exceptText = "EXCEPT";

	public TimelinePopup(Timeline timeline, Action<TimelineType> onTimelineTypeChangeCallback, Action<bool, string> onSelectionChangeCallback, ref Action<Timeline>? updateEvent)
	 : this(timeline, onTimelineTypeChangeCallback, onSelectionChangeCallback) {
		updateEvent += this.UpdateTimeline;
	}

	public TimelinePopup(Timeline timeline, Action<TimelineType> onTimelineTypeChangeCallback, Action<bool, string> onSelectionChangeCallback, bool updateSelf = false) {
		this.bounds = new Rect(-0.3f, -0.2f, 0.3f, 0.2f);
		this.onSelectionChangeCallback = onSelectionChangeCallback;
		this.onTimelineTypeChangeCallback = onTimelineTypeChangeCallback;
		this.timeline = timeline;
		this.updateSelf = updateSelf;
	}

	public T SetButtons<T>(string All = "ALL", string Only = "ONLY", string Except = "EXCEPT") where T : TimelinePopup {
		this.allText = All;
		this.onlyText = Only;
		this.exceptText = Except;
		return (T) this;
	}

	protected void DrawButton(Rect rect, string text, TimelineType type) {
		if (UI.TextButton(text, rect, new UI.TextButtonMods { selected = this.timeline.timelineType == type })) {
			this.onTimelineTypeChangeCallback.Invoke(type);
			if (this.updateSelf)
				this.timeline.timelineType = type;
		}
	}

	public virtual void DrawButtons(float centerX, float buttonY) {
		// draw (and check for clicks on) the view type buttons (ALL, ONLY, EXCEPT)
		int buttonCount = (this.allText != "" ? 1 : 0) + (this.onlyText != "" ? 1 : 0) + (this.exceptText != "" ? 1 : 0);
		if (buttonCount == 0)
			return;
		float buttonWidth = this.bounds.Width / (buttonCount + 1);
		float boundsDist = (this.bounds.x1 - centerX) * 0.9f;
		float buttonDistance = boundsDist * 2f / buttonCount;
		float xPos = centerX - boundsDist + buttonDistance * 0.5f;
		if (this.allText != "") {
			this.DrawButton(Rect.FromSize(xPos - buttonWidth * 0.5f, buttonY - 0.025f, buttonWidth, 0.05f), this.allText, TimelineType.All);
			xPos += buttonDistance;
		}
		if (this.onlyText != "") {
			this.DrawButton(Rect.FromSize(xPos - buttonWidth * 0.5f, buttonY - 0.025f, buttonWidth, 0.05f), this.onlyText, TimelineType.Only);
			xPos += buttonDistance;
		}
		if (this.exceptText != "")
			this.DrawButton(Rect.FromSize(xPos - buttonWidth * 0.5f, buttonY - 0.025f, buttonWidth, 0.05f), this.exceptText, TimelineType.Except);
	}

	public void UpdateTimeline(Timeline timeline) {
		this.timeline.timelineType = timeline.timelineType;
		this.timeline.timelines = timeline.timelines;
	}

	public override void Draw() {
		base.Draw();

		if (this.collapsed)
			return;

		float centerX = this.bounds.CenterX;
		string hover = "";
		float buttonY = this.bounds.y1 - 0.1f;

		this.DrawButtons(centerX, buttonY);

		if (this.timeline.timelineType != TimelineType.All) {
			HashSet<string> timelines = this.timeline.timelines;
			List<string> unknowns = [.. timelines.Where(t => !Mods.HasTimeline(t))];
			int count = Mods.timelines.Count + unknowns.Count;

			int timelineColumns = int.Min(Mathf.FloorToInt((this.bounds.Width + buttonPadding - totalSidePadding) / (buttonSize + buttonPadding)), count);
			int timelineRows = Mathf.FloorDivide(count, timelineColumns);

			for (int row = 0; row <= timelineRows; row++) {
				for (int column = 0; column < timelineColumns; column++) {
					int id = column + row * timelineColumns;
					if (id >= count)
						break;

					bool unknown = id >= Mods.timelines.Count;
					string timeline = unknown ? unknowns[id - Mods.timelines.Count] : Mods.timelines[id];

					bool selected = timelines.Contains(timeline);

					Texture texture = Mods.GetTimelineTexture(timeline);
					UVRect rect = UVRect.FromSize(
						centerX + (column - 0.5f * timelineColumns) * (buttonSize + buttonPadding) + buttonPadding * 0.5f,
						this.bounds.y1 - 0.12f - buttonPadding * 0.5f - (row + 1) * (buttonSize + buttonPadding) - this.scroll,
						buttonSize, buttonSize
					);
					UI.CenteredUV(texture, ref rect);
					UI.ButtonResponse response = UI.TextureButton(rect, new UI.TextureButtonMods { selected = selected, texture = texture, textureColor = selected ? Color.White : Color.Grey });

					if (response.clicked) {
						this.onSelectionChangeCallback.Invoke(selected, timeline);
						if (this.updateSelf) {
							if (!selected)
								this.timeline.timelines?.Add(timeline);
							else
								this.timeline.timelines?.Remove(timeline);
						}
						if (!selected && Input.Pressed(Keys.SoloLayer)) {
							foreach(string otherTimeline in timelines) {
								if (otherTimeline == timeline)
									continue;
								this.onSelectionChangeCallback.Invoke(true, otherTimeline);
								if (this.updateSelf)
									this.timeline.timelines?.Remove(otherTimeline);
							}
						}
					}

					if (response.hovered)
						hover = timeline;
				}
			}
		}

		if (!string.IsNullOrEmpty(hover) && this.isHovered) {
			float width = UI.font.Measure(hover, 0.04f).x + 0.02f;
			Rect rect = Rect.FromSize(Mouse.X, Mouse.Y, width, 0.06f);
			Immediate.Color(Themes.Popup);
			UI.FillRect(rect);
			Immediate.Color(Themes.Border);
			UI.StrokeRect(rect);
			Immediate.Color(Themes.Text);
			UI.font.Write(hover, Mouse.X + 0.01f, Mouse.Y + 0.03f, 0.04f, Font.Align.MiddleLeft);
		}
	}
}