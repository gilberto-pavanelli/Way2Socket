using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class MeasureData
    {
        public Int32 Index { get; set; }
        public DateTime DateTime { get; set; }
        public float Value { get; set; }

        public BitArray GetDateTimeMessage()
        {
            BitArray mesageBitArray = new BitArray(40);
            int index = 0;

            byte[] tempYear = BitConverter.GetBytes((UInt16)this.DateTime.Year);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tempYear);

            mesageBitArray = Helper.SliceBits(tempYear, 4, 12, mesageBitArray, index);
            index += 12;

            byte[] tempMonth = BitConverter.GetBytes((UInt16)this.DateTime.Month);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tempMonth);
            mesageBitArray = Helper.SliceBits(tempMonth, 12, 4, mesageBitArray, index);
            index += 4;

            byte[] tempDay = BitConverter.GetBytes((UInt16)this.DateTime.Day);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tempDay);
            mesageBitArray = Helper.SliceBits(tempDay, 11, 5, mesageBitArray, index);
            index += 5;

            byte[] tempHour = BitConverter.GetBytes((UInt16)this.DateTime.Hour);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tempHour);
            mesageBitArray = Helper.SliceBits(tempHour, 11, 5, mesageBitArray, index);
            index += 5;
            byte[] tempMinutes = BitConverter.GetBytes((UInt16)this.DateTime.Minute);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tempMinutes);
            mesageBitArray = Helper.SliceBits(tempMinutes, 10, 6, mesageBitArray, index);
            index += 6;

            byte[] tempSeconds = BitConverter.GetBytes((UInt16)this.DateTime.Second);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tempSeconds);
            mesageBitArray = Helper.SliceBits(tempSeconds, 10, 6, mesageBitArray, index);
            index += 6;
            return mesageBitArray;
        }

        public byte[] GetMeasureDataMessage()
        {
            byte[] mesageByte = new byte[4];

            mesageByte = Helper.ConvertFromFloatIEEE754Simples(this.Value);

            return mesageByte;
        }

    }

    public class Job
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public ushort StartIndex { get; set; }
        public ushort FinalIndex { get; set; }
        public Boolean Success { get; set; }
        public int Attempts { get; set; }
        public int LastAttemptDateTime { get; set; }
    }
}
