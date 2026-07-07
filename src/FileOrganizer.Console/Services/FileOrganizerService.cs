using FileOrganizer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FileOrganizer.Services
{
    internal class FileOrganizerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileOrganizerService> _logger;
        private string _sourceDirectory;
        private List<OrganizationRule> _rules;

        public FileOrganizerService(IConfiguration configuration, ILogger<FileOrganizerService> logger)
        {

            _configuration = configuration;
            _logger = logger;
            _sourceDirectory = _configuration["FileOrganizer:SourceDirectory"]
                ?? throw new InvalidOperationException("FileOrganizer:SourceDirectory is not configured.");
            _rules = _configuration.GetSection("FileOrganizer:Rules").Get<List<OrganizationRule>>()
                ?? throw new InvalidOperationException("FileOrganizer:Rules is not configured."); 

        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {

            try
            {
                if (string.IsNullOrEmpty(e.Name)) return;
                await Task.Delay(500);
                string filePath = e.FullPath;
                if (!File.Exists(filePath)) return;

                string extension = Path.GetExtension(filePath);
                if (extension == ".tmp") return;

                string fileName = e.Name;
                bool ruleMatched = false;
                foreach(var rule in _rules)
                {
                    if(rule.Extension == extension)
                    {
                        string destinationFolder = Path.Combine(_sourceDirectory, rule.DestinationFolder);
                        string destinationPath = Path.Combine(destinationFolder, fileName);

                        if (!Directory.Exists(destinationFolder))
                        {
                            Directory.CreateDirectory(destinationFolder);
                            _logger.LogInformation("Created folder: {FolderName}",
                                rule.DestinationFolder);
                        }

                        File.Move(filePath, destinationPath, overwrite: false);
                        _logger.LogInformation("Moved {FileName} → {DestinationFolder}",
                            fileName, rule.DestinationFolder);

                        ruleMatched = true;
                    }
                    else if( rule.Extension == null)
                    {
                        _logger.LogWarning("File without extension detected: {FileName}", fileName);
                    }

                }
                if (ruleMatched == false)
                {
                    _logger.LogInformation("No rule matched for extension: {Extension} | File: {FileName}", extension, fileName);

                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FileName}", e.Name);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            
            var watcher = new FileSystemWatcher
            {
                Path = _sourceDirectory,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName |
                   NotifyFilters.DirectoryName |
                   NotifyFilters.LastWrite |
                   NotifyFilters.CreationTime
            };
            watcher.Created += OnFileCreated;

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }


    }
}
