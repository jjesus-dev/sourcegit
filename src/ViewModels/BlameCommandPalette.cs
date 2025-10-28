﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SourceGit.ViewModels
{
    public class BlameCommandPalette : ICommandPalette
    {
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public List<string> VisibleFiles
        {
            get => _visibleFiles;
            private set => SetProperty(ref _visibleFiles, value);
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                    UpdateVisible();
            }
        }

        public string SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        public BlameCommandPalette(Launcher launcher, string repo)
        {
            _launcher = launcher;
            _repo = repo;
            _isLoading = true;

            Task.Run(async () =>
            {
                var files = await new Commands.QueryRevisionFileNames(_repo, "HEAD")
                    .GetResultAsync()
                    .ConfigureAwait(false);

                var head = await new Commands.QuerySingleCommit(_repo, "HEAD")
                    .GetResultAsync()
                    .ConfigureAwait(false);

                Dispatcher.UIThread.Post(() =>
                {
                    IsLoading = false;
                    _repoFiles = files;
                    _head = head;
                    UpdateVisible();
                });
            });
        }

        public override void Cleanup()
        {
            _launcher = null;
            _repo = null;
            _head = null;
            _repoFiles.Clear();
            _filter = null;
            _visibleFiles.Clear();
            _selectedFile = null;
        }

        public void ClearFilter()
        {
            Filter = string.Empty;
        }

        public void Launch()
        {
            if (!string.IsNullOrEmpty(_selectedFile))
                App.ShowWindow(new Blame(_repo, _selectedFile, _head));
            _launcher.CancelCommandPalette();
        }

        private void UpdateVisible()
        {
            if (_repoFiles is { Count: > 0 })
            {
                if (string.IsNullOrEmpty(_filter))
                {
                    VisibleFiles = _repoFiles;
                }
                else
                {
                    var visible = new List<string>();
                    foreach (var f in _repoFiles)
                    {
                        if (f.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                            visible.Add(f);
                    }
                    VisibleFiles = visible;
                }
            }
        }

        private Launcher _launcher = null;
        private string _repo = null;
        private bool _isLoading = false;
        private Models.Commit _head = null;
        private List<string> _repoFiles = null;
        private string _filter = string.Empty;
        private List<string> _visibleFiles = [];
        private string _selectedFile = null;
    }
}
