using FloodForge.Popups;
using FloodForge.History;

namespace FloodForge.World;

public class CreateTimelineRoomPopup : ModularPopup {
	private Room relevantRoom;
	private RoomSettingsPopup parent;
	private bool copyConnections;
	private string newName;
	private Timeline newTimeline;
	private Action<string>? updateNewNameSettingAction = s => {};
	
	private ButtonContainer timelineButton;

	private StringSettingContainer newNameSetting;
	private ButtonContainer generateNewNameButton;

	private ButtonContainer createRoomButton;

	public CreateTimelineRoomPopup(RoomSettingsPopup parent) {
		this.relevantRoom = parent.relevantRoom;
		this.parent = parent;
		this.copyConnections = true;
		this.newName = "";
		this.newTimeline = new(TimelineType.Only, []);

		this.AddToQueue(new BoolSettingContainer("Copy Connections", this.copyConnections, b => this.copyConnections = b));

		this.timelineButton = new ButtonContainer("Timeline", this.OpenTimelinePopup).SetContextCheck(b => {
			b.settingName = "Timeline" + (this.newTimeline.timelines.Count == 0 ? "" : $" - {this.newTimeline}");
			this.RecalculateBounds(true, true);
			return true;
		});
		this.AddToQueue(this.timelineButton);

		this.newNameSetting = new StringSettingContainer("", name => this.newName = name, ref this.updateNewNameSettingAction, prefix: $"{WorldWindow.region.acronym}_", hint: this.relevantRoom.name[(this.relevantRoom.name.IndexOf('_') + 1)..]);
		this.generateNewNameButton = new ButtonContainer("Generate", this.GenerateNewName);
		this.AddToQueue(new HorizontalElement([("", this.newNameSetting), ("", this.generateNewNameButton)], [0f, UI.font.Measure("Generate", 0.03f).x + 0.01f]));

		this.createRoomButton = new ButtonContainer("Create Room", this.CreateRoom).SetContextCheck(_ => {
			return this.newName != "" && $"{WorldWindow.region.acronym}_" + this.newName != this.relevantRoom.name;
		});
		this.AddToQueue(this.createRoomButton);
		this.AddQueuedSettings();
	}
	
	private void OpenTimelinePopup() {
		PopupManager.Add(new TimelinePopup(this.newTimeline, type => this.newTimeline.timelineType = type == TimelineType.All ? TimelineType.Only : type,
		(enabled, timeline) => {
			if (!enabled)
				this.newTimeline.timelines.Add(timeline);
			else
				this.newTimeline.timelines.Remove(timeline);
		}, true).SetButtons<TimelinePopup>("", "EXCLUSIVE", "HIDE").Translate(Mouse.Pos, false).Title("Specify Timeline"));
	}

	private void GenerateNewName() {
		string generatedName = this.relevantRoom.name[(this.relevantRoom.name.IndexOf('_') + 1)..];
		generatedName += (this.newTimeline.timelineType == TimelineType.Only ? "" : "X") + this.newTimeline.timelines.FirstOrDefault();
		this.newName = generatedName;
		this.updateNewNameSettingAction?.Invoke(generatedName);
		this.RecalculateBounds(true, true);
	}

	private void CreateRoom() {
		string newRoomName = $"{WorldWindow.region.acronym}_{this.newName}";

		WorldWindow.selectedDraggables = [];

		Room newRoom = new(newRoomName, this.relevantRoom.width, this.relevantRoom.height, false, false, false);
		this.relevantRoom.CopyTo(newRoom);

		List<Change> changes = [];

		RoomAndConnectionChange addChange = new(adding: true);
		addChange.AddRoom(newRoom);
		changes.Add(addChange);

		newRoom.DevPosition = this.relevantRoom.DevPosition + Vector2.NegY * 5;
		newRoom.CanonPosition = this.relevantRoom.CanonPosition + Vector2.NegY * 5;
		newRoom.conditional.conditions.Add(new(new(), this.newTimeline));

		ConditionChange conditionChange = new(this.relevantRoom.conditional, true);
		conditionChange.AddCondition(new(new(), this.newTimeline.Inverted()));
		changes.Add(conditionChange);
		
		if (this.copyConnections) {
			foreach (Connection connection in this.relevantRoom.connections) {
				if (!connection.conditional.CanOverlapWith(this.newTimeline))
					continue;

				Connection copiedConnection = new Connection(connection.roomA == this.relevantRoom ? newRoom : connection.roomA, connection.roomB == this.relevantRoom ? newRoom : connection.roomB, connection.roomAExitID, connection.roomBExitID);
				addChange.AddConnection(copiedConnection);

				// REVIEW - some kind of redundancy check here would be nice to have.
				// Right now, for example, it'll add a "X-YELLOW" condition to a connection that was already "X-YELLOW" or for example "WHITE".
				ConditionChange originalConditionAdd = new(connection.conditional, true);
				originalConditionAdd.AddCondition(new(new(), this.newTimeline.Inverted()));
				changes.Add(originalConditionAdd);

				ConditionChange copiedConditionAdd = new(copiedConnection.conditional, true);
				copiedConditionAdd.AddCondition(new(new(), this.newTimeline));
				changes.Add(copiedConditionAdd);
			}
		}
		WorldWindow.worldHistory.Apply(new MassChange(changes));
		this.parent.Close();
		this.Close();
	}
}