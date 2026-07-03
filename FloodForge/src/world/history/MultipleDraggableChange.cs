using FloodForge.World;

namespace FloodForge.History;

public abstract class MultipleDraggableChange : Change {
	protected readonly List<IWorldDraggable> draggables = [];

	public virtual void AddDraggable(IWorldDraggable draggable) {
		this.draggables.Add(draggable);
	}
}