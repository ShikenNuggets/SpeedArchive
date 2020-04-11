using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeedrunComSharp;

namespace SpeedArchive{
	class TableGenerator : DataTable{
		public string categoryName;
		private readonly bool isLevel;
		private readonly List<VariableInfo> variables;
		private readonly int playerCount;
		private readonly bool realTime;
		private readonly bool igt;
		private readonly bool loadRemoved;
		private readonly bool hasRegions;
		private readonly bool hasPlatforms;

		private static readonly List<string> restrictedColumnNames = new List<string>{
			"Platform", "Region", "Player 1", "Player 2", "Player 3", "Player 4", "Level",
			"Real Time", "Real Time without Loads", "In-Game Time", "Date", "Video", "Splits",
			"Description", "Status", "Rejection Reason", "ID"
		};

		public TableGenerator(Category category, RunsClient client){
			categoryName = category.Name;
			isLevel = (category.Type == CategoryType.PerLevel);

			variables = new List<VariableInfo>(category.Variables.Count);
			foreach(Variable v in category.Variables){
				variables.Add(Cache.variables[v.ID]);
			}

			playerCount = category.Players.Value;

			realTime = category.Game.Ruleset.TimingMethods.Contains(TimingMethod.RealTime);
			loadRemoved = category.Game.Ruleset.TimingMethods.Contains(TimingMethod.RealTimeWithoutLoads);
			igt = category.Game.Ruleset.TimingMethods.Contains(TimingMethod.GameTime);

			hasRegions = (category.Game.Regions.Count > 1);
			hasPlatforms = (category.Game.Platforms.Count > 1 || category.Game.Ruleset.EmulatorsAllowed);

			SetColumns();

			var runs = client.GetRuns(gameId: category.GameID, categoryId: category.ID, elementsPerPage: 200, embeds: new RunEmbeds(embedPlayers: true), orderBy: RunsOrdering.DateSubmitted);
			foreach(Run r in runs){
				AddRun(r);
			}
		}

		private void SetColumns(){
			if(isLevel){
				Columns.Add("Level");
			}

			foreach(VariableInfo v in variables){
				if(restrictedColumnNames.Contains(v.name) || v.HasDuplicateNames(variables)){
					Columns.Add(v.name + " (" + v.id + ")");
				}else{
					Columns.Add(v.name);
				}
			}

			for(int i = 0; i < playerCount; i++){
				Columns.Add("Player " + (i + 1).ToString());
			}

			if(realTime){
				Columns.Add("Real Time");
			}
			
			if(loadRemoved){
				Columns.Add("Real Time without Loads");
			}

			if(igt){
				Columns.Add("In-Game Time");
			}

			if(hasRegions){
				Columns.Add("Region");
			}

			if(hasPlatforms){
				Columns.Add("Platform");
			}

			Columns.Add("Date");
			Columns.Add("Video");
			Columns.Add("Splits");
			Columns.Add("Description");
			
			Columns.Add("Status");
			Columns.Add("Rejection Reason");
			Columns.Add("ID");
		}

		private void AddRun(Run run){
			//System.Threading.Thread.Sleep(10);
			List<string> runData = new List<string>(13 + variables.Count + playerCount);

			if(isLevel){
				runData.Add(Cache.levels[run.LevelID]);
			}

			List<string> runVariables = new List<string>();
			foreach(VariableValue vv in run.VariableValues){
				runVariables.Add(vv.ID);
			}

			foreach(VariableInfo v in variables){
				if(runVariables.Contains(v.id)){
					runData.Add(v.name);
				}else{
					runData.Add("");
				}
			}

			for(int i = 0; i < playerCount; i++){
				if(i >= run.Players.Count){
					runData.Add("");
				}else{
					runData.Add(run.Players[i].Name);
				}
			}

			if(realTime){
				if(run.Times.RealTime.HasValue){
					runData.Add(run.Times.RealTime.Value.ToString());
				}else{
					runData.Add("");
				}
			}

			if(loadRemoved){
				if(run.Times.RealTimeWithoutLoads.HasValue){
					runData.Add(run.Times.RealTimeWithoutLoads.Value.ToString());
				}else{
					runData.Add("");
				}
			}

			if(igt){
				if(run.Times.GameTime.HasValue){
					runData.Add(run.Times.GameTime.Value.ToString());
				}else{
					runData.Add("");
				}
			}

			if(hasRegions){
				if(run.System.RegionID != null && !Cache.regions.ContainsKey(run.System.RegionID)){
					Console.WriteLine("Fatal error! Region cached incorrectly!");
				}

				if(run.System.RegionID != null){
					runData.Add(Cache.regions[run.System.RegionID]);
				}else{
					runData.Add("");
				}
			}

			if(hasPlatforms){
				if(run.System.PlatformID != null && !Cache.platforms.ContainsKey(run.System.PlatformID)){
					Console.WriteLine("Fatal error! Platform cached incorrectly!");
				}

				if(run.System.PlatformID != null && run.System.IsEmulated){
					runData.Add(Cache.platforms[run.System.PlatformID] + " [EMU]");
				}else if(run.System.PlatformID != null && run.System.IsEmulated == false){
					runData.Add(Cache.platforms[run.System.PlatformID]);
				}else{
					runData.Add("");
				}
			}

			if(run.Date.HasValue){
				runData.Add(run.Date.Value.ToShortDateString());
			}else{
				runData.Add("");
			}

			if(run.Videos != null && run.Videos.Links != null && run.Videos.Links.Count > 0 && run.Videos.Links[0] != null){
				runData.Add(run.Videos.Links[0].ToString());
			}else{
				runData.Add("");
			}

			if(run.SplitsAvailable){
				runData.Add(run.SplitsUri.ToString());
			}else{
				runData.Add("");
			}

			if(run.Comment != null){
				runData.Add(run.Comment);
			}else{
				runData.Add("");
			}

			runData.Add(run.Status.Type.ToString());

			if(run.Status.Type == RunStatusType.Rejected && run.Status.Reason != null){
				runData.Add(run.Status.Reason);
			}else{
				runData.Add("");
			}

			runData.Add(run.ID);

			Rows.Add(runData.ToArray());
		}
	}
}