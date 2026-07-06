using FloodForge.Popups;
using FloodForge.Rendering;
using Stride.Core.Extensions;

namespace FloodForge.World;

public class Room : IWorldDraggable {
	protected VirtualRoom room;

	public Vector2 CanonPosition;
	public Vector2 DevPosition;
	public Vector2 Position {
		get {
			return WorldWindow.PositionType == WorldWindow.RoomPosition.Canon ? this.CanonPosition : this.DevPosition;
		}
		set {
			if (WorldWindow.PositionType == WorldWindow.RoomPosition.Canon) {
				this.CanonPosition = value;
			}
			else {
				this.DevPosition = value;
			}
		}
	}

	public Vector2 InactivePosition {
		get {
			return WorldWindow.PositionType == WorldWindow.RoomPosition.Canon ? this.DevPosition : this.CanonPosition;
		}

		set {
			if (WorldWindow.PositionType == WorldWindow.RoomPosition.Canon) {
				this.DevPosition = value;
			}
			else {
				this.CanonPosition = value;
			}
		}
	}

	public List<ReplaceRoom> replaceRooms = [];

	public string[] preProcessorConditions = [];
	public Timeline timeline = new();
	public ConditionalPopup? conditionalPopup;
	public List<GarbageWormDen> garbageWormDens = [];
	public int hoveredDen = -1; // LATER: Remove / improve
	public int hoveredRoomExit = -1; // LATER: Remove / improve
	public int hoveredShortcutEntrance = -1;

	public List<Connection> connections = [];

	// IDEA: Room alerts/hints? (an exclamation mark that appears above a room's corner if there's something of note - softlocking shortcuts, lack of cameras)
	// then, this could also be added to connections so that a room that connects to the same room multiple times isn't allowed to exist without feedback

	public int GarbageWormDenIndex => this.room.specialExitCount + this.room.nonDenExitCount + this.room.denShortcutEntrances.Count;

	public bool Visible => WorldWindow.VisibleLayers[this.room.data.layer] && this.timeline.OverlapsWith(WorldWindow.VisibleTimeline);
	public bool Draggable => this.Visible;

	public Room(VirtualRoom virtualRoom) {
		this.room = virtualRoom;
		this.room.LoadAndGenerateGeometry();

		this.CanonPosition = Vector2.Zero;
		this.DevPosition = Vector2.Zero;
	}
	
	public bool HasDen(int id) {
		return this.HasDen01(id - this.room.nonDenExitCount) || id == this.GarbageWormDenIndex;
	}

	public bool HasDen01(int id) {
		return id >= 0 && id < this.room.dens.Count;
	}

	public Den GetDen(int id) {
		return this.GetDen01(id - this.room.nonDenExitCount);
	}

	public int GetDenId(Vector2i pos) {
		return this.room.denShortcutEntrances.IndexOf(pos) + this.room.nonDenExitCount;
	}

	public int GetDenId01(Vector2i pos) {
		return this.room.denShortcutEntrances.IndexOf(pos);
	}

	public Den GetDen01(int id) {
		if (id < 0 || id >= this.room.dens.Count) {
			throw new Exception($"Invalid Den {id} for {this.room.name}");
		}

		return this.room.dens[id];
	}

	public bool ValidConnection(uint index) {
		return index < this.room.roomExits.Count;
	}

	public void Connect(Connection connection) {
		this.connections.Add(connection);
	}

	public void Disconnect(Connection connection) {
		this.connections.Remove(connection);
	}

	public void MoveUpdate() {
		foreach (Connection connection in this.connections) {
			connection.recalculateBezier = true;
		}
	}

	public bool AnyConnectionConnectedTo(uint i) {
		if (this.room.roomExits.Count <= i)
			return false;

		foreach (Connection connection in this.connections) {
			if (connection.roomA == this && connection.roomAExitID == i)
				return true;
			if (connection.roomB == this && connection.roomBExitID == i)
				return true;
		}

		return false;
	}

	public bool Inside(Vector2 pos) {
		Vector2 position = this.room.Position;
		return pos.x >= position.x && pos.y >= position.y - this.room.height && pos.x < position.x + this.room.width && pos.y <= position.y;
	}

