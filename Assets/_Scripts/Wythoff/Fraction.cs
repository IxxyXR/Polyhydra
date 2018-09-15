using System;

namespace Wythoff {
    public class Fraction {
    
        public long n;
        public long d;

        public Fraction(int n, int d) {
            this.n = n;
            this.d = d;
        }

        public Fraction(Fraction f) {
            n = f.n;
            d = f.d;
        }

        public Fraction() {
            // empty constructor
        }

        public Fraction(double x) {
            Fraction f = frac(x);
            n = f.n;
            d = f.d;
        }

        // Find the numerator and the denominator using the Euclidean algorithm.
    
        public Fraction frac(double x) {
            Fraction zero = new Fraction(0, 1), inf = new Fraction(1, 0);
            Fraction r0, r = new Fraction(zero), frax = new Fraction(inf);
            long f;
            double s = x;
            for (;;) {
                if (Math.Abs(s) > Double.MaxValue) {
                    return frax;
                }
                f = (long)Math.Floor(s);
                r0 = new Fraction(r);
                r = new Fraction(frax);
                frax.n = frax.n * f + r0.n;
                frax.d = frax.d * f + r0.d;
                if (x == (double) frax.n / (double) frax.d)
                    return frax;
                s = 1 / (s - f);
            }
        }

        public static long numerator(double x) {
            Fraction f = new Fraction().frac(x);
            return f.n;
        }

        public static long denominator(double x) {
            Fraction f = new Fraction().frac(x);
            return f.d;
        }

        public static double compl(double x) {
            Fraction f = new Fraction().frac(x);
            return (double) f.n / (f.n - f.d);
        }
    }
}