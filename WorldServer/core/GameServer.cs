﻿using NLog;
using Shared;
using Shared.database;
using Shared.isc;
using Shared.resources;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using WorldServer.core.commands;
using WorldServer.core.connection;
using WorldServer.core.miscfile;
using WorldServer.core.objects.vendors;
using WorldServer.core.worlds;
using WorldServer.logic;
using WorldServer.logic.loot;
using WorldServer.utils;

namespace WorldServer.core
{
    public sealed class GameServer
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public string InstanceId { get; private set; }
        public ServerConfig Configuration { get; private set; }
        public Resources Resources { get; private set; }
        public Database Database { get; private set; }
        public MarketSweeper MarketSweeper { get; private set; }
        public ConnectionManager ConnectionManager { get; private set; }
        public ConnectionListener ConnectionListener { get; private set; }
        public ChatManager ChatManager { get; private set; }
        public BehaviorDb BehaviorDb { get; private set; }
        public CommandManager CommandManager { get; private set; }
        public DbEvents DbEvents { get; private set; }
        public ISManager InterServerManager { get; private set; }
        public WorldManager WorldManager { get; private set; }
        public SignalListener SignalListener { get; private set; }
        private bool Running { get; set; } = true;
        public DateTime RestartCloseTime { get; private set; }

        public GameServer(string[] appArgs)
        {
			var configPath = appArgs.Length == 0 ? "wServer.json" : appArgs[0];
			Configuration = ServerConfig.ReadFile(configPath);
			
            LogManager.Configuration.Variables["logDirectory"] = $"{Configuration.serverSettings.logFolder}/wServer";
            LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfiguration();

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                StaticLogger.Instance.Fatal(((Exception)args.ExceptionObject).StackTrace.ToString());
            };

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.Name = "Entry";

            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.SetMinThreads(250, completionPortThreads);

            Resources = new Resources(Configuration.serverSettings.resourceFolder, true, true);
            Database = new Database(Resources, Configuration);
            MarketSweeper = new MarketSweeper(Database);
            ConnectionManager = new ConnectionManager(this);
            ConnectionListener = new ConnectionListener(this);
            ChatManager = new ChatManager(this);
            BehaviorDb = new BehaviorDb(this);
            CommandManager = new CommandManager();
            DbEvents = new DbEvents(this);

            InstanceId = Configuration.serverInfo.instanceId = Guid.NewGuid().ToString();
            Console.WriteLine($"[Set] InstanceId [{InstanceId}]");

            InterServerManager = new ISManager(Database.Subscriber, Configuration);
            WorldManager = new WorldManager(this);
            
            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") != null;
			SignalListener = isDocker ? new SignalListenerLinux(this) : new SignalListenerWindows(this);
        }

        public bool IsWhitelisted(int accountId) => Configuration.serverSettings.whitelist.Contains(accountId);

#if DEBUG
        private static bool ExportXMLS = true;
#else
        private static bool ExportXMLS = false;
#endif

        public void Run()
        {
#if DEBUG
            if (!Directory.Exists("GenerateXMLS"))
                _ = Directory.CreateDirectory("GenerateXMLS");

            var f = File.CreateText("GenerateXMLS/EmbeddedData_ObjectsCXML.xml");
            f.Write(Resources.GameData.ObjectCombinedXML.ToString());
            f.Close();

            var f3 = File.CreateText("GenerateXMLS/EmbeddedData_SkinsCXML.xml");
            f3.Write(Resources.GameData.SkinsCombinedXML.ToString());
            f3.Close();

            var f4 = File.CreateText("GenerateXMLS/EmbeddedData_PlayersCXML.xml");
            f4.Write(Resources.GameData.CombinedXMLPlayers.ToString());
            f4.Close();

            var f2 = File.CreateText("GenerateXMLS/EmbeddedData_GroundsCXML.xml");
            f2.Write(Resources.GameData.GroundCombinedXML.ToString());
            f2.Close();

            Log.Info("XMLs have been generated.");
#endif

            CommandManager.Initialize(this);
            Loot.Initialize(this);
            MobDrops.Initialize(this);
            BehaviorDb.Initialize();
            MerchantLists.Initialize(this);
            WorldManager.Initialize();
            InterServerManager.Initialize();
            ChatManager.Initialize();
            ConnectionListener.Initialize();
            MarketSweeper.Start();
            ConnectionListener.Start();
            InterServerManager.JoinNetwork();

            var timeout = TimeSpan.FromHours(Configuration.serverSettings.restartTime);

            var utcNow = DateTime.UtcNow;
            var startedAt = utcNow;
            RestartCloseTime = utcNow.Add(timeout);
            var restartsIn = utcNow.Add(TimeSpan.FromMinutes(5));

            var restart = false;

            var watch = Stopwatch.StartNew();
            while (Running)
            {
                // server close event
                if (!restart && DateTime.UtcNow >= RestartCloseTime)
                {
                    ChatManager.ServerAnnounce("The server will restart in 5 minutes.");

                    Log.Info("Server restart procedure is commencing.");
                    ConnectionListener.Disable();
                    restart = true;
                }

                if (restart && DateTime.UtcNow >= restartsIn)
                    break;

                var current = watch.ElapsedMilliseconds;

                ConnectionManager.Tick(current);

                var logicTime = (int)(watch.ElapsedMilliseconds - current);
                var sleepTime = Math.Max(0, 200 - logicTime);

                Thread.Sleep(sleepTime);
            }

            if (restart)
                Log.Info("A server restart has been triggered.");
            else
                Log.Info("A server shutdown has been triggered.");
            Dispose();

            if (restart)
                _ = Process.Start($"{AppDomain.CurrentDomain.FriendlyName}.exe");

            Log.Info("Program has been terminated.");
            Thread.Sleep(10000);
        }

        public void Stop()
        {
            if (!Running)
                return;
            Running = false;
        }

        public void Dispose()
        {
            Log.Info("Disposed 'ConnectionListener'.");
            ConnectionListener.Shutdown();

            Log.Info("Disposed 'InterServerManager'.");
            InterServerManager.Shutdown();

            Log.Info("Disposed 'Resources'.");
            Resources.Dispose();

            Log.Info("Disposed 'Database'.");
            Database.Dispose();

            Log.Info("Disposed 'MarketSweeper'.");
            MarketSweeper.Stop();

            Log.Info("Disposed 'ChatManager'.");
            ChatManager.Dispose();

            Log.Info("Disposed 'WorldManager'.");
            WorldManager.Dispose();

            Log.Info("Disposed 'Configuration'.");
            Configuration = null;
        }
    }
}
