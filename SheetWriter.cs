using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NanoXLSX;

namespace SpeedArchive{
	class SheetWriter{
		public static void Write(string dir, List<TableGenerator> tables){
			if(tables == null || tables.Count <= 0){
				return;
			}

			Workbook workbook = new Workbook(false);
			workbook.Filename = dir + DateTime.Now.ToString("yyyy-MM-dd__HH-mm-ss") + ".xlsx";
			foreach(TableGenerator tg in tables){
				workbook.AddWorksheet(tg.categoryName, true);
				foreach(DataColumn cl in tg.Columns){
					workbook.WS.Value(cl.ToString());
				}

				workbook.WS.Down();
				foreach(DataRow row in tg.Rows){
					foreach(var s in row.ItemArray){
						workbook.WS.Value(s);
					}
					workbook.WS.Down();
				}
			}

			if(!System.IO.Directory.Exists("Backups")){
				System.IO.Directory.CreateDirectory("Backups");
			}

			if(!System.IO.Directory.Exists(dir)){
				System.IO.Directory.CreateDirectory(dir);
			}

			workbook.Save();

			//Workbook workbook = new Workbook("Test.xlsx", "Sheet1");    //Create new workbook with a worksheet called Sheet1
			//workbook.WS.Value("Some Data");                             //Add cell A1
			//workbook.WS.Formula("=A1");                                 //Add formula to cell B1
			//workbook.WS.Down();                                         //Go to row 2
			//workbook.WS.Value(DateTime.Now);                            //Add formatted value to cell A2
			//workbook.Save();                                            //Save the workbook as myWorkbook.xlsx
		}
	}
}