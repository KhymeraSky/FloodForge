namespace FloodForge.Droplet;

public static class LevelUtils {
	public static List<Vector2i> Line(int x0, int y0, int x1, int y1) {
		List<Vector2i> points = [];

		int dx = Math.Abs(x1 - x0);
		int dy = Math.Abs(y1 - y0);
		int sx = (x0 < x1) ? 1 : -1;
		int sy = (y0 < y1) ? 1 : -1;
		int error = dx - dy;

		while (true) {
			points.Add(new Vector2i(x0, y0));

			if (x0 == x1 && y0 == y1)
				break;

			int e2 = 2 * error;
			if (e2 > -dy) {
				error -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				error += dx;
				y0 += sy;
			}
		}

		return points;
	}
}