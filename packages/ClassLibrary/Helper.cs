using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    static class Helper
    {
        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[bits.Length / 8];
            bits.CopyTo(ret, 0);
            return ret;
        }

        public static BitArray Append(this BitArray current, BitArray after)
        {
            var bools = new bool[current.Count + after.Count];
            current.CopyTo(bools, 0);
            after.CopyTo(bools, current.Count);
            return new BitArray(bools);
        }

        public static byte[] ToByteArray(this BitArray bits)
        {
            int numBytes = bits.Count / 8;
            if (bits.Count % 8 != 0) numBytes++;

            byte[] bytes = new byte[numBytes];
            int byteIndex = 0, bitIndex = 0;

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }



            return bytes;
        }

        public static Byte[] Trim(this byte[] bytes)
        {
            List<byte> byteList = new List<byte>();
            for (int i = 0; i < bytes[2] + 4; i++)
            {
                byteList.Add(bytes[i]);
            }
            return byteList.ToArray();
        }


        public static Int32 ToInt32(this BitArray bits)
        {
            Int32 returnValue = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                    returnValue += (Int32)(Math.Pow(2, (bits.Length - 1 - i)));
            }
            return returnValue;
        }

        public static BitArray SliceBits(byte[] source, int index, int length, BitArray appendTo = null, int appendToIndex = 0)
        {
            int bytetIndex = index % 8;
            BitArray tempArray = new BitArray(length);
            byte mask = 0x80;
            int count_i = 0;
            for (int i = index; i < length + index; i++)
            {
                if (index + count_i == 8)
                    bytetIndex = 0;
                mask = (byte)(0x80 >> bytetIndex);
                tempArray[i - index] = Convert.ToBoolean(source[(count_i + index) / 8] & mask);
                bytetIndex++;
                count_i++;
            }
            if (appendTo != null)
            {
                for (int i = 0; i < length; i++)
                {
                    appendTo[appendToIndex + i] = tempArray[i];
                }
                return appendTo;
            }
            return tempArray;
        }

        public static float ConvertToFloatIEEE754Simple(byte[] bytes)
        {

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToSingle(bytes, 0);

        }
        public static byte[] ConvertFromFloatIEEE754Simples(float value)
        {
            byte[] bytes = BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }



        public static DateTime ExtractDateTime(byte[] message)
        {

            BitArray yearBitArray = new BitArray(12);
            yearBitArray = Helper.SliceBits(message, 0, 12);

            BitArray monthBitArray = new BitArray(4);
            monthBitArray = Helper.SliceBits(message, 12, 4);

            BitArray dayBitArray = new BitArray(5);
            dayBitArray = Helper.SliceBits(message, 16, 5);

            BitArray hourBitArray = new BitArray(5);
            hourBitArray = Helper.SliceBits(message, 21, 5);

            BitArray minutesBitArray = new BitArray(6);
            minutesBitArray = Helper.SliceBits(message, 26, 6);

            BitArray secondsBitArray = new BitArray(6);
            secondsBitArray = Helper.SliceBits(message, 32, 6);


            return new DateTime( yearBitArray.ToInt32(), monthBitArray.ToInt32(), dayBitArray.ToInt32(), hourBitArray.ToInt32(), minutesBitArray.ToInt32(), secondsBitArray.ToInt32());
        }
    }



}
