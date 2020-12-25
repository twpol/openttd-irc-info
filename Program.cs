﻿using System;
using System.Linq;
using System.Net.Sockets;

namespace OpenTTD_IRC_Info
{
    class Program
    {
        static async void Main(string ottdServer, string ircServer, string channel, int ottdPort = 3979, int ircPort = 6667, string ircNickname = "OTTDBot")
        {
            var ottd = new UdpClient(ottdServer, ottdPort);
            var irc = new IRC.Client(ircServer, ircPort, ircNickname);
            var lastYear = 0;

            while (irc.Connected)
            {
                System.Threading.Thread.Sleep(10000);

                await new OpenTTD.Udp.ClientInfo().Send(ottd);
                var serverInfo = await OpenTTD.Udp.Packet.Receive<OpenTTD.Udp.ServerInfo>(ottd);
                var year = (int)Math.Floor(serverInfo.GameDate / 365.24);

                if (lastYear != year)
                {
                    Console.WriteLine($"Year {year}");
                    await new OpenTTD.Udp.ClientDetailInfo().Send(ottd);
                    var serverDetailInfo = await OpenTTD.Udp.Packet.Receive<OpenTTD.Udp.ServerDetailInfo>(ottd);

                    if (year % 10 == 0)
                    {
                        var companies = String.Join(", ", serverDetailInfo.Companies.Select(c => $"{c.Name} ({c.Money:N0} {(c.Income >= 0 ? '+' : '-')}= {Math.Abs(c.Income):N0})"));
                        await irc.WriteCommand($"PRIVMSG {channel} :{year} - {companies}");
                    }

                    var companiesInTrouble = serverDetailInfo.Companies.Where(c => c.Income < 0 && c.Money < -2 * c.Income);
                    foreach (var c in companiesInTrouble)
                    {
                        await irc.WriteCommand($"PRIVMSG {channel} :{year} - {c.Name} might be in trouble! Money: {c.Money:N0} Yearly income: {c.Income:N0}");
                    }
                }
                lastYear = year;
            }
        }
    }
}
