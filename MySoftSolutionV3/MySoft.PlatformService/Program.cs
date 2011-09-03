﻿using System;
using System.ServiceProcess;

namespace MySoft.PlatformService
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            InstallerServer server = new InstallerServer();
            string optionalArgs = string.Empty;

            // 运行服务
            if (args.Length == 0)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new BusinessService(server.WindowsService) };
                ServiceBase.Run(ServicesToRun);
                return;
            }
            else
            {
                optionalArgs = args[0];
            }

            Console.Title = "PlatformService Installer";

            var color = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.DarkBlue;

            if (!string.IsNullOrEmpty(optionalArgs))
            {
                switch (optionalArgs.ToLower())
                {
                    case "/?":
                    case "/help":
                        PrintHelp();
                        break;
                    case "/l":
                    case "/list":
                        {
                            string contains = null;
                            string status = null;
                            if (args.Length >= 2) contains = args[1].Trim();
                            if (args.Length >= 3) status = args[2].Trim();
                            server.ListService(contains, status);
                        }
                        break;
                    case "/c":
                    case "/console":
                        {
                            if (server.StartConsole())
                            {
                                Console.ReadLine();
                                server.StopConsole();
                            }
                            else
                            {
                                Console.ReadLine();
                            }
                        }
                        break;
                    case "/s":
                    case "/start":
                        {
                            string service = null;
                            if (args.Length == 2) service = args[1].Trim();
                            server.StartService(service);
                        }
                        break;
                    case "/p":
                    case "/stop":
                        {
                            string service = null;
                            if (args.Length == 2) service = args[1].Trim();
                            server.StopService(service);
                        }
                        break;
                    case "/r":
                    case "/restart":
                        {
                            string service = null;
                            if (args.Length == 2) service = args[1].Trim();
                            server.StopService(service);
                            server.StartService(service);
                        }
                        break;
                    case "/i":
                    case "/install":
                        server.InstallService();
                        break;
                    case "/u":
                    case "/uninstall":
                        server.UninstallService();
                        break;
                    default:
                        Console.WriteLine("输入的命令无效，输入/?显示帮助！");
                        break;
                }
            }

            Console.BackgroundColor = color;
        }

        static void PrintHelp()
        {
            Console.WriteLine("请输入命令启动相关操作,[]表示可选参数:");
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(@"/? | /help : 显示帮助信息");
            Console.WriteLine(@"/l | /list [服务名称] [-status] ：模糊查询服务");
            Console.WriteLine("      (status取值为running、stopped、paused)");
            Console.WriteLine(@"/s | /start [服务名称] : 启动指定服务");
            Console.WriteLine(@"/p | /stop [服务名称] : 停止指定服务");
            Console.WriteLine(@"/r | /restart [服务名称] : 重启指定服务");
            Console.WriteLine(@"/c | /console : 启动控制台 (仅当前配置有效)");
            Console.WriteLine(@"/i | /install : 安装为windows服务 (仅当前配置有效)");
            Console.WriteLine(@"/u | /uninstall : 卸载windows服务 (仅当前配置有效)");
            Console.WriteLine("----------------------------------------------");
        }
    }
}