	public bool Intersects(Vector2 from, Vector2 to) {
		Vector2 position = this.room.Position;
		Vector2 cornerMin = Vector2.Min(from, to);
		Vector2 cornerMax = Vector2.Max(from, to);

		return cornerMax.x >= position.x && cornerMax.y >= position.y - this.room.height && cornerMin.x < position.x + this.room.width && cornerMin.y <= position.y;
	}

	#region Rendering
	public virtual void DrawBlack(WorldWindow.RoomPosition positionType) {
		Immediate.Color(Themes.RoomSolid);
		if (this.data.hidden != 0) {
			Immediate.Alpha(0.5f);
		}

		Vector2 position = positionType == WorldWindow.RoomPosition.Canon ? this.CanonPosition : this.DevPosition;
		UI.FillRect(position.x, position.y - this.height, position.x + this.width, position.y);
	}

	private void DrawWater(Vector2 position) {
		Immediate.Color(Themes.RoomWater);
		if (!WorldWindow.VisibleDevItems) {
			UI.FillRect(position.x, position.y - this.height + MathF.Min(this.data.waterHeight + 0.5f, this.height), position.x + this.width, position.y - this.height);
			return;
		}

		Program.gl.Enable(EnableCap.Blend);
		foreach (RoomVisuals.WaterSpot spot in this.visuals.water) {
			Rect waterRect = Rect.FromSize(position.x + spot.pos.x / 20f, position.y - this.height + spot.pos.y / 20f, spot.size.x / 20f, spot.size.y / 20f);
			UI.FillRect(waterRect);
		}
		Program.gl.Disable(EnableCap.Blend);
	}

