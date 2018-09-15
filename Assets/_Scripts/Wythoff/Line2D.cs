using System;

namespace Wythoff {
    public class Line2D
    {
        private double tg;
        private double x;
        private double y;

        public Line2D(Line2D l) {
            x = l.x;
            y = l.y;
        }

        public bool Equals(Line2D p) {
            return x == p.x && y == p.y;
        }

        public bool Parallel(Line2D l) {
        
            if (Double.IsNaN(x) && Double.IsNaN(l.x) || Double.IsNaN(y) && Double.IsNaN(l.y)) {
                return true;
            } else {
                return (tg == l.tg);
            }
        }

        public Line2D(Point2D a1, Point2D a2) {
        
            if (a1 == a2) {
                throw new SystemException("Невозможно построить прямую через одну точку");
            }

            if (a1.x == a2.x) {
                x = a1.x;
                y = Double.NaN;
                tg = Double.NaN;
            }
            else if (a1.y == a2.y) {
                x = Double.NaN;
                y = a1.y;
                tg = 0;
            } else {
                double a;
                a = (a1.y - a2.y) / (a1.x - a2.x);
                y = a1.y - a * a1.x;
                tg = a;
                x = -y / a;
            }
        }

        public Point2D Intercept(Line2D l) {
            if (Parallel(l)) {
                throw new SystemException("Параллельные прямые не пересекаются");
            }

            if (Double.IsNaN(x)) {
                if (Double.IsNaN(l.y)) {
                    return new Point2D(l.x, y);
                } else {
                    return new Point2D((y - l.y) / l.tg, y);
                }
            
            } else if (Double.IsNaN(y)) {
         
                if (Double.IsNaN(l.x)) {
                    return new Point2D(x, l.y);
                } else {
                    return new Point2D(x, l.tg * x + l.y);
                }
            
            } else {
            
                if (l.x == Double.NaN || l.y == Double.NaN) {
                    return l.Intercept(this);
                } else {
                    double y0 = (l.y - tg * l.tg * y) / (1 - tg * l.tg);
                    return new Point2D((tg * y0 + l.y), y0);
                }
            }
        }

        public static Point2D Intercept(Line2D l1, Line2D l2) { return l1.Intercept(l2);}

        public double GetX(double y) {
            return (y - this.y)/tg;
        }
    
        public double GetY(double x) {
            return tg*x + y;
        }
    }
}