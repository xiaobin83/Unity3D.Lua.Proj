using System;

public class NetPacket
{
    const int PACKET_LIMIT = 32767;
    const int INIT_LEN = 2048;

    public	int pos;
    public 	byte[] data;

    public NetPacket(int len = INIT_LEN)
    {
        pos = 0;
        data = new byte[len];
    }

    public byte[] Data(){
	    return data;
    }

    public bool EOF()
    {
        return pos >= data.Length;
    }

    //=============================================== Readers
    public bool ReadBool(ref bool ret) {
        bool ok = true;

        byte _ret = 0;
        ok = ReadByte(ref _ret);

        if (_ret == 1)
        {
            ret = true;
        }
        else
        {
            ret = false;
        }

        return ok;
    }

    public bool ReadByte(ref byte ret) {
        bool ok = true;

        if (pos >= data.Length)
        {
		    //err = errors.New("read byte failed");
            ok = false;
		    return ok;
	    }

	    ret = data[pos];
	    pos++;

	    return ok;
    }

    public bool ReadBytes(ref byte[] ret) {
        bool ok = true;

	    if (pos+2 > data.Length ) {
		    ok = false; //err = errors.New("read bytes header failed")
		    return ok;
	    }

        ushort size = 0;
	    ok = ReadU16(ref size);
        if (!ok) return false;

	    if (pos+(int)(size) > data.Length  ) {
		    ok = false; //err = errors.New("read bytes data failed")
		    return ok;
	    }

        ret = new byte[size];
        bytesCopy(data, ref ret, pos, size);

        pos += (short)(size);

	    return ok;
    }

    private void bytesCopy(byte[] from, ref byte[] to, int start, int len)
    {
        for (int i=0; i<len; i++)
        {
            to[i] = from[start+i];
        }
    }

    public bool ReadString(ref string ret) {
        bool ok = true;

	    if (pos+2 > data.Length  ) {
		    ok = false; //err = errors.New("read string header failed")
		    return ok;
	    }

        ushort size = 0;
	    ok = ReadU16(ref size);
        if (!ok) return false;

        if (pos+(int)(size) > data.Length  ) {
		    ok = false; //err = errors.New("read string data failed")
		    return ok;
	    }

        byte[] bytes = new byte[size];

        bytesCopy(data, ref bytes, pos, size);

	    pos += (short)(size);
        ret = System.Text.Encoding.UTF8.GetString ( bytes );

	    return ok;
    }

    public bool ReadU16(ref ushort ret) {
        bool ok = true;

	    if (pos+2 > data.Length  ) {
		    ok = false; //err = errors.New("read uint16 failed")
		    return ok;
	    }

        byte[] buf = new byte[2];
        bytesCopy(data, ref buf, pos, 2);
	    ret = (ushort)((ushort)(buf[0])<<8 | (ushort)(buf[1]));
	    pos += 2;
	    return ok;
    }

    public bool ReadS16(ref short ret) {
        bool ok = true;

        ushort _ret = 0;

	    ok = ReadU16(ref _ret);

	    ret = (short)(_ret);

	    return ok;
    }

    public bool ReadU24(ref uint ret) {
        bool ok = true;

	    if (pos+3 > data.Length  ) {
		    ok = false; //err = errors.New("read uint24 failed")
		    return ok;
	    }

        byte[] buf = new byte[3];
        bytesCopy(data, ref buf, pos, 3);

	    ret = (uint)(buf[0])<<16 | (uint)(buf[1])<<8 | (uint)(buf[2]);
	    pos += 3;

	    return ok;
    }

    public bool ReadS24(ref int ret) {
        bool ok = true;

	    uint _ret = 0;
        ok = ReadU24(ref _ret);

	    ret = (int)(_ret);

        return ok;
    }

    public bool ReadU32(ref uint ret) {
        bool ok = true;

	    if (pos+4 > data.Length  ) {
		    ok = false; //err = errors.New("read uint32 failed")
		    return ok;
	    }

        byte[] buf = new byte[4];
        bytesCopy(data, ref buf, pos, 4);

	    ret = (uint)(buf[0])<<24 | (uint)(buf[1])<<16 | (uint)(buf[2])<<8 | (uint)(buf[3]);
	    pos += 4;
	    return ok;
    }

    public bool ReadS32(ref int ret) {
        bool ok = true;

	    uint _ret = 0;
        ok = ReadU32(ref _ret);
	    ret = (int)(_ret);
	    return ok;
    }

