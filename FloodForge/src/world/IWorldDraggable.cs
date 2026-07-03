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
}