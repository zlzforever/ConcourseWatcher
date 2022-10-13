using System.Diagnostics;

namespace ConcourseWatcher;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var shell = _configuration["SHELL"];
        shell = string.IsNullOrWhiteSpace(shell) ? "bash" : shell;

        var url = _configuration["URL"];
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Url is required");
        }

        if (url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        var team = _configuration["TEAM"];
        if (string.IsNullOrWhiteSpace(team))
        {
            throw new ArgumentException("Team is required");
        }

        var userName = _configuration["USER_NAME"];
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("userName is required");
        }

        var password = _configuration["USER_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required");
        }

        var p = new Process();
        p.StartInfo.FileName = shell;
        p.StartInfo.Arguments = "";
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true; //true表示不显示黑框，false表示显示dos界面 
        p.StartInfo.UseShellExecute = false;
        // p.EnableRaisingEvents = true;

// 49   xiaolintong-web-prod  no  no  2022-09-07 19:44:01 +0800 CST
// 50   xiaolintong-web-test  no  no  2022-09-07 19:44:47 +0800 CST
// 52   xiaolintong-api-test  no  no  2022-09-08 14:00:43 +0800 CST
        p.OutputDataReceived += (sender, a) =>
        {
            var text = a.Data;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return;
            }

            var pieces = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length == 8)
            {
                foreach (var line in lines)
                {
                    pieces = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var name = pieces[1];
                    if (!name.StartsWith("commit"))
                    {
                        continue;
                    }

                    var date = DateTime.Parse($"{pieces[4]} {pieces[5]}");
                    var interval = (DateTime.Now - date).TotalDays;

                    if (interval > 2.0)
                    {
                        p.StandardInput.WriteLine(
                            $"fly -t {team} dp -n --pipeline \"{name}\"");
                        _logger.LogInformation("Destroy pipeline {name}", name);
                    }
                }
            }

            Console.WriteLine(a.Data);
        };
        p.ErrorDataReceived += (sender, a) =>
        {
            var text = a.Data;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (text.Contains("not authorized."))
            {
                p.StandardInput.WriteLine(
                    $"fly -t {team} login -c {url} --username {userName} --password {password}");
                return;
            }

            //not authorized
            Console.WriteLine(a.Data);
        };

        p.Start();

        p.BeginErrorReadLine();
        p.BeginOutputReadLine();

        if (!File.Exists("/usr/local/bin/fly"))
        {
            _logger.LogInformation("Start download fly...");
            var bytes = await new HttpClient().GetByteArrayAsync($"{url}/api/v1/cli?arch=amd64&platform=linux",
                stoppingToken);
            await File.WriteAllBytesAsync("/usr/local/bin/fly", bytes, stoppingToken);
            await p.StandardInput.WriteLineAsync("chmod +x /usr/local/bin/fly");
            _logger.LogInformation("Install fly success");
        }

        await p.StandardInput.WriteLineAsync(
            $"fly -t {team} login -c {url} --username {userName} --password {password}");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker scanning pipelines");
            await p.StandardInput.WriteLineAsync($"fly -t {team} pipelines");
            await Task.Delay(60000, stoppingToken);
        }
    }
}