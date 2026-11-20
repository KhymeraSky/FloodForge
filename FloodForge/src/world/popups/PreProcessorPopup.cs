using FloodForge.History;
using FloodForge.Popups;
using Stride.Core.Extensions;

namespace FloodForge.World;

public class PreProcessorPopup : Popup {
	public HashSet<PreProcessors> relevantPreProcessors;
	public PreProcessorPopup(params PreProcessors[] preProcessors) {
		this.relevantPreProcessors = [.. preProcessors];
		float width = 0.6f;
		foreach (string condition in Mods.preProcessorConditions)
			width = Math.Max(width, UI.font.Measure(condition, 0.03f).x + 0.02f);
		this.bounds = new(-width * 0.5f, -0.25f, width * 0.5f, 0.25f);
	}

	public override void Draw() {
		base.Draw();

		float leftBound = this.bounds.x0 + 0.01f;
		float upperBound = this.bounds.y1 - 0.06f;
		float rightBound = this.bounds.x1 - 0.01f;

		float yValue = upperBound;
		float buttonHeight = 0.05f;

		foreach (string condition in Mods.preProcessorConditions) {
			Rect buttonRect = new(leftBound, yValue - buttonHeight, rightBound, yValue);
			bool conditionEnabled = this.relevantPreProcessors.First().items.Contains(condition);
			if (UI.TextButton(condition, buttonRect, new(conditionEnabled ? Themes.Text : Themes.TextDisabled) { selected = conditionEnabled })) {
				PreProcessorChange preProcessorChange = new(!conditionEnabled, condition);
				preProcessorChange.AddPreProcessors([.. this.relevantPreProcessors]);
				WorldWindow.worldHistory.Apply(preProcessorChange);
			}

			yValue -= buttonHeight + 0.01f;
		}

		Immediate.Color(Themes.Border);
		UI.Line(leftBound, yValue - buttonHeight * 0.5f, rightBound, yValue - buttonHeight * 0.5f);

		yValue -= buttonHeight + 0.01f;
		
		Rect addButtonRect = new(leftBound, yValue - buttonHeight, rightBound, yValue);
		if (UI.TextButton("Add New Preprocessor", addButtonRect))
			PopupManager.Add(new AcronymPopup(this.AddPreProcessorCondition, ",{}")); // REVIEW - verify completeness of banletters
	}

	protected void AddPreProcessorCondition(string condition) {
		if (!Mods.preProcessorConditions.Add(condition))
			PopupManager.Add($"Condition {condition}\nalready exists!");
	}
}