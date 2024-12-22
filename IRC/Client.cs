using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OpenTTD_IRC_Info.IRC
{
    class Client
    {
        NetworkStream Stream;
        string Nickname;
        string Channel;

        public bool Connected { get => Stream.Socket.Connected; }

        public Client(string server, int port, string nickname, string channel)
        {
            Stream = new TcpClient(server, port).GetStream();
            Nickname = nickname;
            Channel = channel;
            Task.Run(this.Run);
        }

        public async Task ReadCommand(string command)
        {
            System.Console.WriteLine($"<<< {command}");

            var prefix = "";
            if (command[0] == ':')
            {
                var prefixEnd = command.IndexOf(' ');
                prefix = command.Substring(0, prefixEnd);
                command = command.Substring(prefixEnd + 1);
            }

            var parts = new List<string>();
            while (command.Length > 0)
            {
                if (command[0] == ':')
                {
                    parts.Add(command.Substring(1));
                    command = "";
                }
                else
                {
                    var partEnd = command.IndexOf(' ');
                    if (partEnd == -1)
                    {
                        parts.Add(command);
                        command = "";
                    }
                    else
                    {
                        parts.Add(command.Substring(0, partEnd));
                        command = command.Substring(partEnd + 1);
                    }
                }
            }

            switch (parts[0])
            {
                case "001":
                    if (!string.IsNullOrEmpty(Channel)) await WriteCommand($"JOIN {Channel}");
                    break;
                case "PING":
                    await WriteCommand($"PONG :{parts[1]}");
                    break;
            }
        }

        public async Task WriteCommand(string command)
        {
            System.Console.WriteLine($">>> {command}");
            await WriteString($"{command}\n");
        }

        async void Run()
        {
            await WriteCommand($"NICK {Nickname}");
            await WriteCommand($"USER {Nickname} * * *");
            var input = "";
            while (true)
            {
                input += await ReadString();
                while (input.IndexOf('\n') >= 0)
                {
                    var line = input.Substring(0, input.IndexOf('\n'));
                    input = input.Substring(line.Length + 1);
                    if (line.EndsWith('\r')) line = line.Substring(0, line.Length - 1);
                    await ReadCommand(line);
                }
            }
        }

        async Task<string> ReadString()
        {
            var b = new byte[1024];
            var length = await Stream.ReadAsync(b, 0, b.Length);
            return Encoding.UTF8.GetString(b, 0, length);
        }

        async Task WriteString(string data)
        {
            var b = Encoding.UTF8.GetBytes(data);
            await Stream.WriteAsync(b, 0, b.Length);
        }
    }
}
