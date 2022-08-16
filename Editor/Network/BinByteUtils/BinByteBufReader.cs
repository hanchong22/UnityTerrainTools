using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static class DataUtils
{
    public static bool AssertTrue(bool asset)
    {
        if (!asset)
        {
            UnityEngine.Debug.LogError("AssertTrue Data Error");
        }
        return asset;
    }
}

public class BinByteBufReader : IDisposable
{
    private byte[] buffer;
    private Int32 iBufSize;
    private Int32 iCurrentPos;
    private Int32 iStartPos;

    public BinByteBufReader()
    {
        buffer = null;
        iBufSize = 0;
        iCurrentPos = 0;
        iStartPos = 0;
    }

    public void Init(byte[] bufferNew, Int32 iBufSizeNew, Int32 iReadPosFromRaw)
    {
        buffer = bufferNew;
        iBufSize = iBufSizeNew;
        iCurrentPos = iReadPosFromRaw;
        iStartPos = iReadPosFromRaw;
    }

    public byte[] GetBuffer()
    {
        return buffer;
    }

    public bool TryReadBytes(byte[] bytes, Int32 iLen)
    {
        int iLenLeft = iBufSize - iCurrentPos;
        if (!DataUtils.AssertTrue(iLen >= 0)) return false;
        if (!DataUtils.AssertTrue(iLen <= bytes.Length)) return false;

        if (!DataUtils.AssertTrue(iLenLeft >= iLen)) return false;

        for (Int32 k = 0; k < iLen; k++)
        {
            bytes[k] = buffer[iCurrentPos++];
        }
        return true;
    }

    public bool TryReadBool(out bool val)
    {
        val = false;

        if (!this.TryReadByte(out byte by))
        {
            return false;
        }

        val = (by != 0);
        return true;
    }

    public bool TryReadByte(out byte val)
    {
        val = 0;
        int iLenLeft = iBufSize - iCurrentPos;
        if (!DataUtils.AssertTrue(iLenLeft >= 1)) return false;

        val = buffer[iCurrentPos++];

        return true;
    }

    public byte ReadByte()
    {
        if (this.TryReadByte(out byte value))
        {
            return value;
        }

        return 0;
    }

    public Int32 GetCurrentPos()
    {
        return iCurrentPos;
    }

    public bool TryReadString(out string val)
    {
        val = string.Empty;
        if (!this.TryReadInt32(out int len))
            return false;

        if (len <= 0)
        {
            return false;
        }

        if (!DataUtils.AssertTrue(iCurrentPos + len <= GetBuffer().Length)) return false;

        UTF8Encoding decoding = new UTF8Encoding();
        val = decoding.GetString(GetBuffer(), iCurrentPos, len);

        iCurrentPos += len;
        return true;
    }

    public string ReadString()
    {
        if (this.TryReadString(out string value))
        {
            return value;
        }

        return string.Empty;
    }

    public Int32 GetDataLeft()
    {
        if (null == buffer)
            return 0;
        if (!DataUtils.AssertTrue(iCurrentPos <= iBufSize)) return 0;
        return iBufSize - iCurrentPos;
    }

    public bool TryReadInt16(out Int16 val)
    {
        val = 0;
        Int32 iBufLeft = GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(Int16))) return false;
        byte[] pbDest = new byte[sizeof(Int16)];
        for (i = 0; i < sizeof(Int16); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }

        val = BitConverter.ToInt16(pbDest, 0);

        return true;

    }

    public Int16 ReadInt16()
    {
        if (this.TryReadInt16(out Int16 value))
        {
            return value;
        }

        return 0;
    }

    public bool TryReadUInt16(out UInt16 val)
    {
        val = 0;
        Int32 iBufLeft = this.GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(UInt16))) return false;

        byte[] pbDest = new byte[sizeof(UInt16)];
        for (i = 0; i < sizeof(UInt16); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }

        val = BitConverter.ToUInt16(pbDest, 0);

        return true;

    }

    public UInt16 ReadUInt16()
    {
        if (this.TryReadUInt16(out UInt16 value))
        {
            return value;
        }

        return 0;
    }

    public bool TryReadInt32(out Int32 val)
    {
        val = 0;
        Int32 iBufLeft = this.GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(Int32))) return false;
        byte[] pbDest = new byte[sizeof(Int32)];
        for (i = 0; i < sizeof(Int32); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }
        val = BitConverter.ToInt32(pbDest, 0);

        return true;
    }

    public Int32 ReadInt32()
    {
        if (this.TryReadInt32(out Int32 value))
            return value;
        return 0;
    }

    public bool TryReadInt64(out Int64 val)
    {
        val = 0;
        Int32 iBufLeft = GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(Int64))) return false;

        byte[] pbDest = new byte[sizeof(Int64)];
        for (i = 0; i < sizeof(Int64); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }
        val = BitConverter.ToInt64(pbDest, 0);

        return true;
    }

    public Int64 ReadInt64()
    {
        if (this.TryReadInt64(out Int64 value))
            return value;

        return 0;
    }

    public bool TryReadUInt64(out UInt64 val)
    {
        val = 0;
        Int32 iBufLeft = GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(UInt64))) return false;
        byte[] pbDest = new byte[sizeof(UInt64)];
        for (i = 0; i < sizeof(UInt64); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }

        val = BitConverter.ToUInt64(pbDest, 0);

        return true;
    }

    public UInt64 ReadUInt64()
    {
        if (this.TryReadUInt64(out UInt64 value))
        {
            return value;
        }

        return 0;
    }

    public bool TryReadUInt32(out UInt32 val)
    {
        val = 0;
        Int32 iBufLeft = GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(UInt32))) return false;
        byte[] pbDest = new byte[sizeof(UInt32)];
        for (i = 0; i < sizeof(Int32); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }

        val = BitConverter.ToUInt32(pbDest, 0);

        return true;
    }

    public UInt32 ReadUInt32()
    {
        if (this.TryReadUInt32(out UInt32 value))
            return value;
        return 0;
    }

    public bool TryReadFloat(out float val)
    {
        val = 0f;
        Int32 iBufLeft = GetDataLeft();
        if (!DataUtils.AssertTrue(iBufLeft > 0)) return false;

        Int32 i = 0;

        if (!DataUtils.AssertTrue(iBufLeft >= sizeof(float))) return false;
        byte[] pbDest = new byte[sizeof(float)];
        for (i = 0; i < sizeof(float); i++)
        {
            pbDest[i] = buffer[iCurrentPos++];
        }

        val = BitConverter.ToSingle(pbDest, 0);

        return true;
    }

    public float ReadFloat()
    {
        if (this.TryReadFloat(out float value))
            return value;
        return 0f;
    }

    public float[] ReadPath()
    {
        UInt32 uCnt = 0;
        if (!this.TryReadUInt32(out uCnt))
        {
            return null;
        }

        float[] pos_arr = new float[uCnt];
        for (UInt32 k = 0; k < uCnt; k++)
        {
            if (!this.TryReadFloat(out pos_arr[k]))
            {
                pos_arr = null;
                return null;
            }
        }

        return pos_arr;
    }

    public Int32 GetReadPos()
    {
        return iCurrentPos;
    }


    public void Dispose()
    {
        buffer = null;
        iBufSize = 0;
        iCurrentPos = 0;
        iStartPos = 0;
    }
}

