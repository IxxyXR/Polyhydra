using System;
using UnityEngine;

public class Fraction {
    
    private long n, d;

    public Fraction(int n, int d) {
        this.n = n;
        this.d = d;
    }

    public Fraction(Fraction f) {
        this.n = f.n;
        this.d = f.d;
    }

    public Fraction() {
        // empty constructor
    }

    public Fraction(double x) {
        Fraction f = frac(x);
        n = f.n;
        d = f.d;
    }

    public long getN() {
        return n;
    }

    public void setN(long n) {
        this.n = n;
    }

    public long getD() {
        return d;
    }

    public void setD(long d) {
        this.d = d;
    }

    /*
     * Find the numerator and the denominator using the Euclidean algorithm.
     */
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
            frax.setN(frax.getN() * f + r0.getN());
            frax.setD(frax.getD() * f + r0.getD());
            if (x == (double) frax.getN() / (double) frax.getD())
                return frax;
            s = 1 / (s - f);
        }
    }

    public static long numerator(double x) {
        Fraction f = new Fraction().frac(x);
        return f.getN();
    }

    public static long denominator(double x) {
        Fraction f = new Fraction().frac(x);
        return f.getD();
    }

    public static double compl(double x) {
        Fraction f = new Fraction().frac(x);
        return (double) f.getN() / (f.getN() - f.getD());
    }
}