using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{


    public class Frame
    {
        private Byte _header;
        private Byte _messageLength;
        public Byte _code { get; private set; }
        public Byte[] _message { get; private set; }

        public Byte _checkSum { get; private set; }
        public Frame(Byte messageLength, Byte code, Byte[] message)
        {
            InitHeader();
            _message = new byte[messageLength];
            _messageLength = messageLength;
            _message = message;
            _code = code;
            _checkSum = CalculateCheckSum();
        }

        public Frame()
        {
            InitHeader();
        }

        public int _frameLength
        {
            get
            {
                return Convert.ToInt16(_messageLength) + 4;
            }
            private set
            {

            }
        }

        private void InitHeader()
        {
            _header = 0x7D;
        }

        public Byte[] GetSerialNumberFrame()
        {
            _messageLength = 0x00;
            _code = 0x01;


            return GetFrame();
        }

        public Byte[] GetStatusFrame()
        {
            _messageLength = 0x00;
            _code = 0x02;


            return GetFrame();
        }

        public Byte[] GetSetIndexReadFrame(Byte[] message)
        {

            _messageLength = 0x02;
            _code = 0x03;
            _message = new byte[_messageLength];
            _message = message;


            return GetFrame();
        }


        public Byte[] GetDateTimeFrame()
        {

            _messageLength = 0x00;
            _code = 0x04;

            return GetFrame();
        }
        public Byte[] GetEnergyMeasureFrame()
        {

            _messageLength = 0x00;
            _code = 0x05;

            return GetFrame();
        }

        public Byte[] GetErrorFrame()
        {
            _messageLength = 0x00;
            _code = 0xFF;

            return GetFrame();
        }

        public Byte[] GetFrame()
        {
            List<Byte> listBytes = new List<byte>();
            listBytes.Add(_header);
            listBytes.Add(_messageLength);
            listBytes.Add(_code);
            if (_message != null)
            {
                foreach (Byte dataByte in _message)
                {
                    listBytes.Add(dataByte);
                }
            }
            _checkSum = CalculateCheckSum();
            listBytes.Add(_checkSum);
            return listBytes.ToArray();
        }



        public Frame Parse(Byte[] frameBytes)
        {
            Frame frame = new Frame();
            frame._header = frameBytes[0];
            frame._messageLength = frameBytes[1];
            frame._code = frameBytes[2];
            if (frame._messageLength > 0)
            {
                frame._message = new byte[frame._messageLength];
            }
            for (int i = 0; i < frame._messageLength; i++)
            {
                if (frameBytes[i + 3] != 0)
                {
                    frame._message[i] = frameBytes[i + 3];
                }
            }

            frame._checkSum = frameBytes[frame._messageLength + 3];
            //Array.Resize(ref );

            return frame;
        }

        public Boolean IsServerCode
        {
            get
            {
                Boolean returnValue = Convert.ToBoolean((0x10000000 & _code));
                return returnValue;
            }
        }

        public Boolean IsClientCode
        {
            get
            {
                return !IsServerCode;
            }
        }

        public Byte GetCode()
        {
            var returnValue = _code;
            return returnValue;
        }

        public Byte CalculateCheckSum()
        {
            Byte checkSum = _messageLength;
            checkSum ^= _code;
            if (_message != null)
            {
                foreach (Byte dataByte in _message)
                {
                    checkSum ^= dataByte;
                }
            }
            return checkSum;
        }
    }
}
