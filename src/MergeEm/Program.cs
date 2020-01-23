using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MergeEm
{
	class Program
	{
		static void Main(string[] args)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			try
			{
				//args = new string[] {
				//	@"..\..\..\..\DacPac3\bin\Debug\DacPac2.dacpac",
				//	@"..\..\..\..\DacPac3\bin\Debug\DacPac3.dacpac"
				//	};

				if (args.Length < 1 || args.Any(p => p == "/?") || args.Any(p => p == "-?") || args.Any(p => p == "/help") || args.Any(p => p == "--help"))
				{
					Console.WriteLine("you need at least two args - baseDacPac (which will be overwritten) sourceDacpac [...]");
					return;
				}

				new DacpacMerge(
					args.First(),
					args.Skip(1).ToArray()
					).Merge();
			}
			catch (Exception _)
			{
				Exception exception = _;
				do
				{
					Console.WriteLine("Exception[{0}]: {1}\r\nStackTrace: {2}\r\n\r\n",
						exception.Source,
						exception.Message,
						exception.StackTrace
						);
					exception = exception.InnerException;
				} while (!(exception is null));
			}
			stopwatch.Stop();
			Console.WriteLine("MergeEm Runtime: {0:c}", stopwatch.Elapsed);
		}
	}

	class DacpacMerge
	{
		private readonly string[] _sources;
		//private readonly Stream _dacStream;
		private readonly TSqlModel _targetModel;
		private readonly DacPackage _dacPackage;

		public DacpacMerge(string target, params string[] sources)
		{
			if (string.IsNullOrWhiteSpace(target)) throw new ArgumentNullException(nameof(target));
			if (sources.Length < 1) throw new ArgumentException(nameof(sources));

			_sources = sources;

			//_dacStream = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			_dacPackage = DacPackage.Load(target, DacSchemaModelStorageType.File, FileAccess.ReadWrite);
			_targetModel = TSqlModel.LoadFromDacpac(target, new ModelLoadOptions(DacSchemaModelStorageType.Memory, true, true));
		}

		public void Merge()
		{
			var options = new TSqlObjectOptions();
			//var pre = String.Empty;
			//var post = String.Empty;
			//Dictionary<string, Reference> externalReferences = new Dictionary<string, Reference>();
			//Dictionary<string, SqlCmdVar> sqlVariables = new Dictionary<string, SqlCmdVar>();

			foreach (var source in _sources)
			{
				TSqlModel model = new TSqlModel(source);

				/*
				var customDatas = model.GetCustomData();
				foreach (CustomData customData in customDatas)
				{
					if (customData.Category == "Reference" && customData.DataType == "SqlSchema")
					{
						var reference = new Reference(customData);
						if (!reference.IsSameDatabaseReference && !externalReferences.ContainsKey(reference.LogicalName))
						{
							externalReferences.Add(reference.LogicalName, reference);
						}
						Console.WriteLine("DacPac Reference: {0} / {1} / {2} / {3}",
							reference.Path,
							reference.LogicalName,
							reference.ExternalParts,
							reference.SuppressMissingDependenciesErrors);
					}
					else if (customData.Category == "SqlCmdVariables")
					{
						var sqlVars = SqlCmdVar.ParseCustomData(customData);
						foreach (SqlCmdVar sqlVar in sqlVars)
						{
							if (!sqlVariables.ContainsKey(sqlVar.Name))
							{
								sqlVariables.Add(sqlVar.Name, sqlVar);
							}
							Console.WriteLine("DacPac SQL Variable: {0} / {1}",
								sqlVar.Name,
								sqlVar.Value);
						}
					}
				}
				*/

				foreach (TSqlObject obj in model.GetObjects(DacQueryScopes.UserDefined))
				{
					if (obj.TryGetAst(out TSqlScript ast))
					//if (obj.TryGetScript(out string script))
					{
						var name = obj.Name.ToString();
						SourceInformation info = obj.GetSourceInformation();
						if (info != null && !string.IsNullOrWhiteSpace(info.SourceName))
						{
							name = info.SourceName;
						}

						if (!string.IsNullOrWhiteSpace(name) && !name.EndsWith(".xsd"))
						{
							_targetModel.AddOrUpdateObjects(ast, name, options);
							//_targetModel.AddObjects(ast);
						}
					}
				}

				//using (var package = DacPackage.Load(source))
				//{
				//	pre += new StreamReader(package.PreDeploymentScript).ReadToEnd();
				//	post += new StreamReader(package.PostDeploymentScript).ReadToEnd();
				//}
			}

			//Console.WriteLine("Start Compile...");
			//foreach (Reference reference in externalReferences.Values)
			//{
			//	_target.AddReference(
			//		reference.Path,
			//		reference.LogicalName,
			//		reference.ExternalParts,
			//		reference.SuppressMissingDependenciesErrors);
			//}
			//_target.AddSqlVariables(sqlVariables.Values.ToList());
			WriteFinalDacpac(_targetModel/*, pre, post*/);

		}

		private void WriteFinalDacpac(TSqlModel model/*, string preScript, string postScript*/)
		{
			var metadata = new PackageMetadata { Name = _dacPackage.Name, Description = _dacPackage.Description, Version = _dacPackage.Version.ToString() };

			try
			{
				_dacPackage.UpdateModel(model, metadata);
				//DacPackageExtensions.BuildPackage(_targetPath, model, metadata);
			}
			catch (DacServicesException exception)
			{
				Console.WriteLine("Exception[{0}]: {1}\r\nStackTrace: {2}\r\n",
					exception.Source,
					exception.Message,
					exception.StackTrace
					);
			}
			//AddScripts(preScript, postScript, _targetPath);
		}

		/*
		private void AddScripts(string pre, string post, string dacpacPath)
		{
			using (var package = Package.Open(dacpacPath, FileMode.Open, FileAccess.ReadWrite))
			{
				if (!string.IsNullOrEmpty(pre))
				{
					var part = package.CreatePart(new Uri("/predeploy.sql", UriKind.Relative), "text/plain");

					using (Stream stream = part.GetStream())
					{
						stream.Write(Encoding.UTF8.GetBytes(pre), 0, pre.Length);
					}
				}


				if (!string.IsNullOrEmpty(post))
				{
					var part = package.CreatePart(new Uri("/postdeploy.sql", UriKind.Relative), "text/plain");

					using (Stream stream = part.GetStream())
					{
						stream.Write(Encoding.UTF8.GetBytes(post), 0, post.Length);
					}
				}
				package.Close();
			}
		}
		*/
	}
}
