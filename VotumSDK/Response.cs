using System;
using System.Linq;

namespace Votum
{
    [Serializable]
    class Response
    {
        public byte ReceiverCommand { get; set; } = 2; //Всегда равно HID_REMOTE_SET_DATA
        public byte PacketLength { get; private set; } //Длина пакета. Включает фактическую длину всех следующих полей данных, кроме, самого поля PacketLength.
        public ushort ComplectId { get; set; } //Номер комплекта, к которому принадлежит пульт. 
        public byte RemoteId { get; set; } //Номер пульта
        public byte RemoteCommand { get; set; } //Команда управления для пульта. TRemoteCommandID. 
        public byte[] Data { get; set; } = new byte[0]; //Данные для пульта или параметры команды. 
                                           //В поле PacketLength включать ФАКТИЧЕСКИЙ размер данного поля, для уменьшения загрузки радиоканала. TSendData = array[0..57] of Byte;

        public Response(PacketRecord record)
        {
            //ReceiverCommand = 1;
            PacketLength = 0;
            ComplectId = record.ReceiverId;
            RemoteId = record.RemoteId;
            RemoteCommand = record.DataId;
        }
    }
}
