using System;

namespace OpenTTD_IRC_Info.OpenTTD.Admin
{
    class ServerFullPacket : ServerPacket
    {
        public ServerFullPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerFull, buffer)
        {
        }
    }

    class ServerBannedPacket : ServerPacket
    {
        public ServerBannedPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerBanned, buffer)
        {
        }
    }

    class ServerErrorPacket : ServerPacket
    {
        public byte ErrorCode;

        public ServerErrorPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerError, buffer)
        {
            ErrorCode = ReadUInt8();
        }
    }

    class ServerProtocolPacket : ServerPacket
    {
        public byte ProtocolVersion;
        public bool Unknown;
        public ushort UpdatePacketType;
        public ushort UpdatePacketFrequencies;

        public ServerProtocolPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerProtocol, buffer)
        {
            ProtocolVersion = ReadUInt8();
            Unknown = ReadBoolean();
            UpdatePacketType = ReadUInt16();
            UpdatePacketFrequencies = ReadUInt16();
        }
    }

    class ServerWelcomePacket : ServerPacket
    {
        public string Name;
        public string Version;
        public bool Dedicated;
        public string MapName;
        public uint Seed;
        public byte MapLandscape;
        public DateTimeOffset StartDate;
        public ushort MapWidth;
        public ushort MapHeight;

        public ServerWelcomePacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerWelcome, buffer)
        {
            Name = ReadString();
            Version = ReadString();
            Dedicated = ReadBoolean();
            MapName = ReadString();
            Seed = ReadUInt32();
            MapLandscape = ReadUInt8();
            StartDate = OpenTTD.Date.GetDate(ReadUInt32());
            MapWidth = ReadUInt16();
            MapHeight = ReadUInt16();
        }
    }

    class ServerDatePacket : ServerPacket
    {
        public DateTimeOffset GameDate;

        public ServerDatePacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerDate, buffer)
        {
            GameDate = OpenTTD.Date.GetDate(ReadUInt32());
        }
    }

    class ServerCompanyInfoPacket : ServerPacket
    {
        public byte ID;
        public string Name;
        public string Manager;
        public byte Colour;
        public bool PasswordProtected;
        public DateTimeOffset InaugurationYear;
        public bool AI;

        public ServerCompanyInfoPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerCompanyInfo, buffer)
        {
            ID = ReadUInt8();
            Name = ReadString();
            Manager = ReadString();
            Colour = ReadUInt8();
            PasswordProtected = ReadBoolean();
            InaugurationYear = OpenTTD.Date.GetDate(ReadUInt32());
            AI = ReadBoolean();
        }
    }

    class ServerCompanyEconomyPacket : ServerPacket
    {
        public byte ID;
        public long Money;
        public ulong Loan;
        public long Income;
        public ushort ThisQuarterDeliveredCargo;
        public ulong LastQuarterCompanyValue;
        public ushort LastQuarterPerformance;
        public ushort LastQuarterDeliveredCargo;
        public ulong PreviousQuarterCompanyValue;
        public ushort PreviousQuarterPerformance;
        public ushort PreviousQuarterDeliveredCargo;

        public ServerCompanyEconomyPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerCompanyEconomy, buffer)
        {
            ID = ReadUInt8();
            Money = ReadInt64();
            Loan = ReadUInt64();
            Income = ReadInt64();
            ThisQuarterDeliveredCargo = ReadUInt16();
            LastQuarterCompanyValue = ReadUInt64();
            LastQuarterPerformance = ReadUInt16();
            LastQuarterDeliveredCargo = ReadUInt16();
            PreviousQuarterCompanyValue = ReadUInt64();
            PreviousQuarterPerformance = ReadUInt16();
            PreviousQuarterDeliveredCargo = ReadUInt16();
        }
    }

    class ServerCompanyStatsPacket : ServerPacket
    {
        public byte ID;
        public ushort VehicleCountTrain;
        public ushort VehicleCountLorry;
        public ushort VehicleCountBus;
        public ushort VehicleCountPlane;
        public ushort VehicleCountShip;
        public ushort StationCountTrain;
        public ushort StationCountLorry;
        public ushort StationCountBus;
        public ushort StationCountPlane;
        public ushort StationCountBoat;

        public ServerCompanyStatsPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerCompanyStats, buffer)
        {
            ID = ReadUInt8();
            VehicleCountTrain = ReadUInt16();
            VehicleCountLorry = ReadUInt16();
            VehicleCountBus = ReadUInt16();
            VehicleCountPlane = ReadUInt16();
            VehicleCountShip = ReadUInt16();
            StationCountTrain = ReadUInt16();
            StationCountLorry = ReadUInt16();
            StationCountBus = ReadUInt16();
            StationCountPlane = ReadUInt16();
            StationCountBoat = ReadUInt16();
        }
    }

    class ServerPongPacket : ServerPacket
    {
        public uint Value;
        public ServerPongPacket(byte[] buffer)
        : base(PacketAdminType.AdminPacketServerPong, buffer)
        {
            Value = ReadUInt32();
        }
    }
}
