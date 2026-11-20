using FloodForge.World;

namespace FloodForge.History;

public class PreProcessorChange : Change {
	protected List<(bool, PreProcessors)> preProcessors = [];
	protected string preProcessorString;
	protected bool enabled;

	public PreProcessorChange(bool enabled, string preProcessorString) {
		this.enabled = enabled;
		this.preProcessorString = preProcessorString;
	}

	public void AddPreProcessors(params PreProcessors[] relevantPreProcessors) {
		foreach (PreProcessors item in relevantPreProcessors)
			this.preProcessors.Add((item.items.Contains(this.preProcessorString), item));
	}

	public override void Undo() {
		foreach ((bool undoValue, PreProcessors item) in this.preProcessors) {
			if (undoValue != this.enabled) {
				if (undoValue)
					item.items.Add(this.preProcessorString);
				else
					item.items.Remove(this.preProcessorString);
			}
		}
	}

	public override void Redo() {
		foreach ((bool undoValue, PreProcessors item) in this.preProcessors) {
			if (undoValue != this.enabled) {
				if (this.enabled)
					item.items.Add(this.preProcessorString);
				else
					item.items.Remove(this.preProcessorString);
			}
		}
	}
}