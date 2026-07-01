namespace FloodForge.History;

public class MassChange : Change {
	readonly Change[] changes;
	readonly bool reverseOnUndo;
	public MassChange(Change[] changes, bool reverseOnUndo = true) {
		this.changes = changes;
		this.reverseOnUndo = reverseOnUndo;
	}

	public override void Redo() {
		foreach (Change change in this.changes) {
			change.Redo();
		}
	}

	public override void Undo() {
		foreach (Change change in this.reverseOnUndo ? this.changes.Reverse() : this.changes) {
			change.Undo();
		}
	}

	public int GetCount() {
		return this.changes.Length;
	}
}