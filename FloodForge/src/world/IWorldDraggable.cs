namespace FloodForge.World;

public interface IWorldDraggable {
	public bool Visible => true;

	public bool Draggable => this.Visible;

	public Vector2 Position {
		get {
			return new();
		}
		set {
			
		}
	}

	// currently unused
	public virtual Vector2 Size {
		get {
			return new();
		}
		set {
			
		}
	}

	public abstract bool Inside(Vector2 pos);
}