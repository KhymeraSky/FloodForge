using FloodForge.Popups;
using FloodForge.Rendering;
using Stride.Core.Extensions;

namespace FloodForge.World;

public class Room : VirtualRoom, IWorldDraggable {
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

	public int GarbageWormDenIndex => this.specialExitCount + this.nonDenExitCount + this.denShortcutEntrances.Count;

	public bool Visible => WorldWindow.VisibleLayers[this.data.layer] && this.timeline.OverlapsWith(WorldWindow.VisibleTimeline);
	public bool Draggable => this.Visible;

	public Room(string path, string name, bool pathOutsideRoomsFolder = false) : base(path, name, pathOutsideRoomsFolder) {
		try {
			this.LoadGeometry();
			this.LoadSettings();
			this.visuals.Refresh();
			this.GenerateMesh();
			this.GenerateWaterMesh();
			this.CheckImages();
		}
		catch (Exception e) {
			Logger.Error($"Failed to load {this.name}!\n{e}");
			try {
				this.SetToInvalidRoom();
				PopupManager.Add(new InfoPopup($"Failed to load {this.name}!"));
			}
			catch { }
		}
	}

	public void SetToInvalidRoom() {
		this.valid = false;
		this.width = 72;
		this.height = 43;
		this.geometry = new uint[this.width * this.height];
	}

	public bool HasDen(int id) {
		return this.HasDen01(id - this.nonDenExitCount) || id == this.GarbageWormDenIndex;
	}

	public bool HasDen01(int id) {
		return id >= 0 && id < this.dens.Count;
	}

	public Den GetDen(int id) {
		return this.GetDen01(id - this.nonDenExitCount);
	}

	public int GetDenId(Vector2i pos) {
		return this.denShortcutEntrances.IndexOf(pos) + this.nonDenExitCount;
	}

	public int GetDenId01(Vector2i pos) {
		return this.denShortcutEntrances.IndexOf(pos);
	}

	public Den GetDen01(int id) {
		if (id < 0 || id >= this.dens.Count) {
			throw new Exception($"Invalid Den {id} for {this.name}");
		}

		return this.dens[id];
	}