	public virtual void Draw(WorldWindow.RoomPosition positionType) {
		if (Settings.DEBUGRoomWireframe) {
			Program.gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
		}
		Vector2 renderedPosition = positionType == WorldWindow.RoomPosition.Canon ? this.CanonPosition : this.DevPosition;

		if (!this.valid) {
			Immediate.Color(1f, 0f, 0f);
			Immediate.Begin(Immediate.PrimitiveType.LINES);
			Immediate.Vertex(renderedPosition.x, renderedPosition.y);
			Immediate.Vertex(renderedPosition.x + this.width, renderedPosition.y - this.height);
			Immediate.Vertex(renderedPosition.x + this.width, renderedPosition.y);
			Immediate.Vertex(renderedPosition.x, renderedPosition.y - this.height);
			Immediate.End();

			UI.StrokeRect(renderedPosition.x, renderedPosition.y, renderedPosition.x + this.width, renderedPosition.y - this.height);

			return;
		}

		Program.gl.Enable(EnableCap.Blend);
		Color tint = this.GetTintColor();
		if (WorldWindow.highlightRoom != null && WorldWindow.highlightRoom != this) {
			tint *= 0.25f;
		}

		float alpha = this.data.hidden != 0 ? 0.5f : tint.a;
		if (positionType != WorldWindow.PositionType) {
			alpha *= 0.5f;
		}

		Vector2 matrixPos = WorldWindow.cameraOffset;
		Vector2 matrixScale = WorldWindow.cameraScale * Main.screenBounds;

		if (this.roomRenderable != null){
			this.roomRenderable.PreDraw();
			this.roomRenderable.UniformMatrix4("projection", false, [.. Matrix4X4.CreateOrthographicOffCenter(-matrixScale.x + matrixPos.x, matrixScale.x + matrixPos.x, -matrixScale.y + matrixPos.y, matrixScale.y + matrixPos.y, 0f, 1f)]);
			this.roomRenderable.UniformMatrix4("model", false, [.. Matrix4X4.CreateTranslation(renderedPosition.x, renderedPosition.y, 0f)]);
			this.roomRenderable.Uniform4("tintColor", tint.r, tint.g, tint.b, alpha);
			this.roomRenderable.Uniform1("tintStrength", Settings.RoomTintStrength);
			this.roomRenderable.DoDraw();
		}

		if (this.data.waterHeight != -1) {
			if (!this.data.waterInFront && this.waterRenderable != null) {
				Color color = Themes.RoomWater;
				this.waterRenderable.PreDraw();
				this.waterRenderable.UniformMatrix4("projection", false, [.. Matrix4X4.CreateOrthographicOffCenter(-matrixScale.x + matrixPos.x, matrixScale.x + matrixPos.x, -matrixScale.y + matrixPos.y, matrixScale.y + matrixPos.y, 0f, 1f)]);
				this.waterRenderable.UniformMatrix4("model", false, [.. Matrix4X4.CreateTranslation(renderedPosition.x, renderedPosition.y, 0f)]);
				this.waterRenderable.Uniform4("tintColor", color.r, color.g, color.b, color.a);
				this.waterRenderable.Uniform1("tintStrength", 0f);
				this.waterRenderable.DoDraw();
			}
			else
				this.DrawWater(renderedPosition);
		}
		if (WorldWindow.VisibleDevItems) {
			this.DrawTerrain(new Vector2(renderedPosition.x, renderedPosition.y - this.height));
		}
		if (Settings.DEBUGRoomWireframe) {
			Program.gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
		}

		Program.gl.Disable(EnableCap.Blend);

		if (positionType == WorldWindow.PositionType) {
			float clippedSelectorScale = Math.Min(WorldWindow.SelectorScale, 10f);
			if (WorldWindow.VisibleDevItems) {
				foreach (DevObject devObject in this.data.objects) {
					devObject.Draw(this.Position + new Vector2(0f, -this.height));
				}
			}

			for (int i = 0; i < this.roomExits.Count; i++) {
				Vector2 exitPos = this.RoomPositionToWorldPosition(this.roomExitPaths[this.roomExits[i]].path.StartPosition);
				Vector2 entrancePos = this.RoomPositionToWorldPosition(this.roomExitPaths[this.roomExits[i]].path.EndPosition);
				bool entranceIsShortcutEntrance = this.roomExitPaths[this.roomExits[i]].endType == RoomPathEndType.shortcutEntrance;
				bool connected = this.AnyConnectionConnectedTo((uint) i);

				// Shortcut Entrance
				Immediate.Color(connected ? Themes.RoomConnection : Themes.RoomShortcutRoom);
				if (entranceIsShortcutEntrance) {
					if (WorldWindow.changeConnectBehaviour)
						UI.StrokeCircle(entrancePos, clippedSelectorScale * (i == this.hoveredRoomExit ? 1.5f : 1f) * (connected ? 0.5f : 1f) * 0.25f, 8);
					else
						UI.FillCircle(entrancePos, clippedSelectorScale * (i == this.hoveredRoomExit ? 1.5f : 1f) * (connected ? 0.5f : 1f) * 0.25f, 8);
				}

				// Room Exit
				if (WorldWindow.changeConnectBehaviour || !entranceIsShortcutEntrance)
					UI.FillCircle(exitPos, clippedSelectorScale * (i == this.hoveredRoomExit ? 1.5f : 1f) * (connected ? 0.5f : 1f) * 0.25f, 8);
				else
					UI.StrokeCircle(exitPos, clippedSelectorScale * (i == this.hoveredRoomExit ? 1.5f : 1f) * (connected ? 0.5f : 1f) * 0.25f, 8);

				// Find the index of the connection associated with this RoomExit (if it's connected to something)
				int getConnectionIndex = 0;
				bool connectionFound = false;
				if (connected) {
					for (int j = 0; j < this.connections.Count; j++) {
						int connection = this.connections[j].roomA == this ? (int) this.connections[j].roomAExitID : (int) this.connections[j].roomBExitID;
						if (connection == i) {
							connectionFound = true;
							getConnectionIndex = j;
							break;
						}
					}
				}

				// Draws shortcutpath if either the associated exit or connection is hovered over.
				bool shouldBeHighlighted = (i == this.hoveredRoomExit || connectionFound && this.connections[getConnectionIndex].Hovered) && this.hoveredShortcutEntrance == -1;
				if (shouldBeHighlighted || Keys.Modifier(Keys.Modifiers.Shift)) {
					if (this.roomExitPaths.TryGetValue(this.roomExits[i], out RoomConnection result)) {
						this.DrawRoomPath(result, i == this.hoveredRoomExit, shouldBeHighlighted);
					}
				}
			}

			if (Settings.DEBUGVisibleShortcutEntranceData) {
				foreach ((RoomConnection connection, bool isMatchedWithRoomExit) in this.shortcutEntrancePaths.Values) {
					Immediate.Color(isMatchedWithRoomExit ? Color.Black : connection.endType switch {
						RoomPathEndType.deadend => new Color(1, 0, 0),
						RoomPathEndType.shortcutEntrance => new Color(1, 1, 1),
						RoomPathEndType.den => new Color(1, 1, 0),
						RoomPathEndType.scavengerDen => new Color(0, 1, 0),
						RoomPathEndType.roomExit => new Color(0.5f, 0.5f, 1),
						RoomPathEndType.wackAMoleHole => new Color(0.2f, 0.4f, 0.6f),
						_ => Color.Black
					});
					UI.StrokeCircle(this.RoomPositionToWorldPosition(connection.path.StartPosition), isMatchedWithRoomExit ? 0.25f : 2f, 8);
					Immediate.Color(isMatchedWithRoomExit ? Color.Black : Color.Magenta);
					UI.StrokeCircle(this.RoomPositionToWorldPosition(connection.path.EndPosition), isMatchedWithRoomExit ? 0.25f : 1f, 8);
				}
			}
			// this bit handles the case where:
			// a shortcut entrance that connects to a roomexit, without said roomexit connecting back to the same entrance
			for (int i = 0; i < this.allShortcutEntrancePoints.Count; i++) {
				if (this.shortcutEntrancePaths.TryGetValue(this.allShortcutEntrancePoints[i], out (RoomConnection connection, bool isMatchedWithRoomExit) value)) {
					bool entranceConnectedToRoomExit = value.connection.endType == RoomPathEndType.roomExit;
					if (!value.isMatchedWithRoomExit && entranceConnectedToRoomExit) {
						Vector2 entrancePos = this.RoomPositionToWorldPosition(this.shortcutEntrancePaths[this.allShortcutEntrancePoints[i]].Item1.path.StartPosition);
						Vector2 exitPos = this.RoomPositionToWorldPosition(this.shortcutEntrancePaths[this.allShortcutEntrancePoints[i]].Item1.path.EndPosition);
						uint exitID = this.GetRoomExitIDFromShortcut((uint) i);
						bool roomExitIsConnected = this.AnyConnectionConnectedTo(exitID);

						// Shortcut Entrance
						Immediate.Color(roomExitIsConnected ? Themes.RoomConnection : Themes.RoomShortcutRoom);
						if (WorldWindow.changeConnectBehaviour) {
							UI.StrokeCircle(entrancePos, clippedSelectorScale * (i == this.hoveredShortcutEntrance ? 1.5f : 1f) * (roomExitIsConnected ? 0.5f : 1f) * 0.25f, 8);
							UI.FillCircle(exitPos, clippedSelectorScale * (i == this.hoveredShortcutEntrance ? 1.5f : 1f) * (roomExitIsConnected ? 0.5f : 1f) * 0.25f, 8);
						}
						else {
							UI.FillCircle(entrancePos, clippedSelectorScale * (i == this.hoveredShortcutEntrance ? 1.5f : 1f) * (roomExitIsConnected ? 0.5f : 1f) * 0.25f, 8);
							UI.StrokeCircle(exitPos, clippedSelectorScale * (i == this.hoveredShortcutEntrance ? 1.5f : 1f) * (roomExitIsConnected ? 0.5f : 1f) * 0.25f, 8);
						}

						// Draws shortcutpath if the connection is hovered over. (since a roomexit isn't related to this entrance
						// (otherwise it'd have been drawn with the roomExits), there is no exit to hover over that should highlight this shortcut entrance)
						bool shouldBeHighlighted = i == this.hoveredShortcutEntrance;
						if (shouldBeHighlighted || Keys.Modifier(Keys.Modifiers.Shift)) {
							this.DrawRoomPath(value.connection, shouldBeHighlighted, shouldBeHighlighted);
						}
					}
				}
			}

			if (WorldWindow.VisibleCreatures) {
				for (int i = 0; i < this.denShortcutEntrances.Count; i++) {
					this.DrawDen(this.dens[i], renderedPosition.x + this.denShortcutEntrances[i].x, renderedPosition.y - this.denShortcutEntrances[i].y, i == this.hoveredDen, WorldWindow.HoveringDraggable == this);
				}
			}
		}

		if (this.timeline.timelineType != TimelineType.All) {
			int i = 0;
			Immediate.Color(1f, 1f, 1f);
			foreach (string timeline in this.timeline.timelines) {
				UI.CenteredTexture(Mods.GetTimelineTexture(timeline), (float) (renderedPosition.x + (i * WorldWindow.SelectorScale) + 1.5f), (float) (renderedPosition.y - 1.5f), WorldWindow.SelectorScale);
				i++;
			}

			if (this.timeline.timelines.Count > 0 && this.timeline.timelineType == TimelineType.Except) {
				Immediate.Color(1f, 0f, 0f);
				UI.Line(renderedPosition.x + 2f - WorldWindow.SelectorScale * 0.5f, renderedPosition.y - 2f, renderedPosition.x + 2f + WorldWindow.SelectorScale * 0.5f + (this.timeline.timelines.Count - 1) * WorldWindow.SelectorScale, renderedPosition.y - 2f, WorldWindow.SelectorScale * 4f);
			}

			if (this.preProcessorConditions.Length != 0) {
				Immediate.Color(1f, 1f, 0f);
				float x0 = renderedPosition.x + 2f - WorldWindow.SelectorScale * 0.5f;
				float y0 = renderedPosition.y - 2f - WorldWindow.SelectorScale * 0.5f;
				float y1 = renderedPosition.y - 2f + WorldWindow.SelectorScale * 0.5f;
				UI.Line(x0, y0, x0, y1, WorldWindow.SelectorScale * 3f);
			}
		}

		Vector2 o = WorldWindow.worldMouse - renderedPosition;
		bool hovered = o.x >= 0f && o.y <= 0f && o.x <= this.width && o.y >= -this.height;
		Immediate.Color(hovered ? Themes.RoomBorderHighlight : Themes.RoomBorder);
		UI.StrokeRect(renderedPosition.x, renderedPosition.y, renderedPosition.x + this.width, renderedPosition.y - this.height);
		this.hoveredShortcutEntrance = -1;
	}

