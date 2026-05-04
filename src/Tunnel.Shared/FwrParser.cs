using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Tunnel.Shared.Models;

namespace Tunnel.Shared;

public static class FwrParser
{
    public static List<PortMapping> Parse(string filePath)
    {
        var ports = new List<PortMapping>();
        if (!File.Exists(filePath)) return ports;

        // .fwr files are usually UTF-16LE, often with BOM
        string content;
        try
        {
            content = File.ReadAllText(filePath, Encoding.Unicode);
        }
        catch
        {
            // fallback
            content = File.ReadAllText(filePath);
        }

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var rules = new Dictionary<int, Dictionary<string, string>>();

        var regex = new Regex(@"^FwdReq_(\d+)_(.+?)=(.*)$", RegexOptions.Compiled);

        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                int index = int.Parse(match.Groups[1].Value);
                string prop = match.Groups[2].Value;
                string val = match.Groups[3].Value;

                if (!rules.ContainsKey(index))
                {
                    rules[index] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                rules[index][prop] = val;
            }
        }

        foreach (var kvp in rules)
        {
            var idx = kvp.Key;
            var rule = kvp.Value;

            rule.TryGetValue("Incoming", out var incoming);
            rule.TryGetValue("Port", out var portStr);
            rule.TryGetValue("HostPort", out var hostPortStr);
            rule.TryGetValue("Host", out var host);
            rule.TryGetValue("Description", out var description);

            // Only parse Client-to-Server forwarding (Incoming=0)
            if (incoming == "0" && int.TryParse(portStr, out int localPort) && int.TryParse(hostPortStr, out int remotePort))
            {
                ports.Add(new PortMapping
                {
                    Name = string.IsNullOrWhiteSpace(description) ? $"Forward_{idx}" : description,
                    Local = localPort,
                    Remote = remotePort,
                    RemoteHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host
                });
            }
        }

        return ports;
    }
}
