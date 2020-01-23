using System;
using System.Collections.Generic;
using System.Linq;

namespace MergeEm
{
	public class CustomData
	{
		private readonly string _category, _dataType;
		private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>(StringComparer.Ordinal);

		public string Category => _category;
		public string DataType => _dataType;
		public Dictionary<string, string> Metadata => _metadata;

		public CustomData(string category, string dataType, Dictionary<string, string> metadata)
		{
			_category = category;
			_dataType = dataType;
			_metadata = metadata;
		}
	}
}