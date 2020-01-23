using Microsoft.SqlServer.Dac.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Collections;

namespace MergeEm
{
	public static class AddReferenceExtension
	{
		public static object GetObjectService(this TSqlModel model)
		{
			MethodInfo getObjectService = model.GetType()
				.GetMethod("GetObjectService", BindingFlags.NonPublic | BindingFlags.Instance);
			return getObjectService.Invoke(model, null);
		}

		public static object GetDataSchemaModel(this TSqlModel model)
		{
			Object objectService = model.GetObjectService();

			Type sqlSchemaModelObjectServiceType = model.GetType().Assembly
				.GetType("Microsoft.SqlServer.Dac.Model.SqlSchemaModelObjectService");

			FieldInfo dataSchemaModel = sqlSchemaModelObjectServiceType
				.GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance);

			return dataSchemaModel.GetValue(objectService);
		}

		public static List<CustomData> GetCustomData(this TSqlModel model)
		{
			List<CustomData> ret = new List<CustomData>();
			var dataSchemaModel = model.GetDataSchemaModel();

			MethodInfo getCustomData = dataSchemaModel.GetType()
				.GetMethod("GetCustomData", BindingFlags.Public | BindingFlags.Instance, null, Array.Empty<Type>(), null);

			Type customDataType = dataSchemaModel.GetType().Assembly
				.GetType("Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData");

			IList customDatas = getCustomData.Invoke(dataSchemaModel, null) as IList;
			// IList<Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData>
			foreach (object customData in customDatas)
			{
				string category = (string)customDataType.GetProperty("Category", BindingFlags.Public | BindingFlags.Instance).GetValue(customData),
					datatype = (string)customDataType.GetProperty("DataType", BindingFlags.Public | BindingFlags.Instance).GetValue(customData);
				Dictionary<string, string> metadata = (Dictionary<string, string>)customDataType.GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(customData);

				ret.Add(new CustomData(category, datatype, metadata));
			}
			return ret;
		}

		// SqlCmdVariables, SqlCmdVariables
		// Key = Var, Values = ""

		public static void AddSqlVariables(this TSqlModel model, List<SqlCmdVar> variables)
		{
			var dataSchemaModel = model.GetDataSchemaModel();

			MethodInfo addCustomData = dataSchemaModel.GetType()
				.GetMethod("AddCustomData", BindingFlags.NonPublic | BindingFlags.Instance);

			Type customSchemaDataType = dataSchemaModel.GetType().Assembly
				.GetType("Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData");

			object newCustomSchemaData = customSchemaDataType
				.GetConstructor(new Type[] { typeof(string), typeof(string) })
				.Invoke(new object[] { "SqlCmdVariables", "SqlCmdVariables" });

			MethodInfo setMetadata = customSchemaDataType
				.GetMethod("SetMetadata", BindingFlags.Public | BindingFlags.Instance);

			foreach (SqlCmdVar variable in variables)
			{
				setMetadata.Invoke(newCustomSchemaData, new[] { variable.Name, variable.Value });
			}

			addCustomData.Invoke(dataSchemaModel, new[] { newCustomSchemaData, true });
		}

		public static void AddReference(this TSqlModel model,
			string filename,
			string logicalName,
			string externalParts,
			bool suppressMissingDependenciesErrors)
		{
			var dataSchemaModel = model.GetDataSchemaModel();

			MethodInfo addCustomData = dataSchemaModel.GetType()
				.GetMethod("AddCustomData", BindingFlags.NonPublic | BindingFlags.Instance);

			Type customSchemaDataType = dataSchemaModel.GetType().Assembly
				.GetType("Microsoft.Data.Tools.Schema.SchemaModel.CustomSchemaData");

			object newCustomSchemaData = customSchemaDataType
				.GetConstructor(new Type[] { typeof(string), typeof(string) })
				.Invoke(new object[] { "Reference", "SqlSchema" });

			MethodInfo setMetadata = customSchemaDataType
				.GetMethod("SetMetadata", BindingFlags.Public | BindingFlags.Instance);

			setMetadata.Invoke(newCustomSchemaData, new[] { "FileName", filename });
			setMetadata.Invoke(newCustomSchemaData, new[] { "LogicalName", logicalName });
			setMetadata.Invoke(newCustomSchemaData, new[] { "SuppressMissingDependenciesErrors", suppressMissingDependenciesErrors.ToString() });
			if (!string.IsNullOrEmpty(externalParts))
			{
				setMetadata.Invoke(newCustomSchemaData, new[] { "ExternalParts", externalParts });
			}

			addCustomData.Invoke(dataSchemaModel, new[] { newCustomSchemaData, true });
		}
	}
}
