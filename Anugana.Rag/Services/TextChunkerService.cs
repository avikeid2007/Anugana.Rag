using System;
using System.Collections.Generic;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public interface ITextChunkerService
{
    List<DocumentChunk> ChunkText(string documentId, string fileName, int pageIndex, string text, int chunkSize, int chunkOverlap);
}

public class TextChunkerService : ITextChunkerService
{
    public List<DocumentChunk> ChunkText(
        string documentId,
        string fileName,
        int pageIndex,
        string text,
        int chunkSize,
        int chunkOverlap)
    {
        var chunks = new List<DocumentChunk>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        if (chunkSize <= 0) chunkSize = 500;
        if (chunkOverlap < 0 || chunkOverlap >= chunkSize) chunkOverlap = 50;

        int step = chunkSize - chunkOverlap;
        int chunkIndex = 0;

        for (int i = 0; i < text.Length; i += step)
        {
            int length = Math.Min(chunkSize, text.Length - i);
            string snippet = text.Substring(i, length).Trim();

            if (!string.IsNullOrWhiteSpace(snippet))
            {
                chunks.Add(new DocumentChunk
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    DocumentId = documentId,
                    FileName = fileName,
                    PageIndex = pageIndex,
                    ChunkIndex = chunkIndex++,
                    TextContent = snippet
                });
            }

            if (i + length >= text.Length) break;
        }

        return chunks;
    }
}
