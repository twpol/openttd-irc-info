﻿using System;
using System.Linq;
using System.Net.Sockets;

namespace OpenTTD_IRC_Info
{
    class Program
    {
        const int MSPerTick = 30;
        const int TicksPerDay = 74;
        const int DaysPerWeek = 7;
        const int MSPerWeek = MSPerTick * TicksPerDay * DaysPerWeek;

        static async void Main(string ottdServer, string ircServer, string ircChannel, int ottdPort = 3979, int ircPort = 6667, string ircNickname = "OpenTTDInfo")
        {
            var ottd = new UdpClient(ottdServer, ottdPort);
            var irc = new IRC.Client(ircServer, ircPort, ircNickname);
            var lastYear = 0;

            while (irc.Connected)
            {
                System.Threading.Thread.Sleep(MSPerWeek);
                try
                {
                    await new OpenTTD.Udp.ClientInfo().Send(ottd);
                    var serverInfo = await OpenTTD.Udp.Packet.Receive<OpenTTD.Udp.ServerInfo>(ottd);
                    var year = serverInfo.GameDate.Year;
                    Console.WriteLine($"OpenTTD {serverInfo.GameDate:yyyy-MM-dd}");

                    if (lastYear != year)
                    {
                        Console.WriteLine($"OpenTTD update for {year}");
                        await new OpenTTD.Udp.ClientDetailInfo().Send(ottd);
                        var serverDetailInfo = await OpenTTD.Udp.Packet.Receive<OpenTTD.Udp.ServerDetailInfo>(ottd);

                        if (year % 10 == 0)
                        {
                            var companies = String.Join(", ", serverDetailInfo.Companies.OrderBy(c => -c.Money).Select(c => $"{c.Name} ({c.Money:N0} {(c.Income >= 0 ? '+' : '-')}= {Math.Abs(c.Income):N0})"));
                            await irc.WriteCommand($"PRIVMSG {ircChannel} :{year} - {companies}");
                        }

                        var companiesInTrouble = serverDetailInfo.Companies.Where(c => c.Money >= 0 && c.Income < 0 && c.Money < -2 * c.Income).OrderBy(c => -c.Money);
                        foreach (var c in companiesInTrouble)
                        {
                            await irc.WriteCommand($"PRIVMSG {ircChannel} :{year} - {c.Name} might be in trouble! Money: {c.Money:N0} Yearly income: {c.Income:N0}");
                        }
                    }
                    lastYear = year;
                }
                catch (TimeoutException error)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }
    }
}
