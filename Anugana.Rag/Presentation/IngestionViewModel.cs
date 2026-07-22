using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;
using Anugana.Rag.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anugana.Rag.Presentation;

public partial class IngestionViewModel : ObservableObject
{
    private readonly IDocumentProcessorService _documentProcessorService;
    private readonly IVectorDbService _vectorDbService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _filePathInput = string.Empty;

    [ObservableProperty]
    private ulong _pointsCount;

    [ObservableProperty]
    private string _collectionStatus = "Unknown";

    [ObservableProperty]
    private string _statusMessage = "Ready to ingest documents.";

    [ObservableProperty]
    private bool _isProcessing;

    public ObservableCollection<IngestedDocument> Documents { get; } = new();

    public IngestionViewModel(
        IDocumentProcessorService documentProcessorService,
        IVectorDbService vectorDbService,
        ISettingsService settingsService)
    {
        _documentProcessorService = documentProcessorService;
        _vectorDbService = vectorDbService;
        _settingsService = settingsService;

        _ = RefreshStatsAsync();
    }

    [RelayCommand]
    public async Task IngestMultipleFilesAsync(List<string> filePaths)
    {
        if (filePaths == null || filePaths.Count == 0 || IsProcessing) return;

        IsProcessing = true;
        int total = filePaths.Count;
        int successCount = 0;

        for (int i = 0; i < total; i++)
        {
            var path = filePaths[i];
            if (!System.IO.File.Exists(path)) continue;

            var fileName = System.IO.Path.GetFileName(path);
            StatusMessage = $"⏳ [{i + 1}/{total}] Processing {fileName}...";

            try
            {
                var doc = await _documentProcessorService.ProcessFileAsync(
                    path,
                    (status, msg) =>
                    {
                        StatusMessage = $"⏳ [{i + 1}/{total}] {fileName}: {msg}";
                    },
                    CancellationToken.None);

                Documents.Insert(0, doc);
                if (doc.Status == DocumentStatus.Completed)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠️ Error processing {fileName}: {ex.Message}";
            }
        }

        StatusMessage = $"✅ Completed! Successfully ingested {successCount} of {total} document(s).";
        await RefreshStatsAsync();
        IsProcessing = false;
    }

    [RelayCommand]
    private async Task IngestFileAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePathInput) || IsProcessing) return;

        var path = FilePathInput.Trim('"').Trim();
        if (!System.IO.File.Exists(path))
        {
            StatusMessage = "⚠️ Specified file does not exist.";
            return;
        }

        await IngestMultipleFilesAsync(new List<string> { path });
        FilePathInput = string.Empty;
    }

    [RelayCommand]
    public async Task RefreshStatsAsync()
    {
        try
        {
            var collectionName = _settingsService.CurrentSettings.CollectionName;
            var (count, status) = await _vectorDbService.GetStatsAsync(collectionName, CancellationToken.None);
            PointsCount = count;
            CollectionStatus = status;
        }
        catch
        {
            CollectionStatus = "Offline";
        }
    }
}
