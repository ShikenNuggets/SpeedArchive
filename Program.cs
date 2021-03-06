﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using NanoXLSX;
using Newtonsoft.Json;
using SpeedrunComSharp;
using Styles;

namespace SpeedArchive{
	class Program{
		private static SpeedrunComClient srcClient;
		private static Dictionary<string, DateTime> gameBackups = new Dictionary<string, DateTime>();
		private static readonly string backupsFile = "Backups/Backups.json";
		private static List<string> backedUpThiSession = new List<string>();

		public static void Main(){
			LoadBackupsFromFile();
			srcClient = new SpeedrunComClient();
			string input = "";

			while (true){
				#if (!DEBUG)
				try{
				#endif
					Console.WriteLine("Speedrun Archive Tool 0.2");
					Console.WriteLine("Enter [1] to backup a game");
					Console.WriteLine("Enter [2] to update my backups");
					Console.WriteLine("Enter [3] to check last backup for a specific game");
					#if (DEBUG)
					Console.WriteLine("Enter [4] to backup ALL games");
					Console.WriteLine("Enter [5] to invalidate all backups");
					#endif
					input = Console.ReadLine();
					switch(input){
						case "1":
							GameHandler();
							break;
						case "2":
							UpdateBackups();
							break;
						case "3":
							GameHandler(check: true);
							break;
						#if (DEBUG)
						case "4":
							BackupAllGames();
							break;
						case "5":
							InvalidateAllBackups();
							break;
						#endif
						default:
							Console.WriteLine("Invalid input!");
							break;
					}
#if (!DEBUG)
				}catch(Exception e){
					Console.WriteLine("Uncaught exception! " + e.Message);
				}
#endif

				Console.WriteLine("Would you like to do anything else? [Y/N]");
				input = Console.ReadLine();
				if(input == "y" || input == "Y"){
					Console.Clear();
				}else{
					break;
				}
			}

			Console.Write("The program is now complete. Press enter to exit.");
			Console.ReadLine();
		}

		private static void GameHandler(bool check = false){
			if(check){
				Console.WriteLine("What game would you like to check?");
			}else{
				Console.WriteLine("What game would you like to backup?");
			}

			string input = Console.ReadLine();
			var game = srcClient.Games.SearchGame(input);
			if(game == null){
				try{
					game = srcClient.Games.GetGame(input);
				}catch(APIException){}
			}

			if(game == null){
				Console.WriteLine("Couldn't find a game with that name/url/ID!");
				return;
			}

			if(check){
				CheckLastBackup(game.ID);
			}else{
				BackupGame(game.ID);
			}
		}

		private static void BackupGame(string id){
			List<TableGenerator> tables = new List<TableGenerator>();

			Game game = srcClient.Games.GetGame(id, new GameEmbeds(embedLevels: true, embedCategories: true, embedPlatforms: true, embedRegions: true, embedVariables: true));

			//Cache platforms
			foreach(string pID in game.PlatformIDs){
				if(!Cache.platforms.ContainsKey(pID)){
					Cache.CachePlatforms(game.Platforms);
					break;
				}
			}

			//Cache regions
			foreach(string rID in game.RegionIDs){
				if(!Cache.regions.ContainsKey(rID)){
					Cache.CacheRegions(game.Regions);
				}
			}

			//Cache variables
			Cache.CacheVariables(game.Variables);

			//Cache levels
			Cache.CacheLevels(game.Levels);

			//Run all categories
			foreach(Category c in game.Categories){
				Console.WriteLine("Grabbing [" + c.Name + "] runs...");
				tables.Add(new TableGenerator(c, srcClient.Runs));
			}

			//Backup file
			string fileName = "Backups/" + game.Abbreviation + " (" + game.ID + ")/";
			Console.WriteLine("Writing to " + fileName + " ...");
			SheetWriter.Write(fileName, tables);

			if(gameBackups.ContainsKey(id)){
				gameBackups[id] = DateTime.Now;
			}else{
				gameBackups.Add(id, DateTime.Now);
			}

			SaveBackupsToFile();
			backedUpThiSession.Add(id);
			Console.WriteLine("Finished backup of " + game.Name);

			//Clear per-game cached data
			Cache.variables.Clear();
			Cache.levels.Clear();
		}

		private static void BackupAllGames(){
			int gamesBackedUp = 0;

			var games = srcClient.Games.GetGameHeaders(orderBy: GamesOrdering.CreationDate);
			foreach(var g in games){
				if(gameBackups.ContainsKey(g.ID)){
					//We've already backed up this game. Skip!
					continue;
				}

				Console.WriteLine("Backing up " + g.Name + "...");

				while(true){
					try{
						System.Threading.Thread.Sleep(4000);
						BackupGame(g.ID);
						break;
					}catch(APIException){
						Console.WriteLine("An error has occurred, restarting proces...");
						System.Threading.Thread.Sleep(60000);
					}
				}

				gamesBackedUp++;
				Console.Clear();

				Console.WriteLine(gamesBackedUp.ToString() + " games backed up");
			}

			UpdateBackups();
		}

		private static void UpdateBackups(){
			var orderedPairs = gameBackups.OrderBy(pair => pair.Value);
			foreach(var op in orderedPairs){
				if(backedUpThiSession.Contains(op.Key)){
					continue;
				}

				BackupGame(op.Key);
			}
		}

		private static void CheckLastBackup(string id){
			if(!gameBackups.ContainsKey(id)){
				Console.WriteLine("This game has never been backed up! Would you like to back it up now? [Y/N]");
				string input = Console.ReadLine();
				if(input == "Y" || input == "y"){
					BackupGame(id);
				}
			}else{
				Console.WriteLine("This game was last backed up on " + gameBackups[id].ToShortDateString());
			}
		}

		private static void LoadBackupsFromFile(){
			if(!System.IO.File.Exists(backupsFile)){
				gameBackups = new Dictionary<string, DateTime>();
				SaveBackupsToFile();
				return;
			}

			gameBackups = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(System.IO.File.ReadAllText(backupsFile));
		}

		private static void SaveBackupsToFile(){
			if(!System.IO.Directory.Exists("Backups")){
				System.IO.Directory.CreateDirectory("Backups");
			}

			if(!System.IO.File.Exists(backupsFile)){
				System.IO.FileStream stream = System.IO.File.Create(backupsFile);
				stream.Close();
			}

			System.IO.File.WriteAllText(backupsFile, JsonConvert.SerializeObject(gameBackups));
		}

		private static void InvalidateAllBackups(){
			Console.WriteLine("Are you sure you want to invalidate all backups? [Y/N]");
			string input = Console.ReadLine();
			if(input == "y" || input == "Y"){
				gameBackups.Clear();
				SaveBackupsToFile();
			}
		}
	}
}