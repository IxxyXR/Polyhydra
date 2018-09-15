namespace Wythoff {
    public class Point2D {
    
        public double x, y;

        public Point2D(double x, double y) {
            this.x = x;
            this.y = y;
        }

        public Point2D(Point2D p) {
            x = p.x;
            y = p.y;
        }

        public bool Equals(Point2D p) {
            return x == p.x && y == p.y;
        }
    }
}