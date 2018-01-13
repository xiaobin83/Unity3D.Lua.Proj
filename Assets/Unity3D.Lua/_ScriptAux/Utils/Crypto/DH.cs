using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DH
{
    const long DH1BASE  = 3;
    const long DH1PRIME = 0x7FFFFFC3;
    static Random rand = new Random();

    public static void DHExchange(out long secret, out long modpower)
    {
        secret   = LongRandom();
        modpower = Montgomery(DH1BASE, secret, DH1PRIME);
    }

    public static long DHKey(long secret, long modpower)
    {
        long key = Montgomery(modpower, secret, DH1PRIME);
        return key;
    }

    static long LongRandom()
    {
        int  left  = rand.Next(1, int.MaxValue);
        int  right = rand.Next(0, int.MaxValue);
        long ret   = ((long) left << 32) + right;
        return ret;
    }

    // http://blog.csdn.net/linraise/article/details/17490769
    static long Montgomery(long base_, long exp, long mod)
    {
        long res = 1;
        while (exp != 0)
        {
            if ((exp & 1) > 0)
                res = (res * base_) % mod;
            exp >>= 1;
            base_ = (base_ * base_) % mod;
        }
        return res;
    }
}
