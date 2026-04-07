namespace FloodForge.World;

public abstract class WorldObject {
    public Vector2 Position;
}

public class ReferenceImage : WorldObject {
    public new Vector2 Position;
    public string imagePath;
    public Texture image;
    public bool visible = true;
    public float Height => this.image.height * this.scale;
    public float Width => this.image.width * this.scale;
    public float scale;
    public Vector2 TopLeft;
    public Vector2 BottomRight;

    public ReferenceImage(string path) {
        if (!Path.Exists(path)) {
            throw new FileNotFoundException("Invalid reference image path!");
        }
        this.imagePath = path;
        this.image = Texture.Load(path, TextureWrapMode.ClampToBorder);
        this.scale = 1f;
        this.TopLeft = new Vector2(- this.Width, + this.Height);
        this.BottomRight = new Vector2(+ this.Width, - this.Height);
    }

    public void Draw() {
        if (Keys.JustPressed(Silk.NET.Input.Key.P)) {
            (this.TopLeft.y, this.BottomRight.y) = (this.BottomRight.y, this.TopLeft.y);
        }
        if (this.visible) {
            UI.CenteredTexture(this.image, this.Position.x, this.Position.y, this.Width * 2);

            if(WorldWindow.HoveringReferenceImage == this) {
                Immediate.Color(Themes.RoomBorderHighlight);
                UI.StrokeRect(new Rect(this.Position + this.TopLeft, this.Position + this.BottomRight));
            }
        }
    }

	public bool Inside(Vector2 pos) {
		return pos.x >= this.Position.x + this.TopLeft.x && pos.y >= this.Position.y + this.BottomRight.y && pos.x < this.Position.x + this.BottomRight.x && pos.y <= this.Position.y + this.TopLeft.y;
	}

	public bool Intersects(Vector2 from, Vector2 to) {
		Vector2 cornerMin = Vector2.Min(from, to);
		Vector2 cornerMax = Vector2.Max(from, to);

		return cornerMax.x >= this.Position.x + this.TopLeft.x && cornerMax.y >= this.Position.y + this.BottomRight.y && cornerMin.x < this.Position.x + this.BottomRight.x && cornerMin.y <= this.Position.y + this.TopLeft.y;
	}
}