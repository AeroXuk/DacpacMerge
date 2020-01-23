using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeEm
{
	public class SqlCmdVar
	{
		public string Name;
		public string Value;

		public SqlCmdVar(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public static List<SqlCmdVar> ParseCustomData(CustomData customData)
		{
			List<SqlCmdVar> ret = new List<SqlCmdVar>();
			foreach (KeyValuePair<string, string> metadata in customData.Metadata)
			{
				ret.Add(new SqlCmdVar(metadata.Key, metadata.Value));
			}
			return ret;
		}
	}
}
