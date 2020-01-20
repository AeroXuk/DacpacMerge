﻿using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;

namespace MergeEm
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 3 || args.Any(p => p == "/?") || args.Any(p => p == "-?") || args.Any(p => p == "/help") || args.Any(p => p == "--help"))
            {
                Console.WriteLine("you need at least three args - targetDacPac (which will be created) sourceDacpac sourceDacpac");
                return;
            }

            var target = args.First<string>();

            var merger = new DacpacMerge(args[0]);
            merger.Merge();
        }
    }

    class DacpacMerge
    {

        private readonly string[] _sources;
        private readonly TSqlModel _first;
        private readonly string _targetPath;
        private readonly TSqlModel _target;

        public DacpacMerge(string target, params string[] sources)
        {
            _sources = sources;
            _first = new TSqlModel(sources.First<string>());
            var options = _first.CopyModelOptions();

            _target = new TSqlModel(_first.Version, options);
            _targetPath = target;
        }

        public void Merge()
        {
            var pre = String.Empty;
            var post = String.Empty;

            foreach (var source in _sources)
            {
                var model = GetModel(source);
                foreach(var obj in model.GetObjects(DacQueryScopes.UserDefined))
                {
                    if (obj.TryGetAst(out TSqlScript ast))
                    {
                        var name = obj.Name.ToString();
                        var info = obj.GetSourceInformation();
                        if (info != null)
                        {
                            name = info.SourceName;
                        }

                        _target.AddOrUpdateObjects(ast, name, new TSqlObjectOptions());    //WARNING throwing away ansi nulls and quoted identifiers!
                    }
                }

                using (var package = DacPackage.Load(source))
                {
                    pre += new StreamReader(package.PreDeploymentScript).ReadToEnd();
                    post += new StreamReader(package.PostDeploymentScript).ReadToEnd();
                }
                
            }            

            WriteFinalDacpac(_target, pre, post);
           
        }

        private void WriteFinalDacpac(TSqlModel model, string preScript, string postScript)
        {
            var metadata = new PackageMetadata
            {
                Name = "dacpac"
            };

            DacPackageExtensions.BuildPackage(_targetPath, model, metadata);
            AddScripts(preScript, postScript, _targetPath);
        }

        TSqlModel GetModel(string source)
        {
            if (source == _sources.FirstOrDefault<string>())
            {
                return _first;
            }

            return new TSqlModel(source);
        }

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
    }
}
