using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Pike
{
    class FF_addikey
    {
        public uint sd;
        public int dis1;
        public int dis2;
        public int index;
        public int carry;
        public uint[] buffer = new uint[64];
    }

    class FF_pike
    {
        public uint sd;
        public int index;
        public FF_addikey[] addikey = new FF_addikey[3] { new FF_addikey(), new FF_addikey(), new FF_addikey() };
        public byte[] buffer = new byte[4096];
    }

    const uint GENIUS_NUMBER = 0x05027919;
    FF_pike ctx;

    public Pike(uint key)
    {
        ctx = NewCtx(key);
    }

    public void Codec(ref byte[] data, int offset = 0)
    {
        Codec(ctx, ref data, offset);
    }

    void Codec(FF_pike ctx, ref byte[] data, int offset = 0)
    {
        if (data.Length - offset <= 0)
            return;

        unsafe
        {
            fixed (byte* pDataBase_ = data, pSequenceBase = ctx.buffer)
            {
                int len = data.Length - offset;
                byte* pDataBase = pDataBase_ + offset;
                byte* pData = pDataBase;
                while (true)
                {
                    int n = 4096 - ctx.index;
                    if (n <= 0)
                    {
                        _Generate(ctx);
                        continue;
                    }

                    if (n > len - (int)(pData - pDataBase))
                        n = len - (int)(pData - pDataBase);

                    byte* pSequence = pSequenceBase + ctx.index;
                    int w = n / sizeof(uint);
                    if (w > 0)
                    {
                        uint* dx = (uint*)pData;
                        uint* ax = (uint*)pSequence;
                        for (int i = 0; i < w; ++i)
                        {
                            *dx ^= *ax;
                            ++dx;
                            ++ax;
                        }
                    }
                    pData     += w * sizeof(uint);
                    pSequence += w * sizeof(uint);

                    for (int i = (n - n % sizeof(uint)); i < n; ++i)
                    {
                        *pData ^= *pSequence;
                        ++pData;
                        ++pSequence;
                    }

                    ctx.index += n;
                    if (pData - pDataBase == len)
                        break;
                }
            }
        }
    }

    FF_pike NewCtx(uint sd)
    {
        FF_pike ctx = new FF_pike();
        ctx.sd = sd ^ GENIUS_NUMBER;

        ctx.addikey[0].sd = ctx.sd;
	    ctx.addikey[0].sd = Linearity(ctx.addikey[0].sd);
	    ctx.addikey[0].dis1 = 55;
	    ctx.addikey[0].dis2 = 24;

        ctx.addikey[1].sd = ((ctx.sd & 0xAAAAAAAA) >> 1) | ((ctx.sd & 0x55555555) << 1);
	    ctx.addikey[1].sd = Linearity(ctx.addikey[1].sd);
	    ctx.addikey[1].dis1 = 57;
	    ctx.addikey[1].dis2 = 7;

	    ctx.addikey[2].sd = ~(((ctx.sd & 0xF0F0F0F0) >> 4) | ((ctx.sd & 0x0F0F0F0F) << 4));
	    ctx.addikey[2].sd = Linearity(ctx.addikey[2].sd);
	    ctx.addikey[2].dis1 = 58;
	    ctx.addikey[2].dis2 = 19;

        for (int i = 0; i < 3; ++i)
        {
            uint tmp = ctx.addikey[i].sd;
            for (int j = 0; j < 64; ++j)
            {
                for (int k = 0; k < 32; ++k)
                    tmp = Linearity(tmp);
                ctx.addikey[i].buffer[j] = tmp;
            }
            ctx.addikey[i].carry = 0;
		    ctx.addikey[i].index = 63;
        }
        ctx.index = 4096;
        return ctx;
    }

    uint Linearity(uint key)
    {
        return ((((key >> 31) ^ (key >> 6) ^ (key >> 4) ^ (key >> 2) ^ (key >> 1) ^ key) & 0x00000001) << 31) | (key >> 1);
    }

    void _AddikeyNext(FF_addikey addikey)
    {
        int tmp = addikey.index + 1;
        addikey.index = tmp & 0x03F;

        int i1 = ((addikey.index | 0x40) - addikey.dis1) & 0x03F;
        int i2 = ((addikey.index | 0x40) - addikey.dis2) & 0x03F;

        addikey.buffer[addikey.index] = addikey.buffer[i1] + addikey.buffer[i2];
        if (addikey.buffer[addikey.index] < addikey.buffer[i1] || addikey.buffer[addikey.index] < addikey.buffer[i2]) 
	        addikey.carry = 1;
        else 
	        addikey.carry = 0;
    }

    void _Generate(FF_pike ctx)
    {
        for (int i = 0; i < 1024; ++i)
        {
            int carry = ctx.addikey[0].carry + ctx.addikey[1].carry + ctx.addikey[2].carry;

            if (carry == 0 || carry == 3)
            {
                _AddikeyNext(ctx.addikey[0]);
		        _AddikeyNext(ctx.addikey[1]);
		        _AddikeyNext(ctx.addikey[2]);
            }
            else
            {
                int flag = 0;
                if (carry == 2)
                    flag = 1;

                for (int j = 0; j < 3; ++j)
                {
                    if (ctx.addikey[j].carry == flag)
                        _AddikeyNext(ctx.addikey[j]);
                }
            }

            uint tmp = ctx.addikey[0].buffer[ctx.addikey[0].index] ^ ctx.addikey[1].buffer[ctx.addikey[1].index] 
                ^ ctx.addikey[2].buffer[ctx.addikey[2].index];
            int base_ = i << 2;
            ctx.buffer[base_]   = (byte) tmp;
		    ctx.buffer[base_+1] = (byte) (tmp >> 8);
		    ctx.buffer[base_+2] = (byte) (tmp >> 16);
		    ctx.buffer[base_+3] = (byte) (tmp >> 24);
        }
        ctx.index = 0;
    }
}