	protected void DrawRoomPath(RoomConnection connectionPathToDraw, bool isHovered, bool isHighlighted) {
		Vector2 positionOffset = this.Position + new Vector2(0.5f, -0.5f);
		Immediate.Color(isHovered ? Themes.RoomConnectionHover : Themes.RoomConnection);
		if (WorldWindow.changeConnectBehaviour && isHighlighted && (WorldWindow.cameraScale < 75f || Keys.Pressed(Silk.NET.Input.Key.P))) {
			bool drawnExit = false;
			foreach (Vector2i dot in connectionPathToDraw.path.Path) { // DRAWING SHORTCUT PATH, STARTS FROM ROOMEXIT, WHICH IS WHY IT DRAWS THE FIRST ORB BIGGER
				UI.FillCircle(dot * new Vector2(1, -1) + positionOffset, drawnExit ? 0.4f : 0.5f, 8);
				drawnExit = true;
			}
		}
		else {
			Vector2i lastDir = Vector2i.Zero;
			Vector2i lastPos = Vector2i.Zero;
			Vector2i newDir;
			Vector2i pointA;
			Vector2i pointB;
			for (int j = 1; j < connectionPathToDraw.path.Path.Length; j++) {
				pointA = connectionPathToDraw.path.Path[j - 1] * new Vector2i(1, -1);
				pointB = connectionPathToDraw.path.Path[j] * new Vector2i(1, -1);
				newDir = pointB - pointA;
				if (lastDir == Vector2i.Zero) {
					lastDir = newDir;
					lastPos = pointA;
				}
				else if (newDir != lastDir) {
					UI.Line(lastPos + positionOffset, pointA + positionOffset);
					lastPos = pointA;
					lastDir = newDir;
				}
				if (j == connectionPathToDraw.path.Path.Length - 1) {
					UI.Line(lastPos + positionOffset, pointB + positionOffset);
				}
			}
		}
	}

