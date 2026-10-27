namespace FloodForge.World;

public class OffscreenRoom : Room {
	public OffscreenRoom(string path, string name) : base(path, name) {
	}

	public Den GetDen() {
		if (this.dens.Count == 0) {
			this.dens.Add(new Den());
			this.denShortcutEntrances.Add(new Vector2i(0, 0));
		}

		return this.dens[0];
	}

	protected override void GenerateWaterMesh() {
	}

	protected override void GenerateMesh() {
	}

	protected override void LoadGeometryFromFile() {
		this.width = 72;
		this.height = 43;
		this.valid = true;
		this.data.tags.Add("OffscreenRoom");
	}

	protected override void LoadSettings() {
	}

	public override void DrawBlack(WorldWindow.RoomPosition positionType) {
		if (!WorldWindow.VisibleCreatures) return;

		base.DrawBlack(positionType);
	}

	public override void DrawEmptyDens(Vector2 renderedPosition) {
		
	}

	public override void DrawOccupiedDens(Vector2 renderedPosition) {
		DrawDen(this.dens[0], renderedPosition.x + this.width * 0.5f, renderedPosition.y - this.height * 0.25f, WorldWindow.denRoom == this && WorldWindow.hoveredDen == 0, WorldWindow.HoveringDraggable == this);
	}

	public override void Draw(WorldWindow.RoomPosition positionType) {
		if (!WorldWindow.VisibleCreatures) return;

		Vector2 position = positionType == WorldWindow.RoomPosition.Canon ? this.CanonPosition : this.DevPosition;
		Immediate.Color(Themes.RoomAir);
		UI.FillRect(position.x, position.y, position.x + this.width, position.y - this.height);

		Immediate.Color(Themes.RoomSolid);
		float fontSize = 5f;
		float textWidth = UI.font.Measure(this.name, fontSize).x;
		if (textWidth > this.width)
			fontSize *= this.width / textWidth;
		UI.font.Write(this.name, position.x + (this.width * 0.5f), position.y - (this.height * 0.5f), fontSize, Font.Align.MiddleCenter);

		if (this.dens.Count == 0) {
			this.dens.Add(new Den());
			this.denShortcutEntrances.Add(new Vector2i(0, 0));
		}

		Vector2 o = WorldWindow.worldMouse - position;
		bool hovered = o.x >= 0f && o.y >= 0f && o.x <= this.width && o.y <= this.height;
		Immediate.Color(hovered ? Themes.RoomBorderHighlight : Themes.RoomBorder);
		UI.StrokeRect(position.x, position.y, position.x + this.width, position.y - this.height);
	}
}