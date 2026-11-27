using FloodForge.History;
using FloodForge.Popups;

namespace FloodForge.World;

public class ConditionalPopup : Popup {
	public Conditional relevantConditional; // TODO - allow this to add multiple conditionals

	protected float scroll;
	protected float targetScroll;
	protected float maxScroll;

	public ConditionalPopup(Conditional conditional) {
		this.bounds = new Rect(-0.4f, -0.4f, 0.4f, 0.4f);
		Main.Scroll += this.Scroll;
		this.relevantConditional = conditional;
		this.relevantConditional.conditionalPopup = this;
	}

	public override void Draw() {
		base.Draw();

		if (this.collapsed)
			return;

		this.scroll += (this.targetScroll - this.scroll) * (1f - MathF.Pow(1f - Settings.PopupScrollSpeed, Program.Delta * 60f));

		float leftBound = this.bounds.x0 + 0.04f;
		float rightBound = this.bounds.x1 - 0.04f;
		float upperBound = this.bounds.y1 - 0.06f;

		Program.gl.Enable(EnableCap.ScissorTest);
		UI.ScissorRect(this.bounds.x0 + 0.01f, this.bounds.y0 + 0.01f, this.bounds.x1 - 0.01f, this.bounds.y1 - 0.06f);

		float yValue = upperBound + this.scroll;
		float buttonHeight = 0.05f;
		float buttonStride = buttonHeight + 0.01f;

		(int, int)? swapIndices = null;
		for (int i = 0; i < this.relevantConditional.conditions.Count; i++) {
			Condition condition = this.relevantConditional.conditions[i];
			Immediate.Color(Themes.Text);
			UI.font.Write($"{(condition.preProcessors.items.Count != 0 ? $"{condition.preProcessors} " : "")}{condition.timeline}", this.bounds.CenterX, yValue - buttonHeight * 0.5f, 0.03f, Font.Align.MiddleCenter);

			UVRect upButton = new(rightBound - buttonHeight * 2 - 0.01f, yValue - buttonHeight, rightBound - buttonHeight - 0.01f, yValue);
			if (UI.TextureButton(upButton.AtlasUV("ChevronUp"), new UI.TextureButtonMods() { disabled = i == 0 }))
				swapIndices = (i, i-1);
			UVRect downButton = new(rightBound - buttonHeight, yValue - buttonHeight, rightBound, yValue);
			if (UI.TextureButton(downButton.AtlasUV("ChevronDown"), new UI.TextureButtonMods() { disabled = i == this.relevantConditional.conditions.Count - 1 }))
				swapIndices = (i, i+1);

			yValue -= buttonStride;

			Rect timelineButtonRect = new(leftBound, yValue - buttonHeight, this.bounds.CenterX - 0.01f, yValue);
			if (UI.TextButton($"Edit Conditional", timelineButtonRect))
				condition.conditionPopup = (ConditionPopup)PopupManager.Add(new ConditionPopup(condition).SetButtons<ConditionPopup>("", "EXCLUSIVE", "HIDE").Title("Editing timelines"));

			Rect deleteButtonRect = new(this.bounds.CenterX + 0.01f, yValue - buttonHeight, rightBound, yValue);
			if (UI.TextButton($"Delete Condition", deleteButtonRect))
				PopupManager.Add(new ConfirmPopup("Delete condition?").SetButtons("Remove", "Keep").Okay(() => this.DeleteCondition(condition)));

			yValue -= buttonStride;

			Immediate.Color(Themes.Border);
			UI.Line(leftBound, yValue - 0.01f, rightBound, yValue - 0.01f);

			yValue -= 0.02f;
		}

		if (swapIndices != null)
			WorldWindow.worldHistory.Apply(new ListSwapChange<Condition>(this.relevantConditional.conditions, swapIndices.Value.Item1, swapIndices.Value.Item2));

		yValue -= 0.01f;

		Rect addNewButtonRect = new(leftBound, yValue - buttonHeight, rightBound, yValue);
		if (UI.TextButton("Add New Condition", addNewButtonRect)) {
			ConditionChange conditionChange = new(this.relevantConditional, true);
			conditionChange.AddCondition(new Condition(new(), Timeline.All));
			WorldWindow.worldHistory.Apply(conditionChange);
		}

		this.maxScroll = -(yValue - 0.09f - this.scroll);

		Program.gl.Disable(EnableCap.ScissorTest);
	}

	protected void DeleteCondition(Condition conditionToDelete) {
		ConditionChange conditionChange = new(this.relevantConditional, false);
		conditionChange.AddCondition(conditionToDelete);
		WorldWindow.worldHistory.Apply(conditionChange);
	}

	protected void Scroll(float x, float y) {
		if (!this.isHovered || this.collapsed) return;

		this.targetScroll -= y * 0.1f;
		this.ClampScroll();
	}

	public override void Close() {
		Main.Scroll -= this.Scroll;
		if (this.relevantConditional.conditionalPopup == this)
			this.relevantConditional.conditionalPopup = null;
		base.Close();
	}

	protected void ClampScroll() {
		float trueMaxScroll = this.maxScroll + this.bounds.y0;
		if (this.targetScroll >= trueMaxScroll) {
			this.targetScroll = trueMaxScroll;
			if (this.scroll >= trueMaxScroll - 0.03f)
				this.scroll = trueMaxScroll + 0.03f;
		}

		if (this.targetScroll < 0) {
			this.targetScroll = 0;
			if (this.scroll < 0.03f)
				this.scroll = -0.03f;
		}
	}
}