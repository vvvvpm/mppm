﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using Humanizer;
using LibGit2Sharp;
using md.stdl.Windows;
using Serilog;
using uppm.Core.Utils;

namespace uppm.Core.Scripting
{
    /// <summary>
    /// A host or global object for scripts. Members can be referenced implicitly in scripts without the need for `ScriptHost.*`
    /// </summary>
    public class ScriptHost : ILogging
    {
        /// <summary>
        /// Information about the target application of the package manager
        /// </summary>
        public TargetApp App { get; set; }

        public InstalledPackageScope Scope { get; set; }

        public string TargetDirectory =>
            ((int) Scope & 2) > 0 ? App.LocalPacksFolder : App.GlobalPacksFolder;

        public string WorkDirectory =>
            Path.Combine(Uppm.Implementation.TemporaryFolder, Pack.Meta.Self.ToString().Dehumanize().Kebaberize());

        /// <summary>
        /// Information about the package manager
        /// </summary>
        public IUppmImplementation PackageManager { get; }

        /// <summary>
        /// The package which script is currently hosted
        /// </summary>
        public Package Pack { get; }
        
        /// <inheritdoc />
        public ILogger Log { get; }

        /// <inheritdoc />
        public event UppmProgressHandler OnProgress;

        /// <inheritdoc />
        public void InvokeProgress(ProgressEventArgs progress) => OnProgress?.Invoke(this, progress);

        /// <summary></summary>
        /// <param name="pack">The associated package</param>
        /// <param name="uppm">The executing package manager</param>
        public ScriptHost(Package pack, IUppmImplementation uppm) : this()
        {
            PackageManager = uppm;
            Pack = pack;
        }

        internal ScriptHost()
        {
            Log = this.GetContext();
        }

        /// <summary>
        /// Just a convenience shortener for <see cref="Action"/>`s, so the pack developer can spare `new `
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Action Action(Action action) => action;
        
        /// <summary>
        /// Gracefully throw exception and tell about it in the package manager
        /// </summary>
        /// <param name="e"></param>
        public void ThrowException(Exception e) =>
            Log.Error(e, "Script of {$PackRef} threw an exception", Pack.Meta.Self);

        /// <summary>
        /// Copy a directory recursively with either black~ or white listing.
        /// </summary>
        /// <param name="srcdir">Source directory</param>
        /// <param name="dstdir">Destination directory</param>
        /// <param name="ignore">Ignoring blacklist, can use wildcards</param>
        /// <param name="match">Matching whitelist, can use wildcards</param>
        public void CopyDirectory(string srcdir, string dstdir, string[] ignore = null, string[] match = null) =>
            FileUtils.CopyDirectory(srcdir, dstdir, ignore, match, this);

        /// <summary>
        /// Clones a git repository. Only HTTP remote supported
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="dstdir"></param>
        /// <param name="options"></param>
        public void GitClone(string remote, string dstdir, string branch = null, CloneOptions options = null)
        {
            options = options ?? new CloneOptions();
            options.BranchName = branch ?? options.BranchName;

            GitUtils.Clone(remote, dstdir, options, this);
        }

        /// <summary>
        /// Delete a directory recursively with either black~ or white listing.
        /// </summary>
        /// <param name="srcdir">Source directory</param>
        /// <param name="recursive"></param>
        /// <param name="ignore">Ignoring blacklist, can use wildcards</param>
        /// <param name="match">Matching whitelist, can use wildcards</param>
        public void DeleteDirectory(string srcdir, bool recursive = true, string[] ignore = null, string[] match = null) =>
            FileUtils.DeleteDirectory(srcdir, recursive, ignore, match, this);

        /// <summary>
        /// Download a file synchronously with progress events fired on behalf of this ScriptHost
        /// </summary>
        /// <param name="url"></param>
        /// <param name="dst"></param>
        public void Download(string url, string dst)
        {
            var client = new WebClient();
            client.DownloadProgressChanged += (sender, args) =>
                this.InvokeAnyProgress(args.TotalBytesToReceive, args.BytesReceived, url, "Downloading");
            
            var dltask = client.DownloadFileTaskAsync(url, dst);
            try
            {
                dltask.Wait();
            }
            catch (Exception e)
            {
                Log.Error(e, "Downloading failed for {Url}", url);
            }
        }

        /// <summary>
        /// Extract an archive which is supported by the SharpCompress library (i.e.: zip, 7z, rar, tar.bz2, etc...)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dstdir"></param>
        public void Extract(string src, string dstdir) =>
            FileUtils.ExtractArchive(src, dstdir, this);

        /// <summary>
        /// When user input is needed during script execution this method should be called.
        /// </summary>
        /// <param name="question">Question to be asked from user</param>
        /// <param name="possibilities">If null any input will be accepted. Otherwise input is compared to these possible entries ignoring case. If no match is found question will be asked again.</param>
        /// <param name="defaultValue">This value is used when user submits an empty input or in a potential unattended mode.</param>
        /// <returns>User answer or default</returns>
        public string AskUser(
            string question,
            IEnumerable<string> possibilities = null,
            string defaultValue = "") =>
            Logging.AskUser(question, possibilities, defaultValue, this);

        /// <summary>
        /// A shortcut to <see cref="AskUser"/> for selecting an enumeration
        /// </summary>
        /// <typeparam name="T">Enumeration type</typeparam>
        /// <param name="question">Question to be asked from user</param>
        /// <param name="possibilities">If null any input will be accepted. Otherwise input is compared to these possible entries ignoring case. If no match is found question will be asked again.</param>
        /// <param name="defaultValue">This value is used when user submits an empty input or in a potential unattended mode.</param>
        /// <returns></returns>
        public T AskUserEnum<T>(
            string question,
            IEnumerable<T> possibilities = null,
            T defaultValue = default(T)) where T : struct =>
            Logging.AskUserEnum<T>(question, possibilities, defaultValue, this);

        /// <summary>
        /// A shortcut to <see cref="AskUser"/> for yes (true) / no (false) questions
        /// </summary>
        /// <param name="question">Question to be asked from user</param>
        /// <param name="defaultValue">This value is used when user submits an empty input or in a potential unattended mode.</param>
        /// <returns>User answer or default</returns>
        public bool ConfirmWithUser(string question, bool defaultValue = true) =>
            Logging.ConfirmWithUser(question, defaultValue, this);
    }
}
