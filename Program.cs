using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using OpenTTD_IRC_Info.OpenTTD.Admin;

namespace OpenTTD_IRC_Info
{
    class Program
    {
        const int MSPerTick = 30;
        const int TicksPerDay = 74;
        const int DaysPerWeek = 7;
        const int MSPerWeek = MSPerTick * TicksPerDay * DaysPerWeek;

        static async void Main(string ottdServer, string ottdPassword, string ircServer, string ircChannel, int ottdPort = 3977, int ircPort = 6667, string ircNickname = "OpenTTDInfo")
        {
            try
            {
                var ottd = new TcpClient(ottdServer, ottdPort);
                ottd.NoDelay = true;
                var ottdStream = ottd.GetStream();
                var irc = new IRC.Client(ircServer, ircPort, ircNickname, ircChannel);
                var lastYear = 0;

                await new AdminJoinPacket(ottdPassword).Send(ottdStream);
                var protocol = await ServerPacket.Receive<ServerProtocolPacket>(ottdStream);
                var welcome = await ServerPacket.Receive<ServerWelcomePacket>(ottdStream);

                Console.WriteLine($"OpenTTD {welcome.Version} - {welcome.Name}");

                while (irc.Connected)
                {
                    try
                    {
                        await new AdminPollPacket(AdminUpdateType.AdminUpdateDate, 0).Send(ottdStream);
                        var date = await ServerPacket.Receive<ServerDatePacket>(ottdStream);
                        var year = date.GameDate.AddDays(8).Year;
                        Console.WriteLine($"OpenTTD {date.GameDate:yyyy-MM-dd}");

                        if (lastYear != year)
                        {
                            Console.WriteLine($"OpenTTD update for {year}");

                            await new AdminPollPacket(AdminUpdateType.AdminUpdateCompanyInfo, uint.MaxValue).Send(ottdStream);
                            var companyInfo = (await ServerPacket.ReceiveList<ServerCompanyInfoPacket>(ottdStream)).ToDictionary(packet => packet.ID);

                            await new AdminPollPacket(AdminUpdateType.AdminUpdateCompanyEconomy, uint.MaxValue).Send(ottdStream);
                            var companyEconomy = (await ServerPacket.ReceiveList<ServerCompanyEconomyPacket>(ottdStream)).ToDictionary(packet => packet.ID);

                            IEnumerable<(string Name, ServerCompanyEconomyPacket Economy)> companyList = companyInfo.Select(kvp => (kvp.Value.Name, companyEconomy[kvp.Key]));

                            if (year % 10 == 0)
                            {
                                var companies = String.Join(", ", companyList.OrderBy(c => -c.Economy.Money).Select(c => $"{c.Name} ({c.Economy.Money:N0} {(c.Economy.Income >= 0 ? '+' : '-')}= {Math.Abs(c.Economy.Income):N0})"));
                                await irc.WriteCommand($"PRIVMSG {ircChannel} :{year} - {companies}");
                            }

                            if (lastYear != 0)
                            {
                                var companiesInTrouble = companyList.Where(c => c.Economy.Money >= 0 && c.Economy.Income < 0 && c.Economy.Money < -2 * c.Economy.Income).OrderBy(c => -c.Economy.Money);
                                foreach (var c in companiesInTrouble)
                                {
                                    await irc.WriteCommand($"PRIVMSG {ircChannel} :{year} - {c.Name} might be in trouble! Money: {c.Economy.Money:N0} Yearly income: {c.Economy.Income:N0}");
                                }
                            }
                        }
                        lastYear = year;
                    }
                    catch (TimeoutException error)
                    {
                        Console.WriteLine(error.Message);
                    }
                    System.Threading.Thread.Sleep(MSPerWeek);
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }
    }
}
