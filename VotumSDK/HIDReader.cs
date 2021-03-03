using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net.Windows;
using ProtoBuf;

namespace Votum
{
    public class HIDReader
    {
        #region FieldsAndOther
        private static readonly DebugLogger Logger = new DebugLogger();
        private static readonly DebugTracer Tracer = new DebugTracer();

        private const int FirstChunkStartIndex = 9;
        private int _InvalidChunksCounter;

        public Type MessageTypeType { get; }
        internal IDevice Device { get; private set; }

        private object _LastWrittenMessage;

        //protected Type GetContractType(PacketRecord messageType, string typeName);
        protected object GetEnumValue(string messageTypeString) => messageTypeString;
        #endregion

        public async Task InitializeVotumAsync()
        {
            WindowsHidDeviceFactory.Register(Logger, Tracer);

            var deviceDefinitions = new List<FilterDeviceDefinition>
            {
                new FilterDeviceDefinition { DeviceType = DeviceType.Hid, VendorId = 0x1fc9, ProductId = 0x80a6, Label = "VOTUM RF-HID Receiver" },
            };

            var devices = await DeviceManager.Current.GetDevicesAsync(deviceDefinitions);
            Device = devices.FirstOrDefault();
            await Device.InitializeAsync();

            var writeBuffer = new byte[64];
            byte[] readBuffer;

            while (true)
            {
                readBuffer = await Device.WriteAndReadAsync(writeBuffer);
                if (readBuffer[8] > 0)
                {
                    var record = new PacketRecord(readBuffer);
                    var resp = new Response(record);
                    await WriteAsync(resp);
                    continue;
                }
            }
        }

        private async Task WriteAsync(Response resp) 
        {
            var responseBytes = SerializeResponse(resp);
            await Device.WriteAsync(responseBytes);
        }

        private static byte[] SerializeResponse(Response resp)
        {
            if (resp == null) return null;
            List<byte> bytes = new List<byte>();
            var properties = typeof(Response).GetProperties();
            foreach (var property in properties)
            {
                var propValue = property.GetValue(resp);
                var res = GetPropertyValueAsBytes(propValue, property.PropertyType);
                bytes.AddRange(res);
            }
            var result = bytes.ToArray();
            return result;
        }

        private static byte[] GetPropertyValueAsBytes(object propValue, Type propType)
        {
            if (propType.Equals(typeof(byte)))
                return new byte[] { (byte)propValue };
            if (propType.Equals(typeof(byte[])))
                return propValue as byte[];
            var conversionMethod = typeof(BitConverter).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(x => x.Name.Equals("GetBytes"))
                .Where(x => x.GetParameters().FirstOrDefault(c => c.ParameterType.Equals(propType)) != null)
                .FirstOrDefault();
            var result = conversionMethod.Invoke(null, new[] { propValue }) as byte[];
            return result;
        }

        private static PacketRecord GetObject(byte[] dsBytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bn = new BinaryFormatter();
                ms.Write(dsBytes, 0, dsBytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                PacketRecord obj = (PacketRecord)bn.Deserialize(ms);
                return obj;
            }
        }

        private static byte[] Append(byte[] x, byte[] y)
        {
            var z = new byte[x.Length + y.Length];
            x.CopyTo(z, 0);
            y.CopyTo(z, x.Length);
            return z;
        }

        private static byte[] GetRange(byte[] bytes, int startIndex, int length)
        {
            return bytes.ToList().GetRange(startIndex, length).ToArray();
        }

        private async Task<PacketRecord> ReadAsync()
        {
            //считать чать массива
            var readBuffer = await Device.ReadAsync();

            //валидация части массива
            var firstByteNot63 = readBuffer.Data[0] != (byte)'?';
            var secondByteNot35 = readBuffer.Data[1] != 35;
            var thirdByteNot35 = readBuffer.Data[3] != 35;

            if (firstByteNot63 || secondByteNot35 || thirdByteNot35)
            {
                var message = $"An error occurred while attempting to read the message from the device. The last written message was a " +
                    $"{_LastWrittenMessage?.GetType().Name}. In the first chunk of data ";
                if (firstByteNot63) { message += "the first byte was not 63"; }
                if (secondByteNot35) { message += "the second byte was not 35"; }
                if (thirdByteNot35) { message += "the third byte was not 35"; }
                throw new ReadException(message, readBuffer, _LastWrittenMessage);
            }

            //Определяем тип части массива под 4-м индексом
            var messageTypeInt = readBuffer.Data[4];
            if (!Enum.IsDefined(MessageTypeType, (int)messageTypeInt))
            {
                throw new Exception($"The number {messageTypeInt} is not a valid MessageType");
            }

            //Получаем тип сообщения
            var messageTypeValueName = Enum.GetName(MessageTypeType, messageTypeInt);
            var messageType = (PacketRecord)Enum.Parse(MessageTypeType, messageTypeValueName);

            var remainingDataLength = ((readBuffer.Data[5] & 0xFF) << 24)
                                      + ((readBuffer.Data[6] & 0xFF) << 16)
                                      + ((readBuffer.Data[7] & 0xFF) << 8)
                                      + (readBuffer.Data[8] & 0xFF);

            var length = Math.Min(readBuffer.Data.Length - (FirstChunkStartIndex), remainingDataLength);

            //Читаем часть 9-64
            var allData = GetRange(readBuffer, FirstChunkStartIndex, length);
            remainingDataLength -= allData.Length;
            _InvalidChunksCounter = 0;

            while (remainingDataLength > 0)
            {
                readBuffer = await Device.ReadAsync();

                //проверяем возвращено ли что-то
                if (readBuffer.Data.Length <= 0) { continue; }

                //Проверяем, поместится ли оставшаясь часть данных в буффер
                length = Math.Min(readBuffer.Data.Length, remainingDataLength);

                if (readBuffer.Data[0] != (byte)'?')
                {
                    if (_InvalidChunksCounter++ > 5)
                    {
                        throw new Exception("messageRead: too many invalid chunks (2)");
                    }
                }

                allData = Append(allData, GetRange(readBuffer, 1, length - 1));

                remainingDataLength -= (length - 1);

                if (remainingDataLength != 1) { continue; }

                allData = Append(allData, GetRange(readBuffer, length, 1));

                remainingDataLength = 0;
            }

            var msg = GetObject(allData);

            //Logger.Log($"Read: {msg}", null, LogSection);

            return msg;
        }

    }
}
