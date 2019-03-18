﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hjson;
using md.stdl.Coding;
using Newtonsoft.Json.Linq;

namespace uppm.Core
{
    /// <summary>
    /// Immutable metadata for an opened package
    /// </summary>
    public class PackageMeta : Versionable
    {
        /// <summary>
        /// Parse metadata from HJSON (Human JSON) format
        /// </summary>
        /// <param name="hjson"></param>
        /// <param name="packmeta"></param>
        public static void ParseFromHjson(string hjson, ref PackageMeta packmeta)
        {
            packmeta = packmeta ?? new PackageMeta();
            ParseFromJson(HjsonValue.Parse(hjson).ToString(), ref packmeta);
        }

        /// <summary>
        /// Parse metadata from JSON format
        /// </summary>
        /// <param name="hjson"></param>
        /// <param name="packmeta"></param>
        public static void ParseFromJson(string json, ref PackageMeta packmeta)
        {
            var jobj = packmeta.MetaData = JObject.Parse(json);

            packmeta.Name = jobj["name"].ToString();
            packmeta.Version = jobj["version"].ToString();

            packmeta.Author = jobj["author"]?.ToString();
            packmeta.License = jobj["license"]?.ToString();
            packmeta.ProjectUrl = jobj["projectUrl"]?.ToString();
            packmeta.Repository = jobj["repository"]?.ToString();
            packmeta.Description = jobj["description"]?.ToString();
            packmeta.CompatibleAppVersion = jobj["compatibleAppVersion"]?.ToString();
            packmeta.CompatibleAppVersion = jobj["compatibleAppVersion"]?.ToString();
            packmeta.InferSelf();
            packmeta.Dependencies.Clear();

            if (jobj.TryGetValue("dependencies", out var jdeps))
            {
                foreach (var jdep in jdeps)
                {
                    packmeta.Dependencies.Update(PackageReference.Parse(jdep.ToString()));
                }
            }
        }

        /// <summary>
        /// The <see cref="JObject"/> representing the package descriptor comment in the package file
        /// </summary>
        public JObject MetaData { get; set; }

        /// <summary>
        /// Reference to self
        /// </summary>
        public PackageReference Self { get; set; }

        /// <summary>
        /// Required uppm version for this pack
        /// </summary>
        public VersionRequirement RequiredUppmVersion { get; set; }

        /// <summary>
        /// Version range of the target application this package is compatible with.
        /// Use it with <see cref="UppmVersion.IsInsideRange"/>.
        /// </summary>
        public string CompatibleAppVersion { get; set; }

        /// <summary>
        /// Friendly name of this package
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A short description about what the package does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Optional author of this package
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Optional url to the package's project website
        /// </summary>
        public string ProjectUrl { get; set; }

        /// <summary>
        /// Optional license text
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// A package can optionally specify a repository explicitly,
        /// however this is only taken into account when a package is processed out of context.
        /// </summary>
        public string Repository { get; set; }

        /// <summary>
        /// The entire text of the package file
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The processed text which is ready to be executed
        /// </summary>
        public string ScriptText { get; set; }

        /// <summary>
        /// References for packages this one is depending on
        /// </summary>
        public HashSet<PartialPackageReference> Dependencies { get; } = new HashSet<PartialPackageReference>();

        /// <summary>
        /// References for packages which scripts should be imported into the current script.
        /// </summary>
        public HashSet<PartialPackageReference> Imports { get; } = new HashSet<PartialPackageReference>();

        /// <summary>
        /// Create a package reference out of this Meta
        /// </summary>
        public void InferSelf()
        {
            if (Self == null)
            {
                Self = new PartialPackageReference
                {
                    Name = Name,
                    Version = Version,
                    RepositoryUrl = Repository
                };
            }
        }
    }
}