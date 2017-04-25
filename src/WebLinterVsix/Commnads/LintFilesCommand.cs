﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using WebLinter;

namespace WebLinterVsix
{
    internal sealed class LintFilesCommand
    {
        private readonly Package _package;

        private LintFilesCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(PackageGuids.WebLinterCmdSet, PackageIds.LintFilesCommand);
                var menuItem = new OleMenuCommand(async (s, e) => { await LintSelectedFiles(s, e); }, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        public static LintFilesCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get { return this._package; }
        }

        public static void Initialize(Package package)
        {
            Instance = new LintFilesCommand(package);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var paths = ProjectHelpers.GetSelectedItemPaths();

            button.Visible = false;

            if (paths.Any(f => string.IsNullOrEmpty(Path.GetExtension(f)) || LinterService.IsFileSupported(f)))
            {
                button.Visible = true;
            }
        }

        private async System.Threading.Tasks.Task LintSelectedFiles(object sender, EventArgs e)
        {
            var paths = ProjectHelpers.GetSelectedItemPaths();
            List<string> files = new List<string>();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    var children = GetFiles(path, "*.*");
                    files.AddRange(children.Where(c => LinterService.IsFileSupported(c)));
                }
                else if (File.Exists(path) && LinterService.IsFileSupported(path))
                {
                    files.Add(path);
                }
            }

            if (files.Any())
            {
                await LinterService.LintAsync(true, files.ToArray());
                Telemetry.TrackEvent($"VS Lint Files");
            }
            else
            {
                WebLinterPackage.Dte.StatusBar.Text = "No files found to lint";
            }
        }

        private static List<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch (UnauthorizedAccessException) { }

            return files;
        }
    }
}