    public bool ReadU64(ref ulong ret) {
        bool ok = true;

	    if (pos+8 > data.Length  ) {
		    ok = false; //err = errors.New("read uint64 failed")
		    return ok;
	    }

	    ret = 0;
        byte[] buf = new byte[8];
        bytesCopy(data, ref buf, pos, 8);

	    for (int i=0; i<8; i++) {
		    ret |= (ulong)(buf[i]) << (ushort)((7-i)*8);
	    }

	    pos += 8;
	    return ok;
    }

    public bool ReadS64(ref long ret) {
        bool ok = true;

        ulong _ret = 0;
        ok = ReadU64(ref _ret);
	    ret = (long)(_ret);
	    return ok;
    }

    public bool ReadFloat32(ref float ret) {
        bool ok = true;
        ret = 0;
        uint bits = 0;
	    ok = ReadU32(ref bits);
	    if (!ok) {
		    return false;
	    }

        ret = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
	    return ok;
    }

    public bool ReadFloat64(ref double ret) {
        bool ok = true;
        ret = 0;
        ulong bits = 0;
	    ok = ReadU64(ref bits);
	    if (!ok) {
		    return false;
	    }

        ret = BitConverter.ToDouble(BitConverter.GetBytes(bits), 0);
	    return ok;
    }

    //================================================ Writers
    private void Append(byte v)
    {
        if (pos + 1 >= data.Length)
            Extend();
        data[pos] = v;
        pos++;
    }

    private void Append(byte[] v)
    {
        Append(v, 0);
    }

    private void Append(byte[] v, int index)
    {
        while (pos + v.Length - index > data.Length)
            Extend();
        Array.Copy(v, index, data, pos, v.Length - index);
        pos += (short) (v.Length - index);
    }

    private void Extend()
    {
        if (this.data.Length + INIT_LEN > PACKET_LIMIT)
            throw new Exception("NetPacket out of limit size.");
        byte[] data = new byte[this.data.Length + INIT_LEN];
        Array.Copy(this.data, 0, data, 0, pos);
        this.data = data;
    }

    public void WriteBool(bool v) {
	    if (v) {
		    Append((byte)(1));
	    } else {
		    Append((byte)(0));
	    }
    }

    public void WriteByte(byte v) {
	    Append(v);
    }

    public void WriteBytes(byte[] v) {
	    Append(v);
    }

    public void WriteBytes(byte[] v, int index) {
	    Append(v, index);
    }

    public void WriteString(string v) {
        if (v == null)
        {
            v = "";
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes (v);
	    WriteU16((ushort)(bytes.Length));
	    Append( bytes);
    }

    public void WriteU16(ushort v) {
        byte[] bytes = new byte[2];
        bytes[0] = (byte)(v >> 8);
        bytes[1] = (byte)v;
        Append(bytes);
    }

    public void WriteS16(short v) {
	    WriteU16((ushort)(v));
    }

    public void WriteU24(uint v) {
        byte[] bytes = new byte[3];
        bytes[0] = (byte)(v >> 16);
        bytes[1] = (byte)(v >> 8);
        bytes[2] = (byte)v;
        Append(bytes);
    }

    public void WriteU32(uint v) {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)(v >> 24);
        bytes[1] = (byte)(v >> 16);
        bytes[2] = (byte)(v >> 8);
        bytes[3] = (byte)v;
        Append(bytes);
    }

    public void WriteS32(int v) {
	    WriteU32((uint)(v));
    }

    public void WriteU64(ulong v) {
        byte[] bytes = new byte[8];
        bytes[0] = (byte)(v >> 56);
        bytes[1] = (byte)(v >> 48);
        bytes[2] = (byte)(v >> 40);
        bytes[3] = (byte)(v >> 32);
        bytes[4] = (byte)(v >> 24);
        bytes[5] = (byte)(v >> 16);
        bytes[6] = (byte)(v >> 8);
        bytes[7] = (byte)v;
        Append(bytes);
    }

    public void WriteS64(long v) {
	    WriteU64((ulong)(v));
    }

    public void WriteFloat32(float f) {
        UInt32 v = BitConverter.ToUInt32(BitConverter.GetBytes(f), 0);
        WriteU32(v);
    }

    public void WriteFloat64(double f) {
        UInt64 v = BitConverter.ToUInt64(BitConverter.GetBytes(f), 0);
        WriteU64(v);
    }
}