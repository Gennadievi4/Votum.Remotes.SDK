using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Votum
{
    public class PacketRecord
    {
        //MessageId = THIDMessageId - Идентификатор сообщения. Для данных, принимаемых от пульта, всегда равен HID_REMOTE_DATA 
        public byte MessageId { get; protected set; }
        public ushort ReceiverId { get; set; } //Идентификатор ресивера. Т.е. номер комплекта. 
        public byte BufferInsertIndex { get; set; } //Индекс записи в буфер ресивера. 
        public byte BufferExtractIndex { get; set; }//Индекс чтения из буффера ресивера.
        public byte RSSI { get; set; } //Уровень сигнала принятого пакета
        public ushort ReceivedPacketCount { get; set; } //Общий счётчик пакетов, принятых с пульта.
        public byte PacketLength { get; set; } //Ддлинна пакета.
        public ushort RemoteReceiverId { get; set; } //Идентификатор ресивера пульта. Т.е. номер комплекта, к которому принадлежит пульт. 
                                                     //Может отличаться от ReceiverID только в случае работы с «универсальным» ресивером в производственных целях. 
        public byte RemoteId { get; set; } //Идентификатор пульта
        public byte MsgIndex { get; set; } // Индекс сообщения. Применяется для идентификации различных посылок одинаковых данных. Например, при различных нажатиях одной и той-же кнопки. 
                                           //Не изменяется при повторной отправке данных, в случае неполучения подтверждения пультом принятия данных ресивером. Не применяется в интерактивном режиме.  
        public byte DataId {get; set;}//DataId = TRemoteCommandId - Идентификатор данных, содержащихся в пакете. 

        public byte BatteryLvl { get; set; } //Уровень батареи пульта. Для получения батареи содержимое поля умножить на 0.1. 
        public byte TransmitRetry { get; set; } //Попытка передачи текущих данных пультом, до получения подтверждения приема. 
        public byte LangId { get; set; } //Идентификатор языка. Содержит информацию о кодовой странице и раскладке клавиатуры пульта.

        public byte[] KeyData { get; set; } //KeyData: TKeyData; TKeyData = array[0...26] of Byte; Передаваемые данные. Содержимое поля меняется в зависимости от режима работы комплекта. 

        public PacketRecord(byte[] data)
        {
            MessageId = data[0];
            ReceiverId = BitConverter.ToUInt16(data.Skip(1).Take(2).ToArray());
            BufferInsertIndex = data[3];
            BufferExtractIndex = data[4];
            RSSI = data[5];
            ReceivedPacketCount = BitConverter.ToUInt16(data.Skip(5).Take(2).ToArray());
            PacketLength = data[8];
            RemoteReceiverId = BitConverter.ToUInt16(data.Skip(8).Take(2).ToArray());
            RemoteId = data[11];
            MsgIndex = data[12];
            DataId = data[13];
            BatteryLvl = data[14];
            TransmitRetry = data[15];
            LangId = data[16];
            KeyData = data[16..];
        }
    }
}
