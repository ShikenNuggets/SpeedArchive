using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeedrunComSharp;

namespace SpeedArchive{
	class VariableInfo{
		public string id;
		public string name;
		public Dictionary<string, string> values;

		public VariableInfo(Variable v){
			id = v.ID;
			name = v.Name;

			values = new Dictionary<string, string>();
			foreach(VariableValue vv in v.Values){
				values.Add(vv.ID, vv.Value);
			}
		}

		public bool HasDuplicateNames(List<VariableInfo> vList){
			int vCount = 0;
			foreach(VariableInfo vi in vList){
				if(name == vi.name){
					vCount++;
				}
			}

			return vCount > 1;
		}
	}

	class Cache{
		public static Dictionary<string, string> platforms = new Dictionary<string, string>();
		public static Dictionary<string, string> regions = new Dictionary<string, string>();
		public static Dictionary<string, VariableInfo> variables = new Dictionary<string, VariableInfo>();
		public static Dictionary<string, string> levels = new Dictionary<string, string>();

		public static void CachePlatform(Platform p){
			if(!platforms.ContainsKey(p.ID)){
				platforms.Add(p.ID, p.Name);
			}
		}

		public static void CachePlatforms(IReadOnlyCollection<Platform> pList){
			foreach(Platform p in pList){
				CachePlatform(p);
			}
		}

		public static void CacheRegion(Region r){
			if(!regions.ContainsKey(r.ID)){
				regions.Add(r.ID, r.Name);
			}
		}

		public static void CacheRegions(IReadOnlyCollection<Region> rList){
			foreach(Region r in rList){
				CacheRegion(r);
			}
		}

		public static void CacheVariable(Variable v){
			if(!variables.ContainsKey(v.ID)){
				variables.Add(v.ID, new VariableInfo(v));
			}
		}

		public static void CacheVariables(IReadOnlyCollection<Variable> vList){
			foreach(Variable v in vList){
				CacheVariable(v);
			}
		}

		public static void CacheLevel(Level lvl){
			if(!levels.ContainsKey(lvl.ID)){
				levels.Add(lvl.ID, lvl.Name);
			}
		}

		public static void CacheLevels(IReadOnlyCollection<Level> lvlList){
			foreach(Level lvl in lvlList){
				CacheLevel(lvl);
			}
		}
	}
}