	public bool ValidConnection(uint index) {
		return index < this.roomExits.Count;
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

	private static void SetCameraAngle(string from, ref Vector2 angle) {
		try {
			int commaIndex = from.IndexOf(',');
			if (commaIndex == -1)
				throw new FormatException();

			double theta = double.Parse(from[..commaIndex]) * (Math.PI / 180.0);
			double radius = double.Parse(from[(commaIndex + 1)..]);

			angle.x = (float) (Math.Sin(theta) * radius);
			angle.y = (float) (Math.Cos(theta) * radius);
		}
		catch (Exception) {
			Logger.Warn("Failed parsing camera angle: " + from);
		}
	}

	protected virtual void LoadGeometry() {
		if (!File.Exists(this.path)) {
			Logger.Warn($"Failed to load '{this.name}'. File '{this.path}' doesn't exist");
			this.SetToInvalidRoom();
			return;
		}

		string[] lines = File.ReadAllLines(this.path);

		string[] levelData = lines[1].Split('|');
		if (levelData.Length <= 0) {
			Logger.Warn($"Failed to load '{this.name}'. File contains no leveldata.");
			this.SetToInvalidRoom();
			return;
		}
		this.width = int.Parse(levelData[0][..levelData[0].IndexOf('*')]);
		this.height = int.Parse(levelData[0][(levelData[0].IndexOf('*') + 1)..]);
		this.geometry = new uint[this.width * this.height];
		if (levelData.Length == 1) {
			this.data.waterHeight = -1;
			this.data.waterInFront = false;
		}
		else {
			this.data.waterHeight = int.Parse(levelData[1]);
			this.data.waterInFront = int.Parse(levelData[2]) == 1;
		}

		string[] camerasData = lines[3].Split('|', StringSplitOptions.RemoveEmptyEntries);
		foreach (string cameraData in camerasData) {
			string[] parts = cameraData.Split(',');
			if (parts.Length < 2) {
				Logger.Warn($"Room has invalid camera position count ({cameraData})");
				continue;
			}

			int x = 0, y = 0;
			try {
				x = int.Parse(parts[0]);
				y = int.Parse(parts[1]);
			}
			catch {
				Logger.Warn($"Room has invalid camera position ({cameraData})");
			}
			this.data.cameras.Add(new RoomData.Camera() {
				position = new Vector2(x, y)
			});
		}

		this.data.enclosedRoom = lines[4].Contains("Solid");

		if (lines.Length >= 13 && lines[12].StartsWith("camera angles:")) {
			string[] angleData = lines[12][(lines[12].IndexOf(':') + 1)..].Split('|');
			for (int i = 0; i < this.data.cameras.Count; i++) {
				if (i >= angleData.Length)
					break;

				string[] angles = angleData[i].Split(';');
				if (angles.Length != 4) {
					Logger.Warn($"Failed to parse camera {i}; Not enough camera angles");
					continue;
				}

				SetCameraAngle(angles[0], ref this.data.cameras[i].angles[0]);
				SetCameraAngle(angles[1], ref this.data.cameras[i].angles[1]);
				SetCameraAngle(angles[2], ref this.data.cameras[i].angles[2]);
				SetCameraAngle(angles[3], ref this.data.cameras[i].angles[3]);
			}
		}

		string[] objectData = lines[5].Split('|', StringSplitOptions.RemoveEmptyEntries);

		string[] geometryData = lines[11].Split('|', StringSplitOptions.RemoveEmptyEntries);
		int idx = 0;
		foreach (string tile in geometryData) {
			byte[] data = [.. tile.Split(',').Select(byte.Parse)];
			this.geometry[idx] = data[0];
			if (data[0] == 4)
				this.geometry[idx] |= FLAG_SHORTCUT;

			for (int i = 1; i < data.Length; i++) {
				switch (data[i]) {
					case 1:
						this.geometry[idx] |= FLAG_VERTICAL_POLE;
						break;
					case 2:
						this.geometry[idx] |= FLAG_HORIZONTAL_POLE;
						break;
					case 3:
						this.geometry[idx] |= FLAG_SHORTCUT;
						break;
					case 6:
						this.geometry[idx] |= FLAG_BACKGROUND_SOLID;
						break;
					case 7:
						this.geometry[idx] |= FLAG_BATFLY_HIVE;
						break;
					case 8:
						this.geometry[idx] |= FLAG_WATERFALL;
						break;
					case 9:
						this.geometry[idx] |= FLAG_WACK_A_MOLE_HOLE;
						break;
					case 10:
						this.geometry[idx] |= FLAG_GARBAGE_WORM_HOLE;
						break;
					case 11:
						this.geometry[idx] |= FLAG_WORMGRASS;
						break;

					case 4:
						this.geometry[idx] = this.geometry[idx] | FLAG_ROOM_EXIT | FLAG_SHORTCUT;
						this.allRoomExitPoints.Add((RoomExitType.Room, new Vector2i(idx / this.height, idx % this.height)));
						break;

					case 5:
						this.geometry[idx] = this.geometry[idx] | FLAG_DEN | FLAG_SHORTCUT;
						this.allRoomExitPoints.Add((RoomExitType.Den, new Vector2i(idx / this.height, idx % this.height)));
						break;

					case 12:
						this.geometry[idx] = this.geometry[idx] | FLAG_SCAVENGER_DEN | FLAG_SHORTCUT;
						this.allRoomExitPoints.Add((RoomExitType.Scavenger, new Vector2i(idx / this.height, idx % this.height)));
						break;
				}
			}
			idx++;
		}

		this.valid = true;
		this.CheckShortcutEntrancePoints();

		idx = 0;
		for (int x = 0; x < this.width; x++) {
			for (int y = 0; y < this.height; y++) {
				if ((this.geometry[idx] & 15) == 2) {
					int bits = 0;
					bits += (this.GetTile(x - 1, y) % 16 == 1u) ? 1 : 0;
					bits += (this.GetTile(x + 1, y) % 16 == 1u) ? 2 : 0;
					bits += (this.GetTile(x, y - 1) % 16 == 1u) ? 4 : 0;
					bits += (this.GetTile(x, y + 1) % 16 == 1u) ? 8 : 0;
					int type = -1;

					if (bits == 1 + 4)
						type = 0;
					else if (bits == 1 + 8)
						type = 1;
					else if (bits == 2 + 4)
						type = 2;
					else if (bits == 2 + 8)
						type = 3;

					if (type == -1) {
						if (Settings.DEBUGLogInvalidSlopes) {
							Logger.Note($"Invalid slope type {this.name}({x}, {y})");
						}
					}
					else {
						this.geometry[idx] += (uint) (1024 * type);
					}
				}

				idx++;
			}
		}

		foreach (string obj in objectData) {
			string[] item = obj.Split(',');
			if (item.Length != 3) {
				Logger.Warn("Failed to parse object: " + obj);
				continue;
			}

			if (!int.TryParse(item[1], out int x)) {
				Logger.Warn("Failed to parse object: " + obj);
			}
			if (!int.TryParse(item[2], out int y)) {
				Logger.Warn("Failed to parse object: " + obj);
			}

			this.geometry[y - 1 + (x - 1) * this.height] += item[0] == "0" ? FLAG_ROCK : FLAG_SPEAR;
		}

		this.EnsureConnections();

		foreach (Vector2i den in this.denShortcutEntrances) {
			this.dens.Add(new Den());
		}
	}

	protected virtual void LoadSettings() {
		if (this.path.IsNullOrEmpty())
			return;

		this.data.objects.Clear();

		string folder = Path.GetDirectoryName(this.path)!;
		string? settingsPath = PathUtil.FindFile(folder, this.name + "_settings.txt");
		if (settingsPath == null)
			return;

		foreach (string line in File.ReadLines(settingsPath)) {
			if (line.StartsWith("PlacedObjects: ")) {
				string data = line["PlacedObjects: ".Length..];
				string[] poData = data.Split([", "], StringSplitOptions.RemoveEmptyEntries);

				foreach (string po in poData) {
					try {
						int start = po.IndexOf('<');
						int next = po.IndexOf('>', start);
						int end = po.IndexOf('>', next + 1);

						string xStr = po.Substring(start + 1, next - start - 1);
						string yStr = po.Substring(next + 2, end - next - 2);
						string last = po[(end + 2)..];

						Vector2 pos = new Vector2(float.Parse(xStr), float.Parse(yStr));
						string[] splits = last.Split('~', StringSplitOptions.None);

						int separatorIdx = po.IndexOf('>');
						string key = separatorIdx != -1 ? po[..separatorIdx] : po;

						if (DevObjects.objectFactories.TryGetValue(key, out Func<DevObject>? createObject)) {
							DevObject obj = createObject();
							if (obj is ISaveableObject saveable) {
								saveable.Load(pos, splits);
							}
							this.data.objects.Add(obj);
						}
						else {
							Texture texture = Mods.GetObjectTexture(key);
							if (texture == Mods.Unknown) continue;

							GenericItemObject obj = new GenericItemObject(key, texture);
							obj.nodes[0].position = pos;
							this.data.objects.Add(obj);
						}
					}
					catch {
						Logger.Warn("Failed to parse Placed Object: " + po);
					}
				}
			}
		}
	}

	void CheckShortcutEntrancePoints() {
		this.allShortcutEntrancePoints.Clear();
		for (int y = 0; y < this.height; y++) {
			for (int x = 0; x < this.width; x++) {
				if ((this.GetTile(x, y) & FLAG_SHORTCUT) > 0) {
					int[] tiles = new int[9];

					int index = 0;
					for (int y2 = y - 1; y2 < y + 2; y2++) {
						for (int x2 = x - 1; x2 < x + 2; x2++) {
							int result = ((this.GetTile(x2, y2) & 15) == 1) ? 1 : 0;                    //  1 == solid
							result += ((this.GetTile(x2, y2) & FLAG_SHORTCUT) > 0) ? 2 : 0;         //  2 == shortcut
							result += ((this.GetTile(x2, y2) & FLAG_ROOM_EXIT) > 0) ? 4 : 0;            //  4 == roomexit
							result += ((this.GetTile(x2, y2) & FLAG_DEN) > 0) ? 8 : 0;              //  8 == den
							result += ((this.GetTile(x2, y2) & FLAG_SCAVENGER_DEN) > 0) ? 16 : 0;       // 16 == scav
							result += ((this.GetTile(x2, y2) & FLAG_WACK_A_MOLE_HOLE) > 0) ? 32 : 0;    // 32 == wack-a-mole-hole
							tiles[index] = result;
							index++;
						}
					}

					int directionCount = 0;
					int airGaps = 0;

					// only check rest if the tile is just a shortcut
					if (tiles[4] == 2 || tiles[4] == 3) {
						// check if all corners are solid
						if ((tiles[0] & 1) == 0 || (tiles[2] & 1) == 0 || (tiles[6] & 1) == 0 || (tiles[8] & 1) == 0)
							airGaps = 99;

						int dirFlags = 0;
						int airFlags = 0;
						if ((tiles[1] & 62) > 0) {
							directionCount++;
							dirFlags |= 1;
						}
						if ((tiles[1] & 1) == 0) {
							airGaps++;
							airFlags |= 8;
						}
						if ((tiles[3] & 62) > 0) {
							directionCount++;
							dirFlags |= 2;
						}
						if ((tiles[3] & 1) == 0) {
							airGaps++;
							airFlags |= 4;
						}
						if ((tiles[5] & 62) > 0) {
							directionCount++;
							dirFlags |= 4;
						}
						if ((tiles[5] & 1) == 0) {
							airGaps++;
							airFlags |= 2;
						}
						if ((tiles[7] & 62) > 0) {
							directionCount++;
							dirFlags |= 8;
						}
						if ((tiles[7] & 1) == 0) {
							airGaps++;
							airFlags |= 1;
						}

						// check:
						// - that only one of the sides is air,
						// - that only one of the sides has direction
						// - that the shortcut's direction and the airgap's direction are correct
						// (correct as in opposite, which is why the bit assignments are opposite)
						if ((directionCount == 1) && (airGaps == 1) && (airFlags == dirFlags)) {
							this.allShortcutEntrancePoints.Add(new Vector2i(x, y));
						}
					}
				}
			}
		}
	}

	protected void CheckImages() {
		if (!Settings.WarnMissingImages)
			return;

		string path = PathUtil.Parent(this.path);
		for (int i = 0; i < this.data.cameras.Count; i++) {
			string imageFile = $"{this.name}_{i + 1}.png";

			if (PathUtil.FindFile(path, imageFile) == null) {
				Logger.Warn($"{this.name} is missing image {imageFile}");
			}
		}
	}

	public bool AnyConnectionConnectedTo(uint i) {
		if (this.roomExits.Count <= i)
			return false;

		foreach (Connection connection in this.connections) {
			if (connection.roomA == this && connection.roomAExitID == i)
				return true;
			if (connection.roomB == this && connection.roomBExitID == i)
				return true;
		}

		return false;
	}

	#region Connection information methods
	public Vector2 GetConnectionConnectPoint(uint i) {
		if (!WorldWindow.changeConnectBehaviour) {
			RoomConnection connection = this.roomExitPaths[this.roomExits[(int) i]];
			if (connection.endType == RoomPathEndType.shortcutEntrance) {
				return this.RoomPositionToWorldPosition(this.roomExitPaths[this.roomExits[(int) i]].path.EndPosition);
			}
		}
		return this.RoomPositionToWorldPosition(this.roomExits[(int) i]);
	}

	public Vector2i GetConnectionConnectDirection(uint i) {
		if (!WorldWindow.changeConnectBehaviour) {
			RoomConnection connection = this.roomExitPaths[this.roomExits[(int) i]];
			if (connection.endType == RoomPathEndType.shortcutEntrance) {
				return this.roomExitPaths[this.roomExits[(int) i]].path.EndDirection * new Vector2i(-1, 1);
			}
		}
		return this.roomExitPaths[this.roomExits[(int) i]].path.StartDirection;
	}

	public Vector2 GetShortcutEntranceWorldPoint(uint i) {
		return this.RoomPositionToWorldPosition(this.roomExitPaths[this.roomExits[(int) i]].path.EndPosition);
	}
	public Vector2i GetShortcutEntranceRoomPoint(uint i) {
		return this.roomExitPaths[this.roomExits[(int) i]].path.EndPosition;
	}

	public Vector2i GetShortcutEntranceDirection(uint i) {
		return this.roomExitPaths[this.roomExits[(int) i]].path.EndDirection;
	}

	public uint GetRoomExitIDFromShortcut(uint i) {
		return (uint) this.roomExits.IndexOf(this.shortcutEntrancePaths[this.allShortcutEntrancePoints[(int) i]].Item1.path.EndPosition);
	}

	public uint GetShortcutEntranceDirectionInt(uint i) {
		if (i >= this.allShortcutEntrancePoints.Count) {
			throw new Exception($"Invalid shortcut index {i} for {this.name}");
		}
		Vector2i connection = this.GetShortcutEntranceDirection(i);

		if (connection.x <= 0 && connection.y == 0)
			return Direction.Right;
		if (connection.x >= 0 && connection.y == 0)
			return Direction.Left;
		if (connection.y >= 0)
			return Direction.Up;
		if (connection.y <= 0)
			return Direction.Down;

		return Direction.Unknown;
	}

	public Vector2 RoomPositionToWorldPosition(Vector2i roomPosition) {
		return roomPosition * new Vector2i(1, -1) + new Vector2(0.5f, -0.5f) + this.Position;
	}
	#endregion

	public bool Inside(Vector2 pos) {
		Vector2 position = this.Position;
		return pos.x >= position.x && pos.y >= position.y - this.height && pos.x < position.x + this.width && pos.y <= position.y;
	}

	public bool Intersects(Vector2 from, Vector2 to) {
		Vector2 position = this.Position;
		Vector2 cornerMin = Vector2.Min(from, to);
		Vector2 cornerMax = Vector2.Max(from, to);

		return cornerMax.x >= position.x && cornerMax.y >= position.y - this.height && cornerMin.x < position.x + this.width && cornerMin.y <= position.y;
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