	protected void DrawDen(Den den, float x, float y, bool hovered, bool roomHovered) {
		bool denEmpty = true;
		bool drawnDen = false;

		float selectorScale = WorldWindow.SelectorScale;
		int drawnCreatures = 0;
		List<DenLineage> visibleLineages = den.creatures.FindAll(d => d.timeline.OverlapsWith(WorldWindow.VisibleTimeline));
		for (int i = 0; i < visibleLineages.Count; i++) {
			DenCreature creature = visibleLineages[i];
			if (creature.type.IsNullOrEmpty() && creature.lineageTo == null) {
				drawnCreatures++;
				continue;
			}
			if (creature is DenLineage denLineage && !denLineage.timeline.OverlapsWith(WorldWindow.VisibleTimeline))
				continue;

			float scale = selectorScale;
			float rectX = x + drawnCreatures * scale - (visibleLineages.Count - 1f) * 0.5f * scale;
			float rectY = y;

			if (hovered)
				scale *= 1.5f;

			if (!creature.type.IsNullOrEmpty()) {
				denEmpty = false;
			}
			if (WorldWindow.cameraScale < 1000f || roomHovered) {
				drawnDen = true;
				Immediate.Color(1f, 1f, 1f);
				if (!denEmpty && !creature.type.IsNullOrEmpty()) {
					UI.CenteredTexture(Mods.GetCreatureTexture(creature.type), rectX, rectY, scale);
				}
				if (creature.lineageTo == null) {
					UI.font.Write(creature.count.ToString(), rectX + 0.5f + scale * 0.25f, rectY - 0.5f - scale * 0.5f, 0.5f * scale, Font.Align.MiddleCenter);
				}
				else {
					while (creature.lineageTo != null) {
						float chance = creature.lineageChance;
						creature = creature.lineageTo;
						rectY -= selectorScale;
						if (!creature.type.IsNullOrEmpty()) {
							UI.CenteredTexture(Mods.GetCreatureTexture(creature.type), rectX, rectY, scale);
						}
						UI.font.Write((int) (chance * 100f) + "%", rectX + 0.5f + scale * 0.25f, rectY + selectorScale - 0.4f - scale * 0.5f, 0.3f * scale, Font.Align.MiddleCenter);
					}
				}
			}
			drawnCreatures++;
		}
		if (!drawnDen && (!denEmpty || denEmpty && WorldWindow.cameraScale < 400f || roomHovered)) {
			Immediate.Color(Themes.RoomShortcutDen);
			UI.FillCircle(x + 0.5f, y - 0.5f, selectorScale * (hovered ? 1.5f : 1f) * 0.25f, 8);
		}
	}

	public Color GetTintColor() {
		switch (WorldWindow.ColorType) {

			case WorldWindow.RoomColors.Layer:
				return this.data.layer switch {
					0 => Themes.Layer0Color,
					1 => Themes.Layer1Color,
					2 => Themes.Layer2Color,
					_ => Color.White,
				};

			case WorldWindow.RoomColors.Subregion: {
				if (this.data.subregion <= -1) {
					return WorldWindow.region.overrideSubregionColors.TryGetValue(-1, out Color value) ? value : Settings.NoSubregionColor;
				}
				else {
					if (WorldWindow.region.overrideSubregionColors.TryGetValue(this.data.subregion, out Color value)) {
						return value;
					}
					else {
						return Settings.SubregionColors.Value.Length == 0
							? Settings.NoSubregionColor
							: Settings.SubregionColors.Value[this.data.subregion % Settings.SubregionColors.Value.Length];
					}
				}
			}

			default:
				return Color.White;
		}
	}

	#endregion
}