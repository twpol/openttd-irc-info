namespace OpenTTD_IRC_Info.OpenTTD.Admin
{
    class AdminJoinPacket : AdminPacket
    {
        public AdminJoinPacket(string password)
        : base(PacketAdminType.AdminPacketAdminJoin)
        {
            WriteString(password);
            WriteString("OpenTTD IRC Info");
            WriteString("1.0");
        }
    }

    class AdminPingPacket : AdminPacket
    {
        public AdminPingPacket(uint value)
        : base(PacketAdminType.AdminPacketAdminPing)
        {
            WriteUInt32(value);
        }
    }

    class AdminPollPacket : AdminPacket
    {
        public AdminPollPacket(AdminUpdateType updateType, uint id)
        : base(PacketAdminType.AdminPacketAdminPoll)
        {
            WriteUInt8((byte)updateType);
            WriteUInt32(id);
        }
    }
}
