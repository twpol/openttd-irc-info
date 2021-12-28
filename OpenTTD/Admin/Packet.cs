using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTTD_IRC_Info.OpenTTD.Admin
{
    enum PacketAdminType
    {
        AdminPacketAdminJoin,                  // The admin announces and authenticates itself to the server.
        AdminPacketAdminQuit,                  // The admin tells the server that it is quitting.
        AdminPacketAdminUpdateFrequency,       // The admin tells the server the update frequency of a particular piece of information.
        AdminPacketAdminPoll,                  // The admin explicitly polls for a piece of information.
        AdminPacketAdminChat,                  // The admin sends a chat message to be distributed.
        AdminPacketAdminRcon,                  // The admin sends a remote console command.
        AdminPacketAdminGamescript,            // The admin sends a JSON string for the GameScript.
        AdminPacketAdminPing,                  // The admin sends a ping to the server, expecting a ping-reply (PONG) packet.
        AdminPacketAdminExternalChat,          // The admin sends a chat message from external source.

        AdminPacketServerFull = 100,           // The server tells the admin it cannot accept the admin.
        AdminPacketServerBanned,               // The server tells the admin it is banned.
        AdminPacketServerError,                // The server tells the admin an error has occurred.
        AdminPacketServerProtocol,             // The server tells the admin its protocol version.
        AdminPacketServerWelcome,              // The server welcomes the admin to a game.
        AdminPacketServerNewgame,              // The server tells the admin its going to start a new game.
        AdminPacketServerShutdown,             // The server tells the admin its shutting down.

        AdminPacketServerDate,                 // The server tells the admin what the current game date is.
        AdminPacketServerClientJoin,           // The server tells the admin that a client has joined.
        AdminPacketServerClientInfo,           // The server gives the admin information about a client.
        AdminPacketServerClientUpdate,         // The server gives the admin an information update on a client.
        AdminPacketServerClientQuit,           // The server tells the admin that a client quit.
        AdminPacketServerClientError,          // The server tells the admin that a client caused an error.
        AdminPacketServerCompanyNew,           // The server tells the admin that a new company has started.
        AdminPacketServerCompanyInfo,          // The server gives the admin information about a company.
        AdminPacketServerCompanyUpdate,        // The server gives the admin an information update on a company.
        AdminPacketServerCompanyRemove,        // The server tells the admin that a company was removed.
        AdminPacketServerCompanyEconomy,       // The server gives the admin some economy related company information.
        AdminPacketServerCompanyStats,         // The server gives the admin some statistics about a company.
        AdminPacketServerChat,                 // The server received a chat message and relays it.
        AdminPacketServerRcon,                 // The server's reply to a remove console command.
        AdminPacketServerConsole,              // The server gives the admin the data that got printed to its console.
        AdminPacketServerCmdNames,             // The server sends out the names of the DoCommands to the admins.
        AdminPacketServerCmdLogging,           // The server gives the admin copies of incoming command packets.
        AdminPacketServerGamescript,           // The server gives the admin information from the GameScript in JSON.
        AdminPacketServerRconEnd,              // The server indicates that the remote console command has completed.
        AdminPacketServerPong,                 // The server replies to a ping request from the admin.

        InvalidAdminPacket = 0xFF,             // An invalid marker for admin packets.
    }

    enum AdminUpdateType
    {
        AdminUpdateDate,                       // Updates about the date of the game.
        AdminUpdateClientInfo,                 // Updates about the information of clients.
        AdminUpdateCompanyInfo,                // Updates about the generic information of companies.
        AdminUpdateCompanyEconomy,             // Updates about the economy of companies.
        AdminUpdateCompanyStats,               // Updates about the statistics of companies.
        AdminUpdateChat,                       // The admin would like to have chat messages.
        AdminUpdateConsole,                    // The admin would like to have console messages.
        AdminUpdateCmdNames,                   // The admin would like a list of all DoCommand names.
        AdminUpdateCmdLogging,                 // The admin would like to have DoCommand information.
        AdminUpdateGamescript,                 // The admin would like to have gamescript messages.
        AdminUpdateEnd,                        // Must ALWAYS be on the end of this list!! (period)
    };

    class Packet
    {
        public byte Type { get; init; }
    }

    class AdminPacket : Packet
    {
        protected List<byte> Buffer;

        protected AdminPacket(PacketAdminType type)
        {
            Type = (byte)type;
            Buffer = new List<byte>(new byte[] { 0, 0, Type });
        }

        public async Task Send(NetworkStream client)
        {
            var b = Buffer.ToArray();
            b[0] = (byte)(b.Length & 0x00FF);
            b[1] = (byte)((b.Length & 0xFF00) >> 8);
            await client.WriteAsync(b, 0, b.Length);
        }

        protected void WriteBoolean(bool value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        protected void WriteUInt8(byte value)
        {
            Buffer.Add(value);
        }

        protected void WriteUInt16(ushort value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        protected void WriteInt32(int value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        protected void WriteUInt32(uint value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        protected void WriteInt64(long value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        protected void WriteUInt64(ulong value)
        {
            Buffer.AddRange(BitConverter.GetBytes(value));
        }

        protected void WriteString(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                Buffer.Add((byte)value[i]);
            }
            Buffer.Add(0);
        }

    }

    class ServerPacket : Packet
    {
        readonly static TimeSpan Timeout = TimeSpan.FromSeconds(15);

        protected byte[] Buffer;
        protected int BufferPosition;

        protected ServerPacket(PacketAdminType type, byte[] buffer)
        {
            Type = (byte)type;
            Buffer = buffer;
            BufferPosition = 3;
        }

        public static async Task<ServerPacket> Receive(NetworkStream client)
        {
            var timeout = DateTimeOffset.Now + Timeout;
            while (!client.DataAvailable && DateTimeOffset.Now < timeout) Thread.Sleep(100);
            if (!client.DataAvailable) throw new TimeoutException($"Timeout of {Timeout} waiting for packet");
            var size0 = (byte)client.ReadByte();
            var size1 = (byte)client.ReadByte();
            var size = size0 + (size1 << 8);
            var buffer = new byte[size];
            buffer[0] = size0;
            buffer[1] = size1;
            await client.ReadAsync(buffer, 2, size - 2);
            switch ((PacketAdminType)buffer[2])
            {
                case PacketAdminType.AdminPacketServerFull: return new ServerFullPacket(buffer);
                case PacketAdminType.AdminPacketServerBanned: return new ServerBannedPacket(buffer);
                case PacketAdminType.AdminPacketServerError: return new ServerErrorPacket(buffer);
                case PacketAdminType.AdminPacketServerProtocol: return new ServerProtocolPacket(buffer);
                case PacketAdminType.AdminPacketServerWelcome: return new ServerWelcomePacket(buffer);
                // case PacketAdminType.AdminPacketServerNewgame: return new ServerNewgamePacket(buffer);
                // case PacketAdminType.AdminPacketServerShutdown: return new ServerShutdownPacket(buffer);

                case PacketAdminType.AdminPacketServerDate: return new ServerDatePacket(buffer);
                // case PacketAdminType.AdminPacketServerClientJoin: return new ServerClientJoinPacket(buffer);
                // case PacketAdminType.AdminPacketServerClientInfo: return new ServerClientInfoPacket(buffer);
                // case PacketAdminType.AdminPacketServerClientUpdate: return new ServerClientUpdatePacket(buffer);
                // case PacketAdminType.AdminPacketServerClientQuit: return new ServerClientQuitPacket(buffer);
                // case PacketAdminType.AdminPacketServerClientError: return new ServerClientErrorPacket(buffer);
                // case PacketAdminType.AdminPacketServerCompanyNew: return new ServerCompanyNewPacket(buffer);
                case PacketAdminType.AdminPacketServerCompanyInfo: return new ServerCompanyInfoPacket(buffer);
                // case PacketAdminType.AdminPacketServerCompanyUpdate: return new ServerCompanyUpdatePacket(buffer);
                // case PacketAdminType.AdminPacketServerCompanyRemove: return new ServerCompanyRemovePacket(buffer);
                case PacketAdminType.AdminPacketServerCompanyEconomy: return new ServerCompanyEconomyPacket(buffer);
                case PacketAdminType.AdminPacketServerCompanyStats: return new ServerCompanyStatsPacket(buffer);
                // case PacketAdminType.AdminPacketServerChat: return new ServerChatPacket(buffer);
                // case PacketAdminType.AdminPacketServerRcon: return new ServerRconPacket(buffer);
                // case PacketAdminType.AdminPacketServerConsole: return new ServerConsolePacket(buffer);
                // case PacketAdminType.AdminPacketServerCmdNames: return new ServerCmdNamesPacket(buffer);
                // case PacketAdminType.AdminPacketServerCmdLogging: return new ServerCmdLoggingPacket(buffer);
                // case PacketAdminType.AdminPacketServerGamescript: return new ServerGamescriptPacket(buffer);
                // case PacketAdminType.AdminPacketServerRconEnd: return new ServerRconEndPacket(buffer);
                case PacketAdminType.AdminPacketServerPong: return new ServerPongPacket(buffer);

                default: throw new InvalidDataException($"Unknown admin server packet type {buffer[2]}");
            }
        }

        public static async Task<T> Receive<T>(NetworkStream client) where T : ServerPacket
        {
            var packet = await Receive(client);
            if (packet is T) return packet as T;
            throw new InvalidDataException($"Expected admin packet {typeof(T).Name}; got {packet.GetType().Name}");
        }

        public static async Task<List<T>> ReceiveList<T>(NetworkStream client) where T : ServerPacket
        {
            await new AdminPingPacket(0).Send(client);
            var list = new List<T>();
            while (true)
            {
                var packet = await Receive(client);
                if (packet is T item) list.Add(item);
                else if (packet is ServerPongPacket) return list;
                else throw new InvalidDataException($"Expected admin packet {typeof(T).Name}; got {packet.GetType().Name}");
            };
        }

        protected bool ReadBoolean()
        {
            var value = BitConverter.ToBoolean(Buffer, BufferPosition);
            BufferPosition++;
            return value;
        }

        protected byte ReadUInt8()
        {
            var value = Buffer[BufferPosition];
            BufferPosition++;
            return value;
        }

        protected ushort ReadUInt16()
        {
            var value = BitConverter.ToUInt16(Buffer, BufferPosition);
            BufferPosition += 2;
            return value;
        }

        protected int ReadInt32()
        {
            var value = BitConverter.ToInt32(Buffer, BufferPosition);
            BufferPosition += 4;
            return value;
        }

        protected uint ReadUInt32()
        {
            var value = BitConverter.ToUInt32(Buffer, BufferPosition);
            BufferPosition += 4;
            return value;
        }

        protected long ReadInt64()
        {
            var value = BitConverter.ToInt64(Buffer, BufferPosition);
            BufferPosition += 8;
            return value;
        }

        protected ulong ReadUInt64()
        {
            var value = BitConverter.ToUInt64(Buffer, BufferPosition);
            BufferPosition += 8;
            return value;
        }

        protected string ReadString()
        {
            var value = "";
            for (; Buffer[BufferPosition] != 0; BufferPosition++)
            {
                value += (char)Buffer[BufferPosition];
            }
            BufferPosition++;
            return value;
        }
    }
}
