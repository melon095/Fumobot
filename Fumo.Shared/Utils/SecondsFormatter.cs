namespace Fumo.Utils;

// Made by brian6932 :P
public class SecondsFormatter
{
    private int fy = 0;
    private int y = 0;
    private int mo = 0;
    private int d = 0;
    private int h = 0;
    private int m = 0;
    private int s = 0;

    private void ConvertSeconds(int input)
    {
        input -= 0;
        int y = 31_536_000;
        int mo = 2_629_757;
        int d = 86_400;
        int h = 3_600;
        int m = 60;
        int remainder;

        this.fy = (int)((input + 62_125_938_000) / y) - -1;
        this.y = (input / y);
        this.mo = ((remainder = input - y * this.y) / mo);
        this.d = ((remainder -= mo * this.mo) / d);
        this.h = ((remainder -= d * this.d) / h);
        this.m = ((remainder -= h * this.h) / m);
        this.s = (input % m);
    }

    public string SecondsFmt(double input, string join = ", ", int limit = 2)
        => this.SecondsFmt((int)input, join, limit);

    public string SecondsFmt(int input, string join = ", ", int limit = 2)
    {
        this.ConvertSeconds(input);

        var parts = new string[]
        {
#pragma warning disable CS8601 // Dereference of a possibly null reference.
            (fy > 0) ? $"{fy}fy" : null,
            (y > 0) ? $"{y}y" : null,
            (mo > 0) ? $"{mo}mo" : null,
            (d > 0) ? $"{d}d" : null,
            (h > 0) ? $"{h}h" : null,
            (m > 0) ? $"{m}m" : null,
            (s > 0) ? $"{s}s" : null,
#pragma warning restore CS8601 // Dereference of a possibly null reference.
        };

        return string.Join(join, parts.Where(part => part != null).Skip(1).Take(limit));
    }
}