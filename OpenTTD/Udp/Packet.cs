using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTTD_IRC_Info.OpenTTD.Udp
{
    enum PacketType
    {
        PACKET_UDP_CLIENT_FIND_SERVER,   // Queries a game server for game information
        PACKET_UDP_SERVER_RESPONSE,      // Reply of the game server with game information
        PACKET_UDP_CLIENT_DETAIL_INFO,   // Queries a game server about details of the game, such as companies
        PACKET_UDP_SERVER_DETAIL_INFO,   // Reply of the game server about details of the game, such as companies
        PACKET_UDP_SERVER_REGISTER,      // Packet to register itself to the master server
        PACKET_UDP_MASTER_ACK_REGISTER,  // Packet indicating registration has succeeded
        PACKET_UDP_CLIENT_GET_LIST,      // Request for serverlist from master server
        PACKET_UDP_MASTER_RESPONSE_LIST, // Response from master server with server ip's + port's
        PACKET_UDP_SERVER_UNREGISTER,    // Request to be removed from the server-list
        PACKET_UDP_CLIENT_GET_NEWGRFS,   // Requests the name for a list of GRFs (GRF_ID and MD5)
        PACKET_UDP_SERVER_NEWGRFS,       // Sends the list of NewGRF's requested.
        PACKET_UDP_MASTER_SESSION_KEY,   // Sends a fresh session key to the client
        PACKET_UDP_END,                  // Must ALWAYS be on the end of this list!! (period)
    }

    class Packet
    {
        readonly static TimeSpan Timeout = TimeSpan.FromSeconds(15);

        public byte Type { get; private set; }

        protected Packet(PacketType type)
        {
            Type = (byte)type;
        }

        protected virtual byte[] GetBytes() => new byte[] { 0, 0, Type };

        public async Task Send(UdpClient client)
        {
            var b = GetBytes();
            b[0] = (byte)(b.Length & 0x00FF);
            b[1] = (byte)((b.Length & 0xFF00) >> 8);
            await client.SendAsync(b, b.Length);
        }

        internal virtual int Receive(byte[] buffer)
        {
            return 3;
        }

        public static async Task<T> Receive<T>(UdpClient client) where T : Packet
        {
            var timeout = DateTimeOffset.Now + Timeout;
            while (client.Available == 0 && DateTimeOffset.Now < timeout) Thread.Sleep(100);
            if (client.Available == 0) throw new TimeoutException($"Timeout of {Timeout} waiting for packet type {typeof(T).Name}");
            var res = await client.ReceiveAsync();
            if (res.Buffer.Length < 3) throw new InvalidDataException("Packet is too short; must be 3 bytes or more");
            var size = res.Buffer[0] + (res.Buffer[1] << 8);
            if (size != res.Buffer.Length) throw new InvalidDataException($"Packet length is {res.Buffer.Length}; encoded size is {size}");
            switch ((PacketType)res.Buffer[2])
            {
                case PacketType.PACKET_UDP_SERVER_RESPONSE:
                    {
                        if (typeof(T) != typeof(ServerInfo)) throw new InvalidDataException($"Expected packet type {PacketType.PACKET_UDP_SERVER_RESPONSE}; got {res.Buffer[2]}");
                        var data = new ServerInfo();
                        data.Receive(res.Buffer);
                        return (Packet)data as T;
                    }
                case PacketType.PACKET_UDP_SERVER_DETAIL_INFO:
                    {
                        if (typeof(T) != typeof(ServerDetailInfo)) throw new InvalidDataException($"Expected packet type {PacketType.PACKET_UDP_SERVER_DETAIL_INFO}; got {res.Buffer[2]}");
                        var data = new ServerDetailInfo();
                        data.Receive(res.Buffer);
                        return (Packet)data as T;
                    }
                default:
                    throw new InvalidDataException($"Unknown packet type {res.Buffer[2]}");
            }
        }

        protected static bool ReadBoolean(byte[] buffer, ref int pos)
        {
            var value = BitConverter.ToBoolean(buffer, pos);
            pos++;
            return value;
        }

        protected static byte ReadUInt8(byte[] buffer, ref int pos)
        {
            var value = buffer[pos];
            pos++;
            return value;
        }

        protected static ushort ReadUInt16(byte[] buffer, ref int pos)
        {
            var value = BitConverter.ToUInt16(buffer, pos);
            pos += 2;
            return value;
        }

        protected static int ReadInt32(byte[] buffer, ref int pos)
        {
            var value = BitConverter.ToInt32(buffer, pos);
            pos += 4;
            return value;
        }

        protected static uint ReadUInt32(byte[] buffer, ref int pos)
        {
            var value = BitConverter.ToUInt32(buffer, pos);
            pos += 4;
            return value;
        }

        protected static long ReadInt64(byte[] buffer, ref int pos)
        {
            var value = BitConverter.ToInt64(buffer, pos);
            pos += 8;
            return value;
        }

        protected static ulong ReadUInt64(byte[] buffer, ref int pos)
        {
            var value = BitConverter.ToUInt64(buffer, pos);
            pos += 8;
            return value;
        }

        protected static string ReadString(byte[] buffer, ref int pos)
        {
            var value = "";
            for (; buffer[pos] != 0; pos++)
            {
                value += (char)buffer[pos];
            }
            pos++;
            return value;
        }
    }

    class ClientInfo : Packet
    {
        public ClientInfo()
        : base(PacketType.PACKET_UDP_CLIENT_FIND_SERVER)
        {
        }
    }

    class ServerInfo : Packet
    {
        public byte Version { get; private set; }

        public DateTimeOffset GameDate { get; private set; }
        public DateTimeOffset StartDate { get; private set; }

        public byte CompaniesMax { get; private set; }
        public byte CompaniesOn { get; private set; }
        public byte SpectatorsMax { get; private set; }

        public string ServerName { get; private set; }
        public string ServerRevision { get; private set; }
        public byte ServerLang { get; private set; }
        public bool UsePassword { get; private set; }
        public byte ClientsMax { get; private set; }
        public byte ClientsOn { get; private set; }
        public byte SpectatorsOn { get; private set; }
        public string MapName { get; private set; }
        public ushort MapWidth { get; private set; }
        public ushort MapHeight { get; private set; }
        public byte MapSet { get; private set; }
        public bool Dedicated { get; private set; }

        internal ServerInfo()
        : base(PacketType.PACKET_UDP_SERVER_RESPONSE)
        {
        }

        internal override int Receive(byte[] buffer)
        {
            var pos = base.Receive(buffer);
            Version = Packet.ReadUInt8(buffer, ref pos);

            var grfCount = Packet.ReadUInt8(buffer, ref pos);
            for (var i = 0; i < grfCount; i++)
            {
                pos += 4 /* GRF ID */ + 16 /* MD5 */;
            }

            GameDate = OpenTTD.Date.GetDate(Packet.ReadInt32(buffer, ref pos));
            StartDate = OpenTTD.Date.GetDate(Packet.ReadInt32(buffer, ref pos));

            CompaniesMax = Packet.ReadUInt8(buffer, ref pos);
            CompaniesOn = Packet.ReadUInt8(buffer, ref pos);
            SpectatorsMax = Packet.ReadUInt8(buffer, ref pos);

            ServerName = Packet.ReadString(buffer, ref pos);
            ServerRevision = Packet.ReadString(buffer, ref pos);
            ServerLang = Packet.ReadUInt8(buffer, ref pos);
            UsePassword = Packet.ReadBoolean(buffer, ref pos);
            ClientsMax = Packet.ReadUInt8(buffer, ref pos);
            ClientsOn = Packet.ReadUInt8(buffer, ref pos);
            SpectatorsOn = Packet.ReadUInt8(buffer, ref pos);
            MapName = Packet.ReadString(buffer, ref pos);
            MapWidth = Packet.ReadUInt16(buffer, ref pos);
            MapHeight = Packet.ReadUInt16(buffer, ref pos);
            MapSet = Packet.ReadUInt8(buffer, ref pos);
            Dedicated = Packet.ReadBoolean(buffer, ref pos);

            return pos;
        }
    }

    class ClientDetailInfo : Packet
    {
        public ClientDetailInfo()
        : base(PacketType.PACKET_UDP_CLIENT_DETAIL_INFO)
        {
        }
    }

    class ServerDetailInfo : Packet
    {
        public byte Version { get; private set; }
        public byte CompanyCount { get; private set; }
        public IReadOnlyList<Company> Companies { get; private set; }

        public class Company
        {
            public byte Index { get; private set; }
            public string Name { get; private set; }
            public uint InauguratedYear { get; private set; }
            public ulong Value { get; private set; }
            public long Money { get; private set; }
            public long Income { get; private set; }
            public ushort PerformanceRating { get; private set; }
            public bool PasswordProtected { get; private set; }

            public ushort VehicleCountTrain { get; private set; }
            public ushort VehicleCountLorry { get; private set; }
            public ushort VehicleCountBus { get; private set; }
            public ushort VehicleCountPlane { get; private set; }
            public ushort VehicleCountShip { get; private set; }
            public ushort StationCountTrain { get; private set; }
            public ushort StationCountLorry { get; private set; }
            public ushort StationCountBus { get; private set; }
            public ushort StationCountPlane { get; private set; }
            public ushort StationCountShip { get; private set; }
            public bool IsAI { get; private set; }

            internal Company(byte[] buffer, ref int pos)
            {
                Index = Packet.ReadUInt8(buffer, ref pos);
                Name = Packet.ReadString(buffer, ref pos);
                InauguratedYear = Packet.ReadUInt32(buffer, ref pos);
                Value = Packet.ReadUInt64(buffer, ref pos);
                Money = Packet.ReadInt64(buffer, ref pos);
                Income = Packet.ReadInt64(buffer, ref pos);
                PerformanceRating = Packet.ReadUInt16(buffer, ref pos);
                PasswordProtected = Packet.ReadBoolean(buffer, ref pos);
                VehicleCountTrain = Packet.ReadUInt16(buffer, ref pos);
                VehicleCountLorry = Packet.ReadUInt16(buffer, ref pos);
                VehicleCountBus = Packet.ReadUInt16(buffer, ref pos);
                VehicleCountPlane = Packet.ReadUInt16(buffer, ref pos);
                VehicleCountShip = Packet.ReadUInt16(buffer, ref pos);
                StationCountTrain = Packet.ReadUInt16(buffer, ref pos);
                StationCountLorry = Packet.ReadUInt16(buffer, ref pos);
                StationCountBus = Packet.ReadUInt16(buffer, ref pos);
                StationCountPlane = Packet.ReadUInt16(buffer, ref pos);
                StationCountShip = Packet.ReadUInt16(buffer, ref pos);
                IsAI = Packet.ReadBoolean(buffer, ref pos);
            }
        }

        internal ServerDetailInfo()
        : base(PacketType.PACKET_UDP_SERVER_DETAIL_INFO)
        {
        }

        internal override int Receive(byte[] buffer)
        {
            var pos = base.Receive(buffer);
            Version = Packet.ReadUInt8(buffer, ref pos);
            CompanyCount = Packet.ReadUInt8(buffer, ref pos);
            var companies = new List<Company>();
            for (var i = 0; i < CompanyCount; i++)
            {
                companies.Add(new Company(buffer, ref pos));
            }
            Companies = companies;
            return pos;
        }
    }
}
