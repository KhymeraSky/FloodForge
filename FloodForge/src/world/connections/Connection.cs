namespace FloodForge.World;

public class Connection : ConnectionVisual{
	public Room roomA;
	public Room roomB;

	public uint roomAExitID;
	public uint roomBExitID;

	public bool invalid;
	public override bool DrawStriped { get => this.invalid; }

	public override Vector2 PointA => this.invalid ? this.roomA.Position : this.roomA.GetConnectionConnectPoint(this.roomAExitID);
	public override Vector2i DirectionA => this.invalid ? new Vector2i(0, 0) : this.roomA.GetConnectionConnectDirection(this.roomAExitID);
	public override Vector2 PointB => this.invalid ? this.roomB.Position : this.roomB.GetConnectionConnectPoint(this.roomBExitID);
	public override Vector2i DirectionB => this.invalid ? new Vector2i(0, 0) : this.roomB.GetConnectionConnectDirection(this.roomBExitID);

	public bool Selectable => this.ConnectionVisible || this.roomA.Visible && this.roomB.Visible;
	public override bool AVisible => this.roomA.Visible;
	public override bool BVisible => this.roomB.Visible;
	public override float SizeMultiplier => WorldWindow.selectedConnections.Contains(this) ? 1.4f : 1f;

	public Conditional conditional;
	public override bool ConnectionVisible => this.conditional.CheckWorldTimelineVisibility();

	public Connection(Room roomA, Room roomB, uint connectionA, uint connectionB) : base(false) {
		this.roomA = roomA;
		this.roomB = roomB;
		this.roomAExitID = connectionA;
		this.roomBExitID = connectionB;
		this.conditional = new();
	}

	public Connection(Room roomA, uint connectionA, Room roomB, uint connectionB) : base(false) {
		this.roomA = roomA;
		this.roomB = roomB;
		this.roomAExitID = connectionA;
		this.roomBExitID = connectionB;
		this.conditional = new();
	}

	protected override (Color, Color) GetColorInformation(bool fadeMiddle, bool AVisible, bool BVisible, bool hovered) {
		bool roomConnectionHoverColor = (!fadeMiddle) && AVisible && BVisible && (hovered || WorldWindow.selectedConnections.Contains(this));
		Color connectionColorA;
		Color connectionColorB;

		if (this.invalid) {
			connectionColorA = Themes.RoomConnectionInvalid;
			connectionColorB = Themes.RoomConnectionInvalid;
		}
		else if (roomConnectionHoverColor) {
			connectionColorA = Themes.RoomConnectionHover;
			connectionColorB = Themes.RoomConnectionHover;
		}
		else {
			connectionColorA = Themes.RoomConnection;
			connectionColorB = Themes.RoomConnection;
		}

		if (WorldWindow.ColorType != WorldWindow.RoomColors.None) {
			connectionColorA = this.roomA.GetTintColor();
			connectionColorB = this.roomB.GetTintColor();
			if (!roomConnectionHoverColor) {
				connectionColorA = Color.Lerp(Themes.RoomAir, connectionColorA, Settings.RoomTintStrength);
				connectionColorB = Color.Lerp(Themes.RoomAir, connectionColorB, Settings.RoomTintStrength);
			}
		}

		float alphaA = AVisible ? Settings.ConnectionOpacity : 0f;
		float alphaB = BVisible ? Settings.ConnectionOpacity : 0f;
		connectionColorA.a = alphaA;
		connectionColorB.a = alphaB;
		return (connectionColorA, connectionColorB);
	}

	public override void CheckRecalculateBezier() {
		if (this.BezierPoints == null || this.BezierPoints.Length == 0 || this.recalculateBezier) {
			bool isSingleExitConnection = this.roomA == this.roomB && this.roomAExitID == this.roomBExitID;
			this.RecalculateBezier(this.PointA, this.PointB, this.DirectionA, this.DirectionB, isSingleExitConnection);
		}
	}

	public override void Draw() {
		if (!this.invalid && (this.roomAExitID >= this.roomA.roomExits.Count || this.roomBExitID >= this.roomB.roomExits.Count)) {
			Logger.Warn($"Connection {this.roomA.name}[{this.roomAExitID}] - {this.roomB.name}[{this.roomBExitID}] connects to invalid index! Converting to InvalidConnection.");
			this.invalid = true;
			this.recalculateBezier = true;
			return;
		}
		base.Draw();
		if (WorldWindow.CullTest(this.fittedAABB)) {
			if (!this.AVisible || !this.BVisible || !this.ConnectionVisible)
				return;
			this.DrawTimelineIcons();
		}
	}

	protected void DrawTimelineIcons() {
		if (WorldWindow.VisibleTimelineIcons) {
			List<Condition> relevantConditions = [];
			foreach (Condition condition in this.conditional.conditions) {
				if (condition.timeline.timelineType != TimelineType.All || condition.preProcessors.items.Count != 0)
					relevantConditions.Add(condition);
			}
			float scale = WorldWindow.SelectorScale;
			float startY = this.bezierOriginOffset.y + this.BezierMiddlePoint.y + (relevantConditions.Count - 1) * scale * 0.5f;

			int i = 0;
			foreach (Condition condition in relevantConditions) {
				Immediate.Color(1f, 1f, 1f);
				float yVal = startY - (i * scale);
				float startX = this.bezierOriginOffset.x + this.BezierMiddlePoint.x - (condition.timeline.timelines.Count - 1) * scale * 0.5f;

				int j = 0;
				foreach (string timeline in condition.timeline.timelines) {
					UI.CenteredTexture(Mods.GetTimelineTexture(timeline), startX + (j * scale), yVal, scale);
					j++;
				}

				if (condition.timeline.timelines.Count > 0 && condition.timeline.timelineType == TimelineType.Except) {
					Immediate.Color(1f, 0f, 0f);
					UI.Line(startX - 0.5f * scale, yVal, startX + (condition.timeline.timelines.Count - 0.5f) * scale, yVal, scale * 4f);
				}

				if (condition.preProcessors.items.Count != 0) {
					Immediate.Color(1f, 1f, 0f);
					float x0 = startX - 0.4f * scale;
					float y0 = yVal + 0.5f * scale;
					float y1 = yVal - 0.5f * scale;
					UI.Line(x0, y0, x0, y1, scale * 3f);
				}

				i++;
			}
		}
	}
}