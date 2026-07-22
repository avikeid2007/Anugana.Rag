using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;
using UglyToad.PdfPig;

namespace Anugana.Rag.Services;

public interface IDocumentProcessorService
{
    Task<IngestedDocument> ProcessFileAsync(
        string filePath,
        Action<DocumentStatus, string?>? progressCallback = null,
        CancellationToken cancellationToken = default);
}

public class DocumentProcessorService : IDocumentProcessorService
{
    private readonly ISettingsService _settingsService;
    private readonly ITextChunkerService _chunkerService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorDbService _vectorDbService;

    public DocumentProcessorService(
        ISettingsService settingsService,
        ITextChunkerService chunkerService,
        IEmbeddingService embeddingService,
        IVectorDbService vectorDbService)
    {
        _settingsService = settingsService;
        _chunkerService = chunkerService;
        _embeddingService = embeddingService;
        _vectorDbService = vectorDbService;
    }

    public async Task<IngestedDocument> ProcessFileAsync(
        string filePath,
        Action<DocumentStatus, string?>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var docInfo = new FileInfo(filePath);
        var doc = new IngestedDocument
        {
            FileName = docInfo.Name,
            FilePath = docInfo.FullName,
            FileSize = docInfo.Exists ? docInfo.Length : 0,
            FileType = docInfo.Extension.ToLowerInvariant(),
            Status = DocumentStatus.Parsing
        };

        progressCallback?.Invoke(DocumentStatus.Parsing, "Parsing document content...");

        try
        {
            var pageTexts = new List<(int PageNumber, string Text)>();

            if (doc.FileType == ".pdf")
            {
                using var pdf = PdfDocument.Open(filePath);
                doc.PageCount = pdf.NumberOfPages;
                for (int pageNum = 1; pageNum <= pdf.NumberOfPages; pageNum++)
                {
                    var page = pdf.GetPage(pageNum);
                    pageTexts.Add((pageNum, page.Text));
                }
            }
            else if (doc.FileType == ".txt" || doc.FileType == ".md")
            {
                doc.PageCount = 1;
                string text = await File.ReadAllTextAsync(filePath, cancellationToken);
                pageTexts.Add((1, text));
            }
            else
            {
                doc.Status = DocumentStatus.Failed;
                doc.ErrorMessage = $"Unsupported file type: {doc.FileType}";
                progressCallback?.Invoke(DocumentStatus.Failed, doc.ErrorMessage);
                return doc;
            }

            var settings = _settingsService.CurrentSettings;
            var allChunks = new List<DocumentChunk>();

            foreach (var page in pageTexts)
            {
                var pageChunks = _chunkerService.ChunkText(
                    doc.Id.ToString(),
                    doc.FileName,
                    page.PageNumber,
                    page.Text,
                    settings.ChunkSize,
                    settings.ChunkOverlap);

                allChunks.AddRange(pageChunks);
            }

            doc.ChunkCount = allChunks.Count;

            if (allChunks.Count == 0)
            {
                doc.Status = DocumentStatus.Completed;
                progressCallback?.Invoke(DocumentStatus.Completed, "No text content found to embed.");
                return doc;
            }

            doc.Status = DocumentStatus.Embedding;
            progressCallback?.Invoke(DocumentStatus.Embedding, $"Generating embeddings for {allChunks.Count} chunks...");

            var textsToEmbed = allChunks.Select(c => c.TextContent).ToList();
            var embeddings = await _embeddingService.GetEmbeddingsBatchAsync(textsToEmbed, cancellationToken);

            for (int i = 0; i < allChunks.Count && i < embeddings.Count; i++)
            {
                allChunks[i].Vector = embeddings[i];
            }

            doc.Status = DocumentStatus.Indexing;
            progressCallback?.Invoke(DocumentStatus.Indexing, "Indexing vectors into Vector Database...");

            await _vectorDbService.UpsertChunksAsync(settings.CollectionName, allChunks, cancellationToken);

            doc.Status = DocumentStatus.Completed;
            progressCallback?.Invoke(DocumentStatus.Completed, "Successfully processed and indexed.");
        }
        catch (Exception ex)
        {
            doc.Status = DocumentStatus.Failed;
            doc.ErrorMessage = ex.Message;
            progressCallback?.Invoke(DocumentStatus.Failed, ex.Message);
        }

        return doc;
    }
}
