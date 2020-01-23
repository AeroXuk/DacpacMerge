using System;
using System.Collections.Generic;

namespace MergeEm
{
	public class Reference
	{
		public string ExternalParts;
		public string LogicalName;
		public string Path;
		public bool SuppressMissingDependenciesErrors;
		public bool IsSameDatabaseReference { get { return string.IsNullOrEmpty(ExternalParts); } }

		public Reference(CustomData customData)
		{
			foreach (KeyValuePair<string,string> metadata in customData.Metadata)
			{
				switch (metadata.Key)
				{
					case "FileName":
						Path = metadata.Value;
						break;
					case "LogicalName":
						LogicalName = metadata.Value;
						break;
					case "ExternalParts":
						ExternalParts = metadata.Value;
						break;
					case "SuppressMissingDependenciesErrors":
						bool.TryParse(metadata.Value, out SuppressMissingDependenciesErrors);
						break;
					default:
						Console.WriteLine("Unknown Property: {0} = {1}", metadata.Key, metadata.Value);
						break;
				}
			}
		}

		/*
		public CustomData GetCustomData()
		{
			var customData = new CustomData("Reference", "SqlSchema");
			customData.AddMetadata("Filename", Path);
			customData.AddMetadata("LogicalName", LogicalName);
			customData.AddMetadata("SuppressMissingDependenciesErrors", SuppressMissingDependenciesErrors.ToString());
			if (string.IsNullOrEmpty(ExternalParts))
			{
				customData.AddMetadata("ExternalParts", ExternalParts);
				customData.RequiredSqlCmdVars.Add(ExternalParts.Replace("[$(", "").Replace(")]", ""));
			}
			return customData;
		}
		*/
	}
}